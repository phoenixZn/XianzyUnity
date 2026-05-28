using System.Collections;
using System.Collections.Generic;

public static class AppConfig
{
    public const string HotAssetFolderName = "HotAssets";
    public const string HotUpdateDLLFolderName = "Dll_HotUpdate";
    public const string PatchedAOTDLLFolderName = "Dll_PatchedAOT";
    public static string HotUpdateDLLPath => $"{HotAssetFolderName}/{HotUpdateDLLFolderName}";
    public static string PatchedAOTDLLPath = $"{HotAssetFolderName}/{PatchedAOTDLLFolderName}";
    
    //public const string DefaultAssetPackageName = "PackPrimaryAsset";
    public const string DefaultAssetPackageName = "PackDemoAsset";
    public const string MainScriptRawPackageName = "PackMainScript";
    public const string HotDLLsTxtFileName = "HotUpdateDLLs"; //$"{AppConfig.HotDLLsTxtFileName}.txt"
}
