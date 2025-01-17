using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace LandArchTools.Commands
{
    public class UnlockAllCommand : Command
    {
        public UnlockAllCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static UnlockAllCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "UnlockAll";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Create enumerator settings
                var settings = new ObjectEnumeratorSettings
                {
                    IncludeLights = true,
                    IncludeGrips = true,
                    NormalObjects = true,
                    LockedObjects = true,
                    HiddenObjects = true,
                    ReferenceObjects = true
                };

                // Get all objects based on settings
                var objects = doc.Objects.GetObjectList(settings);

                // Unlock each object
                foreach (var obj in objects)
                {
                    doc.Objects.Unlock(obj.Id, true);
                }

                // Re-enable redraw and update display
                doc.Views.RedrawEnabled = true;
                doc.Views.Redraw();

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Unable to unlock objects");
                RhinoApp.WriteLine($"Error: {ex.Message}");
                doc.Views.RedrawEnabled = true;
                return Result.Failure;
            }
        }
    }
}