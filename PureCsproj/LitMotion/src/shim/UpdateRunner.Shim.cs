using System;
using Unity.Collections;

namespace LitMotion
{
    internal interface IUpdateRunner
    {
        IMotionStorage Storage { get; }
        void Update(double time, double unscaledTime, double realtime);
        void Reset();
    }

    // CLI：用托管 for 内联原 MotionUpdateJob.Execute，避免 NativeArray + Job.Schedule
    internal sealed class UpdateRunner<TValue, TOptions, TAdapter> : IUpdateRunner
        where TValue : unmanaged
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
    {
        public UpdateRunner(MotionStorage<TValue, TOptions, TAdapter> storage, double time, double unscaledTime, double realtime)
        {
            this.storage = storage;
            prevTime = time;
            prevUnscaledTime = unscaledTime;
            prevRealtime = realtime;
        }

        readonly MotionStorage<TValue, TOptions, TAdapter> storage; // 对应类型分片存储

        double prevTime; // 上一帧 scaled 时间
        double prevUnscaledTime; // 上一帧 unscaled 时间
        double prevRealtime; // 上一帧 realtime

        public MotionStorage<TValue, TOptions, TAdapter> Storage => storage;
        IMotionStorage IUpdateRunner.Storage => storage;

        public unsafe void Update(double time, double unscaledTime, double realtime)
        {
            var count = storage.Count;
            var dataSpan = storage.GetDataSpan();
            var output = dataSpan.Length == 0 ? Array.Empty<TValue>() : new TValue[dataSpan.Length];
            using var completedIndexList = new NativeList<int>(count, Allocator.TempJob);

            var deltaTime = time - prevTime;
            var unscaledDeltaTime = unscaledTime - prevUnscaledTime;
            var realDeltaTime = realtime - prevRealtime;
            prevTime = time;
            prevUnscaledTime = unscaledTime;
            prevRealtime = realtime;

            if (dataSpan.Length == 0)
            {
                storage.RemoveAll(completedIndexList);
                return;
            }

            fixed (MotionData<TValue, TOptions>* dataPtr = dataSpan)
            {
                for (int index = 0; index < count; index++)
                {
                    var ptr = dataPtr + index;
                    ref var state = ref ptr->Core.State;
                    ref var parameters = ref ptr->Core.Parameters;

                    if (state.Status is MotionStatus.Scheduled or MotionStatus.Delayed or MotionStatus.Playing ||
                        (state.IsPreserved && state.Status is MotionStatus.Completed))
                    {
                        if (state.IsInSequence) continue;

                        var dt = parameters.TimeKind switch
                        {
                            MotionTimeKind.Time => deltaTime,
                            MotionTimeKind.UnscaledTime => unscaledDeltaTime,
                            MotionTimeKind.Realtime => realDeltaTime,
                            _ => default
                        };

                        var motionTime = state.Time + dt * state.PlaybackSpeed;
                        ptr->Update<TAdapter>(motionTime, out var result);
                        output[index] = result;
                    }
                    else if ((!state.IsPreserved && state.Status is MotionStatus.Completed) || state.Status is MotionStatus.Canceled)
                    {
                        completedIndexList.Add(index);
                        state.Status = MotionStatus.Disposed;
                    }
                }

                var managedDataSpan = storage.GetManagedDataSpan();
                var invokeLen = managedDataSpan.Length;
                if (invokeLen > dataSpan.Length) invokeLen = dataSpan.Length;
                for (int i = 0; i < invokeLen; i++)
                {
                    var currentDataPtr = dataPtr + i;
                    ref var state = ref currentDataPtr->Core.State;

                    if (state.IsInSequence) continue;

                    var status = state.Status;
                    ref var managedData = ref managedDataSpan[i];

                    bool isPlaying = status == MotionStatus.Playing;
                    bool isCompleted = status == MotionStatus.Completed;
                    bool isDelayed = status == MotionStatus.Delayed;
                    bool canUpdate = isPlaying || isCompleted || (isDelayed && !managedData.SkipValuesDuringDelay);
                    bool loopComplete = (isPlaying || isCompleted || isDelayed) && state.WasLoopCompleted;
                    if (canUpdate)
                    {
                        try
                        {
                            managedData.UpdateUnsafe(output[i]);
                        }
                        catch (Exception ex)
                        {
                            MotionDispatcher.GetUnhandledExceptionHandler()?.Invoke(ex);
                            if (managedData.CancelOnError)
                            {
                                state.Status = MotionStatus.Canceled;
                                managedData.OnCancelAction?.Invoke();
                            }
                        }
                    }
                    if (loopComplete) managedData.InvokeOnLoopComplete(state.CompletedLoops);
                    if (isCompleted && state.WasStatusChanged) managedData.InvokeOnComplete();
                }
            }

            storage.RemoveAll(completedIndexList);
        }

        public void Reset()
        {
            prevTime = 0;
            prevUnscaledTime = 0;
            prevRealtime = 0;
            storage.Reset();
        }
    }
}
