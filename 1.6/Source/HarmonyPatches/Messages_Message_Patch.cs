using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Messages), nameof(Messages.Message), new[] { typeof(Message), typeof(bool) })]
    public static class Messages_Message_Patch
    {
        public static bool Prefix(Message msg)
        {
            if (State.IsLimitedAwareness is false) return true;
            var targets = msg.lookTargets?.targets;
            if (targets.NullOrEmpty()) return true;
            Map map = null;
            foreach (var target in targets)
            {
                if (target.IsValid is false || target.IsMapTarget is false || target.Map == null) return true;
                if (map == null) map = target.Map;
                else if (target.Map != map) return true;
            }
            foreach (var target in targets)
            {
                if (map.IsVisible(target.Cell)) return true;
            }
            map.GetComponent<MapComponent_FogOfWar>().QueueMessage(msg);
            State.MuteSlowdown();
            return false;
        }
    }
}
