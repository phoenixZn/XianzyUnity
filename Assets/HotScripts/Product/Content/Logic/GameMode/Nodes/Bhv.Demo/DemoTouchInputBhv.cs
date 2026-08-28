using UnityEngine;
using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 点击：屏幕射线命中已绑定 GO 时给对应 entity 挂死亡组件。
    /// </summary>
    public class DemoTouchInputBhv : BehaviorNodeBase
    {
        //////////////////////////////////////////////////////////////////////////
        /// BehaviorNodeBase：override

        // 左键按下发线；命中后 TryGetEntityWithUnityObjectRelated → AddComDeath
        protected override float OnUpdate(float dt)
        {
            if (!G.Input.GetKeyDown(KeyCode.Mouse0))
                return dt;

            var cam = Camera.main;
            if (cam == null)
                return dt;

            var world = this.GetLogicWorld();
            if (world == null)
                return dt;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit))
                return dt;

            if (!world.TryGetEntityWithUnityObjectRelated(hit.collider.gameObject, out var entity))
                return dt;

            if (entity == null || !entity.isEnabled || entity.hasComDeath)
                return dt;
            
            entity.SendCmd(new EntityCommand(){CmdType = EntityCmdType.Nt_Death, });
            //entity.SendCmd(new EntityCommand(){CmdType = EntityCmdType.Nt_ForceDeath, });
            return dt;
        }
    }
}
