using System.Collections.Generic;
using UnityEngine;

namespace Xease.UI
{
    /// <summary>
    /// 一次生成对应的类：名字、基类和要绑定的节点。
    /// </summary>
    public class ClassInfo
    {
        public string name; // 预制体名或 v_ 去掉前缀后的名字
        public string baseName; // 生成时使用的基类名
        public bool show = true;
        public GameObject gameObject;
        public List<ElementInfo> elementList = new List<ElementInfo>(); // 本类要绑定的节点

        /// <summary>
        /// 空信息，字段稍后填写。
        /// </summary>
        public ClassInfo()
        {
        }

        /// <summary>
        /// 用类名和基类名创建。
        /// </summary>
        public ClassInfo(string name, string baseName)
        {
            this.name = name;
            this.baseName = baseName;
        }
    }

    /// <summary>
    /// 一个节点上要生成的控件类型、字段名和查找路径。
    /// </summary>
    public class ElementInfo
    {
        public List<EElementType> eleType; // 该节点上的控件种类
        public string eleName; // 字段名，不含类型后缀
        public string path; // 相对所属 Panel/View 的查找路径

        /// <summary>
        /// 空节点信息，字段稍后填写。
        /// </summary>
        public ElementInfo()
        {
        }

        /// <summary>
        /// 用控件种类、字段名和查找路径创建。
        /// </summary>
        public ElementInfo(List<EElementType> eleType, string eleName, string path)
        {
            this.eleName = eleName;
            this.eleType = eleType;
            this.path = path;
        }
    }
}