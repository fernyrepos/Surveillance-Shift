using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public static class GuardMenus
    {
        public static void OpenDutyMenu(Pawn pawn)
        {
            var map = pawn.Map;
            if (map is null) return;
            var guarding = map.GetComponent<MapComponent_Guarding>();
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("SS_DutyNone".Translate(), () => GuardUtility.Unassign(pawn))
            };

            foreach (var post in guarding.Posts.OrderBy(p => p.Position.DistanceToSquared(pawn.Position)))
            {
                var target = post;
                var others = target.Occupants.Where(p => p != pawn).Select(p => p.LabelShort).ToList();
                var label = others.Count == 0 ? "SS_MenuPost".Translate(target.RenamableLabel).ToString() : "SS_MenuPostTaken".Translate(target.RenamableLabel, others.ToCommaList()).ToString();
                options.Add(new FloatMenuOption(label, () => GuardUtility.AssignPost(pawn, target), GuardTextures.Post, UnityEngine.Color.white));
            }

            foreach (var route in guarding.routes.Where(r => r.waypoints.Count > 0))
            {
                var target = route;
                options.Add(new FloatMenuOption("SS_MenuRoute".Translate(target.RenamableLabel, target.waypoints.Count), () => GuardUtility.AssignRoute(pawn, target), GuardTextures.Route, target.Color));
            }

            foreach (var other in map.mapPawns.FreeColonistsSpawned.Where(p => p != pawn))
            {
                var protectee = other;
                options.Add(new FloatMenuOption("SS_MenuBodyguard".Translate(protectee.LabelShortCap), () => GuardUtility.AssignBodyguard(pawn, protectee), GuardTextures.Bodyguard, UnityEngine.Color.white));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public static void OpenStanceMenu(Pawn pawn)
        {
            var options = new List<FloatMenuOption>();
            foreach (var stance in new[] { GuardStance.Hold, GuardStance.Defend, GuardStance.Pursue })
            {
                var chosen = stance;
                options.Add(new FloatMenuOption(StanceLabel(chosen), () => GuardUtility.SetStance(pawn, chosen)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public static string StanceLabel(GuardStance stance) => ("SS_Stance" + stance).Translate();

        public static string StanceDescription(GuardStance stance) => ("SS_Stance" + stance + "Desc").Translate(GuardUtility.Leash(stance).ToString("0"));
    }
}
