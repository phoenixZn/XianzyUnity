using System.Collections.Generic;

namespace Xease.CoreGame
{
    public struct EvtNotifyLevelEventLogic : IValueEvent
    {
        public int EventLogicID;
    }
    
    
    public class GameLevelLogic : CustomLogic
    {
        private List<CustomLogic> _eventLogicEx = new();
        
        public bool SceneLogicLoaded { get; set; }
        
        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            SceneLogicLoaded = false;
            base.InitializeNode(cfg, context);
            if (_eventLogicEx.Count != 0)
            {
                CLogger.LogError(this, "GameLevelLogic _eventLogicEx.Count != 0");  //Debug确认
            }
            G.ValueEvent.AddHandler<EvtNotifyLevelEventLogic>(HandleEvtNotifyLevelEventLogic);
        }
        
        public override void Destroy()
        {
            G.ValueEvent.RemoveHandler<EvtNotifyLevelEventLogic>(HandleEvtNotifyLevelEventLogic);
            foreach (var logic in _eventLogicEx)
            {
                G.CustomLogic.DestroyLogic(logic);
            }
            _eventLogicEx.Clear();
            SceneLogicLoaded = false;
            base.Destroy();
        }

        // 队长首次进入刷怪区域时触发的关卡事件逻辑
        private void HandleEvtNotifyLevelEventLogic(EvtNotifyLevelEventLogic evt)
        {
            int logicID = evt.EventLogicID;
            if (G.IsDev)
                this.Log($"GameLevelLogic EvtNotifyLevelEventLogic： logicID:{logicID}");
            if (logicID <= 0)
            {
                return;
            }
            var logic = CreateEventLogic(logicID);
            if (logic != null)
            {
                _eventLogicEx.Add(logic);
            }
        }

        //关卡中的EventLogic暂时和LevelLogic使用相同的 GameLevelLogicGenInfo，属于SubLevelLogic
        private CustomLogic CreateEventLogic(int logicID)
        {
            var thisGenInfo = GetGenInfo<GameLevelLogicGenInfo>();
            var svc = G.CustomLogic;
            VarEnv varEnv = svc.NewVarEnv();
            var genInfo = GameLevelLogicGenInfo.New(svc, thisGenInfo.ECWorlds, thisGenInfo.WorldCreationInfo);
            genInfo.LogicConfigID = logicID;
            genInfo.ConfigContainerName = LogicContainerKey.LogicConfigs_GameLevelEvent;
            genInfo.PreEnv = varEnv;
            var logic = svc.CreateLogic(genInfo);
            return logic;
        }
        
        public override float Update(float dt)
        {
            foreach (var logic in _eventLogicEx)
            {
                logic.Update(dt);
            }

            for (int i = _eventLogicEx.Count -1; i >= 0; i--)
            {
                var logic = _eventLogicEx[i];
                if (logic.IsNodeCanStop())
                {
                    _eventLogicEx.Remove(logic);
                    G.CustomLogic.DestroyLogic(logic);
                }
            }

            return base.Update(dt);
        }
    }
}