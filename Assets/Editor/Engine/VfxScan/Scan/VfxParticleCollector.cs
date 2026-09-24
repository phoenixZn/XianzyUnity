using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxParticleCollector
    {
        static readonly ParticleSystem.Burst[] BurstBuffer = new ParticleSystem.Burst[32];

        public static VfxParticleSystemMetrics Collect(ParticleSystem ps, Transform prefabRoot)
        {
            var metrics = new VfxParticleSystemMetrics();
            if (ps == null)
                return metrics;

            Transform t = ps.transform;
            metrics.hierarchyPath = VfxHierarchyPath.GetPath(t, prefabRoot);
            metrics.objectName = ps.gameObject.name;
            metrics.gameObjectActive = ps.gameObject.activeSelf;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            metrics.componentEnabled = renderer == null || renderer.enabled;

            ParticleSystem.MainModule main = ps.main;
            ParticleSystem.EmissionModule emission = ps.emission;
            metrics.maxParticles = main.maxParticles;
            metrics.duration = main.duration;
            metrics.looping = main.loop;
            metrics.simulationSpace = main.simulationSpace;
            metrics.lifetimeMax = Mathf.Max(VfxMinMaxCurveUtil.GetMax(main.startLifetime), 0.0001f);
            metrics.rateOverTimeMax = emission.enabled ? Mathf.Max(0f, VfxMinMaxCurveUtil.GetMax(emission.rateOverTime)) : 0f;
            metrics.burstSum = emission.enabled ? SumBursts(ps, emission, metrics.looping, metrics.duration, metrics.lifetimeMax) : 0;
            metrics.derivedParticles = ComputeDerived(metrics);
            metrics.collisionEnabled = ps.collision.enabled;
            metrics.triggerEnabled = ps.trigger.enabled;
            metrics.noiseEnabled = ps.noise.enabled;
            metrics.lightsEnabled = ps.lights.enabled;
            metrics.subEmittersEnabled = ps.subEmitters.enabled;

            if (renderer != null)
            {
                metrics.isMeshRenderMode = renderer.renderMode == ParticleSystemRenderMode.Mesh;
                if (metrics.isMeshRenderMode && renderer.mesh != null)
                {
                    Mesh mesh = renderer.mesh;
                    metrics.meshName = mesh.name;
                    metrics.meshTriangleCount = mesh.triangles != null ? mesh.triangles.Length / 3 : 0;
                }

                metrics.sortingLayer = renderer.sortingLayerName;
                metrics.sortingOrder = renderer.sortingOrder;
                if (renderer.sharedMaterial != null)
                    metrics.renderQueue = renderer.sharedMaterial.renderQueue;
            }

            return metrics;
        }

        static int ComputeDerived(VfxParticleSystemMetrics metrics)
        {
            if (!metrics.componentEnabled)
                return 0;

            float window = metrics.looping
                ? metrics.lifetimeMax
                : Mathf.Min(metrics.duration, metrics.lifetimeMax);
            int derived = Mathf.CeilToInt(metrics.rateOverTimeMax * window + metrics.burstSum);
            if (derived < 0)
                derived = 0;
            return Mathf.Min(derived, Mathf.Max(metrics.maxParticles, 0));
        }

        static int SumBursts(
            ParticleSystem ps,
            ParticleSystem.EmissionModule emission,
            bool looping,
            float duration,
            float lifeMax)
        {
            int burstCount = emission.burstCount;
            if (burstCount <= 0)
                return 0;

            ParticleSystem.Burst[] bursts = BurstBuffer;
            if (burstCount > bursts.Length)
                bursts = new ParticleSystem.Burst[burstCount];

            int written = ps.emission.GetBursts(bursts);
            int sum = 0;
            int count = Mathf.Min(written, burstCount);
            for (int i = 0; i < count; i++)
            {
                ParticleSystem.Burst burst = bursts[i];
                if (!looping && burst.time > duration)
                    continue;
                sum += GetBurstContribution(burst, looping, duration, lifeMax);
            }

            return sum;
        }

        static int GetBurstContribution(ParticleSystem.Burst burst, bool looping, float duration, float lifeMax)
        {
            int count = Mathf.Max(0, Mathf.CeilToInt(VfxMinMaxCurveUtil.GetMax(burst.count)));
            if (count <= 0)
                return 0;

            int cycleCount = burst.cycleCount;
            float interval = Mathf.Max(burst.repeatInterval, 0.0001f);

            int cycles;
            if (cycleCount <= 0)
            {
                float window = looping ? lifeMax : Mathf.Max(0f, duration - burst.time);
                cycles = Mathf.Max(1, Mathf.FloorToInt(window / interval) + 1);
            }
            else
            {
                cycles = cycleCount;
                if (!looping)
                {
                    float window = Mathf.Max(0f, duration - burst.time);
                    int fit = Mathf.FloorToInt(window / interval) + 1;
                    cycles = Mathf.Min(cycles, Mathf.Max(1, fit));
                }
            }

            return count * cycles;
        }
    }
}
