namespace Xease.UI
{
    /// <summary>
    /// 生成文件开头的 using 与类外壳模板。
    /// </summary>
    public static class UsingCodeDefine
    {
        public static string Using =
            @"
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine;
using Xease.UI;

#代码#
";
    }

    /// <summary>
    /// 生成用的 Panel 分部类（BindUI / 事件）模板。
    /// </summary>
    public static class PanelGenCodeDefine
    {
        public static string Class =
            @"
namespace Xease.UI
{
    public partial class #类名#
    {
        #region UIElement声明
        #声明#
        #endregion

        public override void BindUI()
        {
            base.BindUI();

            #region 初始化UIElement
            #初始化声明#
            #endregion
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            #region 注册事件
            #注册事件#
            #endregion
        }
        
        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            #region 反注册事件
            #反注册事件#
            #endregion
        }
    }
}";
    }
    
    
    /// <summary>
    /// 生成用的 View 分部类模板。
    /// </summary>
    public static class ViewGenCodeDefine
    {
        public static string Class =
            @"
namespace Xease.UI
{
    public partial class #类名#
    {
        #region UIElement声明
        #声明#
        #endregion

        public override void BindUI()
        {
            base.BindUI();

            #region 初始化UIElement
            #初始化声明#
            #endregion
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            #region 注册事件
            #注册事件#
            #endregion
        }
        
        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            #region 反注册事件
            #反注册事件#
            #endregion
        }
    }
}";
    }

    /// <summary>
    /// 可手改的 Panel 分部类模板。
    /// </summary>
    public static class PanelEditCodeDefine
    {
        public static string Class = 
            @"
namespace Xease.UI
{
    [UIBaseHandler(UIPanelName.#资源名#)]
    public partial class #类名# : UIPanelBase
    {
        public override void InitializeParams(UIParams uiParam)
        {
            base.InitializeParams(uiParam);
            layerId = UILayerId.Panel;
            // TODO ： 如果没有view，下面这行删除
            ViewNameList ??= new List<string>();
        }
        
        public override IEnumerator Prepare()
        {
            yield break;
        }
        
        public override void onCreate()
        {
            base.onCreate();
        }

        public override void onShow(object[] objs)
        {
            base.onShow(objs);
        }

        public override void onHide()
        {
            base.onHide();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        

        #事件#
    }
}";
    }

    /// <summary>
    /// 可手改的 View 分部类模板。特性参数用类名对应 UIViewName。
    /// </summary>
    public static class ViewEditCodeDefine
    {
        public static string Class = 
            @"
namespace Xease.UI
{
    [UIBaseHandler(UIViewName.#类名#)]
    public partial class #类名# : UIViewBase
    {
        public override IEnumerator Prepare()
        {
            yield break;
        }
        
        public override void onCreate()
        {
            base.onCreate();
        }

        public override void onShow(object[] objs)
        {
            base.onShow(objs);
        }

        public override void onHide()
        {
            base.onHide();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        

        #事件#

    }
}";
    }


    /// <summary>
    /// 滑动适配器模板。当前菜单不会写出这类文件。
    /// </summary>
    public static class AdapterCodeDefine
    {
        public static string Class =
            @"
namespace Xease.UI
{
    public partial class #类名# : BaseScrollDelegateAdapter
    {
        // 设置每个Cell大小
        public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            return 0;
        }
        
        // 获取CellView，可以看下Mission和Main是怎么实现的
        public override EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            return null;
        }
        
        // CellPrefabNameList Add之后，base中实现了初始化Cell的操作
        public override void OnInitCellPrefab()
        {
            base.OnInitCellPrefab();
        }
        
        #region 生命周期
        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override void OnShow()
        {
            base.OnShow();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion
    }
}";
    }

    /// <summary>
    /// Cell 生成分部类模板。当前菜单不会写出这类文件。
    /// </summary>
    public static class CellGenCodeDefine
    {
        public static string Class =
            @"
namespace Xease.UI
{
    public partial class #类名# : EnhancedScrollerCellView
    {
        #region UIElement声明
        #声明#
        #endregion

        public override void BindUI()
        {
            base.BindUI();

            #region 初始化UIElement
            #初始化声明#
            #endregion
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            #region 注册事件
            #注册事件#
            #endregion
        }
        
        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            #region 反注册事件
            #反注册事件#
            #endregion
        }
    }
}";
    }

    /// <summary>
    /// Cell 可手改分部类模板。当前菜单不会写出这类文件。
    /// </summary>
    public static class CellEditCodeDefine
    {
        public static string Class = 
            @"
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine;
using Xease.UI;

namespace Xease.UI
{
    public partial class #类名#
    {
        public override void onCreate()
        {
            base.onCreate();
        }

        public override void onShow()
        {
            base.onShow();
        }

        public override void onHide()
        {
            base.onHide();
        }

        public override void onDestroy()
        {
            base.onDestroy();
        }


        #事件#

    }
}";
    }

    /// <summary>
    /// 循环列表数据和项的模板。当前菜单不会写出这类文件。
    /// </summary>
    public static class ScrollDataItemEditCodeDefine
    {
        public static string Class = 
            @"
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine;
using Xease.UI;

namespace Xease.UI
{
    public class #类名#ScrollData
    {
        
    }

    public class #类名#ScrollItem : LoopScrollItem
    {
        public override void UpdateData(object obj)
        {
            base.UpdateData(obj);
        }
    }
}";
    }
}
