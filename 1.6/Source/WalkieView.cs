using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class WalkieView
    {
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("Things/Item/Special/WalkieTalkie");

        private static Pawn target;

        public static Pawn Target => target;

        public static bool Active => target != null;

        public static bool HasWalkie(Pawn pawn) => pawn.inventory != null && pawn.inventory.innerContainer.Contains(DefsOf.SS_WalkieTalkie);

        public static IEnumerable<Pawn> Contacts(Pawn avatar)
        {
            if (avatar.Map is null) yield break;
            foreach (var colonist in avatar.Map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist != avatar && colonist.Dead is false && HasWalkie(colonist)) yield return colonist;
            }
        }

        public static Gizmo GetGizmo(Pawn pawn)
        {
            if (ModCompatibility.PerspectiveShiftAvailable is false || pawn.Spawned is false) return null;
            if (pawn != ModCompatibility.GetPerspectiveShiftAvatar()) return null;
            var any = false;
            foreach (var _ in Contacts(pawn))
            {
                any = true;
                break;
            }
            var gizmo = new Command_Action
            {
                defaultLabel = "SS_UseWalkie".Translate(),
                defaultDesc = "SS_UseWalkieDesc".Translate(),
                icon = Icon,
                action = () => OpenMenu(pawn)
            };
            if (any is false) gizmo.Disable("SS_NoWalkieContacts".Translate());
            return gizmo;
        }

        private static void OpenMenu(Pawn avatar)
        {
            var options = new List<FloatMenuOption>();
            foreach (var contact in Contacts(avatar))
            {
                var chosen = contact;
                options.Add(new FloatMenuOption("SS_GiveOrdersTo".Translate(chosen.LabelShortCap), () => Begin(avatar, chosen), chosen, Color.white));
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("SS_NoWalkieContacts".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void Begin(Pawn avatar, Pawn contact)
        {
            if (contact.Spawned is false || HasWalkie(contact) is false) return;
            SoundDefOf.Click.PlayOneShotOnCamera();
            State.ClearActiveFeed();
            ScopeView.Clear();
            target = contact;
            ModCompatibility.SetPerspectiveShiftCameraLock(contact.DrawPos);
        }

        public static void End()
        {
            if (target is null) return;
            target = null;
            ModCompatibility.ClearPerspectiveShiftCameraLock();
        }

        private static bool StillValid()
        {
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (avatar is null || avatar.Spawned is false || avatar.Dead || HasWalkie(avatar) is false) return false;
            if (target.Spawned is false || target.Dead || target.Map != avatar.Map || HasWalkie(target) is false) return false;
            return ModCompatibility.HasPerspectiveShiftCameraLock();
        }

        public static void Update()
        {
            if (target is null) return;
            if (StillValid() is false)
            {
                End();
                return;
            }
            ModCompatibility.SetPerspectiveShiftCameraLock(target.DrawPos);
        }

        public static void ApplyFieldOfView(Map map, bool[] visibleCells)
        {
            if (target is null || target.Spawned is false || target.Map != map) return;
            ShadowCaster.ComputeFieldOfView(target.Position, target.SightRadius(), map, visibleCells);
        }

        public static void DrawStopButton()
        {
            const float btnW = 220f;
            const float panelH = 70f;
            var panelRect = new Rect(UI.screenWidth / 2f - btnW / 2f - 10f, UI.screenHeight - 150f - panelH, btnW + 20f, panelH);
            Widgets.DrawWindowBackground(panelRect);
            if (Widgets.ButtonText(new Rect(panelRect.xMin + 10f, panelRect.yMin + 10f, btnW, 50f), "SS_StopUsingWalkie".Translate()))
            {
                End();
            }
        }
    }
}
