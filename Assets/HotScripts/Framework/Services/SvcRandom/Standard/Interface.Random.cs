namespace Xease
{

    //////////////////////////////////////////////////////////////////////////
    
    public interface IRandomService : IService
    {
        void ResetSeed(int seed);
        int RandInt(int min_value, int max_value);
        float RandFloat(float minValue, float maxValue);
        bool RandBool();
    }
}
