using System.Collections.Generic;
using System.Linq;
using Entitas;


namespace Xease.CoreGame
{
    public class UnitTestSystem_Tag : ECWorldSystem, IInitializeSystem
    {
        public UnitTestSystem_Tag(ECWorlds worlds) : base(worlds)
        {
        }

        public void Initialize()
        {
            UnitTest_Tags();
        }

        void UnitTest_Tags()
        {
            G.Log("UnitTest_Tags begin");

            const uint tagsBit0And31 = (1u << 31) | 1u;

            UnitTest_Tags_Static(0, 0, new int[] { }, new int[] { });
            UnitTest_Tags_Static(0b1010, 2, new[] { 2, 8 }, new[] { 1, 3 });
            UnitTest_Tags_Static(tagsBit0And31, 2, new[] { 1, unchecked((int)0x80000000u) }, new[] { 0, 31 });

            UnitTest_Tags_Entity(1, 1, new int[] { 1 }, new int[] { 0 });
            UnitTest_Tags_Entity(0b1010, 2, new[] { 2, 8 }, new[] { 1, 3 });
            UnitTest_Tags_Entity(tagsBit0And31, 2, new[] { 1, unchecked((int)0x80000000u) }, new[] { 0, 31 });

            G.Log("UnitTest_Tags end");
        }

        void UnitTest_Tags_Static(uint tags, int expectedPopCount, int[] expectedKeys, int[] expectedIndices)
        {
            var prefix = $"UnitTest_Tags_Static tags=0b{System.Convert.ToString(tags, 2)}";

            AssertEqual(expectedPopCount, TagComponent.PopCount(tags), $"{prefix} PopCount");
            AssertArrayEqual(expectedKeys, TagComponent.GetTagKeys(tags), $"{prefix} GetTagKeys");
            AssertArrayEqual(expectedIndices, TagComponent.GetTagIndexArray(tags), $"{prefix} GetTagIndexArray");
        }

        void UnitTest_Tags_Entity(uint tags, int expectedPopCount, int[] expectedKeys, int[] expectedIndices)
        {
            var prefix = $"UnitTest_Tags_Entity tags=0b{System.Convert.ToString(tags, 2)}";

            var entity = _logicWorld.CreateEntity();
            entity.AddComTags(tags);
            var comTag = entity.comTag;

            AssertEqual(expectedPopCount, TagComponent.PopCount(comTag.Tags), $"{prefix} PopCount");
            AssertArrayEqual(expectedKeys, comTag.GetTagKeys(), $"{prefix} GetTagKeys");
            AssertArrayEqual(expectedIndices, comTag.GetTagIndexArray(), $"{prefix} GetTagIndexArray");

            foreach (var tagKey in expectedKeys)
            {
                AssertEntityInComTagSet(entity, tagKey, true, $"{prefix} GetEntitiesWithComTag key={tagKey}");
            }

            var negativeKey = GetNegativeTagKey(expectedKeys);
            if (negativeKey != 0)
            {
                AssertEntityInComTagSet(entity, negativeKey, false, $"{prefix} GetEntitiesWithComTag negative key={negativeKey}");
            }

            entity.Destroy();
        }

        static int GetNegativeTagKey(int[] expectedKeys)
        {
            var key = 1;
            while (expectedKeys != null && expectedKeys.Contains(key))
                key <<= 1;
            return key;
        }

        void AssertEntityInComTagSet(LogicEntity entity, int tagKey, bool shouldContain, string name)
        {
            var entities = _logicWorld.GetEntitiesWithComTag(tagKey);
            if (entities == null)
            {
                G.LogError($"UnitTest FAIL: {name} GetEntitiesWithComTag returned null, ensure SysInitializeBasePack runs first");
                return;
            }

            var contains = entities.Contains(entity);
            if (contains != shouldContain)
            {
                G.LogError($"UnitTest FAIL: {name} expected contain={shouldContain} actual={contains}");
                return;
            }

            G.Log($"UnitTest PASS: {name}");
        }

        static void AssertEqual(int expected, int actual, string name)
        {
            if (expected != actual)
            {
                G.LogError($"UnitTest FAIL: {name} expected={expected} actual={actual}");
                return;
            }

            G.Log($"UnitTest PASS: {name}");
        }

        static void AssertArrayEqual(int[] expected, int[] actual, string name)
        {
            if (expected == null || actual == null)
            {
                if (expected == actual)
                {
                    G.Log($"UnitTest PASS: {name}");
                    return;
                }

                G.LogError($"UnitTest FAIL: {name} expected=null actual={(actual == null ? "null" : $"[{string.Join(", ", actual)}]")}");
                return;
            }

            if (!expected.SequenceEqual(actual))
            {
                G.LogError($"UnitTest FAIL: {name} expected=[{string.Join(", ", expected)}] actual=[{string.Join(", ", actual)}]");
                return;
            }

            G.Log($"UnitTest PASS: {name}");
        }
    }
}
