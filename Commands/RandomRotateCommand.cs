using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;

namespace LandArchTools.Commands
{
    public class RandomRotateCommand : Command
    {
        public RandomRotateCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static RandomRotateCommand Instance { get; private set; }

        public override string EnglishName => "RandomRotate";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                
                var rc = RhinoGet.GetMultipleObjects("Pick blocks to rotate randomly", false, ObjectType.InstanceReference, out ObjRef[] objRefs );
                if (rc != Result.Success || objRefs.Length == 0)
                    return rc;

                var random = new Random();
                var vec = new Vector3d(0, 0, 1);

                doc.Views.RedrawEnabled = false;

                foreach (var objRef in objRefs)
                {
                    var instanceRef = objRef.Object() as InstanceObject;
                    if (instanceRef != null)
                    {
                        var num = random.Next(-180, 180);
                        var point = instanceRef.InsertionPoint;
                        var xform = Transform.Rotation(num * Math.PI / 180, vec, point);
                        doc.Objects.Transform(objRef, xform, true);
                    }
                }

                doc.Views.RedrawEnabled = true;
                doc.Views.Redraw();
                return Result.Success;
            }
            catch
            {
                RhinoApp.WriteLine("Failed to execute");
                doc.Views.RedrawEnabled = true;
                return Result.Failure;
            }
        }
    }
}