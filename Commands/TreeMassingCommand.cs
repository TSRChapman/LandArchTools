using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.DocObjects;
using System.Linq;

namespace LandArchTools.Commands
{
    public class TreeMassingCommand : Command
    {
        public TreeMassingCommand()
        {
            Instance = this;
        }

        public static TreeMassingCommand Instance { get; private set; }

        public override string EnglishName => "TreeMassing";

        // Dictionary for litre size to pot Rootball properties
        // [0] = Rootball Diameter, [1] = Rootball Height, [2] = Calliper, [3] = Height, [4] = Spread
        private static readonly Dictionary<int, double[]> PotDict = new Dictionary<int, double[]>
        {
            { 25, new[] { 0.300, 0.250, 0.020, 1.000, 0.500 } },
            { 45, new[] { 0.420, 0.350, 0.025, 2.000, 1.000 } },
            { 75, new[] { 0.465, 0.500, 0.035, 2.500, 2.000 } },
            { 100, new[] { 0.520, 0.560, 0.050, 3.500, 2.000 } },
            { 200, new[] { 0.700, 0.625, 0.070, 4.500, 3.000 } },
            { 400, new[] { 0.980, 0.715, 0.090, 6.000, 4.000 } },
            { 600, new[] { 1.200, 0.600, 0.100, 6.000, 5.000 } },
            { 800, new[] { 1.300, 0.600, 0.120, 7.000, 5.000 } },
            { 1000, new[] { 1.500, 0.600, 0.150, 8.000, 5.000 } },
            { 2000, new[] { 2.000, 0.800, 0.200, 9.000, 5.000 } }
        };

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get user inputs
                double litre = 400;
                RhinoGet.GetNumber("Enter the root ball litres, max 2000 Litres", true, ref litre, 0, 2000);
       
                double soilDepth = 0.8;
                RhinoGet.GetNumber("Enter the soil depth available in m", true, ref soilDepth, 0.1, 10);
                
                double matureHeight = 5.0;
                RhinoGet.GetNumber("Enter the mature tree height in m", true, ref matureHeight, 0.1, 100);
                
                double dbh = 0;
                RhinoGet.GetNumber("Enter the DBH at maturity in m, if unknown hit Enter", false, ref dbh);

                Point3d userPt;
                var rc = RhinoGet.GetPoint(
                    "Pick a point to place rootball",
                    false,
                    out userPt
                );
                if (rc != Result.Success)
                    return rc;

                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Get scale factor based on unit system
                double scaleFactor = GetScaleFactor(doc);
                if (scaleFactor == 0)
                {
                    RhinoApp.WriteLine("This tool can only be used in mm, cm or m model units");
                    return Result.Failure;
                }

                // Calculate soil requirements
                if (dbh == 0)
                {
                    dbh = ((matureHeight / 100) * 4) * 1000; // Gives a DBH in mm
                }

                double reqSoil = (matureHeight * dbh) / 100; // Required soil volume in m³
                double reqSoilRadius = Math.Sqrt(reqSoil / (Math.PI * soilDepth));

                // Create soil cylinder
                var soilCylinder = new Cylinder(
                    new Circle(userPt, reqSoilRadius * scaleFactor),
                    soilDepth * scaleFactor);
                var soilBrep = soilCylinder.ToBrep(true, true);
                var soilId = doc.Objects.AddBrep(soilBrep);
                doc.Objects.FindId(soilId).Attributes.ObjectColor = Color.FromArgb(150, 75, 0);

                // Get closest matching pot size
                int litreMatch = GetClosestPotSize((int)litre);
                double dia = PotDict[litreMatch][0];
                double height = PotDict[litreMatch][1];

                // Create rootball
                var rootballPt = userPt + new Vector3d(0, 0, (soilDepth - height) * scaleFactor);
                var rootballCyl = new Cylinder(
                    new Circle(rootballPt, (dia / 2) * scaleFactor),
                    height * scaleFactor);
                var rootballBrep = rootballCyl.ToBrep(true, true);
                var rootballId = doc.Objects.AddBrep(rootballBrep);
                doc.Objects.FindId(rootballId).Attributes.ObjectColor = Color.FromArgb(0, 128, 0);

                // Create tree trunk and canopy
                double calliper = PotDict[litreMatch][2];
                double treeHeight = PotDict[litreMatch][3];
                double spread = PotDict[litreMatch][4];

                var trunkPt = rootballPt + new Vector3d(0, 0, height * scaleFactor);
                var trunkCyl = new Cylinder(
                    new Circle(trunkPt, calliper * scaleFactor),
                    treeHeight * scaleFactor);
                var trunkBrep = trunkCyl.ToBrep(true, true);
                var trunkId = doc.Objects.AddBrep(trunkBrep);
                doc.Objects.FindId(trunkId).Attributes.ObjectColor = Color.FromArgb(101, 67, 33);

                var canopyCenter = trunkPt + new Vector3d(0, 0, treeHeight * scaleFactor - (spread / 2) * scaleFactor);
                var canopySphere = new Sphere(canopyCenter, (spread / 2) * scaleFactor);
                var canopyBrep = canopySphere.ToBrep();
                var canopyId = doc.Objects.AddBrep(canopyBrep);
                doc.Objects.FindId(canopyId).Attributes.ObjectColor = Color.FromArgb(33, 101, 67);

                // Add text annotations
                var text1 = $"Rootball Height = {height * scaleFactor}, Diameter = {dia * scaleFactor}";
                var text2 = $"Soil Volume Requirement = {reqSoil} m³";

                var textDot1 = new TextDot(text1, userPt);
                var textDot2 = new TextDot(text2, new Point3d(userPt.X, userPt.Y - 0.2 * scaleFactor, userPt.Z));

                doc.Objects.AddTextDot(textDot1);
                doc.Objects.AddTextDot(textDot2);

                // Re-enable redraw
                doc.Views.RedrawEnabled = true;
                doc.Views.Redraw();

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                doc.Views.RedrawEnabled = true;
                return Result.Failure;
            }
        }

        private static double GetScaleFactor(RhinoDoc doc)
        {
            var units = doc.ModelUnitSystem;
            switch (units)
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

        private static int GetClosestPotSize(int targetSize)
        {
            
            int closest = PotDict.Keys.ToArray().OrderBy(x => Math.Abs(x - targetSize)).First(); ;
            int minDiff = Math.Abs(targetSize - closest);

            foreach (var size in PotDict.Keys)
            {
                int diff = Math.Abs(targetSize - size);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = size;
                }
            }

            return closest;
        }
    }
}