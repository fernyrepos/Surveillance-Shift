using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [DefOf]
    public static class DefsOf
    {
        public static ThingDef SS_Mote_PingHostile;
        public static ThingDef SS_Mote_PingFriendly;
        public static SoundDef SS_Sound_PingHostile;
        public static SoundDef SS_Sound_PingFriendly;

        static DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefsOf));
        }
    }
}
