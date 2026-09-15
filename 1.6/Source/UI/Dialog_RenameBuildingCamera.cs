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
    }
}
