using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Map), nameof(Map.DrawMapClippers), MethodType.Getter)]
    public static class Map_DrawMapClippers_Patch
    {
        public static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }
}
