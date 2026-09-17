using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class Dialog_FriendlyScoutNegotiation : Dialog_NodeTree
    {
        private const float BaseLieSuccessChance = 0.35f;
        private const float SocialSkillLieFactor = 0.035f;
        private const int TruthGoodwillBonus = 10;
        private const int LieCaughtGoodwillPenalty = -40;

        public Dialog_FriendlyScoutNegotiation(Pawn negotiator, Pawn scout, Faction faction, LordJob_FriendlyScouts job)
            : base(MakeNode(negotiator, scout, faction, job))
        {
            closeOnCancel = true;
        }

        private static DiaNode MakeNode(Pawn negotiator, Pawn scout, Faction faction, LordJob_FriendlyScouts job)
        {
            var node = new DiaNode("SS_FriendlyScoutsDialogIntro".Translate(faction.Name, scout.LabelShort));

            var truthOption = new DiaOption("SS_ProvideTruth".Translate())
            {
                action = () =>
                {
                    job.negotiated = true;
                    job.CleanupMarker();
                    job.lord.ReceiveMemo("ScoutsNegotiated");
                    State.ApplyDebt(faction, TruthGoodwillBonus);
                    Messages.Message("SS_ScoutsTruthProvided".Translate(faction.Name), MessageTypeDefOf.NeutralEvent);
                },
                resolveTree = true
            };
            node.options.Add(truthOption);

            var socialSkill = negotiator.skills.GetSkill(SkillDefOf.Social).Level;
            var lieSuccessChance = Mathf.Clamp01(BaseLieSuccessChance + socialSkill * SocialSkillLieFactor);
            var lieOption = new DiaOption("SS_LieToScouts".Translate(lieSuccessChance.ToStringPercent()))
            {
                action = () =>
                {
                    job.negotiated = true;
                    job.CleanupMarker();
                    job.lord.ReceiveMemo("ScoutsNegotiated");
                    if (Rand.Chance(lieSuccessChance))
                    {
                        State.ClearDebt(faction);
                        State.ApplyDebt(faction, TruthGoodwillBonus);
                        Messages.Message("SS_ScoutsLieSuccess".Translate(faction.Name), MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        State.ApplyDebt(faction, LieCaughtGoodwillPenalty);
                        Messages.Message("SS_ScoutsLieCaught".Translate(faction.Name), MessageTypeDefOf.NegativeEvent);
                    }
                },
                resolveTree = true
            };
            node.options.Add(lieOption);

            var backOption = new DiaOption("GoBack".Translate())
            {
                resolveTree = true
            };
            node.options.Add(backOption);
            return node;
        }
    }
}
