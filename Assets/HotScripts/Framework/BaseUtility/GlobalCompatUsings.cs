using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// 用于支持 init 访问器的内部类型。
    /// 当目标框架缺少该类型时，添加此定义。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}