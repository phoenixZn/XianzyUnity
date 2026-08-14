namespace Xease.CoreGame.Debug
{
    public partial class SysDebugDemo
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 抽样校验 NextFloat ∈ [0,1) 与 RandInt 在 int.MaxValue 闭区间上不溢出。
        private void TestRandomRange()
        {
            const int sampleCount = 100000;
            var gen = new RandomGeneratorPCG(1);
            var svc = new RandomServicePCG(1);

            float fMin = float.MaxValue;
            float fMax = float.MinValue;
            bool floatOk = true;
            for (int i = 0; i < sampleCount; i++)
            {
                float f = gen.NextFloat();
                if (f < 0f || f >= 1f)
                    floatOk = false;
                if (f < fMin)
                    fMin = f;
                if (f > fMax)
                    fMax = f;
            }

            int iMin = int.MaxValue;
            int iMax = int.MinValue;
            bool intInRange = true;
            bool seenNonZero = false;
            for (int i = 0; i < sampleCount; i++)
            {
                int v = svc.RandInt(0, int.MaxValue);
                if (v < 0)
                    intInRange = false;
                if (v != 0)
                    seenNonZero = true;
                if (v < iMin)
                    iMin = v;
                if (v > iMax)
                    iMax = v;
            }

            bool pointOk = svc.RandInt(int.MaxValue, int.MaxValue) == int.MaxValue
                && svc.RandInt(3, 3) == 3;

            if (!floatOk)
                G.LogError($"TestRandomRange NextFloat out of [0,1): min={fMin}, max={fMax}");
            else
                G.Log($"TestRandomRange NextFloat ok, min={fMin}, max={fMax}");

            if (!intInRange || !seenNonZero)
                G.LogError($"TestRandomRange RandInt(0, MaxValue) fail: inRange={intInRange}, seenNonZero={seenNonZero}, min={iMin}, max={iMax}");
            else
                G.Log($"TestRandomRange RandInt(0, MaxValue) ok, min={iMin}, max={iMax}");

            if (!pointOk)
                G.LogError("TestRandomRange RandInt degenerate range fail");
            else
                G.Log("TestRandomRange RandInt degenerate range ok");
        }
    }
}
