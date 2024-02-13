using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;

namespace LandArchTools.Commands
{
    public class GradeCurveCommand : Command
    {
        public GradeCurveCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static GradeCurveCommand Instance { get; private set; }

        public override string EnglishName => "GradeCurve";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Get the curve from the user
            if (!TryGetCurveFromUser(out Curve curve))
            {
                return Result.Failure;
            }

            // Convert the curve to a NurbsCurve for easier manipulation eg greville points
            NurbsCurve nurbsCurve = curve.ToNurbsCurve();

            // Check if the conversion was successful
            if (nurbsCurve == null)
            {
                RhinoApp.WriteLine("Curve conversion to NurbsCurve failed.");
                return Result.Failure;
            }

            // Get the grade or height from the user
            if (!TryGetUserInput(out double grade, out double height))
            {
                return Result.Failure;
            }

            // Calculate the modified points using either grade or height
            List<Point3d> modifiedPoints = grade > 0 ? CalculateModifiedPointsByGrade(nurbsCurve, grade, doc) : CalculateModifiedPointsByHeight(nurbsCurve, height);

            // Modify the curve and add new curve to the document
            ModifyAndReplaceCurve(doc, curve, modifiedPoints);

            doc.Views.Redraw();
            return Result.Success;
        }


        private bool TryGetCurveFromUser(out Curve curve)
        {
            curve = null;
            Result res = RhinoGet.GetOneObject("Select curve to grade", false, ObjectType.Curve, out ObjRef objRef);

            if (res != Result.Success)
            {
                RhinoApp.WriteLine("No curve selected.");
                return false;
            }

            curve = objRef.Curve();
            return curve != null;
        }

        private bool TryGetUserInput(out double grade, out double height)
        {
            grade = 0;
            height = 0;

            if (RhinoGet.GetNumber("Enter grade ratio number, or ENTER for height", false, ref grade) != Result.Success || grade == 0)
            {
                if (RhinoGet.GetNumber("Enter height", true, ref height) != Result.Success || height == 0)
                {
                    RhinoApp.WriteLine("Invalid input for grade or height.");
                    return false;
                }
            }

            return true;
        }

        private List<Point3d> CalculateModifiedPointsByGrade(NurbsCurve curve, double grade, RhinoDoc doc)
        {
            var modifiedPoints = new List<Point3d>();
            var ctrlPts = curve.Points;

            double startParam = curve.Domain.T0;

            for (int i = 0; i < ctrlPts.Count; i++)
            {
                // Greville points result in less alignment to grade but are more robust in certain cases
                double paramNum = curve.GrevilleParameter(i);
                double length = curve.GetLength(new Interval(startParam, paramNum));
                double rise = length / grade;
                modifiedPoints.Add(new Point3d(ctrlPts[i].X, ctrlPts[i].Y, ctrlPts[i].Z + rise));
            }

            return modifiedPoints;
        }

        private List<Point3d> CalculateModifiedPointsByHeight(NurbsCurve curve, double height)
        {
            var modifiedPoints = new List<Point3d>();
            var ctrlPts = curve.Points;

            for (int i = 0; i < ctrlPts.Count; i++)
            {
                double normParam = (double)i / (ctrlPts.Count - 1);
                double targetHeight = height * normParam;
                modifiedPoints.Add(new Point3d(ctrlPts[i].X, ctrlPts[i].Y, ctrlPts[i].Z + targetHeight));
            }

            return modifiedPoints;
        }

        private void ModifyAndReplaceCurve(RhinoDoc doc, Curve originalCurve, List<Point3d> newGrips)
        {
            var newCurve = originalCurve.ToNurbsCurve();
            if (newCurve == null) return;

            for (int i = 0; i < newGrips.Count && i < newCurve.Points.Count; i++)
            {
                newCurve.Points.SetPoint(i, newGrips[i]);
            }

            doc.Objects.Add(newCurve);
        }


    }
}