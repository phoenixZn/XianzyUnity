using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xease
{
    public class TimeService_Unity : ITimeService
    {
        public void Shutdown()
        {
        }

        //////////////////////////////////////////////////////////////////////////
        public float fixedDeltaTime        {
            get => Time.fixedDeltaTime;
        }
        
        public float fixedUnscaledDeltaTime
        {
            get => Time.fixedUnscaledDeltaTime;
        }

        public float deltaTime
        {
            get => Time.deltaTime;
        }
        
        public float unscaledDeltaTime
        {
            get => Time.unscaledDeltaTime; 
        }

        public float timeScale
        {
            get => Time.timeScale;
            set
            {
                Time.timeScale = value;
            }
        }
    }
}
