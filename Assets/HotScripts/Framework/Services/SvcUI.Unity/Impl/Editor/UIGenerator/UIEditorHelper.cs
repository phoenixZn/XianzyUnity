#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 把选中 UI 节点到最近 UI 根的路径复制到系统剪贴板。
/// </summary>
public class UIEditorHelper : EditorWindow
{
    /// <summary>
    /// 复制路径。向上直到名为 UI 开头的节点为止。
    /// </summary>
    [MenuItem("GameObject/获取UI节点路径")]
    public static void GetUIPath()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("请选择一个UI节点！");
            return;
        }

        var selectionActiveObj = Selection.activeGameObject;
        var path = selectionActiveObj.name;
        GetPath(selectionActiveObj, ref path);
        GUIUtility.systemCopyBuffer = path;
    }

    /// <summary>
    /// 从当前节点向父级拼接路径。
    /// </summary>
    public static void GetPath(GameObject obj, ref string path)
    {
        if (obj.transform.parent == null)
            return;

        var parent = obj.transform.parent;
        path = parent.name + "/" + path;
        if (parent.name.StartsWith("UI"))
            return;

        GetPath(parent.gameObject, ref path);
    }
}
#endif
