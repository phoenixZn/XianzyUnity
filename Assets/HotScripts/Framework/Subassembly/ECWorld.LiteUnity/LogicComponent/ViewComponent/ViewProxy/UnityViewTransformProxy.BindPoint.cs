using System.Collections.Generic;
using UnityEngine;
using Xease.ModelPointTool;

namespace Xease.CoreGame
{
    public sealed partial class UnityViewTransformProxy
    {
        //////////////////////////////////////////////////////////////////////////
        /// BindPoint:

        // Instantiate 追加的实例名后缀，查表前去掉以对齐预制体资源名
        private const string CloneSuffix = "(Clone)";

        // 挂点名 → 已解析的 Transform；随 Bind/Dispose 清空，字典实例保留复用
        private Dictionary<string, Transform> _bpName2Transform;

        // 预制体资源名；未写入时只从当前绑定的 Transform.name 解析一次
        private string _viewName;

        /// <summary>
        /// 按挂点名取 Transform。已缓存则直接返回；未登记或找不到时返回当前绑定根。
        /// </summary>
        public Transform GetBindPoint(string bpName)
        {
            if (string.IsNullOrEmpty(bpName) || _transformRef == null)
                return _transformRef;

            if (_bpName2Transform != null && _bpName2Transform.TryGetValue(bpName, out var value))
                return value;

            // 视图名未写入时只解析一次，避免查找热路径反复分配
            if (string.IsNullOrEmpty(_viewName))
                _viewName = ToPrefabName(_transformRef.name);

            var bpTrans = ModelPointGetter.FindBindPoint(_transformRef, _viewName, bpName);
            if (bpTrans == null)
                return _transformRef;
            SetBindPoint(bpName, bpTrans);
            return bpTrans;
        }

        /// <summary>
        /// 写入挂点缓存；同名已存在时覆盖。
        /// </summary>
        public void SetBindPoint(string bpName, Transform bpTrans)
        {
            _bpName2Transform ??= new Dictionary<string, Transform>();
            _bpName2Transform[bpName] = bpTrans;
        }

        // Bind / Dispose 时丢掉上一份绑定的挂点，字典对象留下次复用
        private void ClearBindPoints()
        {
            _bpName2Transform?.Clear();
            _viewName = null;
        }

        // 去掉实例名上的 Instantiate 后缀，得到生成表使用的预制体名
        private static string ToPrefabName(string instanceName)
        {
            if (string.IsNullOrEmpty(instanceName) || !instanceName.EndsWith(CloneSuffix))
                return instanceName;
            return instanceName.Substring(0, instanceName.Length - CloneSuffix.Length);
        }
    }
}
