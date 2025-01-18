using System;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LandArchTools.Commands
{
    public class RestoreWorldCoordinateCommand : Command
    {
        public RestoreWorldCoordinateCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static RestoreWorldCoordinateCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "RestoreWorldCoordinate";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                doc.Views.RedrawEnabled = false;

                // Find origin text by name
                var originText = FindObjectByName(doc, "_ORIGIN_TEXT_");
                if (originText == null)
                {
                    RhinoApp.WriteLine("Origin text not found. Has TruncateWorldCoordinate been run?");
                    return Result.Failure;
                }

                // Parse coordinates from text
                if (!TryParseCoordinates(originText.Attributes, out double easting, out double northing))
                {
                    RhinoApp.WriteLine("Failed to parse coordinates from origin text");
                    return Result.Failure;
                }

                // Find origin point and create vector
                var originPoint = FindObjectByName(doc, "_ORIGIN_POINT_");
                if (originPoint == null)
                {
                    RhinoApp.WriteLine("Origin point not found");
                    return Result.Failure;
                }

                Point3d orPoint = new Point3d(easting, northing, 0);
                Point3d point = originPoint.Geometry.GetBoundingBox(true).Center;
                Vector3d vector = orPoint - point;

                // Move all objects back to original position
                var allObjects = doc.Objects.GetObjectList(new ObjectEnumeratorSettings());
                foreach (var obj in allObjects)
                {
                    obj.Geometry.Translate(vector);
                    obj.CommitChanges();
                }

                // Handle layer cleanup
                Layer originLayer = doc.Layers.FindName("_ORIGIN_");
                if (originLayer != null)
                {
                    bool isCurrentLayer = doc.Layers.CurrentLayer == originLayer;

                    if (isCurrentLayer)
                    {
                        // Find or create Default layer
                        Layer defaultLayer = doc.Layers.FindName("Default");
                        if (defaultLayer != null)
                        {
                            int defaultLayerIndex = doc.Layers.Add("Default", System.Drawing.Color.Black);
                            doc.Layers.SetCurrentLayerIndex(defaultLayerIndex, true);
                        }
                        doc.Layers.SetCurrentLayerIndex(defaultLayer.Index, true);
                    }

                    // Delete origin layer and its objects
                    doc.Layers.Purge(originLayer.Index, true);
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

        private RhinoObject FindObjectByName(RhinoDoc doc, string name)
        {
            var objects = doc.Objects.FindByUserString("type", "_ORIGIN_POINT_", true);
            return objects[0];
        }

        private bool TryParseCoordinates(ObjectAttributes textDot, out double easting, out double northing)
        {
            easting = 0;
            northing = 0;

            try
            {
                easting = Convert.ToDouble(textDot.GetUserString("E"));
                northing = Convert.ToDouble(textDot.GetUserString("N"));
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error parsing coordinates: {ex.Message}");
            }

            return false;
        }
    }
}