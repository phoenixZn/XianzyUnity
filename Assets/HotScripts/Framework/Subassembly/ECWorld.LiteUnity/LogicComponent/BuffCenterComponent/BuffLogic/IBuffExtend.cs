//////////////////////////////////////////////////////////////////////////
// Buff面向特殊游戏的 外部信息接口扩展
//////////////////////////////////////////////////////////////////////////
namespace Xease.CoreGame
{
    public interface IEntityAddBuffNotify
    {
        void OnEntityAddBuff(LogicEntity e);
    }

    public interface IBuffPreviousRemove
    {
        void OnBuffPreviousRemove();
    }

    public interface IBuffStateChanged
    {
        void OnBuffStateChanged(LogicEntity e, BuffLogic buff);
    }
}
