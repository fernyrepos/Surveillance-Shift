using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace SurveillanceShift
{
    public class Dialog_FriendlyScoutNegotiation : Dialog_NodeTree
    {
        private const float BaseLieSuccessChance = 0.35f;
        private const float SocialSkillLieFactor = 0.03f;

        public Dialog_FriendlyScoutNegotiation(Pawn colonist, Pawn negotiator, Faction faction, LordJob_FriendlyScouts job)
            : base(MakeNode(colonist, negotiator, faction, job))
        {
            closeOnCancel = true;
        }

        public static void Open(Pawn colonist, Pawn negotiator)
        {
            var job = (LordJob_FriendlyScouts)negotiator.GetLord().LordJob;
            Find.WindowStack.Add(new Dialog_FriendlyScoutNegotiation(colonist, negotiator, negotiator.Faction, job));
        }

        private static DiaNode MakeNode(Pawn colonist, Pawn negotiator, Faction faction, LordJob_FriendlyScouts job)
        {
            var node = new DiaNode("SS_FriendlyScoutsDialogIntro".Translate(faction.Name, negotiator.LabelShort));

            var truthOption = new DiaOption("SS_ProvideTruth".Translate())
            {
                action = () =>
                {
                    job.negotiated = true;
                    job.lord.ReceiveMemo("ScoutsNegotiated");
                    State.ApplyDebt(faction, SurveillanceShiftMod.settings.relationsTruthBoost);
                    Messages.Message("SS_ScoutsTruthProvided".Translate(faction.Name), MessageTypeDefOf.NeutralEvent);
                },
                resolveTree = true
            };
            node.options.Add(truthOption);

            var socialSkill = colonist.skills.GetSkill(SkillDefOf.Social).Level;
            var lieSuccessChance = Mathf.Clamp01(BaseLieSuccessChance + socialSkill * SocialSkillLieFactor);
            var lieOption = new DiaOption("SS_LieToScouts".Translate(lieSuccessChance.ToStringPercent()))
            {
                action = () =>
                {
                    job.negotiated = true;
                    job.lord.ReceiveMemo("ScoutsNegotiated");
                    if (Rand.Chance(lieSuccessChance))
                    {
                        State.ClearDebt(faction);
                        State.ApplyDebt(faction, SurveillanceShiftMod.settings.relationsTruthBoost);
                        Messages.Message("SS_ScoutsLieSuccess".Translate(faction.Name), MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        State.ApplyDebt(faction, -SurveillanceShiftMod.settings.relationsLiePenalty);
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
