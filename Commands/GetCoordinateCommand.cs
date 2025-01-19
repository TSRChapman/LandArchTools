using System;
using System.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using LandArchTools.Utilities;

namespace LandArchTools.Commands
{
    public class GetCoordinateCommand : Rhino.Commands.Command
    {
        public GetCoordinateCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static GetCoordinateCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "GetCoordinate";

        // Define colors as static readonly fields
        private static readonly Color PinkColor = Color.FromArgb(255, 0, 133);
        private static readonly Color BlueColor = Color.FromArgb(82, 187, 209);
        private static readonly Color BlackColor = Color.FromArgb(0, 0, 0);

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get the scaling factor
                (double scale, bool imperial) = scaleHelper.Scaling(doc);

                // Create and configure the GetPoint instance
                GetPoint gp = new GetPoint();
                gp.SetCommandPrompt("Pick point to get coordinates");
                gp.DynamicDraw += (sender, e) => GetPointDynamicDrawFunc(e, scale);
                gp.Get();

                if (gp.CommandResult() != Result.Success)
                    return gp.CommandResult();

                Point3d pt = gp.Point();
                string coordText = FormatCoordinateText(pt);

                // Add text dot and copy to clipboard
                doc.Objects.AddTextDot(coordText, pt);
                Clipboard.Instance.Text = coordText;

                doc.Views.Redraw();
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }

        private void GetPointDynamicDrawFunc(GetPointDrawEventArgs e, double scale)
        {
            Point3d point = e.CurrentPoint;

            double worldscale;
            e.Viewport.GetWorldToScreenScale(e.CurrentPoint, out worldscale);
            var scaleFactor = (.05 / scale) / worldscale;

            // Draw circle at point
            Circle circle = new Circle(point, scaleFactor);
            e.Display.DrawCircle(circle, PinkColor, 4);

            // Draw extension lines
            double lineLength = circle.Diameter / 2;

            // Vertical lines
            e.Display.DrawLine(
                point,
                new Point3d(point.X, point.Y + lineLength, point.Z),
                BlueColor,
                4
            );
            e.Display.DrawLine(
                point,
                new Point3d(point.X, point.Y - lineLength, point.Z),
                BlueColor,
                4
            );

            // Horizontal lines
            e.Display.DrawLine(
                point,
                new Point3d(point.X + lineLength, point.Y, point.Z),
                BlueColor,
                4
            );
            e.Display.DrawLine(
                point,
                new Point3d(point.X - lineLength, point.Y, point.Z),
                BlueColor,
                4
            );

            // Show tooltip with coordinates
            string coordText = FormatCoordinateText(point);
            RhinoApp.SetCommandPrompt(coordText);
        }

        private string FormatCoordinateText(Point3d pt)
        {
            return $"E {Math.Round(pt.X, 3)} N {Math.Round(pt.Y, 3)} Z {Math.Round(pt.Z, 3)}";
        }
    }
}