using System;
using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 把外部登记的无参函数做成按钮的 GM 功能块。列表只存在本类；编辑器和真机窗都读这里。
    /// </summary>
    public partial class GmStaticAction : IGmBlock
    {
        public const string BlockId = "static_action"; // 插装标识

        readonly List<Entry> _entries = new(); // Add 的顺序即按钮顺序

        /// <summary>一个已登记按钮：名字，以及点击时调用的函数。</summary>
        readonly struct Entry
        {
            /// <summary>记下按钮名和要调用的函数。</summary>
            public Entry(string name, Action action)
            {
                Name = name;
                Action = action;
            }

            public string Name { get; } // 按钮文字
            public Action Action { get; } // 点击时调用的无参函数
        }

        //////////////////////////////////////////////////////////////////////////
        /// IGmBlock:

        public string Id => BlockId;

        public string Title => "静态动作";

        /// <summary>本块所在的顶条页。编辑器和真机窗共用。</summary>
        public string Tab => "Action";

        //////////////////////////////////////////////////////////////////////////
        /// This：

        /// <summary>按调用顺序追加一个按钮。名字或函数无效时不写入。</summary>
        /// <param name="name">按钮文字。</param>
        /// <param name="action">点击时调用的无参函数。</param>
        public void Add(string name, Action action)
        {
            if (string.IsNullOrEmpty(name) || action == null)
            {
                G.LogError("[GmStaticAction] Add failed, name or action is null");
                return;
            }

            _entries.Add(new Entry(name, action));
        }
    }
}
