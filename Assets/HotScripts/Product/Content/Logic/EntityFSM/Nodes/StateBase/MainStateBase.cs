namespace Xease.CoreGame
{
    public class MainStateBase : CustomBhvState, IEntityCommandHandler
    {
        protected LogicEntity _ownerEntity { get; private set; }
        protected string ChoiceNextStateID { get; private set; }
        protected string _logName = null;
        
        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            base.InitializeNode(cfg, context);
            _ownerEntity = this.GetOwnerEntity();
                
            if (_ownerEntity == null)
            {
                this.LogError($"MainStateBase _ownerEntity == null");
                return;
            }
            //_logName = $"{_ownerEntity.ID}";
        }

        public override void Destroy()
        {
            _ownerEntity = null;
            _logName = null;
            base.Destroy();
        }
        
        public override void Enter()
        {
            base.Enter();
            if (_logName != null)
                this.LogInfo($"{_logName} Enter --> {GetType()}");
        }

        public override void Exit()
        {
            ChoiceNextStateID = null;
            if (_logName != null)
                this.LogInfo($"{_logName} Exit <-- {GetType()}");
            base.Exit();
        }

        public override string CheckTransitions()
        {
            //层1：使用子类继承 直接条件return的状态
            
            //层2：使用硬编码 在其他函数中动态设置的状态：
            if (ChoiceNextStateID != null)
            {
                return ChoiceNextStateID;
            }
            
            //层3：使用静态配置的缺省状态转换，或配置的转换节点
            return base.CheckTransitions();
        }

        //////////////////////////////////////////////////////////////////////////
        public virtual bool HandleEntityCommand(LogicEntity entity, EntityCommand cmd)
        {
            if (cmd.CmdType == EntityCmdType.Nt_Death)
            {
                if (this.isDebug())
                    this.Log($"{StateID}.HandleEntityCommand Nt_Death: -> MST_Die");
                ChooseNextState("MST_Die");
                return false;
            }
            return false;
        }
        
        //弱 状态抉择: 如果没有已选择的新状态，才去设置
        protected void ChooseNextStateIfEmpty(string stateID)
        {
            if (string.IsNullOrEmpty(ChoiceNextStateID))
                ChoiceNextStateID = stateID;
        }
        
        //强 状态抉择：
        protected void ChooseNextState(string stateID)
        {
            ChoiceNextStateID = stateID;
        }

    }
}