using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(ChoiceLetter), "Option_JumpToLocationAndPostpone", MethodType.Getter)]
    public static class ChoiceLetter_Option_JumpToLocationAndPostpone_Patch
    {
        public static bool Prefix(ChoiceLetter __instance, ref DiaOption __result)
        {
            if (State.IsLimitedAwareness is false) return true;

            var target = __instance.lookTargets.TryGetPrimaryTarget();
            if (target.Map is null || target.Cell.IsValid is false) return true;
            var hostile = __instance.def == LetterDefOf.ThreatBig ||
                            __instance.def == LetterDefOf.ThreatSmall ||
                            __instance.relatedFaction != null && __instance.relatedFaction.HostileTo(Faction.OfPlayer);

            __result = new DiaOption("SS_PingLocation".Translate())
            {
                action = () =>
                {
                    if (Find.CurrentMap != target.Map && ModCompatibility.IsPerspectiveShiftAuthentic() is false)
                    {
                        Current.Game.CurrentMap = target.Map;
                    }
                    State.SpawnPing(target.Cell, target.Map, hostile);
                },
                resolveTree = true
            };
            return false;
        }
    }
}
