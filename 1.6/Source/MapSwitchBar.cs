using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    [HotSwappable]
    public static class MapSwitchBar
    {
        private const float IconSize = 48f;
        private const float Spacing = 10f;
        private const float TopMargin = 21f;
        private const float LabelHeight = 18f;
        private const float LabelOverhang = 20f;
        private const int WindowId = 74212094;

        public static void Draw()
        {
            if (SurveillanceShiftMod.settings.HideColonistBar is false) return;
            if (WorldRendererUtility.WorldSelected) return;
            var maps = Find.Maps;
            if (maps.Count <= 1) return;

            var count = maps.Count;
            var totalWidth = count * IconSize + (count - 1) * Spacing;
            var panelRect = new Rect((UI.screenWidth - totalWidth) / 2f - LabelOverhang, TopMargin, totalWidth + LabelOverhang * 2f, IconSize + LabelHeight);

            Find.WindowStack.ImmediateWindow(WindowId, panelRect, WindowLayer.GameUI, () =>
            {
                for (var i = 0; i < count; i++)
                {
                    var iconRect = new Rect(LabelOverhang + i * (IconSize + Spacing), 0f, IconSize, IconSize);
                    DrawMapIcon(iconRect, maps[i]);
                }
            }, doBackground: false, shadowAlpha: 0f);
        }

        private static void DrawMapIcon(Rect rect, Map map)
        {
            var parent = map.Parent;
            var icon = parent?.ExpandingIcon;
            var label = parent?.Label;
            if (string.IsNullOrEmpty(label)) label = null; else label = label.CapitalizeFirst();
            if (icon == null) icon = Faction.OfPlayer.def.FactionIcon;

            var current = map == Find.CurrentMap;
            Widgets.DrawRectFast(rect, new Color(0.5f, 0.5f, 0.5f, current ? 0.55f : 0.3f));

            var iconColor = GUI.color;
            GUI.color = parent?.ExpandingIconColor ?? Color.white;
            GUI.DrawTexture(rect.ContractedBy(4f), icon);
            GUI.color = iconColor;

            if (label != null)
            {
                var labelRect = new Rect(rect.x - LabelOverhang, rect.yMax, rect.width + LabelOverhang * 2f, LabelHeight);
                var oldAnchor = Text.Anchor;
                var oldFont = Text.Font;
                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = GameFont.Tiny;
                GUI.color = Color.white;
                Widgets.Label(labelRect, label.Truncate(labelRect.width));
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
                GUI.color = Color.white;
            }

            TooltipHandler.TipRegion(rect, label ?? "Colony");

            if (Widgets.ButtonInvisible(rect) && current is false)
            {
                SoundDefOf.MapSelected.PlayOneShotOnCamera();
                Current.Game.CurrentMap = map;
            }
        }
    }
}
