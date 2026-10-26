using System.Collections.Generic;
using Xease.CoreGame;

namespace Xease
{
    /// <summary>
    /// 产品侧游戏环境。Unity 与命令行共用同一套注册；命令行工程编不进的服务在 CONSOLE_CLIENT 下跳过。
    /// </summary>
    public class GEnvProduct : GEnv
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        /// <summary>
        /// 用注入的环境参数构造产品环境。
        /// </summary>
        /// <param name="param">日志与随机种子；非命令行时还包括 Unity 宿主。</param>
        public GEnvProduct(GEnvParam param) : base(param)
        {
        }

        //////////////////////////////////////////////////////////////////////////
        /// GEnv：override

        protected override void Inner_CreateServices()
        {
            base.Inner_CreateServices();

            Services.AddService_TickTime();
            Services.AddService_Timer();
            Services.AddService_Random(Param.EnvBaseSeed);
            Services.AddService_ValueEvent();
            Services.AddService_SharedPool();
#if CONSOLE_CLIENT
            // 命令行无 MonoBehaviour 宿主，协程走无参重载
            Services.AddService_Coroutine();
#else
            Services.AddService_Coroutine(Param.UnityHost);
            Services.AddService_Asset();
#endif
            Services.AddService_Config();
            Services.AddService_Localization(new LocalizationLanguageTable());
#if !CONSOLE_CLIENT
            // 实现位于 *.Unity，命令行工程不编入
            Services.AddService_GameObjectPools();
            Services.AddService_FxPool(Services.GameObjectPoolSvc);
            Services.AddService_Input();
            Services.AddService_DeviceProfile();
            Services.AddService_Audio();
            Services.AddService_UI();
            Services.AddService_Camera(Services.UISvc.UICamera);
#endif
            var svcLogic = Services.AddService_CustomLogic();
            svcLogic.AddConfigContainers();
#if !CONSOLE_CLIENT
            // 预热仅 Unity 宿主执行
            svcLogic.BaseWarmup();
#endif
        }

        protected override void Inner_CreateManagers()
        {
            base.Inner_CreateManagers();

            Dictionary<string, EnvStateBase> states = new()
            {
                [EnvStateID.ES_EnvInit] = new EnvInitState(),
                [EnvStateID.ES_Login] = new EnvLoginState(),
                [EnvStateID.ES_Main] = new EnvMainState(),
                [EnvStateID.ES_Battle] = new EnvBattleState(),
            };
            EnvStateMng.Initialize(states, EnvStateID.ES_EnvInit);
        }
    }
}
