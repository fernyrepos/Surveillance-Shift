using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(CameraJumper), "TrySelectInternal", new[] { typeof(Thing) })]
    public static class CameraJumper_TrySelectInternal_Patch
    {
        public static bool Prefix(Thing thing)
        {
            if (SurveillanceShiftMod.settings.limitViewingMaps is false || State.IsLimitedAwareness is false || Current.ProgramState != ProgramState.Playing || WorldComponent_GravshipController.CutsceneInProgress || ModCompatibility.IsPerspectiveShiftAuthentic() is false && thing.Faction == Faction.OfPlayer || thing == ModCompatibility.GetPerspectiveShiftAvatar()) return true;

            if (thing.Map.IsVisible(thing.Position) is false)
            {
                State.SpawnPing(thing.Position, thing.Map, thing.HostileTo(Faction.OfPlayer));
                return false;
            }
            return true;
        }
    }
}
