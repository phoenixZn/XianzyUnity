using IngameDebugConsole;
using UnityEngine;


namespace Xease
{
    
    /// <summary>
    /// 功能块的真机调试窗视图。只负责往滚动内容里加自己的一块。
    /// </summary>
    public interface IGmCanvasBlock
    {
        /// <summary>在调试窗滚动内容下追加本块。</summary>
        /// <param name="content">滚动内容根节点。</param>
        void AddToCanvas(RectTransform content);
    }

    //////////////////////////////////////////////////////////////////////////
    /// ModuleGM For GmDebugCanvas:
    public partial class ModuleGM
    {
        GmDebugCanvas _canvas; // 代码生成的调试窗，关闭模块时销毁

        /// <summary>生成或重新显示运行时 GM 窗。窗体只搭一次，顶条切换当前页。</summary>
        public void ShowCanvas()
        {
            if (_canvas == null || _canvas.Root == null)
                _canvas = GmDebugCanvas.Create(this);

            _canvas.Root.SetActive(true);
        }

        partial void BindRuntimeViews()
        {
            DebugLogConsole.AddCommand_Spec(ShowCanvasCommand, CommandGroup, "打开 GM 调试窗");
        }
        
        partial void UnbindRuntimeViews()
        {
            DebugLogConsole.RemoveCommand_Spec(ShowCanvasCommand);
            if (_canvas != null)
            {
                _canvas.Destroy();
                _canvas = null;
            }
        }
    }


}
