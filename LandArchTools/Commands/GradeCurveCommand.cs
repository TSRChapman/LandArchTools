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
            // Get the curve object
            ObjRef objRef;
            Result res = RhinoGet.GetOneObject("Select curve to grade", false, ObjectType.Curve, out objRef);

            if (res != Result.Success || objRef == null)
            {
                RhinoApp.WriteLine("No curve selected.");
                return Result.Failure;
            }

            Curve crv = objRef.Curve();
            NurbsCurve pCrv = crv.ToNurbsCurve();

            if (crv == null)
            {
                RhinoApp.WriteLine("No curve selected.");
                return Result.Failure;
            }

            // Get the grade ratio or height from the user
            double grade = 0;
            double height = 0;
            
            RhinoGet.GetNumber("Enter grade ratio number, or ENTER for height", false, ref grade);
            
            if (grade == 0)
            {
                RhinoGet.GetNumber("Enter height", true, ref height);

                if (height == 0)
                {
                    RhinoApp.WriteLine("No user input.");
                    return Result.Failure;
                }
            }

            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            // get greville points from pCrv
            var ctrlPts = pCrv.GrevillePoints();
            var crvLengths = new List<double>();
            double startParam = pCrv.Domain.T0;

            if (grade > 0)
            {
                for (int i = 0; i < ctrlPts.Count; i++)
                {
                    /*double paramNum = crv.Domain.ParameterAtNormalizedLength(crv.GetLength(new Interval(startParam, ctrlPts[i].Location.Z)) / crv.GetLength());*/
                    double paramNum = pCrv.GrevilleParameter(i);
                    crvLengths.Add(crv.GetLength(new Interval(startParam, paramNum)));
                }

                // Calculate new Z values based on grade
                var newGrips = new List<Point3d>();
                for (int i = 0; i < crvLengths.Count; i++)
                {
                    double rise = crvLengths[i] / grade;
                    Point3d newPt = new Point3d(ctrlPts[i].X, ctrlPts[i].Y, ctrlPts[i].Z + rise);
                    newGrips.Add(newPt);
                }

                // create a copy of the curve in the doc
                var newCrv = pCrv.DuplicateCurve();
                doc.Objects.AddCurve(newCrv);

                // Modify the curve
                ModifyCurve(doc, objRef, newGrips);
            }
            else if (height > 0)
            {
                var newGrips = new List<Point3d>();
                for (int i = 0; i < ctrlPts.Count; i++)
                {
                    double normParam = (double)i / (ctrlPts.Count - 1);
                    double targetHeight = (height * normParam);
                    Point3d newPt = new Point3d(ctrlPts[i].X, ctrlPts[i].Y, ctrlPts[i].Z + targetHeight);
                    newGrips.Add(newPt);
                }

                // create a copy of the curve in the doc
                var newCrv = pCrv.DuplicateCurve();
                doc.Objects.AddCurve(newCrv);

                // Modify the curve
                ModifyCurve(doc, objRef, newGrips);
            }

            doc.Views.Redraw();
            return Result.Success;
        }

        private void ModifyCurve(RhinoDoc doc, ObjRef objRef, List<Point3d> newGrips)
        {
            Curve curve = objRef.Curve();
            if (curve == null) return;

            var newCurve = curve.ToNurbsCurve();
            if (newCurve == null) return;

            for (int i = 0; i < newGrips.Count && i < newCurve.Points.Count; i++)
            {
                newCurve.Points.SetPoint(i, newGrips[i]);
            }

            doc.Objects.Replace(objRef, newCurve);
        }
    }
}