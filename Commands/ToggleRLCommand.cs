using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace LandArchTools.Commands
{
    public class ToggleRLCommand : Command
    {
        public ToggleRLCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static ToggleRLCommand Instance { get; private set; }

        public override string EnglishName => "ToggleLevels";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            ToggleLevels(doc);
            return Result.Success;
        }

        private void ToggleLevels(RhinoDoc doc)
        {
            var settings = new ObjectEnumeratorSettings
            {
                IncludeLights = false,
                IncludeGrips = false,
                NormalObjects = true,
                LockedObjects = true,
                HiddenObjects = true,
                ReferenceObjects = false,
                ObjectTypeFilter = ObjectType.TextDot // Get text dots only
            };

            bool? shouldBeHidden = null;
            foreach (var obj in doc.Objects.GetObjectList(settings))
            {
                if (obj is TextDotObject textDotObj)
                {
                    var stringCheck = textDotObj.Attributes.GetUserString("LandArchTools");
                    if (stringCheck == "RLTextDot")
                    {
                        if (!shouldBeHidden.HasValue)
                        {
                            // Determine the action based on the first matching object's visibility
                            shouldBeHidden = !textDotObj.IsHidden;
                        }
                        // Apply the determined action
                        if (shouldBeHidden.Value)
                        {
                            doc.Objects.Hide(textDotObj.Id, true);
                        }
                        else
                        {
                            doc.Objects.Show(textDotObj.Id, true);
                        }
                    }
                }
            }

            doc.Views.Redraw();
        }
    }
}
