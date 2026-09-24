using System;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    [Serializable]
    public class VfxScanIssue
    {
        public VfxHealthStatus severity;
        public VfxIssueCategory category;
        public string title;
        public string advice;
        public string hierarchyPath;
        public string assetPath;
        public VfxQuickFixKind quickFixKind;
        public int quickFixIntValue;

        public static VfxScanIssue Create(
            VfxHealthStatus severity,
            VfxIssueCategory category,
            string title,
            string advice,
            string hierarchyPath = null,
            string assetPath = null,
            VfxQuickFixKind quickFixKind = VfxQuickFixKind.None,
            int quickFixIntValue = 0)
        {
            return new VfxScanIssue
            {
                severity = severity,
                category = category,
                title = title,
                advice = advice,
                hierarchyPath = hierarchyPath ?? string.Empty,
                assetPath = assetPath ?? string.Empty,
                quickFixKind = quickFixKind,
                quickFixIntValue = quickFixIntValue
            };
        }
    }
}
