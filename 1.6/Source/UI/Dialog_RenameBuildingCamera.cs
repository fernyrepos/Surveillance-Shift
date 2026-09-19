using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class Dialog_RenameBuildingCamera : Dialog_Rename<Building_Camera>
    {
        public Dialog_RenameBuildingCamera(Building_Camera camera) : base(camera)
        {
        }

        public override AcceptanceReport NameIsValid(string name)
        {
            return name.Length > 0 && name.Length <= 28;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            var flag = false;
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                flag = true;
                Event.current.Use();
            }
            var rect = new Rect(inRect);
            Text.Font = GameFont.Medium;
            rect.height = Text.LineHeight + 10f;
            Widgets.Label(rect, renaming.customName.NullOrEmpty() ? "SS_Name".Translate() : "Rename".Translate());
            Text.Font = GameFont.Small;
            GUI.SetNextControlName("RenameField");
            var text = Widgets.TextField(new Rect(0f, rect.height, inRect.width, 35f), curName);
            if (text.Length < MaxNameLength)
            {
                curName = text;
            }
            if (focusedRenameField is false)
            {
                UI.FocusControl("RenameField", this);
                focusedRenameField = true;
            }
            if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 10f, inRect.width - 15f - 15f, 35f), "OK") is false && flag is false)
            {
                return;
            }
            var report = NameIsValid(curName);
            if (report.Accepted is false)
            {
                Messages.Message(report.Reason.NullOrEmpty() ? "NameIsInvalid".Translate() : report.Reason, MessageTypeDefOf.RejectInput, false);
            }
            else
            {
                if (renaming != null)
                {
                    renaming.RenamableLabel = curName;
                }

                OnRenamed(curName);
                Find.WindowStack.TryRemove(this);
            }
        }
    }
}
