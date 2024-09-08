using UnityEngine;
using System;
using Seb.Vis;
using Seb.Vis.UI;

namespace Seb.Readme
{
    // VERY WORK IN PROGRESS!!
    public static class SebVis_Readme
    {

        //  -- DRAWING --
        static void Draw_Info()
        {
            // -- DRAWING --
            // Start drawing text/shapes by starting a new Layer.
            // When a layer is rendered, all the 2D shapes will be drawn first (in the order they were submitted), and all text 
            // for that layer willbe drawn afterwards. This means that it is impossible for a 2D shape to be drawn on top of text.
            // If this behaviour is required, a new layer must be started. There is some overhead to having multiple layers,
            // so this should be done reasonably sparingly.
            Draw.StartLayer(offset: Vector2.zero, scale: 1, useScreenSpace: false);

        }

        // -- UI --
        static void UI_Info()
        {
            // Start drawing UI elements by creating a UI scope.
            // Inside this scope, positions and sizes will be given in UISpace.
            // In this space, the canvas always has a width of 100 units (with the height then depending on the given aspect ratio).
            using (UI.CreateFixedAspectUIScope(16, 9, drawLetterbox: true))
            {

            };
        }

        static Type[] dependencies =
        {
            typeof(Seb.Types.Bounds2D),
            typeof(Seb.Helpers.InputHelper),
            typeof(Seb.Helpers.ComputeHelper)
        };
    }
}