using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch]
    public static class TimeSlower_Patches
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeed));
            yield return AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeedShort));
        }

        public static bool Prefix()
        {
            if (State.IsLimitedAwareness && ModCompatibility.IsPerspectiveShiftAuthentic())
            {
                var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
                if (avatar is null || avatar.Map is null) return false;

                if (avatar.InMentalState || avatar.Drafted) return true;

                var map = avatar.Map;
                foreach (var p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p.HostileTo(Faction.OfPlayer) && p.Dead is false && map.IsVisible(p.Position))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
    }
}
