using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Geometry;
using LandArchTools.Utilities;

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
        public override string EnglishName => "ImportNearMaps";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                (double scale, bool imperial) = scaleHelper.Scaling(doc);
                if (scale == 0)
                {
                    RhinoApp.WriteLine("This tool can only be used in mm, cm or m model units");
                    return Result.Failure;
                }

                // Get JGW file
                string jgwFilePath;
                if (!RhinoGet.GetFileName(out jgwFilePath, OpenFileMode.Open, "Select JGW file", "JGW Files (*.JGW)|*.JGW"))
                    return Result.Cancel;

                // Parse JGW file
                string[] jgwLines = File.ReadAllLines(jgwFilePath);
                double scaleFactor01 = double.Parse(jgwLines[0]);
                double worldX = double.Parse(jgwLines[4]) * scale;
                double worldY = double.Parse(jgwLines[5]) * scale;

                // Get JPG file
                string jpgFilePath;
                if (!RhinoGet.GetFileName(out jpgFilePath, OpenFileMode.Open, "Select JPG file", "JPG Files (*.JPG)|*.JPG"))
                    return Result.Cancel;

                // Get image dimensions
                Size imageSize = GetImageDimensions(jpgFilePath);
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
                    jpgFilePath,
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

        private Size GetImageDimensions(string imagePath)
        {
            try
            {
                using (var image = Image.FromFile(imagePath))
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
    }
}