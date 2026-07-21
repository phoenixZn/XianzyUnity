using System.Collections.Generic;


namespace Xease.CoreGame
{
    public struct EvtNotifyLevelEvent_Wave : IValueEvent
    {
        public int WaveGroupIndex;
        public int WaveExLogicID;
    }
    
    
    public class LevelLogic_WaveGroup : CustomLogic
    {
        public int LastWaveGroupIndex { get; protected set; }

        private List<CustomLogic> _eventLogicEx = new();
        
        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            base.InitializeNode(cfg, context);
            if (_eventLogicEx.Count != 0)
            {
                CLogger.LogError(this, "GameLevelLogic _eventLogicEx.Count != 0"); 
            }
            G.ValueEvent.AddHandler<EvtNotifyLevelEvent_Wave>(HandleEvtNotifyWaveBegin);
            LastWaveGroupIndex = 0;
        }
        
        public override void Destroy()
        {
            G.ValueEvent.RemoveHandler<EvtNotifyLevelEvent_Wave>(HandleEvtNotifyWaveBegin);
            foreach (var logic in _eventLogicEx)
            {
                G.CustomLogic.DestroyLogic(logic);
            }
            _eventLogicEx.Clear();
            LastWaveGroupIndex = -1;
            base.Destroy();
        }

        private void HandleEvtNotifyWaveBegin(EvtNotifyLevelEvent_Wave evt)
        {
            LastWaveGroupIndex = evt.WaveGroupIndex;
            int logicID = evt.WaveExLogicID;
            CLogger.LogInfo($"GameLevelLogic EvtNotifyWaveBegin： WaveGroupIndex={evt.WaveGroupIndex}, logicID:{logicID}");
            if (logicID <= 0)
            {
                return;
            }
            // var metaWorld = this.GetMetaWorld();
            // var svc = G.CustomLogicService;
            // VarEnv varEnv = svc.NewVarEnv();
            // varEnv.WriteVar(CvKey.CV_LogicWorld, this.GetLogicWorld());
            // varEnv.WriteVar(CvKey.CV_MetaWorld, metaWorld);
            // var genInfo = svc.NewGenInfo<CustomLogicGenInfo>();
            // genInfo.LogicConfigID = logicID;
            // genInfo.ConfigContainerName = LogicContainerKey.LogicConfigs_GameLevel;
            // genInfo.PreEnv = varEnv;
            // var logic = svc.CreateLogic(genInfo);
            // if (logic != null)
            // {
            //     _eventLogicEx.Add(logic);    
            // }
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