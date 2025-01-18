using System;
using Rhino;
using Rhino.Commands;

namespace LandArchTools.Commands
{
    public class SetTopCommand : Command
    {
        public SetTopCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SetTopCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SetTop";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Execute the SetView command with Top view parameters
                RhinoApp.RunScript("_SetView _World _Top", false);

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