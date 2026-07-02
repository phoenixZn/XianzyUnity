using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xease
{
    public static class DemoStatic
    {
        public static int DemoKey;

        public static void DemoStart()
        {
            DemoKey = 860971;
            Debug.Log("DemoStatic.DemoStart Run 4");
        }

        public static void DemoStep1()
        {
            Debug.Log("DemoStatic.DemoStep1 Run");
        }

        public static void DemoStep2()
        {
            Debug.Log("DemoStatic.DemoStep2 Run");
        }

        public static void DemoError()
        {
            Debug.LogError($"DemoStatic.DemoError DemoKey={DemoKey}");
        }
    }
}