using System;
using Rhino;
using Rhino.Commands;

namespace LandArchTools.Commands
{
    public class SetPerspectiveCommand : Command
    {
        public SetPerspectiveCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SetPerspectiveCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SetPerspective";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Execute the SetView command with Top view parameters
                RhinoApp.RunScript("_SetView _World _Perspective", false);

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}