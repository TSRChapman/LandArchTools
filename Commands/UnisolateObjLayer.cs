using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace LandArchTools.Commands
{
    public class UnisolateLayerCommand : Command
    {
        public UnisolateLayerCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static UnisolateLayerCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "UnisolateObjLayer";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Get all hidden objects
                var settings = new ObjectEnumeratorSettings
                {
                    IncludeLights = true,
                    IncludeGrips = false,
                    NormalObjects = true,
                    LockedObjects = true,
                    HiddenObjects = true,  // Include hidden objects
                    ReferenceObjects = true
                };

                var hiddenObjects = doc.Objects.GetObjectList(settings);

                // Show all hidden objects
                foreach (var obj in hiddenObjects)
                {
                    if (obj.IsHidden)
                    {
                        doc.Objects.Show(obj, true);
                    }
                }

                // Enable redraw and update display
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
    }
}