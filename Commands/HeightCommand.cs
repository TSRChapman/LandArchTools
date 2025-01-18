using System;
using System.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace LandArchTools.Commands
{
    public class HeightCommand : Rhino.Commands.Command
    {
        public static HeightCommand Instance { get; private set; }

        // Consolidate color definitions to reduce clutter
        private static readonly Color PinkColor = Color.FromArgb(255, 0, 133);
        private static readonly Color BlueColor = Color.FromArgb(82, 187, 209);
        private static readonly Color GreyColor = Color.FromArgb(216, 220, 219);
        private static readonly Color BlackColor = Color.Black;

        public HeightCommand()
        {
            Instance = this;
        }

        public override string EnglishName => "Height";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            GetPoint gp = CreateGetPointInstance("Select first point:");
            if (!TryGetPoint(gp, out Point3d pt01))
                return gp.CommandResult();

            // Register dynamic draw function directly
            gp.DynamicDraw += (sender, e) => DrawDynamicGeometry(e, pt01);

            gp.SetCommandPrompt("Select second point:");
            if (!TryGetPoint(gp, out Point3d pt02))
                return gp.CommandResult();

            string finalHeightString = FormatHeightString(pt02.Z - pt01.Z);
            RhinoApp.WriteLine("Height = " + finalHeightString);
            Clipboard.Instance.Text = finalHeightString;

            return Result.Success;
        }

        private static GetPoint CreateGetPointInstance(string prompt)
        {
            GetPoint gp = new GetPoint();
            gp.SetCommandPrompt(prompt);
            return gp;
        }

        private static bool TryGetPoint(GetPoint getPoint, out Point3d point)
        {
            getPoint.Get();
            if (getPoint.CommandResult() == Result.Success)
            {
                point = getPoint.Point();
                return true;
            }
            point = default;
            RhinoApp.WriteLine("Failed to get point");
            return false;
        }

        private static void DrawDynamicGeometry(GetPointDrawEventArgs e, Point3d pt01)
        {
            Point3d currentPoint = e.CurrentPoint;
            Point3d projectedPoint = new Point3d(currentPoint.X, currentPoint.Y, pt01.Z);
            Line line01 = new Line(pt01, currentPoint);
            Line line02 = new Line(currentPoint, projectedPoint);
            Line line03 = new Line(projectedPoint, pt01);

            // Drawing operations
            e.Display.DrawCircle(new Circle(pt01, line03.Length), BlackColor, 2);
            e.Display.DrawLine(line01, BlueColor, 4);
            e.Display.DrawLine(line02, PinkColor, 5);
            e.Display.DrawLine(line03, BlueColor, 4);
            DrawMeasurementDots(e, line01, line02, line03);
        }

        private static void DrawMeasurementDots(
            GetPointDrawEventArgs e,
            Line line01,
            Line line02,
            Line line03
        )
        {
            e.Display.DrawDot(
                line01.PointAt(0.5),
                $"{Math.Round(line01.Length, 3)}",
                GreyColor,
                BlackColor
            );
            e.Display.DrawDot(
                line02.PointAt(0.5),
                $"{Math.Round(line02.FromZ - line02.ToZ, 3)}",
                GreyColor,
                BlackColor
            );
            e.Display.DrawDot(
                line03.PointAt(0.5),
                $"{Math.Round(line03.Length, 3)}",
                GreyColor,
                BlackColor
            );
        }

        private static string FormatHeightString(double height)
        {
            return $"{Math.Round(height, 3)}";
        }
    }
}
