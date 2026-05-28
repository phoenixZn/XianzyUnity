using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System.Threading;
using System;
using YooAsset;
using System.IO;
using System.Reflection;
using HotUpdate;
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
        var gamePackage = YooAssets.GetPackage("PackDemoAsset");
        var scriptPackage = YooAssets.GetPackage("PackMainScript");
        uiText.text =
            $"Demo:{DemoStatic.DemoKey} CurrentVersion： AssetVer:{gamePackage.GetPackageVersion()}, ScriptVer:{scriptPackage.GetPackageVersion()}";
    }

    public void CallTestByBuildHelper()
    {
        
    }
    
    void Update()
    {
    }
    
}
