using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Xease
{
    public static class GameObjectPoolExtensions
    {
        public static GameObject SetParent(this GameObject go, Transform parent, bool worldPositionStays)
        {
            if (go) go.transform.SetParent(parent, worldPositionStays);
            return go;
        }

        /// <summary>
        /// 挂到 parent 下并保持世界坐标（worldPositionStays = true）。
        /// </summary>
        public static GameObject SetParentKeepWorld(this GameObject go, Transform parent)
        {
            if (go) go.transform.SetParent(parent, true);
            return go;
        }

        /// <summary>
        /// 挂到 parent 下并保持本地坐标（worldPositionStays = false）。
        /// </summary>
        public static GameObject SetParentKeepLocal(this GameObject go, Transform parent)
        {
            if (go) go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject SetPosition(this GameObject go, Vector3 position)
        {
            if (go) go.transform.position = position;
            return go;
        }

        public static GameObject SetLocalPosition(this GameObject go, Vector3 localPosition)
        {
            if (go) go.transform.localPosition = localPosition;
            return go;
        }

        public static GameObject SetRotation(this GameObject go, Quaternion rotation)
        {
            if (go) go.transform.rotation = rotation;
            return go;
        }

        public static GameObject SetLocalRotation(this GameObject go, Quaternion localRotation)
        {
            if (go) go.transform.localRotation = localRotation;
            return go;
        }

        public static GameObject SetScale(this GameObject go, Vector3 scale)
        {
            if (go) go.transform.localScale = scale;
            return go;
        }

        public static GameObject SetActiveState(this GameObject go, bool state)
        {
            if (go) go.SetActive(state);
            return go;
        }


        public static async UniTask<GameObject> SetParent(this UniTask<GameObject> task, Transform parent, bool worldPositionStays)
        {
            var go = await task;
            if (go) go.transform.SetParent(parent, worldPositionStays);
            return go;
        }

        /// <summary>
        /// 挂到 parent 下并保持世界坐标（worldPositionStays = true）。
        /// </summary>
        public static async UniTask<GameObject> SetParentKeepWorld(this UniTask<GameObject> task, Transform parent)
        {
            var go = await task;
            if (go) go.transform.SetParent(parent, true);
            return go;
        }

        /// <summary>
        /// 挂到 parent 下并保持本地坐标（worldPositionStays = false）。
        /// </summary>
        public static async UniTask<GameObject> SetParentKeepLocal(this UniTask<GameObject> task, Transform parent)
        {
            var go = await task;
            if (go) go.transform.SetParent(parent, false);
            return go;
        }

        public static async UniTask<GameObject> SetPosition(this UniTask<GameObject> task, Vector3 position)
        {
            var go = await task;
            if (go) go.transform.position = position;
            return go;
        }

        public static async UniTask<GameObject> SetLocalPosition(this UniTask<GameObject> task, Vector3 localPosition)
        {
            var go = await task;
            if (go) go.transform.localPosition = localPosition;
            return go;
        }

        public static async UniTask<GameObject> SetRotation(this UniTask<GameObject> task, Quaternion rotation)
        {
            var go = await task;
            if (go) go.transform.rotation = rotation;
            return go;
        }

        public static async UniTask<GameObject> SetLocalRotation(this UniTask<GameObject> task, Quaternion localRotation)
        {
            var go = await task;
            if (go) go.transform.localRotation = localRotation;
            return go;
        }

        public static async UniTask<GameObject> SetScale(this UniTask<GameObject> task, Vector3 scale)
        {
            var go = await task;
            if (go) go.transform.localScale = scale;
            return go;
        }

        public static async UniTask<GameObject> SetActiveState(this UniTask<GameObject> task, bool state)
        {
            var go = await task;
            if (go) go.SetActive(state);
            return go;
        }

    }
}