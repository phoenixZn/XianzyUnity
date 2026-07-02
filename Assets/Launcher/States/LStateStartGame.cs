using System;
using System.Linq;
using System.Reflection;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 启动游戏状态 - 切换到主页面场景
    /// </summary>
    public class LStateStartGame : LauncherState
    {
        public override void Enter()
        {
            base.Enter();
            
            PatchEventDefine.PatchStepsChange.SendEventMessage("启动游戏！");
            SceneEventDefine.StartGame.SendEventMessage();
            
            YooAssets.LoadSceneAsync("DemoHotScene");
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
            if (assembly == null)
            {
                _contextRef.LogError("LStateStartGame assembly == null");
                return;
            }
            CallAssemblyStaticMethod(assembly, "Xease.DemoStatic", "DemoStart");
            CallAssemblyStaticMethod(assembly, "Xease.DemoStatic", "DemoStep1");
            CallAssemblyStaticMethod(assembly, "Xease.DemoStatic", "DemoStep2");
            
            CallAssemblyStaticMethod(assembly, "Xease.GameEntry", "GameEntryInit");
        }

        public override void Leave()
        {
            base.Leave();
        }

        public override void Update(float dt)
        {
            base.Update(dt);
        }

        public override string CheckTransitions()
        {
            return _stateID; // 保持当前状态，不再转换
        }

        //////////////////////////////////////////////////////////////////////////
        public void CallAssemblyStaticMethod(Assembly assembly, string typeName, string methodName)
        {
            var type = assembly.GetType(typeName);
            if (type == null)
            {
                _contextRef.LogError($"CallAssemblyStaticMethod assembly.GetType({typeName}) == null");
                return;
            }
            var methodInfo = type.GetMethod(methodName);
            if (methodInfo == null)
            {
                _contextRef.LogError($"CallAssemblyStaticMethod methodInfo == null, typeName={typeName}, methodName={methodName}");
                return;
            }
            var param = new object[methodInfo.GetParameters().Length];
            methodInfo.Invoke(null, param);
        }

    }
}
