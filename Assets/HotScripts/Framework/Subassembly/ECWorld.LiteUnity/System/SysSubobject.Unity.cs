using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// SysSubobject 的场景调试绘制：批处理记录画查询球与本帧扫掠线段，受击体画受击球。
    /// </summary>
    public sealed partial class SysSubobject : IGizmosSystem
    {
        private static readonly Color RecordColor = new(1f, 0.6f, 0.1f);
        private static readonly Color SweepColor = new(1f, 0.9f, 0.2f);
        private static readonly Color HitBodyColor = new(0.2f, 0.8f, 1f);

        //////////////////////////////////////////////////////////////////////////
        /// IGizmosSystem:

        public void OnGizmos()
        {
            var subobjects = _metaWorld?.comUniSubobjects;
            if (subobjects == null)
                return;

            var targets = subobjects.Targets;
            Gizmos.color = HitBodyColor;
            for (var i = 0; i < targets.Count; i++)
                Gizmos.DrawWireSphere(targets.PositionAt(i), targets.RadiusAt(i));

            for (short r = 0; r < subobjects.RoutineCount; r++)
            {
                var routine = subobjects.GetRoutine(r);
                for (var i = 0; i < routine.Count; i++)
                {
                    ref readonly var motion = ref routine.MotionAt(i);
                    Gizmos.color = SweepColor;
                    Gizmos.DrawLine(motion.PrevPosition, motion.Position);
                    Gizmos.color = RecordColor;
                    Gizmos.DrawWireSphere(motion.Position, routine.PolicyAt(i).Radius);
                }
            }
        }
    }
}
