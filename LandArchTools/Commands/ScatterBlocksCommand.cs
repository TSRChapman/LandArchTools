using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using LandArchTools.Utilities;
using System.Runtime.InteropServices;

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
                Result rs = getUserOptions(
                    out ObjRef surface,
                    out ObjRef[] blocks,
                    out int numBlocks,
                    out double scale,
                    out bool rotation
                );
                if (rs != Result.Success)
                {
                    RhinoApp.WriteLine("Failed to get user options");
                    return Result.Failure;
                }

                // Turn redraw off for performance
                doc.Views.RedrawEnabled = false;

                // convert to a compatible mesh, low poly and triangles only
                Mesh mesh = convertToMesh(surface);
                if (mesh == null)
                {
                    RhinoApp.WriteLine("Failed to convert surface to mesh");
                    return Result.Failure;
                }

                // Get mesh info
                getMeshInfo(
                    out Point3d[][] verts,
                    out double totalArea,
                    out int totalFaces,
                    mesh,
                    doc
                );

                // scatter points onto mesh
                scatterPointsOnMesh(verts, totalArea, numBlocks, out Point3d[] points, doc);

                // Move blocks to points
                moveBlockstoPoints(points, blocks, rotation, doc);

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
        /// Move blocks to points
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private static void moveBlockstoPoints(Point3d[] points, ObjRef[] blocks, bool rotation, RhinoDoc doc)
        {
            // Shuffle points to randomize placement, chunk points into sub lists for each block
            Random rand = new Random();
            points = points.OrderBy(x => rand.Next()).ToArray();
            int pointDivision = points.Length / blocks.Length;
            var genList = listHelper.ChunkBy(points.ToList(), pointDivision);
            int blockIndex = 0;

            foreach (var pts in genList)
            {
                var block = blocks[blockIndex].Object() as InstanceObject;
                var blockPt = block.InsertionPoint;

                foreach (var pt in pts)
                {
                    RhinoApp.WriteLine("Moving block to point");
                   Vector3d vec = pt - blockPt;

                    // create new block instance and move to point

                    var newBlock = block.InstanceDefinition.Index;
                    doc.Objects.AddInstanceObject(newBlock, Transform.Translation(vec));

                   
                }   
            }
            
            

        }

        /// <summary>
        /// Scatter points onto mesh
        /// </summary>
        /// <param name="verts"></param>
        private static void scatterPointsOnMesh(
            Point3d[][] verts,
            double totalArea,
            int scatterNum,
            out Point3d[] points,
            RhinoDoc doc
        )
        {
            points = null;
            double pointBucket = 0.0;
            foreach (var i in verts)
            {
                // Get area of each triangle in the mesh
                Point3d a = i[0];
                Point3d b = i[1];
                Point3d c = i[2];

                double dist1 = (a - b).Length;
                double dist2 = (b - c).Length;
                double dist3 = (c - a).Length;

                var s = (dist1 + dist2 + dist3) / 2;
                var tArea = Math.Sqrt(s * (s - dist1) * (s - dist2) * (s - dist3));

                // get number of points to allocate per unit of area then get triangle area and assign appropriate amount of points to that triangle
                double numPointsPerUnit = (double)(totalArea / scatterNum);
                double ptAllocation = (double)(tArea / numPointsPerUnit);
                pointBucket += ptAllocation;

                // if the triangle area is too small add to culmulative area and add points to the next triangle
                if (pointBucket < 1)
                {
                    continue;
                }
                else
                {
                    ptAllocation += pointBucket;
                    pointBucket = 0;
                }

                // create vectors for each point
                Vector3d ac = new Vector3d(c - a);
                Vector3d ab = new Vector3d(b - a);
                Vector3d originVector = new Vector3d(a - new Point3d(0, 0, 0));

                for (int j = 0; j < ptAllocation; j++)
                {
                    // generate random number between 0 and 1
                    Random rand = new Random();
                    double rand1 = rand.NextDouble();
                    double rand2 = rand.NextDouble();

                    // create random points inside the triangle in 2d space
                    Vector3d p;
                    if (rand1 + rand2 < 1)
                    {
                        p = rand1 * ac + rand2 * ab;
                    }
                    else
                    {
                        p = (1 - rand1) * ac + (1 - rand2) * ab;
                    }

                    // tranform 2d points to 3d space
                    var point = new Point3d(p + originVector);

                    doc.Objects.AddPoint(new Point3d(point));
                }
            }
        }

        /// <summary>
        /// Get properties of the passed in mesh
        /// </summary>
        /// <param name="points"></param>
        /// <param name="totalArea"></param>
        /// <param name="totalFaces"></param>
        /// <param name="mesh"></param>
        private static void getMeshInfo(
            out Point3d[][] points,
            out double totalArea,
            out int totalFaces,
            Rhino.Geometry.Mesh mesh,
            RhinoDoc doc
        )
        {
            int faceCount = mesh.Faces.Count;
            List<Point3d[]> pointsList = new List<Point3d[]>();
            for (int i = 0; i < faceCount; i++)
            {
                mesh.Faces.GetFaceVertices(
                    i,
                    out Point3f a,
                    out Point3f b,
                    out Point3f c,
                    out Point3f d
                );
                Point3d[] faceVerts = new Point3d[] { a, b, c };
                pointsList.Add(faceVerts);
            }

            totalArea = AreaMassProperties.Compute(mesh).Area;
            totalFaces = mesh.Faces.Count;
            points = pointsList.ToArray();
        }

        /// <summary>
        /// Converts a surface or brep into a mesh
        /// </summary>
        /// <param name="surface"></param>
        /// <returns>
        /// returns a mesh if successful, null if failed
        /// </returns>
        private static Rhino.Geometry.Mesh convertToMesh(ObjRef surface)
        {
            var isMesh = surface.Object().Geometry.ObjectType == ObjectType.Mesh;
            Mesh mesh = new Mesh();

            if (isMesh == true)
            {
                // if mesh convert to lower poly mesh or remain same if already low poly
                // this is done so the density of scatter is consistent
                mesh = surface.Object().Geometry as Mesh;
                mesh.Reduce(1000, true, 1, true);
                mesh.Faces.ConvertQuadsToTriangles();
            }
            else
            {
                RhinoObject surfaceObj = surface.Object();

                // convert geometry to brep
                if (surfaceObj.Geometry.HasBrepForm == false)
                {
                    RhinoApp.WriteLine("Failed to convert surface to mesh");
                    return null;
                }
                Brep surfaceBrep = Brep.TryConvertBrep(surfaceObj.Geometry);

                // Convert to mesh and rebuild mesh faces into single mesh
                Mesh[] meshParts = Mesh.CreateFromBrep(surfaceBrep, MeshingParameters.FastRenderMesh);
                foreach (var i in meshParts)
                {
                    mesh.Append(i);
                }

                // triangulate mesh to remove any quads
                // this is done so that the scatter formula works correctly
                var rc = mesh.Faces.ConvertQuadsToTriangles();
                if (mesh == null || rc != true)
                {
                    RhinoApp.WriteLine("Failed to convert surface to mesh");
                    return null;
                }
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
        private static Result getUserOptions(
            out ObjRef surface,
            out ObjRef[] blocks,
            out int numBlocks,
            out double scale,
            out bool rotation
        )
        {
            surface = null;
            blocks = null;
            numBlocks = 0;
            scale = 0;
            rotation = false;

            // Select surface to scatter on
            ObjRef surfaceChoice;
            Result r1 = RhinoGet.GetOneObject(
                "Select surface to scatter on",
                false,
                ObjectType.Surface | ObjectType.Brep | ObjectType.Mesh,
                out surfaceChoice
            );
            if (r1 != Result.Success)
                return r1;
            surface = surfaceChoice;

            // Select blocks to scatter
            ObjRef[] blockRefs;
            Result r2 = RhinoGet.GetMultipleObjects(
                "Select blocks to scatter",
                false,
                ObjectType.InstanceReference,
                out blockRefs
            );
            if (r2 != Result.Success)
                return r2;
            blocks = blockRefs;

            // get int of blocks to scatter
            int numChoice = 100;
            Result r3 = RhinoGet.GetInteger(
                "Enter number of blocks to scatter",
                true,
                ref numChoice,
                1,
                10000
            );
            if (r3 != Result.Success)
                return r3;
            numBlocks = numChoice;

            // get double for scaling of objects
            double scaleChoice = 0;
            Result r4 = RhinoGet.GetNumber(
                "Enter scale multiplyer (0 for no scaling)",
                true,
                ref scaleChoice,
                0,
                1000
            );
            if (r4 != Result.Success)
                return r4;
            scale = scaleChoice;

            // get user input for yes or no on rotation
            bool rotationChoice = false;
            Result r5 = RhinoGet.GetBool("Rotate Objects?", true, "No", "Yes", ref rotationChoice);
            if (r5 != Result.Success)
                return r5;
            rotation = rotationChoice;

            return Result.Success;
        }
    }
}
