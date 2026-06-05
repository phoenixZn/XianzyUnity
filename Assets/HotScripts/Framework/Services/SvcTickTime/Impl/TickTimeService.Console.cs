using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xease
{
    public class TimeService_Console : ITimeService
    {
        public void Shutdown()
        {
        }

        //////////////////////////////////////////////////////////////////////////
        public float fixedDeltaTime        
        {
            get => 0.02f * _timeScale;
        }
        
        public float fixedUnscaledDeltaTime
        {
            get => 0.02f;
        }

        public float deltaTime
        {
            get => 0.02f * _timeScale;
        }
        
        public float unscaledDeltaTime
        {
            get => 0.02f; 
        }

        private float _timeScale = 1f;
        public float timeScale
        {
            get => _timeScale;
            set
            {
                _timeScale = value;
            }
        }
    }
}
