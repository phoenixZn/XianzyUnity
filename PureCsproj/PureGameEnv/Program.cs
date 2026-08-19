using System;
using System.Threading;

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
    }
}
