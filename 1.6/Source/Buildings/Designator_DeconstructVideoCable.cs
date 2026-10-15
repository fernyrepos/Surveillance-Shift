using PipeSystem;
using Verse;

namespace SurveillanceShift
{
    public class Designator_DeconstructVideoCable : Designator_DeconstructPipe
    {
        public Designator_DeconstructVideoCable() : base(DefDatabase<PipeNetDef>.GetNamed("SS_VideoCableNet"))
        {
            defaultLabel = "SS_DeconstructVideoCables".Translate();
        }
    }
}
