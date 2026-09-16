using HarmonyLib;
using RimWorld;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Alert), nameof(Alert.Recalculate))]
    public static class Alert_Recalculate_Patch
    {
        public static bool Prefix(Alert __instance)
        {
            if (State.IsLimitedAwareness is false) return true;
            var report = __instance.GetReport();
            var active = report.active && HasVisibleCulprit(report);
            __instance.cachedActive = active;
            if (active)
            {
                __instance.cachedLabel = __instance.GetLabel();
            }
            return false;
        }

        private static bool HasVisibleCulprit(AlertReport report)
        {
            var checkable = false;
            foreach (var culprit in report.AllCulprits)
            {
                var resolved = State.ResolveTarget(culprit);
                if (resolved.IsMapTarget is false || resolved.Map is null) continue;
                checkable = true;
                if (State.IsTargetFogged(resolved) is false) return true;
            }
            return checkable is false;
        }
    }
}
