using System;

namespace Xease.UI
{
    /// <summary>
    /// 将 UIBase 派生类登记到 UIManager；PrefabName 即资源 location。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class UIBaseHandlerAttribute : Attribute
    {
        public string PrefabName; // YooAsset location，同时作为 Show/Hide 的 name

        public UIBaseHandlerAttribute(string prefabName)
        {
            PrefabName = prefabName;
        }
    }
}
