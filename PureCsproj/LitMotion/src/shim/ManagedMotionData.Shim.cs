#if DEVELOPMENT_BUILD || UNITY_EDITOR
#define LITMOTION_DEBUG
#endif

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LitMotion
{
    /// <summary>
    /// 持有 Motion 的托管回调；CLI 用普通强制转换代替 UnsafeUtility.As。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ManagedMotionData
    {
        public bool CancelOnError; // Bind 异常时是否取消
        public bool SkipValuesDuringDelay; // Delay 期间是否跳过 Update 回调
        public byte StateCount; // 零闭包 Bind 的状态个数 0..3
        public object State0; // Bind 状态 0
        public object State1; // Bind 状态 1
        public object State2; // Bind 状态 2
        public object UpdateAction; // Bind 回调（Action<TValue> 或带 State 的多参委托）
        public Action<int> OnLoopCompleteAction; // 单圈结束
        public Action OnCompleteAction; // 整体结束
        public Action OnCancelAction; // 取消

#if LITMOTION_DEBUG
        public string DebugName;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateUnsafe<TValue>(in TValue value) where TValue : unmanaged
        {
            switch (StateCount)
            {
                case 0:
                    ((Action<TValue>)UpdateAction)?.Invoke(value);
                    break;
                case 1:
                    ((Action<TValue, object>)UpdateAction)?.Invoke(value, State0);
                    break;
                case 2:
                    ((Action<TValue, object, object>)UpdateAction)?.Invoke(value, State0, State1);
                    break;
                case 3:
                    ((Action<TValue, object, object, object>)UpdateAction)?.Invoke(value, State0, State1, State2);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void InvokeOnCancel()
        {
            try
            {
                OnCancelAction?.Invoke();
            }
            catch (Exception ex)
            {
                MotionDispatcher.GetUnhandledExceptionHandler()?.Invoke(ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void InvokeOnComplete()
        {
            try
            {
                OnCompleteAction?.Invoke();
            }
            catch (Exception ex)
            {
                MotionDispatcher.GetUnhandledExceptionHandler()?.Invoke(ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void InvokeOnLoopComplete(int completedLoops)
        {
            try
            {
                OnLoopCompleteAction?.Invoke(completedLoops);
            }
            catch (Exception ex)
            {
                MotionDispatcher.GetUnhandledExceptionHandler()?.Invoke(ex);
            }
        }
    }
}
