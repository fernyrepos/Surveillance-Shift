using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public interface IPriorityTabTarget
    {
        StoragePriority Priority { get; set; }
    }

    public class ITab_Priority : ITab
    {
        public ITab_Priority()
        {
            labelKey = "SS_TabPriority";
        }

        public override bool IsVisible => SelThing is IPriorityTabTarget && SelThing.Faction == Faction.OfPlayer;

        public override void FillTab()
        {
        }

        public static void OpenMenu(IPriorityTabTarget target)
        {
            var options = new List<FloatMenuOption>();
            foreach (StoragePriority priority in System.Enum.GetValues(typeof(StoragePriority)))
            {
                if (priority == StoragePriority.Unstored) continue;
                var chosen = priority;
                options.Add(new FloatMenuOption(chosen.Label().CapitalizeFirst(), () => target.Priority = chosen));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    [HarmonyPatch]
    public static class InspectPaneUtility_ToggleTab_Patch
    {
        public static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(typeof(InspectPaneUtility), "ToggleTab");

        public static bool Prefix(InspectTabBase tab)
        {
            if (tab is not ITab_Priority || Find.Selector.SingleSelectedThing is not IPriorityTabTarget target) return true;
            ITab_Priority.OpenMenu(target);
            return false;
        }
    }
}
