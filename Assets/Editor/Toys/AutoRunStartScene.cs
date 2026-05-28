using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoadAttribute]
public static class AutoRunStartScene
{
    public static string StartSceneName = "StartScene";
    public const string MenuName = "Toys/自动运行初始场景";
    static AutoRunStartScene()
    {
        EditorApplication.playModeStateChanged += OnPlayerModeStateChanged;
    }
    private static void OnPlayerModeStateChanged(PlayModeStateChange playModeState)
    {
        if (playModeState != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }
        var currentStartScene = EditorSceneManager.GetActiveScene();
        if (Menu.GetChecked(MenuName))
        {
            if (currentStartScene.name != StartSceneName)
            {
                var targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
                EditorSceneManager.playModeStartScene = targetScene;
            }
        }
        else
        {
            var targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentStartScene.path);
            EditorSceneManager.playModeStartScene = targetScene;
        }
        //Debug.LogFormat("state:{0} will:{1} isPlaying:{2}", playModeState, EditorApplication.isPlayingOrWillChangePlaymode, EditorApplication.isPlaying);
    }
    [MenuItem(MenuName)]
    public static void RunStartScene()
    {
        bool isRunStartScene = Menu.GetChecked(MenuName);
        Menu.SetChecked(MenuName, !isRunStartScene);
    }
    static bool ValidatePlayModeUseFirstScene()
    {
        Menu.SetChecked("BuildTools/PlayModeUseFirstScene", EditorSceneManager.playModeStartScene != null); return !EditorApplication.isPlaying;
    }
    static void UpdatePlayModeUseFirstScene()
    {
        EditorApplication.playModeStateChanged += null;
        if (Menu.GetChecked("BuildTools/PlayModeUseFirstScene"))
        {
            EditorSceneManager.playModeStartScene = null;
        }
        else
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
            EditorSceneManager.playModeStartScene = scene;
        }
    }
}
