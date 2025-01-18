using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Geometry;

namespace LandArchTools.Commands
{
    public class PatchMultiCommand : Command
    {
        public PatchMultiCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static PatchMultiCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "MultiPatch";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get selected objects or prompt for selection
                var rc = RhinoGet.GetMultipleObjects(
                    "Select Closed Polylines",
                    false,
                    ObjectType.Curve,
                    out ObjRef[] objRefs);

                if (rc != Result.Success || objRefs == null || objRefs.Length == 0)
                    return rc;

                // Get UV divisions from user
                int uvDivisions = 1;
                rc = RhinoGet.GetInteger(
                    "Enter number of UV divisions",
                    true,
                    ref uvDivisions,
                    1,
                    1000);

                if (rc != Result.Success || uvDivisions < 1)
                    return rc;

                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                foreach (var objRef in objRefs)
                {
                    var curve = objRef.Curve();
                    if (curve != null && curve.IsClosed)
                    {
                        var curveList = new System.Collections.Generic.List<Curve> { curve };
                        var patch = Brep.CreatePatch(curveList, uvDivisions, uvDivisions, 0.01);

                        if (patch != null)
                        {
                            doc.Objects.AddBrep(patch);
                        }
                    }
                }

                // Re-enable redraw and update display
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