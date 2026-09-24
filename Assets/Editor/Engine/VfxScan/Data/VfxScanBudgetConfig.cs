using System.Collections.Generic;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    [CreateAssetMenu(fileName = "VfxScanBudgetConfig", menuName = "KTools/Vfx Scan Budget Config")]
    public class VfxScanBudgetConfig : ScriptableObject
    {
        public List<VfxTypeBudget> budgets = new List<VfxTypeBudget>();

        void Reset()
        {
            budgets = VfxScanBuiltinBudgets.CreateDefaultList();
        }

        void OnEnable()
        {
            if (budgets == null || budgets.Count == 0)
                budgets = VfxScanBuiltinBudgets.CreateDefaultList();
        }

        public VfxTypeBudget GetBudget(VfxEffectType type)
        {
            return VfxScanBuiltinBudgets.Get(budgets, type);
        }
    }
}
