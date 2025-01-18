using System;
using System.IO;
using System.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;



namespace LandArchTools.Commands
{
    public class ImportNearMapsCommand : Command
    {
        public ImportNearMapsCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ImportNearMapsCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "ImportJpgWorldFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                (double scale, bool imperial) = Scaling(doc);
                if (imperial)
                {
                    RhinoApp.WriteLine("This tool can only be used in mm, cm or m model units");
                    return Result.Failure;
                }

                // Get JGW file
                string jgwPath;
                if (!OpenFileDialog("Select JGW file", "JGW Files (*.jgw)|*.jgw|All Files (*.*)|*.*", out jgwPath))
                    return Result.Cancel;

                // Parse JGW file
                string[] jgwLines = File.ReadAllLines(jgwPath);
                double scaleFactor01 = double.Parse(jgwLines[0]);
                double worldX = double.Parse(jgwLines[4]) * scale;
                double worldY = double.Parse(jgwLines[5]) * scale;

                RhinoApp.WriteLine($"scale: {worldX}");

                // Get JPG file

                string jpgPath;
                if (!OpenFileDialog("Select JPG image file", "JPG Files (*.jpg)|*.jpg|All Files (*.*)|*.*", out jpgPath))
                    return Result.Cancel;

                // Get image dimensions
                Size imageSize = GetImageDimensions(jpgPath);
                if (imageSize.IsEmpty)
                {
                    RhinoApp.WriteLine("Failed to read image dimensions");
                    return Result.Failure;
                }

                // Calculate scale factors
                double scaleFactor02 = imageSize.Width * scale;
                double scaleFactor03 = imageSize.Height * scale;
                double scaleFactorWidth = scaleFactor01 * scaleFactor02;
                double scaleFactorHeight = scaleFactor01 * scaleFactor03;

                // Create picture frame
                Point3d origin = new Point3d(worldX, worldY - scaleFactorHeight, 0);
                Plane picturePlane = new Plane(origin, Vector3d.XAxis, Vector3d.YAxis);

                doc.Objects.AddPictureFrame(
                    picturePlane,
                    jpgPath,
                    false,
                    scaleFactorWidth,
                    scaleFactorHeight,
                    true,
                    true
                );

                doc.Views.Redraw();
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }

        private bool OpenFileDialog(string title, string filter, out string filePath)
        {
            var fd = new Rhino.UI.OpenFileDialog { Title = title, Filter = filter };
            fd.ShowOpenDialog();
            if (fd.FileName.Length > 0)
            {
                RhinoApp.WriteLine("success");
                filePath = fd.FileName;
                return true;
            }
            filePath = null;
            return false;
        }

        private Size GetImageDimensions(string imagePath)
        {
            try
            {
                using (Image image = Image.FromFile(imagePath))
                {
                    return image.Size;
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error reading image: {ex.Message}");
                return Size.Empty;
            }
        }

        private (double, bool) Scaling(RhinoDoc doc)
        {
            var unitSystem = doc.ModelUnitSystem;
            var imperial = unitSystem != UnitSystem.Millimeters &&
                           unitSystem != UnitSystem.Centimeters &&
                           unitSystem != UnitSystem.Meters &&
                           unitSystem != UnitSystem.Kilometers;

            var targetSystem = imperial ? UnitSystem.Feet : UnitSystem.Meters;
            var scale = RhinoMath.UnitScale(targetSystem, unitSystem );

            return (scale, imperial);
        }
    }
}