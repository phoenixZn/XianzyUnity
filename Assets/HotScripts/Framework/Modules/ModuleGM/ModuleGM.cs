using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 测试用 GM 模块。持有功能块列表，并把同一批实例接到编辑器、命令行和运行时调试窗。
    /// 不分支具体功能；块的配置与生效留在块自身。
    /// </summary>
    [ModuleAutoRegister]
    public partial class ModuleGM : Module
    {
        readonly List<IGmBlock> _blocks = new();

        /// <summary>已插装的功能块，顺序与目录一致。</summary>
        public IReadOnlyList<IGmBlock> Blocks => _blocks;

        protected override void OnInit()
        {
            base.OnInit();
            RegisterCatalogBlocks();
        }

        protected override void OnStart()
        {
            base.OnStart();
            BindLogConsole();
            BindRuntimeViews();
        }

        protected override void OnShutdown()
        {
            UnBindLogConsole();
            UnbindRuntimeViews();
            _blocks.Clear();
            base.OnShutdown();
        }

        void Register(IGmBlock block)
        {
            if (block == null)
            {
                G.LogError("[ModuleGM] Register failed, block is null");
                return;
            }

            _blocks.Add(block);
        }

        // 视图实现在 *.Unity.cs；命令行宿主不编入该文件时，这两处调用会被抹掉
        partial void BindLogConsole();
        partial void UnBindLogConsole();
        
        partial void BindRuntimeViews();
        partial void UnbindRuntimeViews();

    }
}
