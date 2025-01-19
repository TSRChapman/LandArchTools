using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;

namespace LandArchTools.Commands
{
    public class TurnOffSelectedLayerCommand : Command
    {
        public TurnOffSelectedLayerCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static TurnOffSelectedLayerCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "TurnOffSelectedLayer";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get objects from user
                var rc = RhinoGet.GetMultipleObjects(
                    "Select objects on layers to turn off",
                    false,
                    ObjectType.AnyObject,
                    out ObjRef[] objRefs
                );

                if (rc != Result.Success || objRefs == null || objRefs.Length == 0)
                    return rc;

                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Store processed layer indices to avoid duplicate operations
                var processedLayerIndices = new System.Collections.Generic.HashSet<int>();

                foreach (var objRef in objRefs)
                {
                    var obj = objRef.Object();
                    if (obj == null) continue;

                    var layerIndex = obj.Attributes.LayerIndex;

                    // Skip if we've already processed this layer
                    if (!processedLayerIndices.Add(layerIndex))
                        continue;

                    var layer = doc.Layers.FindIndex(layerIndex);
                    if (layer != null)
                    {
                        layer.IsVisible = false;
                    }
                }

                // Re-enable redraw and refresh views
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