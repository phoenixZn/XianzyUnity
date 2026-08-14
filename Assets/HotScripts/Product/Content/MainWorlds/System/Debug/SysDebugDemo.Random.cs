namespace Xease.CoreGame.Debug
{
    public partial class SysDebugDemo
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 抽样校验 NextFloat ∈ [0,1) 与 RandInt 闭区间（含负值、跨零、全 int）不越界。
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

            // 闭区间抽样：越界则失败，并记录观测 min/max。
            bool SampleInclusive(int lo, int hi, out int oMin, out int oMax)
            {
                oMin = int.MaxValue;
                oMax = int.MinValue;
                bool ok = true;
                for (int i = 0; i < sampleCount; i++)
                {
                    int v = svc.RandInt(lo, hi);
                    if (v < lo || v > hi)
                        ok = false;
                    if (v < oMin)
                        oMin = v;
                    if (v > oMax)
                        oMax = v;
                }
                return ok;
            }

            bool intInRange = SampleInclusive(0, int.MaxValue, out int iMin, out int iMax);
            bool seenNonZero = iMin != 0 || iMax != 0;

            bool mixOk = SampleInclusive(-10, 10, out int mixMin, out int mixMax);
            bool mixHasNeg = mixMin < 0;
            bool mixHasPos = mixMax > 0;

            bool negOk = SampleInclusive(-5, -1, out int negMin, out int negMax);

            bool fullOk = SampleInclusive(int.MinValue, int.MaxValue, out int fullMin, out int fullMax);
            bool fullHasNeg = fullMin < 0;
            bool fullHasPos = fullMax > 0;

            bool pointOk = svc.RandInt(int.MaxValue, int.MaxValue) == int.MaxValue
                && svc.RandInt(3, 3) == 3
                && svc.RandInt(-3, -3) == -3
                && svc.RandInt(int.MinValue, int.MinValue) == int.MinValue;

            if (!floatOk)
                G.LogError($"TestRandomRange NextFloat out of [0,1): min={fMin}, max={fMax}");
            else
                G.Log($"TestRandomRange NextFloat ok, min={fMin}, max={fMax}");

            if (!intInRange || !seenNonZero)
                G.LogError($"TestRandomRange RandInt(0, MaxValue) fail: inRange={intInRange}, seenNonZero={seenNonZero}, min={iMin}, max={iMax}");
            else
                G.Log($"TestRandomRange RandInt(0, MaxValue) ok, min={iMin}, max={iMax}");

            if (!mixOk || !mixHasNeg || !mixHasPos)
                G.LogError($"TestRandomRange RandInt(-10, 10) fail: inRange={mixOk}, hasNeg={mixHasNeg}, hasPos={mixHasPos}, min={mixMin}, max={mixMax}");
            else
                G.Log($"TestRandomRange RandInt(-10, 10) ok, min={mixMin}, max={mixMax}");

            if (!negOk)
                G.LogError($"TestRandomRange RandInt(-5, -1) fail: min={negMin}, max={negMax}");
            else
                G.Log($"TestRandomRange RandInt(-5, -1) ok, min={negMin}, max={negMax}");

            if (!fullOk || !fullHasNeg || !fullHasPos)
                G.LogError($"TestRandomRange RandInt(MinValue, MaxValue) fail: inRange={fullOk}, hasNeg={fullHasNeg}, hasPos={fullHasPos}, min={fullMin}, max={fullMax}");
            else
                G.Log($"TestRandomRange RandInt(MinValue, MaxValue) ok, min={fullMin}, max={fullMax}");

            if (!pointOk)
                G.LogError("TestRandomRange RandInt degenerate range fail");
            else
                G.Log("TestRandomRange RandInt degenerate range ok");
        }
    }
}
