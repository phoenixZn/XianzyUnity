using System.Collections.Generic;
using UnityEngine;
using Xease.ModelPointTool;

namespace Xease.CoreGame
{
    public sealed partial class UnityViewTransformProxy
    {
        //////////////////////////////////////////////////////////////////////////
        /// BindPoint:

        // 挂点名 → 已解析的 Transform；随 Bind/Dispose 清空，字典实例保留复用
        private Dictionary<string, Transform> _bpName2Transform;

        // 挂点查表用的预制体名；由 Bind 从外部写入，不从实例名解析
        private string _prefabName;

        /// <summary>
        /// 按挂点名取 Transform。已缓存则直接返回；未登记或找不到时返回当前绑定根。
        /// </summary>
        public Transform GetBindPoint(string bpName)
        {
            if (string.IsNullOrEmpty(bpName) || _transformRef == null)
                return _transformRef;

            if (_bpName2Transform != null && _bpName2Transform.TryGetValue(bpName, out var value))
                return value;

            var bpTrans = ModelPointGetter.FindBindPoint(_transformRef, _prefabName, bpName);
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
            _prefabName = null;
        }
    }
}
