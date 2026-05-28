using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// 补丁完成状态
    /// </summary>
    public class LStateEndPatch : LauncherState
    {
        public override void Enter()
        {
            base.Enter();
            var packageName = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_PackageName);
            _contextRef.LogInfo($"xCore: LState {packageName} is patch completed");
            // 通知外部补丁完成
            var isCompleted = _contextRef.GetBlackboardValue(LSVKey.LSV_IsPackageCompleted, false);
            if (!(bool)isCompleted)
            {
                _contextRef.SetBlackboardValue(LSVKey.LSV_IsPackageCompleted, true);
            }
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
            // 检查是否还有下一个包需要处理
            var hasNextPackage = _contextRef.GetBlackboardValue(LSVKey.LSV_HasNextPackage, false);
            if ((bool)hasNextPackage)
            {
                // 更新到下一个包的信息
                var currentIndex = (int)_contextRef.GetBlackboardValue(LSVKey.LSV_CurrentPackageIndex, 0);
                var totalCount = (int)_contextRef.GetBlackboardValue(LSVKey.LSV_TotalPackageCount, 0);
                currentIndex++;
                
                // 从黑板获取包列表（需要Launcher设置）
                var packageList = _contextRef.GetBlackboardValue(LSVKey.LSV_PackageList) as System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>;
                if (packageList != null && currentIndex < packageList.Count)
                {
                    var nextPackage = packageList[currentIndex];
                    _contextRef.SetBlackboardValue(LSVKey.LSV_PackageName, nextPackage.Key);
                    _contextRef.SetBlackboardValue(LSVKey.LSV_Version, nextPackage.Value);
                    _contextRef.SetBlackboardValue(LSVKey.LSV_CurrentPackageIndex, currentIndex);
                    _contextRef.SetBlackboardValue(LSVKey.LSV_HasNextPackage, currentIndex < packageList.Count - 1);
                    _contextRef.SetBlackboardValue(LSVKey.LSV_IsPackageCompleted, false);
                    return "LS_InitPackage"; // 处理下一个包
                }
            }
            
            // 所有包处理完成，转到加载AOT元数据
            return "LS_LoadMetadataForAOTAssemblies";
        }
    }
}
