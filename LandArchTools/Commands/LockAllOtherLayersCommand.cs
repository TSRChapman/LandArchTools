using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using System.Linq;

namespace LandArchTools.Commands
{
    public class LockAllOtherLayersCommand : Command
    {
        public LockAllOtherLayersCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static LockAllOtherLayersCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "LockAllOtherLayers";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get object from user
                ObjRef objRef;
                var rc = RhinoGet.GetOneObject(
                    "Select the object on the layer you want to stay unlocked",
                    false,
                    ObjectType.AnyObject,
                    out objRef
                );
                if (rc != Result.Success)
                    return rc;

                // Disable redraw for performance
                doc.Views.RedrawEnabled = false;

                // Get the layer of selected object
                var obj = objRef.Object();
                var layer = obj.Attributes.LayerIndex;

                // Get all objects on the same layer
                var objectsOnLayer = doc.Objects
                    .FindByLayer(doc.Layers[layer])
                    .Select(o => o.Id)
                    .ToList();

                // Get all objects in the document
                var allObjects = doc.Objects
                    .GetObjectList(new ObjectEnumeratorSettings
                    {
                        IncludeLights = false,
                        IncludeGrips = false,
                        ReferenceObjects = false,
                        NormalObjects = true,
                        LockedObjects = false
                    })
                    .Select(o => o.Id)
                    .ToList();

                // Get objects to be locked (all objects except those on the selected layer)
                var objectsToLock = allObjects
                    .Except(objectsOnLayer)
                    .ToList();

                // Create a temporary group
                var groupName = Guid.NewGuid().ToString();
                var groupIndex = doc.Groups.Add(groupName);

                // Add objects to group
                foreach (var id in objectsToLock)
                {
                    doc.Groups.AddToGroup(groupIndex, id);
                }

                // Lock the group
                doc.Groups.Lock(groupIndex);

                // Delete the group (objects remain locked)
                doc.Groups.Delete(groupIndex);

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