using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System.Threading;
using System;
using YooAsset;
using System.IO;
using Xease;
using HybridCLR;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class DemoHotRoot : MonoBehaviour
{
    public Text uiText;
    public Button uiButton;
    
    public static void Run()
    {
        
    }
    // Start is called before the first frame update
    public async UniTaskVoid Start()
    {
        uiButton.onClick.RemoveListener(OnUiButtonClicked);
        uiButton.onClick.AddListener(OnUiButtonClicked);
        RefreshUiText();
    }

    void OnUiButtonClicked()
    {
        Debug.Log($"[DemoHotRoot] uiButton clicked, refreshing uiText. Demo:{DemoStatic.DemoKey}");
        RefreshUiText();
    }

    void RefreshUiText()
    {
        var asetPackage = YooAssets.GetPackage(AppConfig.DefaultAssetPackageName);
        var scriptPackage = YooAssets.GetPackage(AppConfig.MainScriptRawPackageName);
        uiText.text =
            $"Demo:{DemoStatic.DemoKey} CurrentVersion： AssetVer:{asetPackage.GetPackageVersion()}, ScriptVer:{scriptPackage.GetPackageVersion()}";
    }

    public void CallTestByBuildHelper()
    {
        
    }
    
    void Update()
    {
    }
    
}
