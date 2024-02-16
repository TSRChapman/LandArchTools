using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Input;
using Rhino.DocObjects;
using Rhino.Geometry.Collections;
using System.Collections.Generic;
using Rhino.FileIO;

namespace LandArchTools.Commands
{
    public class ScatterBlocksCommand : Command
    {
        public ScatterBlocksCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static ScatterBlocksCommand Instance { get; private set; }

        public override string EnglishName => "ScatterBlocks";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            try
            {

                // Get user options
                Result rs = getUserOptions(out ObjRef surface, out ObjRef[] blocks, out int numBlocks, out double scale, out bool rotation);



                doc.Views.Redraw();
                return Result.Success;
            }
            catch
            {
                RhinoApp.WriteLine("Failed to execute");
                doc.Views.RedrawEnabled = true;
                return Result.Failure;
            }   
        }

        private static Result getUserOptions(out ObjRef surface, out ObjRef[] blocks, out int numBlocks, out double scale, out bool rotation)
        {
            surface = null;
            blocks = null;
            numBlocks = 0;
            scale = 0;
            rotation = false;

            // Select surface to scatter on
            var gSurface = new GetObject();
            gSurface.SetCommandPrompt("Select surface to scatter on");
            gSurface.GeometryFilter = ObjectType.Surface | ObjectType.Brep | ObjectType.Mesh;
            gSurface.SubObjectSelect = false;
            gSurface.Get();
            if (gSurface.CommandResult() != Result.Success) return gSurface.CommandResult(); 
            surface = gSurface.Object(0);

            // Select blocks to scatter
            var gBlocks = new GetObject();
            gBlocks.SetCommandPrompt("Select blocks to scatter");
            gBlocks.GeometryFilter = ObjectType.InstanceReference;
            gBlocks.GroupSelect = true;
            gBlocks.GetMultiple(1, 0);
            if (gBlocks.CommandResult() != Result.Success) return gBlocks.CommandResult();
            blocks = gBlocks.Objects();

            // get int of blocks to scatter
            var gInt = new GetInteger();
            gInt.SetCommandPrompt("Number of blocks to scatter");
            gInt.AcceptNothing(false);
            gInt.SetDefaultNumber(1);
            gInt.SetLowerLimit(1, false);
            gInt.SetUpperLimit(10000, false);
            gInt.Get();
            if (gInt.CommandResult() != Result.Success) return gInt.CommandResult();
            numBlocks = gInt.Number();

            // get double for scaling of objects
            var gNum = new GetNumber();
            gNum.SetCommandPrompt("enter scale multiplyer (0 for no scaling)");
            gNum.SetDefaultNumber(0);
            gNum.Get();
            if (gNum.CommandResult() != Result.Success) return gNum.CommandResult();
            scale = gNum.Number();

            // get bool for rotation of objects
            var gBool = new GetOption();
            gBool.SetCommandPrompt("random rotation of blocks?");
            var opt1 = gBool.AddOption("Yes");
            var opt2 = gBool.AddOption("No");
            gBool.Get();
            if (gBool.CommandResult() != Result.Success) return gBool.CommandResult();
            if (gBool.Option().Index == opt1) rotation = true;
            else rotation = false;

            return Result.Success;

        }

    }
}