using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PureGameEnv
{
    internal static class Program
    {
        public static bool isWorking { get; set; } = true;

        private static void Main(string[] args)
        {
            Console.WriteLine("PureGameEnv: smoke build / run.");
            if (args.Length > 0)
            {
                Console.WriteLine(string.Join(" ", args));
            }

            Console.CancelKeyPress += (_, e) =>
            {
                isWorking = false;
                e.Cancel = true;
            };

            SmokeUniTaskYield();

            var gameEntry = Xease.GameEntry.GameEntryInit();
            while (isWorking)
            {
                gameEntry.FixedUpdate();
                gameEntry.Update();
                gameEntry.LateUpdate();
                Thread.Sleep(20);
            }

            gameEntry.Destroy();
        }

        // 确认 NetCore UniTask 续体可用（Yield 走线程池 / SynchronizationContext，不挂 PlayerLoop）
        static void SmokeUniTaskYield()
        {
            var tcs = new UniTaskCompletionSource();
            UniTask.Void(async () =>
            {
                await UniTask.Yield();
                Console.WriteLine("UniTask: Yield completed.");
                tcs.TrySetResult();
            });
            // UniTask.GetResult 在 Pending 时会抛，不能当阻塞等待用
            tcs.Task.AsTask().GetAwaiter().GetResult();
        }
    }
}
