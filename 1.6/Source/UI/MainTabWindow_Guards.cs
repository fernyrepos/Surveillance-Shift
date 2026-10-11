using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class MainTabWindow_Guards : MainTabWindow_PawnTable
    {
        public override PawnTableDef PawnTableDef => DefsOf.SS_GuardsTable;

        public override IEnumerable<Pawn> Pawns
        {
            get
            {
                var map = Find.CurrentMap;
                if (map is null) return Enumerable.Empty<Pawn>();
                return GuardUtility.EligibleGuards(map).OrderBy(p => p.RaceProps.Humanlike ? 0 : p.RaceProps.IsMechanoid ? 1 : 2).ThenBy(p => p.LabelShort);
            }
        }

        public override float ExtraBottomSpace => 32f;

        public override void DoWindowContents(Rect rect)
        {
            base.DoWindowContents(rect);
            var map = Find.CurrentMap;
            if (map is null) return;
            var guarding = map.GetComponent<MapComponent_Guarding>();
            var posts = guarding.showPosts;
            var waypoints = guarding.showWaypoints;
            Text.Font = GameFont.Small;
            Widgets.CheckboxLabeled(new Rect(rect.x + 4f, rect.yMax - 28f, 190f, 24f), "SS_ShowGuardPosts".Translate(), ref posts);
            Widgets.CheckboxLabeled(new Rect(rect.x + 214f, rect.yMax - 28f, 230f, 24f), "SS_ShowPatrolWaypoints".Translate(), ref waypoints);
            if (posts != guarding.showPosts || waypoints != guarding.showWaypoints) guarding.SetVisibility(posts, waypoints);
        }

        public override void PostOpen()
        {
            base.PostOpen();
            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
        }
    }
}
