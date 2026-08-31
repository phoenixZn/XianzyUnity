using System.Collections.Generic;
using Xease;

namespace Xease.CoreGame
{
    public interface IEntityCommandHandler
    {
        bool HandleEntityCommand(LogicEntity entity, EntityCommand cmd);
    }
    
    //值类型Cmd，外部自定义扩展
    public partial struct EntityCommand
    {
        public int CmdType { get; set; }
    }

    //////////////////////////////////////////////////////////////////////////
    public sealed class CommandSenderComponent : LogicComponent
    {
        private List<EntityCommand> _sendQueue = new(4);

        public List<EntityCommand> SendQueue
        {
            get { return _sendQueue; }
            set { _sendQueue = value; }
        }

        private IEntityCommandPreHandler _preHandler;

        public void Initialize(IEntityCommandPreHandler preHandler = null)
        {
            _preHandler = preHandler;
        }

        //预处理命令
        //常见的处理有：服务器确认前 先做预测性表现、RTS低级指令转高级指令、连续指令输入型出招表
        public void PreHandleCommand()
        {
            if (_preHandler == null)
                return;

            for (int i = 0; i < _sendQueue.Count; i++)
            {
                var cmd = _sendQueue[i];
                _preHandler.PreHandleCommand(_hostEntity, cmd);
            }
        }
        
        public bool PreHandleSilentlyAndImmediately(EntityCommand cmd)
        {
            if (_preHandler == null)
                return false;
            return _preHandler.PreHandleSilentlyAndImmediately(_hostEntity, cmd);
        }

        public override void DisposeOnRemove()
        {
            _preHandler?.Recycle();
            _preHandler = null;
            _sendQueue.Clear();
            base.DisposeOnRemove();
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public CommandSenderComponent comCommandSender
        {
            get { return (CommandSenderComponent)GetComponent(LogicComponentsLookup.ComCommandSender); }
        }

        public bool hasComCommandSender
        {
            get { return HasComponent(LogicComponentsLookup.ComCommandSender); }
        }

        /// <summary>
        /// 从 SharedPool 租用预处理器并挂 CommandSender；移除时 Recycle 归还。
        /// </summary>
        public void AddComCommandSender<T>() where T : class, IEntityCommandPreHandler, new()
        {
            AddComCommandSender(G.SharedPool.Rent<T>());
        }

        public void AddComCommandSender(IEntityCommandPreHandler preHandler = null)
        {
            var index = LogicComponentsLookup.ComCommandSender;
            var component = (CommandSenderComponent)CreateComponent(index, typeof(CommandSenderComponent));
            component.Initialize(preHandler);
            AddComponent(index, component);
        }

        public void SendCmd(EntityCommand cmd)
        {
            if (!hasComCommandSender)
            {
                WLogger.LogError("entity.SendCmd but !hasComCommandSender");
                return;
            }
                
            var index = LogicComponentsLookup.ComCommandSender;
            if (comCommandSender.PreHandleSilentlyAndImmediately(cmd))
            {
                return;
            }
            comCommandSender.SendQueue.Add(cmd);
            ReplaceComponent(index, comCommandSender);
        }
    }

    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComCommandSenderIndex = new(typeof(CommandSenderComponent));
        public static int ComCommandSender => _ComCommandSenderIndex.Index;
    }
}