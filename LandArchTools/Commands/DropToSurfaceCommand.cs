using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LandArchTools.Commands
{
    public class DropToSurfaceCommand : Command
    {
        public DropToSurfaceCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static DropToSurfaceCommand Instance { get; private set; }

        public override string EnglishName => "DropToSurface";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                getUserOptions(out ObjRef[] objRefs, out ObjRef surfaceRef, doc);

                dropObjects(objRefs, surfaceRef, doc);

            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Failed to execute");
                RhinoApp.WriteLine(ex.Message);
            }

            doc.Views.Redraw();
            return Result.Success;
        }

        private static Result dropObjects(ObjRef[] objRefs, ObjRef surfaceRef, RhinoDoc doc)
        {
            foreach (var objRef in objRefs)
            {
                var bBox = objRef.Object().Geometry.GetBoundingBox(true);

                // check if bBox is not empty
                if (bBox.IsValid)
                {
                    // get center point of bBox and move to lowest Z
                    Point3d centerPt = bBox.Center;
                    centerPt.Z = bBox.Min.Z;

                    // use ray to find intersection with surface
                    Ray3d ray01 = new Ray3d(centerPt, new Vector3d(0, 0, 1));
                    Ray3d ray02 = new Ray3d(centerPt, new Vector3d(0, 0, -1));

                    // check if nurb or mesh
                    bool isMesh = surfaceRef.Object().Geometry.ObjectType == ObjectType.Mesh;


                    Point3d intersectionPt;

                    if (isMesh)
                    {
                        var t01 = Intersection.MeshRay(surfaceRef.Mesh(), ray01);
                        var t02 = Intersection.MeshRay(surfaceRef.Mesh(), ray02);

                        var intersection01 = ray01.PointAt(t01);
                        var intersection02 = ray02.PointAt(t02);

                        // Check which ray has an intersection and move object to that point
                        intersectionPt = intersection01.IsValid
                            ? intersection01
                            : intersection02.IsValid
                                ? intersection02
                                : Point3d.Unset;
                    }
                    else
                    {
                        IEnumerable<GeometryBase> surfaceGeometries = new[]
                        {
                                surfaceRef.Object().Geometry
                            };
                        var intersections01 = Intersection.RayShoot(ray01, surfaceGeometries, 1);
                        var intersections02 = Intersection.RayShoot(ray02, surfaceGeometries, 1);

                        // Check which ray has an intersection and move object to that point
                        intersectionPt = intersections01.Any()
                            ? intersections01.First()
                            : intersections02.Any()
                                ? intersections02.First()
                                : Point3d.Unset;
                    }

                    // tranform object to intersection point
                    if (intersectionPt != Point3d.Unset)
                    {
                        Transform xform = Transform.Translation(intersectionPt - centerPt);
                        doc.Objects.Transform(objRef, xform, true);
                    }
                }
            }


            return Result.Success;
        }

        /// <summary>
        /// Gets user options
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="surface"></param>
        /// <returns></returns>
        private static Result getUserOptions(
            out ObjRef[] objectRefs,
            out ObjRef surfaceRef,
            RhinoDoc doc
        )
        {
            surfaceRef = null;
            objectRefs = null;

            // Select surface to scatter on
            ObjRef surfaceChoice;
            Result r1 = RhinoGet.GetOneObject(
                "Select surface to drop to",
                false,
                ObjectType.Surface | ObjectType.Brep | ObjectType.Mesh,
                out surfaceChoice
            );
            if (r1 != Result.Success)
                return r1;
            surfaceRef = surfaceChoice;

            // stop selection from remaining on
            doc.Views.Redraw();

            GetObject go = new GetObject();
            go.SetCommandPrompt("Select objects to drop onto surface");
            go.GeometryFilter = ObjectType.AnyObject;
            go.GroupSelect = true;
            go.SubObjectSelect = false;
            go.AlreadySelectedObjectSelect = false;
            go.DisablePreSelect();
            go.GetMultiple(1, 0);
            if (go.CommandResult() != Result.Success)
                return go.CommandResult();

            objectRefs = go.Objects();

            return Result.Success;
        }
    }
}
