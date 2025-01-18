using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;

namespace LandArchTools.Commands
{
    public class IsolateLayerCommand : Command
    {
        public IsolateLayerCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static IsolateLayerCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "IsolateObjLayer";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get objects from user
                ObjRef[] selectedObjects;
                var result = RhinoGet.GetMultipleObjects(
                    "Select objects on layers to isolate",
                    false,
                    ObjectType.AnyObject,
                    out selectedObjects
                );

                if (result != Result.Success || selectedObjects == null || selectedObjects.Length == 0)
                    return Result.Cancel;

                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Get unique layers from selected objects
                var selectedLayerIndices = selectedObjects
                    .Select(objRef => objRef.Object()?.Attributes.LayerIndex)
                    .Where(index => index.HasValue)
                    .Distinct()
                    .ToHashSet();

                // Get all visible objects in the document
                var allObjects = doc.Objects.GetObjectList(new ObjectEnumeratorSettings
                {
                    IncludeLights = true,
                    IncludeGrips = false,
                    NormalObjects = true,
                    LockedObjects = true,
                    HiddenObjects = false
                });

                // Hide objects that are not on the selected layers
                foreach (var obj in allObjects)
                {
                    if (obj != null && !selectedLayerIndices.Contains(obj.Attributes.LayerIndex))
                    {
                        doc.Objects.Hide(obj, true);
                    }
                }

                // Enable redraw and update display
                doc.Views.RedrawEnabled = true;
                doc.Views.Redraw();

                RhinoApp.WriteLine($"Isolated {selectedLayerIndices.Count} layers");
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