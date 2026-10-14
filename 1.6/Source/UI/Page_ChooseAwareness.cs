using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public class Page_ChooseAwareness : Page
    {
        private const float CardWidth = 400f;
        private const float Gap = 75f;

        private static Texture2D _limitedAwarenessIcon;
        private static Texture2D LimitedAwarenessIcon =>
            _limitedAwarenessIcon ??= ContentFinder<Texture2D>.Get("MainMenu/LimitedAwareness");

        private static Texture2D _fullAwarenessIcon;
        private static Texture2D FullAwarenessIcon =>
            _fullAwarenessIcon ??= ContentFinder<Texture2D>.Get("MainMenu/FullAwareness");

        public override string PageTitle => "SS_SelectAwarenessStyle".Translate();

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 35f), PageTitle);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            var mainRect = GetMainRect(rect);
            mainRect.yMin += 10f;
            DrawCards(mainRect);

            DoBottomButtons(rect, "Next".Translate());
        }

        private void DrawCards(Rect rect)
        {
            rect.y += 10f;
            rect.height -= 40f;
            var startX = rect.x + (rect.width - (CardWidth * 2f + Gap)) / 2f;
            var startY = rect.y + (rect.height - rect.height) / 2f - 10f;

            var leftRect = new Rect(startX, startY, CardWidth, rect.height);
            var rightRect = new Rect(startX + CardWidth + Gap, startY, CardWidth, rect.height);

            DrawOption(leftRect, "SS_AreaOmniscience".Translate(), "SS_AreaOmniscienceDesc".Translate(), FullAwarenessIcon, State.AwarenessMode == AwarenessMode.AreaOmniscience, () => State.AwarenessMode = AwarenessMode.AreaOmniscience);

            DrawOption(rightRect, "SS_LimitedAwareness".Translate(), "SS_LimitedAwarenessDesc".Translate(), LimitedAwarenessIcon, State.AwarenessMode == AwarenessMode.LimitedAwareness, SelectLimitedAwareness);
        }

        public override void DoNext()
        {
            var fogPage = next as Page_FogStyle;
            if (State.IsLimitedAwareness)
            {
                if (fogPage is null)
                {
                    fogPage = new Page_FogStyle { prev = this, next = next, nextAct = nextAct };
                    if (next != null)
                    {
                        next.prev = fogPage;
                    }
                    next = fogPage;
                    nextAct = null;
                }
            }
            else if (fogPage != null)
            {
                next = fogPage.next;
                nextAct = fogPage.nextAct;
                if (next != null)
                {
                    next.prev = this;
                }
            }
            base.DoNext();
        }

        private void SelectLimitedAwareness()
        {
            if (State.AwarenessMode == AwarenessMode.LimitedAwareness) return;
            State.AwarenessMode = AwarenessMode.LimitedAwareness;
            if (Current.Game is null) return;
            foreach (var map in Current.Game.Maps)
            {
                map.GetComponent<MapComponent_FogOfWar>().RecalculateVisibility();
            }
        }

        private void DrawOption(Rect rect, string label, string desc, Texture2D icon, bool selected, Action onSelect)
        {
            Widgets.DrawMenuSection(rect);
            if (selected)
            {
                Widgets.DrawHighlight(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                onSelect();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            var contentRect = rect.ContractedBy(20f);

            var iconRect = new Rect(rect.x, rect.y, rect.width, rect.width).ExpandedBy(21);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            var labelHeight = Text.CalcHeight(label, contentRect.width);
            var labelRect = new Rect(contentRect.x, contentRect.yMax - 80f, contentRect.width, labelHeight);
            Widgets.Label(labelRect, label);

            Text.Font = GameFont.Small;
            var descHeight = Text.CalcHeight(desc, contentRect.width);
            var descRect = new Rect(contentRect.x, labelRect.yMax + 5f, contentRect.width, descHeight);
            Widgets.Label(descRect, desc);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
