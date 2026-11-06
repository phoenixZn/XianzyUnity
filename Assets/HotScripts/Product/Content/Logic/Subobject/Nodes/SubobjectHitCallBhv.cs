namespace Xease.CoreGame
{
    public static partial class NodeConfigTypeRegistry
    {
        static bool _SubobjectHitCallBhvCfg = Register(typeof(SubobjectHitCallBhvCfg), NodeCategory.Bhv);
    }

    // 关联记录命中时的回调
    public delegate void SubobjectHitAction(CustomNode node, in SubobjectHitEvent hit);

    /// <summary>
    /// 命中回调配置：所属 SubobjectLogic 的关联记录每命中一次，调用一次 Func。
    /// </summary>
    public class SubobjectHitCallBhvCfg : ICustomNodeCfg
    {
        public SubobjectHitAction Func { get; protected set; }

        /// <summary>
        /// 以回调创建配置。
        /// </summary>
        public SubobjectHitCallBhvCfg(SubobjectHitAction func)
        {
            Func = func;
        }

        //////////////////////////////////////////////////////////////////////////
        /// ICustomNodeCfg:

        /// <summary>
        /// 返回运行时节点类型。
        /// </summary>
        public System.Type NodeType()
        {
            return typeof(SubobjectHitCallBhv);
        }
    }

    /// <summary>
    /// 运行时命中回调节点：由 SubobjectLogic.OnRoutineHit 通知，把命中事件转给配置的回调。
    /// </summary>
    public class SubobjectHitCallBhv : BehaviorNode<SubobjectHitCallBhvCfg>, ISubobjectHitNotify
    {
        //////////////////////////////////////////////////////////////////////////
        /// ISubobjectHitNotify:

        /// <summary>
        /// 转发一次命中。
        /// </summary>
        public void OnRoutineHit(in SubobjectHitEvent hit)
        {
            mCfg?.Func?.Invoke(this, in hit);
        }
    }
}
