using System;
using System.IO;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.UI;

namespace LandArchTools.Commands
{
    public class ImportNearMapsCommand : Command
    {
        public ImportNearMapsCommand()
        {
            Instance = this;
        }

        public static ImportNearMapsCommand Instance { get; private set; }

        public override string EnglishName => "ImportNearMaps";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Check unit system
                double scaleFactor = GetScaleFactor(doc.ModelUnitSystem);
                if (scaleFactor == 0)
                {
                    RhinoApp.WriteLine("This tool can only be used in mm, cm or m model units");
                    return Result.Failure;
                }

                // Get JGW file
                string jgwPath = "";
                if (!GetJgwFile(ref jgwPath))
                    return Result.Cancel;

                // Read JGW file
                var jgwData = ReadJgwFile(jgwPath);
                if (jgwData == null)
                    return Result.Failure;

                // Calculate world coordinates
                double worldX = jgwData.X * scaleFactor;
                double worldY = jgwData.Y * scaleFactor;

                // Get JPG file
                string jpgPath = "";
                if (!GetJpgFile(ref jpgPath))
                    return Result.Cancel;

                // Get image dimensions
                var imageSize = GetImageDimensions(jpgPath);
                if (imageSize == null)
                    return Result.Failure;

                // Calculate final dimensions
                double scaleFactorWidth = jgwData.ScaleFactor * imageSize.Width * scaleFactor;
                double scaleFactorHeight = jgwData.ScaleFactor * imageSize.Height * scaleFactor;

                // Create picture frame
                Point3d origin = new Point3d(worldX, worldY - scaleFactorHeight, 0);
                Plane plane = new Plane(origin, Vector3d.XAxis, Vector3d.YAxis);

                // Add picture frame to document
                var pictureFrame = CreatePictureFrame(plane, jpgPath, scaleFactorWidth, scaleFactorHeight);
                if (pictureFrame == null)
                    return Result.Failure;

                doc.Objects.AddPictureFrame(pictureFrame);
                doc.Views.Redraw();

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }

        private double GetScaleFactor(UnitSystem unitSystem)
        {
            switch (unitSystem)
            {
                case UnitSystem.Millimeters:
                    return 1000;
                case UnitSystem.Centimeters:
                    return 100;
                case UnitSystem.Meters:
                    return 1;
                default:
                    return 0;
            }
        }

        private bool GetJgwFile(ref string filePath)
        {
            var fd = new Rhino.UI.OpenFileDialog
            {
                Filter = "JGW Files (*.jgw)|*.jgw||",
                Title = "Select .JGW file"
            };

            if (!fd.ShowOpenDialog())
                return false;

            filePath = fd.FileName;
            return true;
        }

        private bool GetJpgFile(ref string filePath)
        {
            var fd = new Rhino.UI.OpenFileDialog
            {
                Filter = "JPG Files (*.jpg)|*.jpg||",
                Title = "Select .JPG image file"
            };

            if (!fd.ShowOpenDialog())
                return false;

            filePath = fd.FileName;
            return true;
        }

        private class JgwData
        {
            public double ScaleFactor { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        private JgwData ReadJgwFile(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length < 6)
                    return null;

                return new JgwData
                {
                    ScaleFactor = double.Parse(lines[0]),
                    X = double.Parse(lines[4]),
                    Y = double.Parse(lines[5])
                };
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error reading JGW file: {ex.Message}");
                return null;
            }
        }

        private class ImageSize
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private ImageSize GetImageDimensions(string imagePath)
        {
            try
            {
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        // Read file header (24 bytes)
                        byte[] header = reader.ReadBytes(24);
                        if (header.Length != 24)
                            return null;

                        // Check file type and extract dimensions
                        if (IsJpeg(header))
                        {
                            return GetJpegDimensions(stream);
                        }
                        else if (IsPng(header))
                        {
                            return GetPngDimensions(header);
                        }
                        else if (IsGif(header))
                        {
                            return GetGifDimensions(header);
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error reading image dimensions: {ex.Message}");
                return null;
            }
        }

        private bool IsJpeg(byte[] header)
        {
            return header[0] == 0xFF && header[1] == 0xD8;
        }

        private bool IsPng(byte[] header)
        {
            return header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        }

        private bool IsGif(byte[] header)
        {
            return header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46;
        }

        private ImageSize GetJpegDimensions(Stream stream)
        {
            try
            {
                stream.Position = 0;
                byte[] buffer = new byte[2];
                stream.Read(buffer, 0, 2); // Read initial bytes

                while (buffer[0] == 0xFF)
                {
                    stream.Read(buffer, 0, 2);
                    if ((buffer[0] >= 0xC0 && buffer[0] <= 0xCF) && buffer[0] != 0xC4 && buffer[0] != 0xC8)
                    {
                        stream.Seek(3, SeekOrigin.Current);
                        buffer = new byte[4];
                        stream.Read(buffer, 0, 4);
                        return new ImageSize
                        {
                            Height = (buffer[0] << 8) | buffer[1],
                            Width = (buffer[2] << 8) | buffer[3]
                        };
                    }
                    else
                    {
                        stream.Read(buffer, 0, 2);
                        int size = (buffer[0] << 8) | buffer[1];
                        stream.Seek(size - 2, SeekOrigin.Current);
                        stream.Read(buffer, 0, 2);
                    }
                }
            }
            catch { }
            return null;
        }

        private ImageSize GetPngDimensions(byte[] header)
        {
            return new ImageSize
            {
                Width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19],
                Height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23]
            };
        }

        private ImageSize GetGifDimensions(byte[] header)
        {
            return new ImageSize
            {
                Width = header[6] | (header[7] << 8),
                Height = header[8] | (header[9] << 8)
            };
        }

        private PictureFrame CreatePictureFrame(Plane plane, string imagePath, double width, double height)
        {
            try
            {
                return new PictureFrame(plane, imagePath, false, width, height, 1, false);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error creating picture frame: {ex.Message}");
                return null;
            }
        }
    }
}