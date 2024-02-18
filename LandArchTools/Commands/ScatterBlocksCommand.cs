using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Input;
using Rhino.DocObjects;
using Rhino.Geometry.Collections;
using System.Collections.Generic;
using Rhino.FileIO;

namespace LandArchTools.Commands
{
    public class ScatterBlocksCommand : Command
    {
        public ScatterBlocksCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static ScatterBlocksCommand Instance { get; private set; }

        public override string EnglishName => "ScatterBlocks";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            try
            {

                // Get user options
                Result rs = getUserOptions(out ObjRef surface, out ObjRef[] blocks, out int numBlocks, out double scale, out bool rotation);
                if (rs != Result.Success)
                {
                    RhinoApp.WriteLine("Failed to get user options");
                    return Result.Failure;
                }
                
                // Turn redraw off for performance
                doc.Views.RedrawEnabled = false;

                // Test if surface is a mesh If not a mesh, convert to mesh
                Mesh mesh = new Mesh();
                var isMesh = surface.Object().Geometry.ObjectType == ObjectType.Mesh;
                if (isMesh == false)
                {
                    mesh = convertToMesh(surface);
                    if (mesh == null)
                    {
                        RhinoApp.WriteLine("Failed to convert surface to mesh");
                        return Result.Failure;
                    }
                }
                else
                {
                    mesh = surface.Object().Geometry as Mesh;
                }




                // Turn Redraw back on
                doc.Views.RedrawEnabled = true;
                // Close script
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

        /// <summary>
        /// Converts a surface or brep into a mesh
        /// </summary>
        /// <param name="surface"></param>
        /// <returns>
        /// returns a mesh if successful, null if failed
        /// </returns>
        private static Mesh convertToMesh(ObjRef surface)
        {
            // convert geometry to brep
            RhinoObject surfaceObj = surface.Object();
            if (surfaceObj.Geometry.HasBrepForm == false) return null;
            Brep surfaceBrep = Brep.TryConvertBrep(surfaceObj.Geometry);

            // Convert to mesh and rebuild mesh parts
            Mesh mesh = new Mesh();
            Mesh[] meshParts = Mesh.CreateFromBrep(surfaceBrep, MeshingParameters.QualityRenderMesh);
            foreach (var i in meshParts)
            {
                mesh.Append(i);
            }

            if (mesh == null)
            {
                RhinoApp.WriteLine("Failed to convert surface to mesh");
                return null;
            }

            return mesh;
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
        private static Result getUserOptions(out ObjRef surface, out ObjRef[] blocks, out int numBlocks, out double scale, out bool rotation)
        {
            surface = null;
            blocks = null;
            numBlocks = 0;
            scale = 0;
            rotation = false;

            // Select surface to scatter on
            ObjRef surfaceChoice;
            Result r1 = RhinoGet.GetOneObject("Select surface to scatter on", false, ObjectType.Surface | ObjectType.Brep | ObjectType.Mesh, out surfaceChoice);
            if (r1 != Result.Success) return r1;
            surface = surfaceChoice;

            // Select blocks to scatter
            ObjRef[] blockRefs;
            Result r2 = RhinoGet.GetMultipleObjects("Select blocks to scatter", false, ObjectType.InstanceReference, out blockRefs);
            if (r2 != Result.Success) return r2;
            blocks = blockRefs;

            // get int of blocks to scatter
            int numChoice = 1;
            Result r3 = RhinoGet.GetInteger("Enter number of blocks to scatter", true, ref numChoice, 1, 10000);
            if (r3 != Result.Success) return r3;
            numBlocks = numChoice;

            // get double for scaling of objects
            double scaleChoice = 0;
            Result r4 = RhinoGet.GetNumber("Enter scale multiplyer (0 for no scaling)", true, ref scaleChoice, 0, 1000);
            if (r4 != Result.Success) return r4;
            scale = scaleChoice;

            // get user input for yes or no on rotation
            bool rotationChoice = false;
            Result r5 = RhinoGet.GetBool("Rotate Objects?", true, "No", "Yes", ref rotationChoice);
            if (r5 != Result.Success) return r5;
            rotation = rotationChoice;
            
            return Result.Success;

        }

    }
}