using UnityEngine;

namespace Xease
{
    public class TimeService_Unity : TimeServiceBase
    {
        public override void Shutdown()
        {
            ResetTimeScale();
        }

        //////////////////////////////////////////////////////////////////////////
        /// ITickTimeService：

        public override float fixedDeltaTime
        {
            get => Time.fixedDeltaTime;
        }

        public override float fixedUnscaledDeltaTime
        {
            get => Time.fixedUnscaledDeltaTime;
        }

        public override float deltaTime
        {
            get => Time.deltaTime;
        }

        public override float unscaledDeltaTime
        {
            get => Time.unscaledDeltaTime;
        }

        public override float timeScale
        {
            get => Time.timeScale;
            protected set
            {
                Time.timeScale = value;
            }
        }
    }
}
