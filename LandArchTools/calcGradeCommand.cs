using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using Rhino.DocObjects;
using System.Drawing;

namespace LandArchTools
{
    public class calcGradeCommand : Command
    {
        public calcGradeCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static calcGradeCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "calcGrade";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: start here modifying the behaviour of your command.
            // ---

            try
            {
                (double scale, bool imperial) = Scaling(doc);

                // Get first point
                GetPoint gp = new GetPoint();
                gp.SetCommandPrompt("Select first point");
                gp.Get();
                if (gp.CommandResult() != Result.Success)
                {
                    RhinoApp.WriteLine("Failed to get First Point");
                    return gp.CommandResult();
                }
                Point3d pt1 = gp.Point();

                // Get second point with dynamic draw
                gp.SetCommandPrompt("Select second point");
                gp.DynamicDraw += (sender, e) => GetPointDynamicDrawFunc(sender, e, pt1, scale, imperial, doc);
                gp.Get();
                if (gp.CommandResult() != Result.Success)
                {
                    RhinoApp.WriteLine("Failed to get Second Point");
                    return gp.CommandResult();
                }
                Point3d pt2 = gp.Point();

                CalculateAndDisplayGrade(pt1, pt2, scale, imperial, doc);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Failed to execute: " + ex.Message);
                return Result.Failure;
            }

            // ---
            return Result.Success;
        }

        private void GetPointDynamicDrawFunc(object sender, GetPointDrawEventArgs e, Point3d pt1, double scale, bool imperial, RhinoDoc doc)
        {
            // Similar dynamic drawing logic as in your Python script
            Point3d currentPoint = e.CurrentPoint;
            Point3d projectedPoint = new Point3d(currentPoint.X, currentPoint.Y, pt1.Z);
            Line line01 = new Line(pt1, currentPoint);
            Line line02 = new Line(currentPoint, projectedPoint);
            Line line03 = new Line(projectedPoint, pt1);
            Point3d midPoint01 = line01.PointAt(0.5);
            Circle circle = new Circle(pt1, line03.Length);

            // Define colors
            Color pinkColour = Color.FromArgb(255, 0, 133);
            Color blueColour = Color.FromArgb(82, 187, 209);
            Color greyColour = Color.FromArgb(216, 220, 219);
            Color blackColour = Color.FromArgb(0, 0, 0);

            e.Display.DrawCircle(circle, blackColour, 2);
            e.Display.DrawLine(line01, pinkColour, 4);
            e.Display.DrawLine(line02, blueColour, 5);
            e.Display.DrawLine(line03, blueColour, 4);
            // Grade calculation and display logic here
        }

        private void CalculateAndDisplayGrade(Point3d pt1, Point3d pt2, double scale, bool imperial, RhinoDoc doc)
        {
            // Logic to calculate and display grade similar to your Python script
        }

        private (double, bool) Scaling(RhinoDoc doc)
        {
            UnitSystem unitSystem = doc.ModelUnitSystem;
            bool imperial = unitSystem != UnitSystem.Millimeters && unitSystem != UnitSystem.Centimeters && unitSystem != UnitSystem.Meters;
            double scale = RhinoMath.UnitScale(unitSystem, UnitSystem.Millimeters);
            return (scale, imperial);
        }
    }
}
