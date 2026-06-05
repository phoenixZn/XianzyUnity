using System;

namespace PureGameEnv
{
    internal static class Program
    {
        public static bool isWorking { get; set; } = true;

        private static void Main(string[] args)
        {
            Console.WriteLine("PureGameEnv: smoke build / run.");
            _ = typeof(Xease.GEnv);
            if (args.Length > 0)
            {
                Console.WriteLine(string.Join(" ", args));
            }

            while (isWorking)
            {
                
            }
        }
    }
}
