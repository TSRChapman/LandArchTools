using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LandArchTools.Commands
{
    public class DelOffScreenCommand : Command
    {
        public DelOffScreenCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static DelOffScreenCommand Instance { get; private set; }

        public override string EnglishName => "DelOffScreen";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Select objects to test
                var getObject = new GetObject();
                getObject.SetCommandPrompt("Select objects to test");
                getObject.GroupSelect = true;
                getObject.SubObjectSelect = false;
                getObject.GetMultiple(1, 0);
                if (getObject.CommandResult() != Result.Success)
                    return getObject.CommandResult();

                var objects = getObject.Objects();

                // Ask user if they want to delete or hide objects
                var options = new GetOption();
                options.SetCommandPrompt("Delete or Hide");
                int deleteOptionIndex = options.AddOption("Delete");
                int hideOptionIndex = options.AddOption("Hide");
                options.Get();
                if (options.CommandResult() != Result.Success)
                    return options.CommandResult();

                bool delete = options.OptionIndex() == deleteOptionIndex;

                // Disable screen redraw
                doc.Views.RedrawEnabled = false;

                foreach (var objRef in objects)
                {
                    var obj = objRef.Object().Geometry;
                    if (obj == null) continue;

                    // current active view
                    var activeView = doc.Views.ActiveView;

                    bool isVisible = activeView.MainViewport.IsVisible(obj.GetBoundingBox(false));

                    // If the object is not visible, delete or hide it based on user choice
                    if (!isVisible)
                    {
                        if (delete)
                        {
                            doc.Objects.Delete(objRef, true);
                        }
                        else
                        {
                            doc.Objects.Hide(objRef, true);
                        }
                    }
                }

                // Enable screen redraw
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