using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;
    
    public partial class LogicConfig_Subobject
    {
        // 冒烟测试用的实体子物体逻辑，由 SysDebugDemo.Subobject 生成并读回黑板
        public const int DemoSmokeLogicId = 6900001;
        public const string DemoSmokeTickVar = "SmokeTickCount"; // 每次 Update 累加
        public const string DemoSmokeHitVar = "SmokeHitCount";   // 关联记录每命中一次累加

        protected void InitConfigs_Demo()
        {
            AddConfig(DemoSmokeLogicId, new Nodes()
            {
                UpdateCall((CustomNode node, float dt) =>
                {
                    node.SetVar(DemoSmokeTickVar, node.GetVar<int>(DemoSmokeTickVar) + 1);
                }),
                new SubobjectHitCallBhvCfg((CustomNode node, in SubobjectHitEvent hit) =>
                {
                    node.SetVar(DemoSmokeHitVar, node.GetVar<int>(DemoSmokeHitVar) + 1);
                }),
            });
        }
    }
}
