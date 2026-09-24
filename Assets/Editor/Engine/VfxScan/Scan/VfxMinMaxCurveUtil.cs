using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxMinMaxCurveUtil
    {
        public static float GetMax(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return Mathf.Max(curve.constantMin, curve.constantMax);
                case ParticleSystemCurveMode.Curve:
                    return GetCurveMax(curve.curve, curve.curveMultiplier);
                case ParticleSystemCurveMode.TwoCurves:
                    return Mathf.Max(
                        GetCurveMax(curve.curveMin, curve.curveMultiplier),
                        GetCurveMax(curve.curveMax, curve.curveMultiplier));
                default:
                    return Mathf.Max(curve.constant, curve.constantMax);
            }
        }

        static float GetCurveMax(AnimationCurve curve, float multiplier)
        {
            if (curve == null || curve.length == 0)
                return 0f;

            float max = float.MinValue;
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
                max = Mathf.Max(max, keys[i].value);

            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                max = Mathf.Max(max, curve.Evaluate(t));
            }

            if (max == float.MinValue)
                max = 0f;

            return max * multiplier;
        }
    }
}
