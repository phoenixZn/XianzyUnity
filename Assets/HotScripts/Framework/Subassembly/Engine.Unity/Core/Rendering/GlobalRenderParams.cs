using UnityEngine;

namespace Xease.Engine.Core.Rendering
{
    /// <summary>
    /// 全局渲染参数存储与 Shader 同步。值由 RenderingEnviroment 栈同步写入；
    /// URP 管线每帧调用 Apply() 写入 Shader 全局量。
    /// 应用层请通过 RenderingEnviroment 静态 API 或场景组件访问，勿直接引用本类。
    /// </summary>
    public static class GlobalRenderParams
    {
        public const float DefaultWindStrength = 1.0f;

        private static readonly int s_WindParamsId = Shader.PropertyToID("_KaGlobalWindParams");

        private static float s_WindStrength = DefaultWindStrength;

        public static float WindStrength
        {
            get => s_WindStrength;
            set => s_WindStrength = Mathf.Max(0f, value);
        }

        public static void ResetWindStrength()
        {
            s_WindStrength = DefaultWindStrength;
        }

        public static void Apply()
        {
            Shader.SetGlobalVector(s_WindParamsId, new Vector4(s_WindStrength, 0f, 0f, 0f));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            s_WindStrength = DefaultWindStrength;
            Apply();
        }
    }
}
