using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Input;
using System.Drawing;
using static Rhino.Render.TextureGraphInfo;

namespace LandArchTools.Commands
{
    public class TruncateWorldCoordinateCommand : Command
    {
        public TruncateWorldCoordinateCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static TruncateWorldCoordinateCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "TruncateWorldCoordinate";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get setout point
                Point3d point;
                Result getPointResult = RhinoGet.GetPoint("Pick Point for World Coordinate value", false, out point);
                if (getPointResult != Result.Success)
                    return getPointResult;

                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Round coordinates
                double northing = Math.Round(point.Y, 5);
                double easting = Math.Round(point.X, 5);

                // Create origin layer
                string originLayerName = "_ORIGIN_";
                Layer originLayer = doc.Layers.FindName(originLayerName);
                int originLayerIndex = originLayer?.Index ?? -1;

                if (originLayerIndex < 0)
                {
                    Layer layer = new Layer();
                    layer.Name = "_ORIGIN_";
                    layer.IsLocked = true;
                    layer.Color = Color.Red;
                    originLayerIndex = doc.Layers.Add(layer);
                }

                Point3d originPoint = new Point3d(0, 0, 0);

                // Move all objects to origin
                var allObjects = doc.Objects.GetObjectList(new ObjectEnumeratorSettings());
                var vector = new Vector3d(-point.X, -point.Y, 0);
                foreach (var obj in allObjects)
                {
                    obj.Geometry.Translate(vector);
                    obj.CommitChanges();
                }

                // Draw origin marker
                var circle = new Circle(originPoint, 1);
                ObjectAttributes circatt = new ObjectAttributes();
                circatt.LayerIndex = originLayerIndex;
                var circleId = doc.Objects.AddCircle(circle, circatt);
              

                // Add quadrant lines
                Point3d origin = circle.Plane.Origin;
                Vector3d xAxis = circle.Radius * circle.Plane.XAxis;
                Vector3d yAxis = circle.Radius * circle.Plane.YAxis;

                ObjectAttributes lineatt = new ObjectAttributes();
                lineatt.LayerIndex = originLayerIndex;

                var line1Id = doc.Objects.AddLine(origin - xAxis, origin + xAxis, lineatt);
                var line2Id = doc.Objects.AddLine(origin - yAxis, origin + yAxis, lineatt);


                // Add text marker
                string text = $" E {easting} N {northing}";
                var textDot = new TextDot(text, originPoint);
                ObjectAttributes Dotatt = new ObjectAttributes();
                Dotatt.LayerIndex = originLayerIndex;
                Dotatt.Name = "_ORIGIN_POINT_";
                Dotatt.SetUserString("type", "_ORIGIN_POINT_");
                Dotatt.SetUserString("E", $"{easting}");
                Dotatt.SetUserString("N", $"{northing}");
                var textDotId = doc.Objects.AddTextDot(textDot, Dotatt);
                

                // Enable redraw and update display
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