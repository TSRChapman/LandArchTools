using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;

namespace LandArchTools.Commands
{
    public class ProjectCurvesToSurface : Command
    {
        public ProjectCurvesToSurface()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ProjectCurvesToSurface Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "ProjectCurvesToSurface";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get curves to project
                ObjRef[] curveRefs;
                var rc = RhinoGet.GetMultipleObjects(
                    "Select curves to project",
                    false,
                    ObjectType.Curve,
                    out curveRefs
                );
                if (rc != Result.Success || !curveRefs.Any())
                    return rc;

                // Get target surface/mesh
                ObjRef targetRef;
                rc = RhinoGet.GetOneObject(
                    "Select the TIN to project onto",
                    false,
                    ObjectType.Surface | ObjectType.Brep | ObjectType.Mesh,
                    out targetRef
                );
                if (rc != Result.Success)
                    return rc;

                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Convert mesh to NURBS if necessary
                var isMesh = targetRef.Geometry().ObjectType == ObjectType.Mesh;
                Brep targetBrep = null;

                if (isMesh)
                {
                    var mesh = targetRef.Mesh();
                    if (mesh != null)
                    {
                        targetBrep = Brep.CreateFromMesh(mesh, false);
                    }
                }
                else
                {
                    targetBrep = targetRef.Brep();
                }

                if (targetBrep == null)
                {
                    RhinoApp.WriteLine("Failed to convert target to valid geometry");
                    return Result.Failure;
                }

                // Process each curve
                foreach (var curveRef in curveRefs)
                {
                    var curve = (curveRef.Curve()).ToNurbsCurve();
                    
                    if (curve == null) continue;

                    // Get control points
                    var points = curve.Points;
                    var newPoints = new List<ControlPoint>();

                    // Project each point
                    foreach (var point in points)
                    {
                        var pt = point.Location;
                        Point3d projectedPt = ProjectPoint(pt, targetBrep);

                        // Create new control point with projected location
                        var newPoint = new ControlPoint(projectedPt);
                        newPoint.Weight = point.Weight;
                        newPoints.Add(newPoint);
                    }

                    // Create new curve with projected points
                    var newCurve = curve.Duplicate() as Curve;
                    if (newCurve is NurbsCurve nurbsCurve)
                    {
                        for (int i = 0; i < newPoints.Count; i++)
                        {
                            nurbsCurve.Points[i] = newPoints[i];
                        }
                    }

                    // Add the new curve to the document
                    doc.Objects.AddCurve(newCurve);
                }

                // Clean up if we created a temporary brep
                if (isMesh)
                {
                    targetBrep.Dispose();
                }

                doc.Views.RedrawEnabled = true;
                doc.Views.Redraw();
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to project curves: {ex.Message}");
                doc.Views.RedrawEnabled = true;
                return Result.Failure;
            }
        }

        private Point3d ProjectPoint(Point3d point, Brep target)
        {
            // Try shooting rays in both directions
            var upRay = new Ray3d(point, Vector3d.ZAxis);
            var downRay = new Ray3d(point, -Vector3d.ZAxis);

            var upIntersections = Intersection.RayShoot(upRay, new[] { target }, 1);
            var downIntersections = Intersection.RayShoot(downRay, new[] { target }, 1);

            // Check up direction first
            if (upIntersections != null && upIntersections.Any())
            {
                return upIntersections.First();
            }

            // Then check down direction
            if (downIntersections != null && downIntersections.Any())
            {
                return downIntersections.First();
            }

            // If no intersection found, find closest point on target
            var cp = target.ClosestPoint(point);
            if (cp.IsValid)
            {
                return cp;
            }

            // Return original point if all else fails
            return point;
        }
    }
}