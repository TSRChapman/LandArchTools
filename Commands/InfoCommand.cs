using System;
using System.Diagnostics;
using Rhino;
using Rhino.Commands;

namespace LandArchTools.Commands
{
    public class InfoCommand : Command
    {
        public InfoCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static InfoCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "LATInfo";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.landarchtools.com/",
                    UseShellExecute = true
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to open website: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}