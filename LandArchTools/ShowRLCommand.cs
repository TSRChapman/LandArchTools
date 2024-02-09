using System;
using System.Drawing;
using Eto.Forms;
using LandArchTools.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace LandArchTools
{
    public class ShowRLCommand : Rhino.Commands.Command
    {
        public ShowRLCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static ShowRLCommand Instance { get; private set; }

        public override string EnglishName => "ShowRL";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get scale and units used in the document
                (double scale, bool imperial) = scaleHelper.Scaling(doc);

                // Get point with dynamic draw
                var gp = new GetPoint();
                gp.DynamicDraw += (sender, args) =>
                    GetPointDynamicDrawFunc(sender, args, scale, imperial, doc);
                gp.SetCommandPrompt("Select point to show RL");
                gp.Get();

                if (gp.CommandResult() != Result.Success)
                    return gp.CommandResult();

                var point = gp.Point();

                double pointZ = point.Z * scale;
                string rlText;

                if (!imperial)
                {
                    rlText = $"+RL {Math.Round(pointZ, 3)} m";
                }
                else
                {
                    rlText = $"+RL {Math.Round(pointZ, 3)} ft";
                }

                var textDot = new TextDot(rlText, point);
                var textDotId = doc.Objects.AddTextDot(textDot);
                doc.Objects.FindId(textDotId)
                    .Attributes.SetUserString("LandArchTools", "RLTextDot");
                doc.Views.Redraw();

                // Copy RL to Clipboard
                Clipboard.Instance.Text = rlText;
                

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }

        private void GetPointDynamicDrawFunc(
            object sender,
            GetPointDrawEventArgs e,
            double scale,
            bool imperial,
            RhinoDoc doc
        )
        {
            // print to the command line the scale and imperial status
            RhinoApp.WriteLine($"Scale: {scale}, Imperial: {imperial}");


            var point = e.CurrentPoint;
            var circle = new Circle(point, 1 / scale);
            var line01 = new Line(point, new Point3d(point.X + (1 / scale), point.Y, point.Z));
            var line02 = new Line(point, new Point3d(point.X - (1 / scale), point.Y, point.Z));
            var line03 = new Line(point, new Point3d(point.X, point.Y + (1 / scale), point.Z));
            var line04 = new Line(point, new Point3d(point.X, point.Y - (1 / scale), point.Z));

            var pinkColour = Color.FromArgb(255, 0, 133);
            var blueColour = Color.FromArgb(82, 187, 209);
            var greyColour = Color.FromArgb(216, 220, 219);
            var blackColour = Color.FromArgb(0, 0, 0);

            e.Display.DrawCircle(circle, pinkColour, 2);
            e.Display.DrawLine(line01, blueColour, 4);
            e.Display.DrawLine(line02, blueColour, 4);
            e.Display.DrawLine(line03, blueColour, 4);
            e.Display.DrawLine(line04, blueColour, 4);

            string rl;
            if (!imperial)
            {
                rl = $"+RL {Math.Round(point.Z * scale, 3)} m";
            }
            else
            {
                rl = $"+RL {Math.Round(point.Z * scale, 3)} ft";
            }


            e.Display.DrawDot((new Point3d(e.CurrentPoint.X, e.CurrentPoint.Y, (e.CurrentPoint.Z + 0.5 / scale))), rl, greyColour, blackColour);
        }

    }
}
