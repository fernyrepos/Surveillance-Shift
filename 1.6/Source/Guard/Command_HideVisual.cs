using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public class Command_HideVisual : Command_Toggle
    {
        public Func<IEnumerable<FloatMenuOption>> rightClickOptions;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => rightClickOptions?.Invoke() ?? Enumerable.Empty<FloatMenuOption>();
    }
}
