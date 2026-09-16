using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(LetterStack), nameof(LetterStack.ReceiveLetter), new[] { typeof(Letter), typeof(string), typeof(int), typeof(bool) })]
    public static class LetterStack_ReceiveLetter_Patch
    {
        public static bool Prefix(Letter let)
        {
            if (State.IsLimitedAwareness is false) return true;
            var targets = let.lookTargets?.targets;
            if (targets.NullOrEmpty()) return true;
            Map map = null;
            foreach (var target in targets)
            {
                var resolved = State.ResolveTarget(target);
                if (resolved.IsValid is false || resolved.IsMapTarget is false || resolved.Map is null) return true;
                if (map == null) map = resolved.Map;
                else if (resolved.Map != map) return true;
            }
            foreach (var target in targets)
            {
                if (State.IsTargetFogged(target) is false) return true;
            }
            map.GetComponent<MapComponent_FogOfWar>().QueueLetter(let);
            return false;
        }
    }
}
