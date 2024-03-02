using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;

namespace LandArchTools.Commands
{
    public class FaceCameraCommand : Command
    {
        public FaceCameraCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static FaceCameraCommand Instance { get; private set; }

        public override string EnglishName => "FaceCamera";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                Result r1 = getUserOptions(out ObjRef[] surfaces);
                if (r1 != Result.Success)
                    return r1;

                rotateSurfaces(surfaces, doc);

                doc.Views.Redraw();
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }

        private static Result rotateSurfaces(ObjRef[] surfaces, RhinoDoc doc)
        {
            // get camera location
            Point3d cameraPt = doc.Views.ActiveView.ActiveViewport.CameraLocation;
            cameraPt.Z = 0;

            foreach (ObjRef surface in surfaces)
            {
                // get surface object
                Surface srf = surface.Surface();
                if (srf == null)
                    return Result.Failure;

                // reparameterize surface
                srf.SetDomain(0, new Interval(0, 1));
                srf.SetDomain(1, new Interval(0, 1));

                // get midpoint of surface and set Z coordinate to 0
                var midPt = srf.PointAt(0.5, 0.5);
                midPt.Z = 0;

                // get normal of surface
                Vector3d normal = srf.NormalAt(0.5, 0.5);
                normal.Z = 0;

                // get vector from camera to midpoint
                Vector3d cameraToSurface = midPt - cameraPt;

                // calculate rotation axis using cross product
                Vector3d rotationAxis = Vector3d.CrossProduct(normal, cameraToSurface);

                // get angle between normal and camera to surface
                double angle = Vector3d.VectorAngle(normal, cameraToSurface);

                // rotate the surface to face the camera
                Transform rotation = Transform.Rotation(angle, rotationAxis, midPt);
                doc.Objects.Transform(surface, rotation, true);
            }

            return Result.Success;
        }

        /// <summary>
        /// Gets user options for the scatter command
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="blocks"></param>
        /// <param name="numBlocks"></param>
        /// <param name="scale"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        private static Result getUserOptions(out ObjRef[] surface)
        {
            surface = null;

            // Select surface to scatter on
            ObjRef[] surfaceChoice;
            Result r1 = RhinoGet.GetMultipleObjects(
                "Select surfaces to face towards camera",
                false,
                ObjectType.Surface,
                out surfaceChoice
            );

            if (r1 != Result.Success)
                return r1;

            surface = surfaceChoice;
            return Result.Success;
        }
    }
}
