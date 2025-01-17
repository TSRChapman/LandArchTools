using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;

namespace LandArchTools.Commands
{
    public class SelectObjsOnLayerCommand : Command
    {
        public SelectObjsOnLayerCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SelectObjsOnLayerCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SelectObjsOnLayer";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get the initial object to determine the layer
                ObjRef objRef;
                Result result = RhinoGet.GetOneObject(
                    "Select object to select all on layer",
                    false,
                    ObjectType.AnyObject,
                    out objRef
                );

                if (result != Result.Success)
                    return result;

                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Get the layer of the selected object
                var obj = objRef.Object();
                var layerIndex = obj.Attributes.LayerIndex;

                // Create settings for object enumeration
                var settings = new ObjectEnumeratorSettings
                {
                    IncludeLights = false,
                    IncludeGrips = false,
                    NormalObjects = true,
                    LockedObjects = true,
                    HiddenObjects = true,
                    ReferenceObjects = true
                };

                // Select all objects on the same layer
                foreach (var rhObj in doc.Objects.GetObjectList(settings))
                {
                    if (rhObj.Attributes.LayerIndex == layerIndex)
                    {
                        rhObj.Select(true);
                    }
                }

                // Enable redraw and update the view
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