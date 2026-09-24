using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxHierarchyCollector
    {
        public static VfxHierarchyMetrics Collect(GameObject prefabRoot)
        {
            var metrics = new VfxHierarchyMetrics();
            if (prefabRoot == null)
                return metrics;

            CollectRecursive(prefabRoot.transform, prefabRoot, 1, metrics);
            CollectRedundants(prefabRoot, metrics);
            return metrics;
        }

        static void CollectRecursive(Transform t, GameObject prefabRoot, int depth, VfxHierarchyMetrics metrics)
        {
            metrics.nodeCount++;
            if (depth > metrics.maxDepth)
                metrics.maxDepth = depth;

            if (t.gameObject != prefabRoot && VfxHierarchyPath.IsInsideNestedPrefab(t.gameObject, prefabRoot))
                return;

            int childCount = t.childCount;
            for (int i = 0; i < childCount; i++)
                CollectRecursive(t.GetChild(i), prefabRoot, depth + 1, metrics);
        }

        static void CollectRedundants(GameObject prefabRoot, VfxHierarchyMetrics metrics)
        {
            Transform root = prefabRoot.transform;

            ParticleSystem[] particles = prefabRoot.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem ps = particles[i];
                if (!VfxHierarchyPath.IsOwnedByPrefab(ps.gameObject, prefabRoot))
                    continue;

                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                bool rendererDisabled = renderer != null && !renderer.enabled;
                if (ps.gameObject.activeSelf && !rendererDisabled)
                    continue;

                metrics.redundants.Add(new VfxRedundantComponentInfo
                {
                    hierarchyPath = VfxHierarchyPath.GetPath(ps.transform, root),
                    componentType = "ParticleSystem",
                    reason = !ps.gameObject.activeSelf ? "节点已禁用" : "粒子 Renderer 已禁用"
                });
            }

            Animator[] animators = prefabRoot.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (!VfxHierarchyPath.IsOwnedByPrefab(animator.gameObject, prefabRoot))
                    continue;
                if (animator.runtimeAnimatorController != null)
                    continue;

                metrics.redundants.Add(new VfxRedundantComponentInfo
                {
                    hierarchyPath = VfxHierarchyPath.GetPath(animator.transform, root),
                    componentType = "Animator",
                    reason = "未指定 Animator Controller"
                });
            }

            AudioSource[] audios = prefabRoot.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audios.Length; i++)
            {
                if (!VfxHierarchyPath.IsOwnedByPrefab(audios[i].gameObject, prefabRoot))
                    continue;

                metrics.redundants.Add(new VfxRedundantComponentInfo
                {
                    hierarchyPath = VfxHierarchyPath.GetPath(audios[i].transform, root),
                    componentType = "AudioSource",
                    reason = "特效 Prefab 上残留音频组件"
                });
            }

            Camera[] cameras = prefabRoot.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (!VfxHierarchyPath.IsOwnedByPrefab(cameras[i].gameObject, prefabRoot))
                    continue;

                metrics.redundants.Add(new VfxRedundantComponentInfo
                {
                    hierarchyPath = VfxHierarchyPath.GetPath(cameras[i].transform, root),
                    componentType = "Camera",
                    reason = "特效 Prefab 上残留相机组件"
                });
            }
        }
    }
}
