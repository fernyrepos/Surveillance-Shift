using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class PawnColumnWorker_GuardDuty : PawnColumnWorker
    {
        public override int GetMinWidth(PawnTable table) => 190;
        public override int GetOptimalWidth(PawnTable table) => 230;
        public override int GetMaxWidth(PawnTable table) => 300;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.CanGuard() is false) return;
            var button = rect.ContractedBy(2f);
            var active = pawn.GetGuardAssignment().IsActive;
            if (Widgets.ButtonText(button, GuardUtility.DutyLabel(pawn).Truncate(button.width - 8f), true, true, active ? Color.white : Color.gray))
            {
                GuardMenus.OpenDutyMenu(pawn);
            }
            TooltipHandler.TipRegion(button, "SS_DutyColumnTip".Translate());
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return string.CompareOrdinal(GuardUtility.DutyLabel(a), GuardUtility.DutyLabel(b));
        }
    }

    public class PawnColumnWorker_GuardThreatReport : PawnColumnWorker
    {
        public override int GetMinWidth(PawnTable table) => 150;
        public override int GetOptimalWidth(PawnTable table) => 170;
        public override int GetMaxWidth(PawnTable table) => 210;

        private static bool Applies(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike) return pawn.Faction == Faction.OfPlayer && pawn.IsPrisoner is false;
            return pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer;
        }

        private static string Label(Pawn pawn)
        {
            return pawn.RaceProps.Humanlike ? InspectStripButtons.ThreatLabel(pawn) : InspectStripButtons.AlertLabel(pawn);
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (Applies(pawn) is false) return;
            var button = rect.ContractedBy(2f);
            var label = Label(pawn);
            if (Widgets.ButtonText(button, label.Truncate(button.width - 8f)))
            {
                if (pawn.RaceProps.Humanlike) InspectStripButtons.OpenThreatMenu(pawn);
                else InspectStripButtons.OpenAlertMenu(pawn);
            }
            TooltipHandler.TipRegion(button, "SS_ThreatReportReactionDesc".Translate(label));
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return string.CompareOrdinal(Applies(a) ? Label(a) : "", Applies(b) ? Label(b) : "");
        }
    }

    public class PawnColumnWorker_GuardStance : PawnColumnWorker
    {
        public override int GetMinWidth(PawnTable table) => 100;
        public override int GetOptimalWidth(PawnTable table) => 110;
        public override int GetMaxWidth(PawnTable table) => 140;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            var assignment = pawn.GetGuardAssignment();
            var button = rect.ContractedBy(2f);
            if (Widgets.ButtonText(button, GuardMenus.StanceLabel(assignment.stance)))
            {
                GuardMenus.OpenStanceMenu(pawn);
            }
            TooltipHandler.TipRegion(button, GuardMenus.StanceDescription(assignment.stance));
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return a.GetGuardAssignment().stance.CompareTo(b.GetGuardAssignment().stance);
        }
    }

    public class PawnColumnWorker_GuardWeapon : PawnColumnWorker
    {
        public override int GetMinWidth(PawnTable table) => 150;
        public override int GetOptimalWidth(PawnTable table) => 170;
        public override int GetMaxWidth(PawnTable table) => 220;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            var weapon = pawn.equipment?.Primary;
            var cell = rect.ContractedBy(2f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            if (weapon is null)
            {
                var text = pawn.RaceProps.Humanlike ? "SS_Unarmed".Translate().ToString() : "SS_NaturalWeapons".Translate().ToString();
                Widgets.Label(new Rect(cell.x + 4f, cell.y, cell.width - 4f, cell.height), text.Truncate(cell.width - 8f));
            }
            else
            {
                var icon = new Rect(cell.x, cell.y, cell.height, cell.height);
                Widgets.ThingIcon(icon, weapon);
                Widgets.Label(new Rect(icon.xMax + 4f, cell.y, cell.width - icon.width - 4f, cell.height), weapon.LabelCap.Truncate(cell.width - icon.width - 8f));
                TooltipHandler.TipRegion(cell, weapon.LabelCap);
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return (a.equipment?.Primary is null).CompareTo(b.equipment?.Primary is null);
        }
    }

    public class PawnColumnWorker_GuardSkills : PawnColumnWorker
    {
        public override int GetMinWidth(PawnTable table) => 80;
        public override int GetOptimalWidth(PawnTable table) => 90;
        public override int GetMaxWidth(PawnTable table) => 110;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            if (pawn.skills is null)
            {
                Widgets.Label(rect, "—");
            }
            else
            {
                var shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                var melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                Widgets.Label(rect, "SS_GuardSkills".Translate(Level(shooting), Level(melee)));
                TooltipHandler.TipRegion(rect, "SS_GuardSkillsTip".Translate());
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string Level(SkillRecord record) => record.TotallyDisabled ? "—" : record.Level.ToString();

        public override int Compare(Pawn a, Pawn b)
        {
            return Score(a).CompareTo(Score(b));
        }

        private static int Score(Pawn pawn)
        {
            if (pawn.skills is null) return -1;
            return pawn.skills.GetSkill(SkillDefOf.Shooting).Level + pawn.skills.GetSkill(SkillDefOf.Melee).Level;
        }
    }
}
