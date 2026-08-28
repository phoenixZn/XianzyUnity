using UnityEngine;
using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 空格按下；
    /// </summary>
    public struct EvtDemoSpacePressed : IValueEvent
    {
    }

    /// <summary>
    /// Demo 输入：空格发 EvtDemoSpacePressed；左键射线命中已绑定 GO 时发死亡命令。
    /// </summary>
    public class DemoTouchInputBhv : BehaviorNodeBase
    {
        //////////////////////////////////////////////////////////////////////////
        /// BehaviorNodeBase：override

        protected override float OnUpdate(float dt)
        {
            if (G.Input.GetKeyDown(KeyCode.Space))
                G.ValueEvent.Dispatch(new EvtDemoSpacePressed());

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
