using System;
using System.Drawing;
using Eto.Forms;
using LandArchTools.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace LandArchTools.Commands
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
                var (scale, imperial) = scaleHelper.Scaling(doc);

                var gp = new GetPoint();
                gp.DynamicDraw += (sender, args) => GetPointDynamicDrawFunc(sender, args, scale, imperial);
                gp.SetCommandPrompt("Select point to show RL");
                gp.Get();

                if (gp.CommandResult() != Result.Success)
                    return gp.CommandResult();

                var point = gp.Point();
                var pointZ = point.Z * scale;
                var unit = imperial ? "ft" : "m";
                var rlText = $"+RL {Math.Round(pointZ, 3)} {unit}";

                var textDot = new TextDot(rlText, point);
                var textDotId = doc.Objects.AddTextDot(textDot);
                doc.Objects.FindId(textDotId).Attributes.SetUserString("LandArchTools", "RLTextDot");
                doc.Views.Redraw();
                Clipboard.Instance.Text = rlText;

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }

        private void GetPointDynamicDrawFunc(object sender, GetPointDrawEventArgs e, double scale, bool imperial)
        {
            var scaleFactor = 1 / scale;
            var point = e.CurrentPoint;
            var circle = new Circle(point, scaleFactor);
            var lines = new[]
            {
                new Line(point, point + new Vector3d(scaleFactor, 0, 0)),
                new Line(point, point - new Vector3d(scaleFactor, 0, 0)),
                new Line(point, point + new Vector3d(0, scaleFactor, 0)),
                new Line(point, point - new Vector3d(0, scaleFactor, 0))
            };

            e.Display.DrawCircle(circle, Color.FromArgb(255, 0, 133), 2);
            foreach (var line in lines)
            {
                e.Display.DrawLine(line, Color.FromArgb(82, 187, 209), 4);
            }

            var unit = imperial ? "ft" : "m";
            var rl = $"+RL {Math.Round(point.Z * scale, 3)} {unit}";
            e.Display.DrawDot(point + new Vector3d(0, 0, 0.5 * scaleFactor), rl, Color.FromArgb(216, 220, 219), Color.Black);
        }

    }
}
