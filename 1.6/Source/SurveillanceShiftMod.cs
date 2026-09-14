using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    public class SurveillanceShiftMod : Mod
    {
        public SurveillanceShiftMod(ModContentPack pack) : base(pack)
        {
            new Harmony("ferny.SurveillanceShift").PatchAll();
        }
    }
}
