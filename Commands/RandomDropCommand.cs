using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.Input;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LandArchTools.Commands
{
    public class RandomDropCommand : Command
    {
        public RandomDropCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static RandomDropCommand Instance { get; private set; }

        public override string EnglishName => "RandomDrop";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var rc = RhinoGet.GetMultipleObjects("Pick blocks to rotate randomly", false, ObjectType.InstanceReference, out ObjRef[] objRefs);
                if (rc != Result.Success || objRefs.Length == 0)
                    return rc;  

                // Get max drop distance
                double dropnum = 0;
                var result = RhinoGet.GetNumber("Enter max drop distance", true, ref dropnum);
                if (result != Result.Success)
                    return result;

                // Disable redraw
                doc.Views.RedrawEnabled = false;

                Random rnd = new Random();
                foreach (var objRef in objRefs)
                {
                    var obj = objRef.Object() as InstanceObject;
                    if (obj == null) continue;

                    // Calculate random drop distance
                    double num = rnd.NextDouble() * (-Math.Abs(dropnum));
                    var vec = new Rhino.Geometry.Vector3d(0, 0, num);

                    // Move object
                    var xform = Transform.Translation(vec);
                    doc.Objects.Transform(objRef, xform, true);
                    RhinoApp.WriteLine($"Dropped {obj.InstanceDefinition.Name} by {num}");
                }

                // Enable redraw
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