namespace Xease
{
    /// <summary>
    /// 一个 GM 功能块。配置与生效状态由实现类独占，模块只负责插装，并把同一实例接到各视图。
    /// </summary>
    public interface IGmBlock
    {
        /// <summary>插装用的稳定标识。</summary>
        string Id { get; }

        /// <summary>各视图上的标题。</summary>
        string Title { get; }

        /// <summary>顶条页签名。同名块画在同一页，顺序跟目录。编辑器和真机窗共用。</summary>
        string Tab { get; }
    }
}
