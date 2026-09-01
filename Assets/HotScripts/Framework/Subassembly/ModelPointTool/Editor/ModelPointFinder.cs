using System.Collections.Generic;
using UnityEngine;

namespace ModelPointTool.Editor
{
    /// <summary>
    /// 在预制体层级上按节点名 BFS 查找挂点，并得到相对预制体根的路径。
    /// </summary>
    public static class ModelPointFinder
    {
        /// <summary>
        /// 按名称查找第一个挂点；路径不含预制体根节点名，可供 Transform.Find 使用。未找到返回空串。
        /// </summary>
        public static string Search(Transform searchRoot, string targetName)
        {
            if (searchRoot == null || string.IsNullOrEmpty(targetName))
                return string.Empty;

            var found = FindFirstByBfs(searchRoot, targetName);
            if (found == null)
                return string.Empty;

            return BuildRelativePath(found);
        }

        static Transform FindFirstByBfs(Transform root, string targetName)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            Transform first = null;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.name == targetName)
                {
                    if (first != null)
                    {
                        Debug.LogWarning(
                            $"挂点同名不同路径，保留先找到的：{BuildRelativePath(first)}，忽略：{BuildRelativePath(current)}");
                    }
                    else
                    {
                        first = current;
                    }
                }

                foreach (Transform child in current)
                    queue.Enqueue(child);
            }

            return first;
        }

        // 跳过预制体根，使路径可从实例根 Transform.Find
        static string BuildRelativePath(Transform node)
        {
            var path = node.name;
            AppendAncestors(node, ref path);
            return path;
        }

        static void AppendAncestors(Transform node, ref string path)
        {
            if (node.parent == null)
                return;
            if (node.parent.parent == null)
                return;

            var parent = node.parent;
            path = parent.name + "/" + path;
            AppendAncestors(parent, ref path);
        }
    }
}
