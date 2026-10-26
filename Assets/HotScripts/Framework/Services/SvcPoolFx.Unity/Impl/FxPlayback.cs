using System;
using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 挂在特效实例上的播放缓存。由特效池在运行时添加，首次收集子层级粒子和拖尾，之后重复使用。
    /// 池实例的 Stop Action 保持为 None，结束时机由特效池决定；预制体资产本身不改。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    internal sealed class FxPlayback : MonoBehaviour
    {
        // 子层级粒子，含未激活节点
        private ParticleSystem[] _particles = Array.Empty<ParticleSystem>();

        // 需要 Play 的根粒子下标
        private int[] _roots = Array.Empty<int>();

        // 子层级拖尾
        private TrailRenderer[] _trails = Array.Empty<TrailRenderer>();

        // 缓存已建立
        private bool _ready;

        // 激活前调用：收集粒子并关掉实例上的 Stop Action
        internal void Prepare()
        {
            EnsureCache();
        }

        // 激活之后调用：清掉换位前的粒子和拖尾，再播根粒子
        internal void Play()
        {
            EnsureCache();
            ClearParticles();
            ClearTrails();

            int rootCount = _roots.Length;
            for (int i = 0; i < rootCount; i++)
            {
                ParticleSystem ps = _particles[_roots[i]];
                if (ps != null && ps.gameObject.activeInHierarchy)
                {
                    ps.Play(true);
                }
            }
        }

        // 归还前调用：停止并清空粒子，丢掉拖尾点数
        internal void StopAndClear()
        {
            EnsureCache();
            int count = _particles.Length;
            for (int i = 0; i < count; i++)
            {
                ParticleSystem ps = _particles[i];
                if (ps != null)
                {
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            ClearTrails();
        }

        // 只收集一次。子发射器不进入根列表，由父粒子的 Play 带上。
        private void EnsureCache()
        {
            if (_ready)
            {
                return;
            }

            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
            TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>(true);
            _particles = particles ?? Array.Empty<ParticleSystem>();
            _trails = trails ?? Array.Empty<TrailRenderer>();

            int count = _particles.Length;
            var skip = new bool[count];
            MarkSubEmitters(skip);
            SilenceStopAction();

            int rootCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (skip[i])
                {
                    continue;
                }

                if (HasParticleAncestor(_particles[i]))
                {
                    skip[i] = true;
                    continue;
                }

                rootCount++;
            }

            _roots = new int[rootCount];
            int write = 0;
            for (int i = 0; i < count; i++)
            {
                if (skip[i])
                {
                    continue;
                }

                _roots[write++] = i;
            }

            _ready = true;
        }

        // Destroy / Callback 会在 Stop 时拆掉池对象或再归还一次
        private void SilenceStopAction()
        {
            int count = _particles.Length;
            for (int i = 0; i < count; i++)
            {
                ParticleSystem ps = _particles[i];
                if (ps == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = ps.main;
                if (main.stopAction != ParticleSystemStopAction.None)
                {
                    main.stopAction = ParticleSystemStopAction.None;
                }
            }
        }

        private void MarkSubEmitters(bool[] skip)
        {
            int count = _particles.Length;
            for (int i = 0; i < count; i++)
            {
                ParticleSystem ps = _particles[i];
                if (ps == null)
                {
                    continue;
                }

                int subCount = ps.subEmitters.subEmittersCount;
                for (int s = 0; s < subCount; s++)
                {
                    ParticleSystem sub = ps.subEmitters.GetSubEmitterSystem(s);
                    if (sub == null)
                    {
                        continue;
                    }

                    for (int k = 0; k < count; k++)
                    {
                        if (_particles[k] == sub)
                        {
                            skip[k] = true;
                            break;
                        }
                    }
                }
            }
        }

        // 父链上已有粒子则交给那个根的 Play(withChildren)
        private bool HasParticleAncestor(ParticleSystem ps)
        {
            if (ps == null)
            {
                return true;
            }

            if (ps.transform == transform)
            {
                return false;
            }

            Transform cursor = ps.transform.parent;
            while (cursor != null && cursor != transform)
            {
                if (cursor.GetComponent<ParticleSystem>() != null)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private void ClearParticles()
        {
            int count = _particles.Length;
            for (int i = 0; i < count; i++)
            {
                ParticleSystem ps = _particles[i];
                if (ps != null)
                {
                    ps.Clear(false);
                }
            }
        }

        private void ClearTrails()
        {
            int count = _trails.Length;
            for (int i = 0; i < count; i++)
            {
                TrailRenderer trail = _trails[i];
                if (trail != null)
                {
                    trail.Clear();
                }
            }
        }
    }
}
