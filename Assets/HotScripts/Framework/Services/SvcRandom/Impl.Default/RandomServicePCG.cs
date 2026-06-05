namespace Xease
{
    public class RandomServicePCG : IRandomService
    {
        private RandomGeneratorPCG _generator;

        public RandomServicePCG(int seed)
        {
            _generator = new RandomGeneratorPCG((ulong)seed);
        }

        public void Shutdown()
        {
        }

        public void ResetSeed(int seed)
        {
            _generator = new RandomGeneratorPCG((ulong)seed);
        }

        public int RandInt(int min_value, int max_value)
        {
            return _generator.Next(min_value, max_value + 1);
        }

        public float RandFloat(float minValue, float maxValue)
        {
            return _generator.NextFloat(minValue, maxValue);
        }

        public bool RandBool()
        {
            return _generator.NextBool();
        }
    }
}
