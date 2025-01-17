using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LandArchTools.Commands
{
    public class MultiOffsetCommand : Command
    {
        public MultiOffsetCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static MultiOffsetCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "MultiOffset";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get objects to offset
                var go = new GetObject();
                go.SetCommandPrompt("Select Closed Curves for Offset");
                go.GeometryFilter = ObjectType.Curve;
                go.SubObjectSelect = false;
                go.GetMultiple(1, 0);
                if (go.CommandResult() != Result.Success)
                    return go.CommandResult();

                // Get offset direction
                bool inward = false;
                var rc = RhinoGet.GetBool("Offset Direction", true,"Inward","Outward", ref inward);
                if (rc != Result.Success)
                    return rc;

                // Get offset distance
                double distance = 0.0;
                rc = RhinoGet.GetNumber("Distance to Offset", true, ref distance);
                if (rc != Result.Success)
                    return rc;

                doc.Views.RedrawEnabled = false;

                foreach (var obj in go.Objects())
                {
                    var curve = obj.Curve();
                    if (curve != null && curve.IsClosed)
                    {
                        Point3d offsetPoint;
                        if (!inward)
                        {
                            // Use curve centroid for inward offset
                            var areaMassProps = AreaMassProperties.Compute(curve);
                            if (areaMassProps == null) continue;
                            offsetPoint = areaMassProps.Centroid;
                        }
                        else
                        {
                            // Use far point for outward offset
                            offsetPoint = new Point3d(1000000, 1000000, 1000000);
                        }

                        var offsetCurve = curve.Offset(
                            offsetPoint,
                            Vector3d.ZAxis,
                            distance,
                            doc.ModelAbsoluteTolerance,
                            CurveOffsetCornerStyle.Sharp
                        );

                        if (offsetCurve != null)
                        {
                            doc.Objects.AddCurve(offsetCurve[0]);
                        }
                    }
                }

                doc.Views.RedrawEnabled = true;
                doc.Views.Redraw();
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                doc.Views.RedrawEnabled = true;
                return Result.Failure;
            }
        }
    }
}