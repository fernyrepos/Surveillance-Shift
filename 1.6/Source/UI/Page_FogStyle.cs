using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class Page_FogStyle : Page
    {
        private const float ControlsWidth = 410f;
        private const float ColumnGap = 30f;
        private const float RowHeight = 30f;
        private const float LabelWidth = 110f;
        private const float ValueWidth = 70f;

        private FogPreview preview;

        public override string PageTitle => "SS_FogStyleTitle".Translate();

        public override void PreClose()
        {
            base.PreClose();
            preview?.Dispose();
            preview = null;
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 35f), PageTitle);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            var main = GetMainRect(rect);
            main.yMin += 10f;
            var controls = new Rect(main.x, main.y, ControlsWidth, main.height);
            var previewRect = new Rect(controls.xMax + ColumnGap, main.y, main.width - ControlsWidth - ColumnGap, main.height);

            DrawControls(controls);
            DrawPreview(previewRect);

            DoBottomButtons(rect, next == null && nextAct == null ? "Close".Translate() : "Next".Translate());
        }

        private void DrawControls(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            var shaderReady = FogShader.Available;

            var hardcore = FogStyle.HardcoreFog || shaderReady is false;
            var row = listing.GetRect(RowHeight);
            var before = hardcore;
            Widgets.CheckboxLabeled(row, "SS_FogHardcore".Translate(), ref hardcore, disabled: shaderReady is false);
            TooltipHandler.TipRegion(row, "SS_FogHardcoreDesc".Translate());
            if (hardcore != before)
            {
                FogStyle.HardcoreFog = hardcore;
                FogStyle.Touch();
            }

            var editable = hardcore is false;

            var pawnsVisible = FogStyle.PawnsVisibleInFog;
            row = listing.GetRect(RowHeight);
            before = pawnsVisible;
            Widgets.CheckboxLabeled(row, "SS_FogPawnsVisible".Translate(), ref pawnsVisible, disabled: editable is false);
            TooltipHandler.TipRegion(row, "SS_FogPawnsVisibleDesc".Translate());
            if (pawnsVisible != before)
            {
                FogStyle.PawnsVisibleInFog = pawnsVisible;
                FogStyle.Touch();
            }

            listing.Gap(10f);

            if (shaderReady is false)
            {
                listing.Label("SS_FogShaderUnavailable".Translate());
                listing.Gap(6f);
            }
            else if (editable is false)
            {
                GUI.color = ColoredText.SubtleGrayColor;
                listing.Label("SS_FogSlidersLocked".Translate());
                GUI.color = Color.white;
                listing.Gap(6f);
            }

            var blur = Slider(listing, "SS_FogBlur".Translate(), FogStyle.Blur, 0f, FogStyle.BlurMax, FogStyle.Blur.ToString("0.00"), "SS_FogBlurDesc".Translate(), editable);
            var contrast = Slider(listing, "SS_FogContrast".Translate(), FogStyle.Contrast, FogStyle.ContrastMin, FogStyle.ContrastMax, FogStyle.Contrast.ToString("0.00"), "SS_FogContrastDesc".Translate(), editable);
            var saturation = Slider(listing, "SS_FogSaturation".Translate(), FogStyle.Saturation, 0f, FogStyle.SaturationMax, FogStyle.Saturation.ToString("0.00"), "SS_FogSaturationDesc".Translate(), editable);
            var brightness = Slider(listing, "SS_FogBrightness".Translate(), FogStyle.Brightness, 0f, FogStyle.BrightnessMax, FogStyle.Brightness.ToString("0.00"), "SS_FogBrightnessDesc".Translate(), editable);

            if (editable && (blur != FogStyle.Blur || contrast != FogStyle.Contrast || saturation != FogStyle.Saturation || brightness != FogStyle.Brightness))
            {
                FogStyle.Blur = blur;
                FogStyle.Contrast = contrast;
                FogStyle.Saturation = saturation;
                FogStyle.Brightness = brightness;
                FogStyle.Touch();
            }

            listing.Gap(14f);
            var resetRect = listing.GetRect(32f);
            resetRect.width = 170f;
            if (Widgets.ButtonText(resetRect, "SS_FogReset".Translate()))
            {
                FogStyle.Reset();
            }

            listing.End();
        }

        private static float Slider(Listing_Standard listing, string label, float value, float min, float max, string valueText, string tip, bool enabled)
        {
            var row = listing.GetRect(RowHeight + 6f);
            TooltipHandler.TipRegion(row, tip);

            var color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            GUI.color = color;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x, row.y, LabelWidth, row.height), label);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(row.xMax - ValueWidth, row.y, ValueWidth, row.height), valueText);
            Text.Anchor = TextAnchor.UpperLeft;

            var sliderRect = new Rect(row.x + LabelWidth + 8f, row.y, row.width - LabelWidth - ValueWidth - 16f, row.height);
            var result = Widgets.HorizontalSlider(sliderRect, value, min, max, true);
            GUI.color = Color.white;

            return enabled ? result : value;
        }

        private void DrawPreview(Rect rect)
        {
            preview ??= new FogPreview();

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 32f), "SS_FogPreview".Translate());
            Text.Font = GameFont.Small;

            var imageTop = rect.y + 40f;
            var width = rect.width;
            var height = width * FogPreview.Height / FogPreview.Width;
            var imageRect = new Rect(rect.x, imageTop, width, height);

            Widgets.DrawBoxSolid(imageRect.ExpandedBy(2f), new Color(0.25f, 0.25f, 0.25f));
            GUI.DrawTexture(imageRect, preview.Texture, ScaleMode.StretchToFill);

            GUI.color = ColoredText.SubtleGrayColor;
            var captionRect = new Rect(rect.x, imageRect.yMax + 10f, rect.width, 60f);
            Widgets.Label(captionRect, "SS_FogPreviewCaption".Translate());
            GUI.color = Color.white;
        }
    }
}
