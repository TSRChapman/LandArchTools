using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace LandArchTools.Commands
{
    public class CreateStandardLayersCommand : Command
    {
        public CreateStandardLayersCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static CreateStandardLayersCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "CreateStandardLayers";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Disable screen redraw for performance
                doc.Views.RedrawEnabled = false;

                // Define standard layer names
                var layerList = new List<string>
                {
                    "L_BLDS",
                    "L_BNDY_WRKS",
                    "L_EDGE",
                    "L_HARD_ROCK",
                    "L_HARD_WALL",
                    "L_HARD_STEP",
                    "L_HARD_RAMP",
                    "L_HARD_FNCE",
                    "L_HARD_PAV1",
                    "L_HARD_PAV2",
                    "L_HARD_PAV3",
                    "L_HARD_PAV4",
                    "L_LGHT",
                    "L_ENTO",
                    "L_PLAY_EQUI",
                    "L_PLNT",
                    "L_STRU",
                    "L_TEXT",
                    "L_TREE_PROP",
                    "L_TREE_RETN",
                    "L_WALL",
                    "_L_WORKING",
                    "L_SOFT_GRDN",
                    "L_SOFT_MLCH",
                    "L_SOFT_LAWN",
                    "L_SOFT_PLANT",
                    "L_FURN",
                    "_L_OFF"
                };

                // Sort the layer list
                layerList.Sort();

                // Create parent layer
                var parentLayerIndex = doc.Layers.Add("LANDSCAPE", Color.Black);
                var parentLayer = doc.Layers[parentLayerIndex];

                // Create random number generator
                var random = new Random();

                // Add each layer as a child of the LANDSCAPE layer
                foreach (var layerName in layerList)
                {
                    // Generate random color
                    var randomColor = Color.FromArgb(
                        random.Next(256),
                        random.Next(256),
                        random.Next(256)
                    );

                    // Create new layer
                    var layer = new Layer
                    {
                        Name = layerName,
                        ParentLayerId = parentLayer.Id,
                        Color = randomColor,
                        IsVisible = true,
                        IsLocked = false
                    };

                    // Add layer to document
                    doc.Layers.Add(layer);
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