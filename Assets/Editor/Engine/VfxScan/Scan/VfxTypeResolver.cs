using System;
using System.Collections.Generic;
using UnityEditor;
//using Script.Editor.Extended.Engine.ScanPreferences;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxTypeResolver
    {
        public static VfxEffectType Resolve(
            string[] labels,
            string assetPath,
            IReadOnlyList<VfxTypeBudget> budgets,
            IReadOnlyList<VfxPathMatchRule> pathRules)
        {
            VfxEffectType fromLabel = ResolveByLabels(labels, budgets);
            if (fromLabel != VfxEffectType.Default)
                return fromLabel;
            if (TryMatchPath(assetPath, pathRules, out VfxEffectType pathType))
                return pathType;
            return VfxEffectType.Default;
        }

        public static bool MatchedByPath(string assetPath, IReadOnlyList<VfxPathMatchRule> pathRules)
        {
            return TryMatchPath(assetPath, pathRules, out _);
        }

        public static VfxEffectType ResolveByLabels(string[] labels, IReadOnlyList<VfxTypeBudget> budgets)
        {
            if (labels == null || labels.Length == 0 || budgets == null)
                return VfxEffectType.Default;

            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                if (string.IsNullOrEmpty(label))
                    continue;

                for (int j = 0; j < budgets.Count; j++)
                {
                    VfxTypeBudget budget = budgets[j];
                    if (budget == null || string.IsNullOrEmpty(budget.matchingLabel))
                        continue;
                    if (string.Equals(label, budget.matchingLabel, StringComparison.OrdinalIgnoreCase))
                        return budget.type;
                }
            }

            return VfxEffectType.Default;
        }

        public static bool TryMatchPath(
            string assetPath,
            IReadOnlyList<VfxPathMatchRule> pathRules,
            out VfxEffectType type)
        {
            type = VfxEffectType.Default;
            if (string.IsNullOrEmpty(assetPath) || pathRules == null)
                return false;

            for (int i = 0; i < pathRules.Count; i++)
            {
                VfxPathMatchRule rule = pathRules[i];
                if (rule == null || rule.type == VfxEffectType.Default)
                    continue;
                // if (!ScanPathMatch.Matches(assetPath, rule.pathContains))
                //     continue;
                type = rule.type;
                return true;
            }

            return false;
        }

        public static string[] GetLabels(UnityEngine.Object asset)
        {
            if (asset == null)
                return Array.Empty<string>();
            string[] labels = AssetDatabase.GetLabels(asset);
            return labels ?? Array.Empty<string>();
        }

        public static List<string> GetClassificationChoices(IReadOnlyList<VfxTypeBudget> budgets)
        {
            var names = new List<string>();
            ForEachChoice(budgets, (_, name) => names.Add(name));
            return names;
        }

        public static VfxTypeBudget FindBudgetByChoice(IReadOnlyList<VfxTypeBudget> budgets, string choice)
        {
            if (string.IsNullOrEmpty(choice))
                return null;

            VfxTypeBudget found = null;
            ForEachChoice(budgets, (budget, name) =>
            {
                if (found == null && string.Equals(name, choice, StringComparison.Ordinal))
                    found = budget;
            });
            return found;
        }

        public static string GetChoiceName(VfxEffectType type, IReadOnlyList<VfxTypeBudget> budgets)
        {
            string found = string.Empty;
            ForEachChoice(budgets, (budget, name) =>
            {
                if (string.IsNullOrEmpty(found) && budget.type == type)
                    found = name;
            });
            if (!string.IsNullOrEmpty(found))
                return found;
            return GetDisplayName(VfxScanBuiltinBudgets.Get(budgets, type));
        }

        public static bool SetClassification(UnityEngine.Object asset, VfxTypeBudget selected, IReadOnlyList<VfxTypeBudget> budgets)
        {
            if (asset == null)
                return false;

            string[] current = GetLabels(asset);
            var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (budgets != null)
            {
                for (int i = 0; i < budgets.Count; i++)
                {
                    VfxTypeBudget budget = budgets[i];
                    if (budget == null || string.IsNullOrEmpty(budget.matchingLabel))
                        continue;
                    classification.Add(budget.matchingLabel);
                }
            }

            var kept = new List<string>();
            for (int i = 0; i < current.Length; i++)
            {
                string label = current[i];
                if (string.IsNullOrEmpty(label) || classification.Contains(label))
                    continue;
                kept.Add(label);
            }

            if (selected != null && !string.IsNullOrEmpty(selected.matchingLabel))
                kept.Add(selected.matchingLabel);

            AssetDatabase.SetLabels(asset, kept.ToArray());
            return true;
        }

        static void ForEachChoice(IReadOnlyList<VfxTypeBudget> budgets, Action<VfxTypeBudget, string> callback)
        {
            if (budgets == null || callback == null)
                return;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < budgets.Count; i++)
            {
                VfxTypeBudget budget = budgets[i];
                if (budget == null)
                    continue;

                string name = GetDisplayName(budget);
                if (!seen.Add(name))
                    name = name + " (" + (string.IsNullOrEmpty(budget.matchingLabel) ? budget.type.ToString() : budget.matchingLabel) + ")";
                callback(budget, name);
            }
        }

        static string GetDisplayName(VfxTypeBudget budget)
        {
            if (budget == null)
                return string.Empty;
            return string.IsNullOrEmpty(budget.displayName) ? budget.type.ToString() : budget.displayName;
        }
    }
}
