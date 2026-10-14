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

    public interface IBehaviorTabTarget
    {
        GuardStance Stance { get; set; }
    }

    public class ITab_Behavior : ITab
    {
        public ITab_Behavior()
        {
            labelKey = "SS_TabBehavior";
        }

        public override bool IsVisible => SelThing is IBehaviorTabTarget && SelThing.Faction == Faction.OfPlayer;

        public override void FillTab()
        {
        }

        public static void OpenMenu(IBehaviorTabTarget target)
        {
            var options = new List<FloatMenuOption>();
            foreach (var stance in new[] { GuardStance.Hold, GuardStance.Defend, GuardStance.Pursue })
            {
                var chosen = stance;
                options.Add(new FloatMenuOption(GuardMenus.StanceLabel(chosen), () => target.Stance = chosen) { tooltip = new TipSignal(GuardMenus.StanceDescription(chosen)) });
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
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
            if (tab is ITab_Priority && Find.Selector.SingleSelectedThing is IPriorityTabTarget priorityTarget)
            {
                ITab_Priority.OpenMenu(priorityTarget);
                return false;
            }
            if (tab is ITab_Behavior && Find.Selector.SingleSelectedThing is IBehaviorTabTarget behaviorTarget)
            {
                ITab_Behavior.OpenMenu(behaviorTarget);
                return false;
            }
            return true;
        }
    }
}
