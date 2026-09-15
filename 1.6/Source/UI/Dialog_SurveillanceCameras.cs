using System.Collections.Generic;
using System.Linq;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    [HotSwappable]
    public class Dialog_SurveillanceCameras : Window
    {
        private const float TitleHeight = 32f;
        private const float HeaderMargin = 40f;
        private const float ScrollbarMargin = 16f;
        private const float CardHeight = 100f;
        private const float CardGap = 12f;
        private const float ButtonWidth = 130f;
        private const float ButtonHeight = 28f;
        private const float IconSize = 80f;
        private const float CardPadding = 10f;
        private const float ContentSpacing = 12f;
        private const float LabelHeight = 26f;
        private const int Columns = 2;

        private readonly Map map;
        private readonly PipeNet pipeNet;
        private Vector2 scrollPosition;
        private List<Building_Camera> cachedCameras;

        public override Vector2 InitialSize => new(660f, 480f);

        public Dialog_SurveillanceCameras(Map map, PipeNet pipeNet = null)
        {
            this.map = map;
            this.pipeNet = pipeNet;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            cachedCameras = map.listerBuildings.AllBuildingsColonistOfClass<Building_Camera>().ToList();
            if (pipeNet != null)
            {
                cachedCameras = cachedCameras.Where(c => c.GetComp<CompResource>().PipeNet == pipeNet).ToList();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, TitleHeight), "SS_SurveillanceCameras".Translate());
            Text.Font = GameFont.Small;

            var outRect = new Rect(inRect.x, inRect.y + HeaderMargin, inRect.width, inRect.height - HeaderMargin);

            if (cachedCameras.Count == 0)
            {
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.Label(outRect, "SS_NoCamerasLinked".Translate());
                GUI.color = Color.white;
            }
            else
            {
                var rows = Mathf.CeilToInt((float)cachedCameras.Count / Columns);
                var viewWidth = outRect.width - ScrollbarMargin;
                var cardWidth = (viewWidth - (Columns - 1) * CardGap) / Columns;
                var viewRect = new Rect(0f, 0f, viewWidth, rows * (CardHeight + CardGap));

                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

                for (var i = 0; i < cachedCameras.Count; i++)
                {
                    var cam = cachedCameras[i];
                    var cardRect = new Rect((i % Columns) * (cardWidth + CardGap), (i / Columns) * (CardHeight + CardGap), cardWidth, CardHeight);

                    Widgets.DrawMenuSection(cardRect);

                    var inner = cardRect.ContractedBy(CardPadding);
                    var iconRect = new Rect(inner.x, inner.y, IconSize, IconSize);
                    GUI.DrawTexture(iconRect, cam.def.uiIcon, ScaleMode.ScaleToFit);
                    var contentX = iconRect.xMax + ContentSpacing;
                    var contentWidth = inner.xMax - contentX;

                    Text.Anchor = TextAnchor.MiddleLeft;
                    var nameRect = new Rect(contentX, inner.y + 10, contentWidth, LabelHeight);
                    Widgets.Label(nameRect, cam.RenamableLabel);

                    var btnRect = new Rect(contentX, nameRect.yMax, ButtonWidth, ButtonHeight);
                    GUI.enabled = cam.IsActive;
                    if (Widgets.ButtonText(btnRect, "SS_AccessFeed".Translate()))
                    {
                        AccessFeed(cam);
                    }
                    GUI.enabled = true;
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                Widgets.EndScrollView();
            }
        }

        private void AccessFeed(Building_Camera cam)
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            State.ActiveFeedCamera = cam;
            State.ActiveFeedMap = map;
            State.ActiveFeedPipeNet = pipeNet;
            ModCompatibility.SetPerspectiveShiftCameraLock(cam.Position.ToVector3Shifted());
            Close();
        }
    }
}
