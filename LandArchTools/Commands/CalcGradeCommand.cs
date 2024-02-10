﻿using System;
using System.Collections.Generic;
using System.Drawing;
/* Unmerged change from project 'LandArchTools (net7.0)'
Before:
using LandArchTools.Utilities;
After:
using LandArchTools.Utilities;
using LandArchTools;
using LandArchTools.Commands;
*/
using LandArchTools.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LandArchTools.Commands
{
    public class CalcGradeCommand : Command
    {
        public CalcGradeCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static CalcGradeCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "calcGrade";

        // Define colors as static readonly fields
        private static readonly Color PinkColor = Color.FromArgb(255, 0, 133);
        private static readonly Color BlueColor = Color.FromArgb(82, 187, 209);
        private static readonly Color GreyColor = Color.FromArgb(216, 220, 219);
        private static readonly Color BlackColor = Color.FromArgb(0, 0, 0);

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Main Command Logic
            try
            {
                (double scale, bool imperial) = scaleHelper.Scaling(doc);

                Point3d pt1 = GetPoint("Select first point");
                if (pt1 == Point3d.Unset)
                    return Result.Failure;

                Point3d pt2 = GetPoint(
                    "Select second point",
                    (sender, e) => GetPointDynamicDrawFunc(sender, e, pt1, scale, imperial, doc)
                );
                if (pt2 == Point3d.Unset)
                    return Result.Failure;

                CalculateAndDisplayGrade(pt1, pt2, scale, imperial, doc);

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }

        private Point3d GetPoint(
            string commandPrompt,
            EventHandler<GetPointDrawEventArgs> dynamicDrawFunc = null
        )
        {
            GetPoint gp = new GetPoint();
            gp.SetCommandPrompt(commandPrompt);
            if (dynamicDrawFunc != null)
            {
                gp.DynamicDraw += dynamicDrawFunc;
            }
            gp.Get();
            return gp.CommandResult() == Result.Success ? gp.Point() : Point3d.Unset;
        }

        private void GetPointDynamicDrawFunc(
            object sender,
            GetPointDrawEventArgs e,
            Point3d pt1,
            double scale,
            bool imperial,
            RhinoDoc doc
        )
        {
            Point3d currentPoint = e.CurrentPoint;
            Line line01 = new Line(pt1, currentPoint);
            e.Display.DrawLine(line01, PinkColor, 4);

            string gradeText = CalculateGradeText(pt1, currentPoint, scale, imperial);
            Point3d midPoint01 = line01.PointAt(0.5);
            e.Display.DrawDot(midPoint01, gradeText, GreyColor, BlackColor);
        }

        private string CalculateGradeText(Point3d pt1, Point3d pt2, double scale, bool imperial)
        {
            double rise = Math.Abs(pt1.Z - pt2.Z) * scale;
            double run = pt1.DistanceTo(pt2) * scale;
            // Calculate the rise and run in percentage and ratio, catch divide by zero
            double rGrade = rise != 0 ? run / rise : 0;
            double pGrade = rGrade = rGrade != 0 ? 1 / rGrade * 100 : 0;

            return imperial
                ? $"{Math.Abs(Math.Round(pGrade, 2))}% Grade"
                : $"1:{Math.Abs(Math.Round(rGrade, 2))} / {Math.Abs(Math.Round(pGrade, 2))}% Grade";
        }

        private void CalculateAndDisplayGrade(
            Point3d pt1,
            Point3d pt2,
            double scale,
            bool imperial,
            RhinoDoc doc
        )
        {
            string gradeText = CalculateGradeText(pt1, pt2, scale, imperial);
            Point3d midpoint = new Line(pt1, pt2).PointAt(0.5);
            doc.Objects.AddTextDot(gradeText, midpoint);
            doc.Views.Redraw();
        }
    }
}
