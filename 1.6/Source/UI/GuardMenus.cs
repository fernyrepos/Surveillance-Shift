using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class GuardMenus
    {
        private static readonly Texture2D HideVisualIcon = ContentFinder<Texture2D>.Get("UI/Guard/HideVisual");

        public static string StanceLabel(GuardStance stance) => ("SS_Stance" + stance).Translate();

        public static string StanceDescription(GuardStance stance) => ("SS_Stance" + stance + "Desc").Translate(GuardUtility.Leash(stance).ToString("0"));

        public static Gizmo HideVisualGizmo(Building_GuardPost post)
        {
            return new Command_HideVisual
            {
                defaultLabel = "SS_HideVisual".Translate(),
                defaultDesc = "SS_HideVisualDesc".Translate(),
                icon = HideVisualIcon,
                isActive = () => post.hideVisual,
                toggleAction = () => post.SetVisualHidden(post.hideVisual is false),
                rightClickOptions = () => VisibilityOptions(post.Map.GetComponent<MapComponent_Guarding>().Posts.Where(p => p.IsMarker).ToList(), "SS_GuardSpots", p => p.hideVisual, (p, hidden) => p.SetVisualHidden(hidden))
            };
        }

        public static Gizmo HideVisualGizmo(Building_PatrolWaypoint waypoint)
        {
            return new Command_HideVisual
            {
                defaultLabel = "SS_HideVisual".Translate(),
                defaultDesc = "SS_HideVisualDesc".Translate(),
                icon = HideVisualIcon,
                isActive = () => waypoint.hideVisual,
                toggleAction = () => waypoint.SetVisualHidden(waypoint.hideVisual is false),
                rightClickOptions = () => VisibilityOptions(waypoint.Map.listerBuildings.allBuildingsColonist.OfType<Building_PatrolWaypoint>().ToList(), "SS_PatrolRoutes", w => w.hideVisual, (w, hidden) => w.SetVisualHidden(hidden))
            };
        }

        private static IEnumerable<FloatMenuOption> VisibilityOptions<T>(List<T> things, string noun, Func<T, bool> isHidden, Action<T, bool> setHidden)
        {
            var label = noun.Translate();
            if (things.Any(t => isHidden(t) is false))
            {
                yield return new FloatMenuOption("SS_HideAll".Translate(label), () => things.ForEach(t => setHidden(t, true)));
            }
            if (things.Any(isHidden))
            {
                yield return new FloatMenuOption("SS_ShowAll".Translate(label), () => things.ForEach(t => setHidden(t, false)));
            }
        }
    }
}
