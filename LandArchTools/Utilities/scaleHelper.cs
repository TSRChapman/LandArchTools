using Rhino;

namespace LandArchTools.Utilities
{
    public static class scaleHelper
    {
        public static (double, bool) Scaling(RhinoDoc doc)
        {
            var unitSystem = doc.ModelUnitSystem;
            var imperial = unitSystem != UnitSystem.Millimeters &&
                           unitSystem != UnitSystem.Centimeters &&
                           unitSystem != UnitSystem.Meters &&
                           unitSystem != UnitSystem.Kilometers;

            var targetSystem = imperial ? UnitSystem.Feet : UnitSystem.Meters;
            var scale = RhinoMath.UnitScale(unitSystem, targetSystem);

            return (scale, imperial);
        }

    }
}
