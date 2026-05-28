#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XEditor.UnityPartingTool
{
    // 分离 / 还原：用 AssetDatabase.MoveAsset 做剪切，.cs 与 .meta、文件夹与 .meta 会一起动，GUID 不丢。
    internal static class UnityPartingProcessor
    {
        // 目录名、文件名（去扩展名后）需以此结尾才算「Unity 侧」资源。
        private const string UnityFolderSuffix = ".Unity";

        // 从 Root 剪切到 Target：匹配到的 .Unity 目录与零散 .Unity 文件会保持相对 Root 的路径落到 Target 下。
        public static UnityPartingOperationResult SeparateUnityCode(string rootFolderAssetPath, string targetFolderAssetPath)
        {
            ValidateFolderPair(rootFolderAssetPath, targetFolderAssetPath);
            EnsureAssetFolderExists(targetFolderAssetPath);
            // 第三个参数排除 Target 自身（Target 常在 Root 下），避免扫描时把已分离内容再当源处理。
            UnityPartingOperationResult result = MoveUnityAssets(
                rootFolderAssetPath,
                targetFolderAssetPath,
                targetFolderAssetPath,
                "分离完成");

            // 目录剪走后可能留下空文件夹，删掉以免工程里一堆空目录。
            DeleteEmptyFolders(rootFolderAssetPath, targetFolderAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        // 从 Target 剪切回 Root，与分离方向相反；同样只动匹配规则下的资源树。
        public static UnityPartingOperationResult RestoreUnityCode(string rootFolderAssetPath, string targetFolderAssetPath)
        {
            ValidateFolderPair(rootFolderAssetPath, targetFolderAssetPath);
            EnsureAssetFolderExists(rootFolderAssetPath);

            string rootFolderFullPath = AssetPathToFullPath(rootFolderAssetPath);
            string targetFolderFullPath = AssetPathToFullPath(targetFolderAssetPath);

            if (!Directory.Exists(targetFolderFullPath))
            {
                throw new DirectoryNotFoundException($"Target folder does not exist: {targetFolderAssetPath}");
            }

            // 源根 = Target，目的根 = Root；还原时无需再排除子路径。
            UnityPartingOperationResult result = MoveUnityAssets(
                targetFolderAssetPath,
                rootFolderAssetPath,
                null,
                "还原完成");

            DeleteEmptyFolders(targetFolderAssetPath, null);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        // 按 Assets/... 层级逐级 CreateFolder，保证中间路径都存在。
        public static void EnsureAssetFolderExists(string assetFolderPath)
        {
            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                throw new ArgumentException("Folder path cannot be null or empty.", nameof(assetFolderPath));
            }

            if (!assetFolderPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Folder path must start with Assets.", nameof(assetFolderPath));
            }

            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string[] pathParts = assetFolderPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string currentPath = pathParts[0];

            for (int index = 1; index < pathParts.Length; index++)
            {
                string nextPath = $"{currentPath}/{pathParts[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, pathParts[index]);
                }

                currentPath = nextPath;
            }
        }

        // 在 sourceRoot 下找所有待移动项，拼到 destinationRoot 下相同相对路径；excluded 用于跳过某棵子树（分离时的 Target）。
        private static UnityPartingOperationResult MoveUnityAssets(
            string sourceRootAssetPath,
            string destinationRootAssetPath,
            string excludedSourceAssetPath,
            string operationName)
        {
            string sourceRootFullPath = AssetPathToFullPath(sourceRootAssetPath);
            string excludedSourceFullPath = string.IsNullOrWhiteSpace(excludedSourceAssetPath)
                ? null
                : AssetPathToFullPath(excludedSourceAssetPath);

            // 先整棵移动 .Unity 目录，再处理不在这些目录里的零散 .Unity 文件，避免重复移动子文件。
            List<string> matchedDirectories = CollectTopLevelUnityDirectories(sourceRootFullPath, excludedSourceFullPath);
            List<string> matchedFiles = CollectStandaloneUnityFiles(sourceRootFullPath, excludedSourceFullPath, matchedDirectories);

            int movedDirectoryCount = 0;
            foreach (string sourceDirectory in matchedDirectories)
            {
                string sourceDirectoryAssetPath = FullPathToAssetPath(sourceDirectory);
                string relativeAssetPath = GetRelativeAssetPath(sourceRootAssetPath, sourceDirectoryAssetPath);
                string destinationDirectoryAssetPath = CombineAssetPath(destinationRootAssetPath, relativeAssetPath);
                MoveAsset(sourceDirectoryAssetPath, destinationDirectoryAssetPath);
                movedDirectoryCount++;
            }

            int movedFileCount = 0;
            foreach (string sourceFile in matchedFiles)
            {
                string sourceFileAssetPath = FullPathToAssetPath(sourceFile);
                string relativeAssetPath = GetRelativeAssetPath(sourceRootAssetPath, sourceFileAssetPath);
                string destinationFileAssetPath = CombineAssetPath(destinationRootAssetPath, relativeAssetPath);
                MoveAsset(sourceFileAssetPath, destinationFileAssetPath);
                movedFileCount++;
            }

            return new UnityPartingOperationResult(
                movedDirectoryCount,
                movedFileCount,
                $"{operationName}，共剪切 {movedDirectoryCount} 个目录，{movedFileCount} 个文件。");
        }

        // 收集「最外层」的 .Unity 目录：若 A/B.Unity 已选中，则不会再单独处理 B.Unity 的子路径。
        private static List<string> CollectTopLevelUnityDirectories(string rootFolderFullPath, string excludedSourceFullPath)
        {
            string[] directories = Directory.GetDirectories(rootFolderFullPath, "*", SearchOption.AllDirectories);
            Array.Sort(directories, StringComparer.OrdinalIgnoreCase);

            List<string> matchedDirectories = new List<string>();
            foreach (string directory in directories)
            {
                if (ShouldSkipPath(directory, excludedSourceFullPath))
                {
                    continue;
                }

                if (!Path.GetFileName(directory).EndsWith(UnityFolderSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 已被某个已选中的祖先 .Unity 目录包含则跳过，避免嵌套重复。
                if (IsContainedInDirectories(directory, matchedDirectories))
                {
                    continue;
                }

                matchedDirectories.Add(directory);
            }

            return matchedDirectories;
        }

        // 不在已匹配 .Unity 目录内的、命名符合规则的单个文件；.meta 不单独列，交给 MoveAsset 随主资源一起动。
        private static List<string> CollectStandaloneUnityFiles(
            string rootFolderFullPath,
            string excludedSourceFullPath,
            List<string> matchedDirectories)
        {
            string[] files = Directory.GetFiles(rootFolderFullPath, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            List<string> matchedFiles = new List<string>();
            foreach (string file in files)
            {
                if (ShouldSkipPath(file, excludedSourceFullPath))
                {
                    continue;
                }

                if (IsMetaFile(file) || !IsUnityNamedFile(file) || IsContainedInDirectories(file, matchedDirectories))
                {
                    continue;
                }

                matchedFiles.Add(file);
            }

            return matchedFiles;
        }

        // 判断 path 是否落在已选目录列表中任一棵树下。
        private static bool IsContainedInDirectories(string path, IReadOnlyList<string> parentDirectories)
        {
            foreach (string parentDirectory in parentDirectories)
            {
                if (IsSameOrChildPath(path, parentDirectory))
                {
                    return true;
                }
            }

            return false;
        }

        // 分离时跳过 excluded（例如 Target）整棵子树，防止把输出目录当输入再扫一遍。
        private static bool ShouldSkipPath(string path, string targetFolderFullPath)
        {
            if (string.IsNullOrWhiteSpace(targetFolderFullPath))
            {
                return false;
            }

            return IsSameOrChildPath(path, targetFolderFullPath);
        }

        // 用「去掉扩展名后的文件名」判断，这样 CoroutineHandler.Unity.cs 会命中。
        private static bool IsUnityNamedFile(string fileFullPath)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileFullPath);
            return fileNameWithoutExtension.EndsWith(UnityFolderSuffix, StringComparison.OrdinalIgnoreCase);
        }

        // 先确保父文件夹存在；若目标已存在则先删再移（覆盖语义）；文件夹移动时禁止目的路径落在源目录内部。
        private static void MoveAsset(string sourceAssetPath, string destinationAssetPath)
        {
            if (string.Equals(NormalizeAssetPath(sourceAssetPath), NormalizeAssetPath(destinationAssetPath), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (AssetDatabase.IsValidFolder(sourceAssetPath) && IsSameOrChildAssetPath(destinationAssetPath, sourceAssetPath))
            {
                throw new InvalidOperationException($"目标路径不能位于待移动目录内部：{sourceAssetPath}");
            }

            string destinationDirectoryAssetPath = GetParentAssetPath(destinationAssetPath);
            EnsureAssetFolderExists(destinationDirectoryAssetPath);

            if (AssetExists(destinationAssetPath) && !AssetDatabase.DeleteAsset(destinationAssetPath))
            {
                throw new InvalidOperationException($"无法删除已存在的目标资源：{destinationAssetPath}");
            }

            // MoveAsset 会连同同名 .meta 一起更新，无需手动拷 meta。
            string error = AssetDatabase.MoveAsset(sourceAssetPath, destinationAssetPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException($"移动资源失败：{sourceAssetPath} -> {destinationAssetPath}，原因：{error}");
            }
        }

        // 从深到浅删空目录，这样先清空子目录再删父目录；不排除根本身，也不动 excluded 子树（分离时保留 Target）。
        private static void DeleteEmptyFolders(string rootFolderAssetPath, string excludedFolderAssetPath)
        {
            string rootFolderFullPath = AssetPathToFullPath(rootFolderAssetPath);
            string excludedFolderFullPath = string.IsNullOrWhiteSpace(excludedFolderAssetPath)
                ? null
                : AssetPathToFullPath(excludedFolderAssetPath);

            string[] directories = Directory.GetDirectories(rootFolderFullPath, "*", SearchOption.AllDirectories);
            Array.Sort(directories, (left, right) => right.Length.CompareTo(left.Length));

            foreach (string directory in directories)
            {
                if (ShouldSkipPath(directory, excludedFolderFullPath))
                {
                    continue;
                }

                if (!IsDirectoryEmpty(directory))
                {
                    continue;
                }

                string directoryAssetPath = FullPathToAssetPath(directory);
                if (string.Equals(
                        NormalizeAssetPath(directoryAssetPath),
                        NormalizeAssetPath(rootFolderAssetPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(directoryAssetPath) && !AssetDatabase.DeleteAsset(directoryAssetPath))
                {
                    throw new InvalidOperationException($"无法删除空文件夹：{directoryAssetPath}");
                }
            }
        }

        // Root/Target 都须在 Assets 下且 Root 不能把 Target 包在里面（否则扫描/移动边界难定义）。
        private static void ValidateFolderPair(string rootFolderAssetPath, string targetFolderAssetPath)
        {
            if (string.IsNullOrWhiteSpace(rootFolderAssetPath))
            {
                throw new ArgumentException("Root folder path cannot be null or empty.", nameof(rootFolderAssetPath));
            }

            if (string.IsNullOrWhiteSpace(targetFolderAssetPath))
            {
                throw new ArgumentException("Target folder path cannot be null or empty.", nameof(targetFolderAssetPath));
            }

            if (!rootFolderAssetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Root folder path must start with Assets.", nameof(rootFolderAssetPath));
            }

            if (!targetFolderAssetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Target folder path must start with Assets.", nameof(targetFolderAssetPath));
            }

            if (!AssetDatabase.IsValidFolder(rootFolderAssetPath))
            {
                throw new DirectoryNotFoundException($"Root folder does not exist: {rootFolderAssetPath}");
            }

            if (string.Equals(rootFolderAssetPath, targetFolderAssetPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Root folder and Target folder cannot be the same.");
            }

            string rootFolderFullPath = AssetPathToFullPath(rootFolderAssetPath);
            string targetFolderFullPath = AssetPathToFullPath(targetFolderAssetPath);

            if (IsSameOrChildPath(rootFolderFullPath, targetFolderFullPath))
            {
                throw new InvalidOperationException("Root 文件夹不能位于 Target 文件夹内部。");
            }
        }

        // 磁盘上是否存在该资源路径（文件或文件夹）。
        private static bool AssetExists(string assetPath)
        {
            string fullPath = AssetPathToFullPath(assetPath);
            return Directory.Exists(fullPath) || File.Exists(fullPath);
        }

        // 取 Assets/... 路径的父级，用于 EnsureAssetFolderExists。
        private static string GetParentAssetPath(string assetPath)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            int lastSlashIndex = normalizedAssetPath.LastIndexOf('/');
            if (lastSlashIndex <= 0)
            {
                throw new InvalidOperationException($"无法解析资源父目录：{assetPath}");
            }

            return normalizedAssetPath.Substring(0, lastSlashIndex);
        }

        // 扫描文件列表时跳过 .meta，避免对 meta 单独 MoveAsset。
        private static bool IsMetaFile(string path)
        {
            return path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        // 磁盘路径：判断是否为同一路径或 parent的子路径（统一分隔符后比较）。
        private static bool IsSameOrChildPath(string path, string parentPath)
        {
            string normalizedPath = NormalizeFullPath(path);
            string normalizedParentPath = EnsureTrailingSeparator(NormalizeFullPath(parentPath));

            return string.Equals(
                       normalizedPath,
                       normalizedParentPath.TrimEnd(Path.DirectorySeparatorChar),
                       StringComparison.OrdinalIgnoreCase)
                   || normalizedPath.StartsWith(normalizedParentPath, StringComparison.OrdinalIgnoreCase);
        }

        // 资源路径版「是否为子路径」，用于防止把文件夹移动到自身子目录下。
        private static bool IsSameOrChildAssetPath(string assetPath, string parentAssetPath)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            string normalizedParentAssetPath = EnsureAssetTrailingSeparator(NormalizeAssetPath(parentAssetPath));

            return string.Equals(
                       normalizedAssetPath,
                       normalizedParentAssetPath.TrimEnd('/'),
                       StringComparison.OrdinalIgnoreCase)
                   || normalizedAssetPath.StartsWith(normalizedParentAssetPath, StringComparison.OrdinalIgnoreCase);
        }

        // 计算 child 相对 parent 的 Assets相对路径片段（不含开头的 /）。
        private static string GetRelativeAssetPath(string parentAssetPath, string childAssetPath)
        {
            string normalizedParentAssetPath = EnsureAssetTrailingSeparator(NormalizeAssetPath(parentAssetPath));
            string normalizedChildAssetPath = NormalizeAssetPath(childAssetPath);

            if (!normalizedChildAssetPath.StartsWith(normalizedParentAssetPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Path '{childAssetPath}' is not inside '{parentAssetPath}'.");
            }

            return normalizedChildAssetPath.Substring(normalizedParentAssetPath.Length);
        }

        // Assets/xxx -> 工程磁盘绝对路径。
        private static string AssetPathToFullPath(string assetPath)
        {
            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRootPath))
            {
                throw new InvalidOperationException("Cannot resolve Unity project root path.");
            }

            return NormalizeFullPath(Path.Combine(projectRootPath, assetPath));
        }

        // 磁盘绝对路径 -> Assets/...（必须在工程根目录下）。
        private static string FullPathToAssetPath(string fullPath)
        {
            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRootPath))
            {
                throw new InvalidOperationException("Cannot resolve Unity project root path.");
            }

            string normalizedProjectRootPath = EnsureTrailingSeparator(NormalizeFullPath(projectRootPath));
            string normalizedFullPath = NormalizeFullPath(fullPath);
            if (!normalizedFullPath.StartsWith(normalizedProjectRootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Path '{fullPath}' is outside of the project root.");
            }

            return normalizedFullPath.Substring(normalizedProjectRootPath.Length).Replace('\\', '/');
        }

        // 拼接 Assets 相对路径，统一用 /。
        private static string CombineAssetPath(string baseAssetPath, string relativeAssetPath)
        {
            string normalizedBaseAssetPath = NormalizeAssetPath(baseAssetPath);
            string normalizedRelativeAssetPath = NormalizeAssetPath(relativeAssetPath);
            if (string.IsNullOrEmpty(normalizedRelativeAssetPath))
            {
                return normalizedBaseAssetPath;
            }

            return $"{normalizedBaseAssetPath}/{normalizedRelativeAssetPath}";
        }

        // 目录内无任何子目录、文件才算空（.meta 在同级，不在目录「里面」）。
        private static bool IsDirectoryEmpty(string directoryFullPath)
        {
            return Directory.GetDirectories(directoryFullPath).Length == 0
                   && Directory.GetFiles(directoryFullPath).Length == 0;
        }

        // 规范化磁盘路径，去掉末尾分隔符，便于字符串前缀比较。
        private static string NormalizeFullPath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        // 磁盘路径比较用：父路径末尾加 \，避免 "Foo" 误匹配 "FooBar"。
        private static string EnsureTrailingSeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : $"{path}{Path.DirectorySeparatorChar}";
        }

        // Assets 路径：反斜杠转正斜杠、去掉首尾空白和末尾 /。
        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath
                .Replace('\\', '/')
                .Trim()
                .TrimEnd('/');
        }

        // 资源路径父前缀比较用：保证以 / 结尾。
        private static string EnsureAssetTrailingSeparator(string assetPath)
        {
            return assetPath.EndsWith("/", StringComparison.Ordinal)
                ? assetPath
                : $"{assetPath}/";
        }
    }

    // 给窗口展示用：目录个数 + 零散文件个数 + 汇总文案。
    internal sealed class UnityPartingOperationResult
    {
        public UnityPartingOperationResult(int directoryCount, int fileCount, string message)
        {
            DirectoryCount = directoryCount;
            FileCount = fileCount;
            Message = message;
        }

        public int DirectoryCount { get; }

        public int FileCount { get; }

        public string Message { get; }
    }
}
#endif
