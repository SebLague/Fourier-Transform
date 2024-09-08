using UnityEngine;
using Seb.Types;
using Seb.Helpers;

namespace Seb.Vis.UI.Examples
{
    [ExecuteAlways]
    public class ExampleScrollView : MonoBehaviour
    {
        public Vector2 pos;
        public Vector2 size;
        public int numButtons;
        UIHandle scrollID = new UIHandle("scrollbar");

        ThemeCreator themes = new ThemeCreator(FontType.FiraCodeBold);

        void Update()
        {
            UITheme theme = themes.ThemeA;

            using (UI.CreateFixedAspectUIScope())
            {
                DrawScrollView(theme);
            }
        }


        // Vertical scroll containing buttons
        void DrawScrollView(UITheme theme)
        {
            const float padding = 1;
            const float spacing = 0.5f;

            UI.DrawPanel(pos, size, theme.colBG);
            Bounds2D scrollArea = new(UI.PrevBoundingBox.Min + Vector2.one * padding, UI.PrevBoundingBox.Max - Vector2.one * padding);

            Vector2 buttonTopLeft = scrollArea.TopLeft;
            Bounds2D buttonBounds;
            ScrollBarState scrollState = UI.GetScrollbarState(scrollID);

            using (UI.CreateMaskScope(scrollArea.Min, scrollArea.Max))
            {
                buttonBounds = DrawButtons(draw: false, buttonTopLeft);

                float maxScrollOffsetY = Mathf.Max(0, buttonBounds.Height - scrollArea.Height);
                float scrollOffsetY = maxScrollOffsetY * scrollState.scrollT;
                DrawButtons(draw: true, buttonTopLeft + Vector2.up * scrollOffsetY);
            }

            Vector2 scrollbarMin = scrollArea.BottomRight + Vector2.right * 2;
            Bounds2D scrollbarArea = new Bounds2D(scrollbarMin, scrollbarMin + new Vector2(2, scrollArea.Height));
            scrollState.Scroll(InputHelper.GetMouseScrollDelta() * -0.35f, scrollArea.Height, buttonBounds.Height);
            UI.DrawScrollbar(scrollArea, scrollbarArea, buttonBounds.Height, Color.red, scrollID);

            Bounds2D DrawButtons(bool draw, Vector2 topLeft)
            {
                using (UI.CreateBoundsScope(draw))
                {
                    for (int i = 0; i < numButtons; i++)
                    {
                        bool pressed = UI.DrawButton($"Button {i}", theme.buttonTheme, topLeft, new Vector2(size.x - padding * 2, 0), true, false, true, Anchor.TopLeft);
                        topLeft = UI.PrevBoundingBox.BottomLeft + Vector2.down * spacing;
                        if (pressed) Debug.Log("Pressed " + i);
                    }
                    return UI.GetCurrentBoundsScope();
                }
            }

        }
    }
}