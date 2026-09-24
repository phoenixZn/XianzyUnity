using UnityEditor;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxScanDirtyHub
    {
        public static event System.Action<string[], string[], string[], string[]> AssetsChanged;

        public static void Raise(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            AssetsChanged?.Invoke(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }

    public class VfxScanAssetWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            VfxScanDirtyHub.Raise(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }
}
