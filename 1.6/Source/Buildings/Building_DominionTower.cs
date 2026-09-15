
namespace SurveillanceShift
{
    public class Building_DominionTower : Building_Camera
    {
        private const int DominionSightRadius = 45;

        public override int SightRadius => DominionSightRadius;
    }
}
