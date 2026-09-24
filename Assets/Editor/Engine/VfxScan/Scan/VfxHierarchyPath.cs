using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxHierarchyPath
    {
        public static bool IsInsideNestedPrefab(GameObject go, GameObject prefabRoot)
        {
            if (go == null || prefabRoot == null)
                return false;

            GameObject nearest = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (nearest == null || nearest == prefabRoot)
                return false;
            return true;
        }

        public static bool IsOwnedByPrefab(GameObject go, GameObject prefabRoot)
        {
            return go != null && !IsInsideNestedPrefab(go, prefabRoot);
        }

        public static string GetPath(Transform target, Transform root)
        {
            if (target == null || root == null || target == root)
                return string.Empty;

            var stack = new Stack<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            if (stack.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            bool first = true;
            while (stack.Count > 0)
            {
                if (!first)
                    sb.Append('/');
                sb.Append(stack.Pop());
                first = false;
            }

            return sb.ToString();
        }

        public static Transform Find(Transform root, string hierarchyPath)
        {
            if (root == null)
                return null;
            if (string.IsNullOrEmpty(hierarchyPath))
                return root;

            Transform current = root;
            string[] parts = hierarchyPath.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                Transform child = current.Find(parts[i]);
                if (child == null)
                    return null;
                current = child;
            }

            return current;
        }

        public static GameObject FindGameObject(GameObject root, string hierarchyPath)
        {
            if (root == null)
                return null;
            Transform t = Find(root.transform, hierarchyPath);
            return t != null ? t.gameObject : null;
        }
    }
}
