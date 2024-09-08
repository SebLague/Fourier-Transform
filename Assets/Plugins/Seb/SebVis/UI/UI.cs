using UnityEngine;
using System;
using Seb.Vis.Internal;
using System.Collections.Generic;
using Seb.Helpers;
using Seb.Types;

namespace Seb.Vis.UI
{
    public static class UI
    {
        const string MString = "M";
        static readonly Pool<LayoutScope> layoutPool = new();

        static UIScope currUIScope => uiScopes.CurrentScope;
        static Vector2 canvasBottomLeft => currUIScope.canvasBottomLeft;

        public const float Width = 100;
        public const float HalfWidth = Width / 2;
        public static float Height => Width * currUIScope.aspect;
        public static float HalfHeight => Height * 0.5f;
        public static Vector2 Centre => new Vector2(Width, Height) * 0.5f;

        // Canvas size in screenspace
        static Vector2 canvasSize => currUIScope.canvasSize;
        static Vector2 screenSize => currUIScope.screenSize;
        static float scale => currUIScope.scale; // Scale to go from UISpace to ScreenSpace
        static float invScale => currUIScope.invScale;

        static Stack<LayoutScope> layoutStack = new();

        static Scopes<BoundsScope> boundsScopes = new();
        static Scopes<UIScope> uiScopes = new();
        //static Stack<BoundsScope> uiStack = new();

        static LayoutScope.Kind ActiveLayoutKind => layoutStack.Count > 0 ? layoutStack.Peek().kind : LayoutScope.Kind.None;

        public static Bounds2D PrevBoundingBox;

        static readonly Dictionary<UIHandle, InputFieldState> inputFieldStates = new();
        static readonly Dictionary<UIHandle, ButtonState> buttonStates = new();
        static readonly Dictionary<UIHandle, ColourPickerState> colPickerStates = new();
        static readonly Dictionary<UIHandle, ScrollBarState> scrollbarStates = new();

        //  --------------------------- UI Scope functions ---------------------------

        // Begin drawing full-screen UI
        public static UIScope CreateUIScope()
        {
            return CreateUIScope(Vector2.zero, new Vector2(Screen.width, Screen.height), false);
        }

        // Begin drawing UI with fixed aspect ratio. If aspect doesn't match screen aspect, letterboxes can optionally be displayed.
        public static UIScope CreateFixedAspectUIScope(int aspectX = 16, int aspectY = 9, bool drawLetterbox = true)
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            // Display size is narrower than desired aspect, must add top/bottom bars
            if (Screen.width * aspectY < Screen.height * aspectX)
            {
                float canvasWidth = Screen.width;
                float canvasHeight = canvasWidth * aspectY / (float)aspectX;
                float bottomY = (Screen.height - canvasHeight) / 2f;
                return CreateUIScope(new Vector2(0, bottomY), new(canvasWidth, canvasHeight), drawLetterbox);
            }
            // Display size is wider than desired aspect, must add left/right bars
            else if (Screen.width * aspectY > Screen.height * aspectX)
            {
                float canvasHeight = Screen.height;
                float canvasWidth = canvasHeight * aspectX / (float)aspectY;
                float leftX = (Screen.width - canvasWidth) / 2f;
                return CreateUIScope(new Vector2(leftX, 0), new(canvasWidth, canvasHeight), drawLetterbox);
            }
            // Display size has desired aspect ratio, no bars required
            else return CreateUIScope(Vector2.zero, screenSize, false);
        }

        // Begin drawing UI with custom size and position on the screen
        public static UIScope CreateUIScope(Vector2 bottomLeft, Vector2 size, bool drawLetterboxes)
        {
            UIScope scope = uiScopes.CreateScope();
            scope.canvasBottomLeft = bottomLeft;
            scope.canvasSize = size;
            scope.screenSize = new Vector2(Screen.width, Screen.height);
            scope.scale = size.x / 100f;
            scope.invScale = 1f / scope.scale;
            scope.drawLetterboxes = drawLetterboxes;
            scope.aspect = size.y / size.x;
            Draw.StartLayer(Vector2.zero, 1, true);

            return scope;
        }

        public static void StartNewLayer()
        {
            Draw.StartLayer(Vector2.zero, 1, true);
        }

        // WIP!
        public static LayoutScope CreateLayoutScope(LayoutScope.Kind kind, Vector2 pos, float spacing)
        {
            LayoutScope layout = layoutPool.GetNextAvailableOrCreate();
            layout.Init(kind, pos, spacing);
            layoutStack.Push(layout);
            return layout;
        }

        // Creates a scope in which the bounding box of all UI elements is tracked.
        // If draw is set to false, elements will not be rendered; only the bounds will be calculated.
        // Usage: using (UI.CreateBoundsScope(draw = true)) { var bounds = UI.GetCurrentBoundsScope(); }
        public static BoundsScope CreateBoundsScope(bool draw)
        {
            BoundsScope scope = boundsScopes.CreateScope();
            scope.Init(draw);
            return scope;
        }

        public static Draw.MaskScope CreateMaskScope(Vector2 maskMin, Vector2 maskMax)
        {
            return Draw.CreateMaskScope(UIToScreenSpace(maskMin), UIToScreenSpace(maskMax));
        }

        public static Draw.MaskScope CreateMaskScope(Bounds2D bounds)
        {
            return CreateMaskScope(bounds.Min, bounds.Max);
        }

        // Get the bounding box of the current bounds scope.
        // Note: a scope must have been created with CreateBoundsScope()
        public static Bounds2D GetCurrentBoundsScope() => boundsScopes.CurrentScope.GetBounds();

        //  --------------------------- Draw functions ---------------------------

        public static void DrawSlider(Vector2 pos, Vector2 size, Anchor anchor, ref SliderState state)
        {
            Vector2 centre = CalculateCentre(pos, size, anchor, true);
            (Vector2 centre, Vector2 size) ss = UIToScreenSpace(pos, size);

            Draw.Quad(ss.centre, ss.size, Color.white);


            Vector2 handlePos_ss = Vector2.Lerp(ss.centre + Vector2.left * ss.size.x / 2, ss.centre + Vector2.right * ss.size.x / 2, state.progressT);
            float handleSize_ss = ss.size.y * 1.5f;

            bool mouseOverHandle = InputHelper.MouseInPoint_ScreenSpace(handlePos_ss, handleSize_ss);
            if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && mouseOverHandle)
            {
                state.handleSelected = true;
            }
            else if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
            {
                state.handleSelected = false;
            }

            if (state.handleSelected)
            {
                float minX = ss.centre.x - ss.size.x / 2;
                float maxX = ss.centre.x + ss.size.x / 2;
                state.progressT = (InputHelper.MousePos.x - minX) / (maxX - minX);
            }

            Draw.Point(handlePos_ss, handleSize_ss, (mouseOverHandle || state.handleSelected) ? Color.red : Color.yellow);
            OnFinishedDrawingUIElement(centre, size);
        }

        public static ScrollBarState DrawScrollbar(Bounds2D scrollableArea, Bounds2D scrollbarArea, float contentHeight, Color col, UIHandle id)
        {
            ScrollBarState state = GetScrollbarState(id);
            // Draw background
            (Vector2 centre, Vector2 size) barArea_ss = UIToScreenSpace(scrollbarArea.Centre, scrollbarArea.Size);
            Draw.Quad(barArea_ss.centre, barArea_ss.size, Color.black);

            // ContentOverflow: 1 if all content fits within the scroll area. Values greater than 1 indicate how much
            // taller the area would need to be to fit the content. For example, 1.5 would mean it has be 1.5x taller.
            float contentOverflow = scrollableArea.Height >= contentHeight ? 1 : contentHeight / scrollableArea.Height;
            float scrollBarHeight = scrollbarArea.Height / contentOverflow;
            float scrollEndTopLeftY = scrollbarArea.BottomLeft.y + scrollBarHeight;
            float scrollBarCurrY = Mathf.Lerp(scrollbarArea.TopRight.y, scrollEndTopLeftY, state.scrollT);

            Vector2 scrollbarSize = new Vector2(scrollbarArea.Width, scrollBarHeight);
            Vector2 scrollbarCentre = CalculateCentre(new Vector2(scrollbarArea.Left, scrollBarCurrY), scrollbarSize, Anchor.TopLeft, true);
            (Vector2 centre, Vector2 size) scrollbar_ss = UIToScreenSpace(scrollbarCentre, scrollbarSize);

            // -- Handle input --
            bool mouseOverScrollbarArea = InputHelper.MouseInBounds_ScreenSpace(barArea_ss.centre, barArea_ss.size);
            bool mouseOverScrollbar = InputHelper.MouseInBounds_ScreenSpace(scrollbar_ss.centre, scrollbar_ss.size);

            if (InputHelper.IsMouseUpThisFrame(MouseButton.Left) || InputHelper.KeyPressedThisFrame(KeyCode.Escape))
            {
                state.isDragging = false;
            }

            if (mouseOverScrollbarArea)
            {
                if (mouseOverScrollbar) col = Color.green;
                if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
                {
                    state.isDragging = true;
                    state.dragInputStartY = InputHelper.MousePos.y;
                    state.dragScrollOffset = mouseOverScrollbar ? InputHelper.MousePos.y - scrollbar_ss.centre.y : 0;
                }
            }

            if (state.isDragging)
            {
                col = Color.yellow;
                float inputPosMin = barArea_ss.centre.y - barArea_ss.size.y / 2 + scrollbar_ss.size.y / 2;
                float inputPosMax = barArea_ss.centre.y + barArea_ss.size.y / 2 - scrollbar_ss.size.y / 2;
                state.scrollT = 1 - Mathf.InverseLerp(inputPosMin, inputPosMax, InputHelper.MousePos.y - state.dragScrollOffset);
            }

            Draw.Quad(scrollbar_ss.centre, scrollbar_ss.size, col);
            OnFinishedDrawingUIElement(scrollbarArea.Centre, scrollbarArea.Size);
            return state;
        }

        public static void DrawText(string text, FontType font, float fontSize, Vector2 pos, Anchor anchor, Color col)
        {
            Draw.Text(font, text, fontSize * scale, UIToScreenSpace(pos), anchor, col);
            var b = Draw.CalculateTextBounds(text.AsSpan(), font, fontSize, pos, anchor);
            OnFinishedDrawingUIElement(b.Centre, b.Size);
        }

        public static Vector2 CalculateTextSize(ReadOnlySpan<char> text, float fontSize, FontType font)
        {
            return Draw.CalculateTextBoundsSize(text, fontSize, font);
        }

        public static InputFieldState DrawInputField(InputFieldTheme theme, Vector2 pos, Vector2 unpaddedSize, Anchor anchor, string defaultText, UIHandle id, Func<string, bool> validation = null)
        {
            InputFieldState state = GetInputFieldState(id);

            Vector2 pad = unpaddedSize.y * theme.paddingScale;
            Vector2 size = unpaddedSize + pad;
            Vector2 centre = CalculateCentre(pos, size, anchor, true);
            (Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);

            Draw.Quad(ss.centre, ss.size, theme.bgCol);

            // Focus input
            if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
            {
                bool focus = InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size);
                bool gainedFocusThisFrame = state.focused && state.lastGainedFocusFrame == Time.frameCount;
                // Don't allow losing focus on same frame it was gained
                if (focus || !gainedFocusThisFrame) state.SetFocus(InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size));
            }

            // Draw focus outline and update text
            if (state.focused)
            {
                Draw.QuadOutline(ss.centre, ss.size, 0.1f * scale, theme.focusBorderCol);
                state.text ??= string.Empty;
                foreach (char c in InputHelper.InputStringThisFrame)
                {
                    if (!char.IsControl(c))
                    {
                        string newText = state.text;
                        if (state.cursorBeforeCharIndex == state.text.Length) newText += c;
                        else newText = newText.Insert(state.cursorBeforeCharIndex, c + "");

                        if (validation == null || validation(newText))
                        {
                            state.text = newText;
                            state.IncrementCursor();
                        }
                    }
                }

                // Backspace
                if (state.text.Length > 0 && state.cursorBeforeCharIndex > 0)
                {
                    if (CanTrigger(ref state.backspaceTrigger, KeyCode.Backspace))
                    {
                        state.text = state.text.Remove(state.cursorBeforeCharIndex - 1, 1);
                        state.DecrementCursor();
                    }
                }
                // Delete
                if (state.text.Length > 0 && state.cursorBeforeCharIndex < state.text.Length)
                {
                    if (CanTrigger(ref state.deleteTrigger, KeyCode.Delete))
                    {
                        state.text = state.text.Remove(state.cursorBeforeCharIndex, 1);
                        state.UpdateLastInputTime();
                    }
                }

                // Arrow keys
                if (CanTrigger(ref state.arrowKeyTrigger, KeyCode.LeftArrow)) state.DecrementCursor();
                if (CanTrigger(ref state.arrowKeyTrigger, KeyCode.RightArrow)) state.IncrementCursor();
            }

            // Draw text
            Vector2 textCentreLeft_ss = ss.centre + Vector2.right * (-ss.size.x / 2 + pad.x / 2 * scale);
            bool showDefaultText = string.IsNullOrEmpty(state.text);
            string displayString = showDefaultText ? defaultText : state.text;

            Color textCol = showDefaultText ? theme.defaultTextCol : theme.textCol;
            Draw.Text(theme.font, displayString, theme.fontSize * scale, textCentreLeft_ss, Anchor.TextCentreLeft, textCol);

            // Draw caret
            Vector2 textBoundsSize = Draw.CalculateTextBoundsSize(displayString.AsSpan(0, state.cursorBeforeCharIndex), theme.fontSize, theme.font);

            const float blinkDuration = 0.5f;
            if (state.focused && (int)((Time.time - state.lastInputTime) / blinkDuration) % 2 == 0)
            {
                float h = unpaddedSize.y;
                float caretOffset = h * 0.075f * (state.cursorBeforeCharIndex == 0 ? -1 : 1);
                Vector2 caretPos_ss = textCentreLeft_ss + Vector2.right * (textBoundsSize.x + caretOffset) * scale;
                Vector2 caretSize = new Vector2(0.1f * theme.fontSize, h * 1.8f);
                Draw.Quad(caretPos_ss, caretSize * scale, theme.textCol);
            }

            OnFinishedDrawingUIElement(centre, size);
            return state;

            static bool CanTrigger(ref InputFieldState.TriggerState triggerState, KeyCode key)
            {
                InputHelper.InputState keyState = InputHelper.GetKeyState(key);
                if (keyState.PressedThisFrame) triggerState.lastManualTime = Time.time;

                if (keyState.PressedThisFrame || (keyState.IsHeld && CanAutoTrigger(triggerState)))
                {
                    triggerState.lastAutoTiggerTime = Time.time;
                    return true;
                }
                return false;
            }

            static bool CanAutoTrigger(InputFieldState.TriggerState triggerState)
            {
                const float autoTriggerStartDelay = 0.5f;
                const float autoTriggerRepeatDelay = 0.04f;
                bool initialDelayOver = Time.time - triggerState.lastManualTime > autoTriggerStartDelay;
                bool canRepeat = Time.time - triggerState.lastAutoTiggerTime > autoTriggerRepeatDelay;
                return initialDelayOver && canRepeat;
            }
        }

        // Reserve spot in the drawing order for a panel. Returns an ID which can be used
        // to modify its properties (position, size, colour etc) at a later point.
        public static Draw.ID ReservePanel()
        {
            return Draw.ReserveQuad();
        }

        public static void ModifyPanel(Draw.ID id, Vector2 pos, Vector2 size, Color col, Anchor anchor = Anchor.Centre)
        {
            Vector2 centre = CalculateCentre(pos, size, anchor, true);
            (Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);
            Draw.ModifyQuad(id, ss.centre, ss.size, col);
            OnFinishedDrawingUIElement(centre, size);
        }

        public static void DrawPanel(Bounds2D bounds, Color col)
        {
            DrawPanel(bounds.Centre, bounds.Size, col);
        }

        public static void DrawFullscreenPanel(Color col)
        {
            DrawPanel(Vector2.zero, new Vector2(Width, Height), col, Anchor.BottomLeft);
        }

        public static void DrawPanel(Vector2 pos, Vector2 size, Color col, Anchor anchor = Anchor.Centre)
        {
            Vector2 centre = CalculateCentre(pos, size, anchor, true);
            (Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);
            Draw.Quad(ss.centre, ss.size, col);

            OnFinishedDrawingUIElement(centre, size);
        }

        public static ColourPickerState DrawColourPicker(UIHandle id, Vector2 pos, Vector2 size, Anchor anchor = Anchor.Centre)
        {
            ColourPickerState state = GetColourPickerState(id);

            Vector2 centre = CalculateCentre(pos, size, anchor, true);
            (Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);

            Vector2 hueCentre = centre + Vector2.right * (size.x / 2 + 2);
            Vector2 hueSize = new Vector2(2, size.y);
            (Vector2 centre, Vector2 size) hue_ss = UIToScreenSpace(hueCentre, hueSize);

            Draw.SatValQuad(ss.centre, ss.size, state.hue);
            Draw.HueQuad(hue_ss.centre, hue_ss.size);

            // Draw hue handle
            float hueTopY_ss = hue_ss.centre.y + hue_ss.size.y / 2;
            float hueBottomY_ss = hue_ss.centre.y - hue_ss.size.y / 2;
            if (state.hueHandleSelected)
            {
                state.hue = Remap01(hueBottomY_ss, hueTopY_ss, InputHelper.MousePos.y);
            }

            float hueHandleY_ss = Mathf.Lerp(hueBottomY_ss, hueTopY_ss, state.hue);
            Vector2 hueHandlePos_ss = new Vector2(hue_ss.centre.x, hueHandleY_ss);
            Vector2 hueHandleSize_ss = new Vector2(hue_ss.size.x * 1.1f, hue_ss.size.x * 0.5f);
            Draw.Quad(hueHandlePos_ss, hueHandleSize_ss, Color.white);
            bool mouseOverHueHandle = InputHelper.MouseInBounds_ScreenSpace(hueHandlePos_ss, hueHandleSize_ss);

            if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && mouseOverHueHandle)
            {
                state.hueHandleSelected = true;
            }

            // Draw sat-val handle
            Vector2 satValBottomLeft_ss = ss.centre - ss.size / 2;
            Vector2 satValTopRight_ss = ss.centre + ss.size / 2;
            float satPos_ss = Mathf.Lerp(satValBottomLeft_ss.x, satValTopRight_ss.x, state.sat);
            float valPos_ss = Mathf.Lerp(satValBottomLeft_ss.y, satValTopRight_ss.y, state.val);
            if (state.satValHandleSelected)
            {
                state.sat = Remap01(satValBottomLeft_ss.x, satValTopRight_ss.x, InputHelper.MousePos.x);
                state.val = Remap01(satValBottomLeft_ss.y, satValTopRight_ss.y, InputHelper.MousePos.y);
            }

            Vector2 satValHandlePos_ss = new Vector2(satPos_ss, valPos_ss);
            float satValHandleRadius_ss = hueHandleSize_ss.y / 2;
            Draw.Point(satValHandlePos_ss, satValHandleRadius_ss, Color.white);

            bool mouseOverSatValHandle = InputHelper.MouseInPoint_ScreenSpace(satValHandlePos_ss, satValHandleRadius_ss);
            if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && mouseOverSatValHandle)
            {
                state.satValHandleSelected = true;
            }

            if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
            {
                state.hueHandleSelected = false;
                state.satValHandleSelected = false;
            }

            OnFinishedDrawingUIElement(centre, size);
            return state;
        }

        public static bool DrawButton(string text, ButtonTheme theme, Vector2 pos, bool enabled = true, Anchor anchor = Anchor.Centre)
        {
            return DrawButton(text, theme, pos, Vector2.zero, true, true, enabled, anchor);
        }

        public static bool DrawButton(string text, ButtonTheme theme, Vector2 pos, Vector2 size, bool enabled = true, bool fitToText = true, Anchor anchor = Anchor.Centre)
        {
            return DrawButton(text, theme, pos, Vector2.zero, fitToText, fitToText, enabled, anchor);
        }

        public static bool DrawButton(string text, ButtonTheme theme, Vector2 pos, Vector2 size, bool enabled, bool fitTextX, bool fitTextY, Anchor anchor = Anchor.Centre)
        {
            // --- Calculate centre and size in screen space ---
            // Optionally resize button to fit text (given size is treated as padding in this case; text assumed single line)
            if (fitTextX || fitTextY)
            {
                float minSizeX = Draw.CalculateTextBoundsSize(text.AsSpan(), theme.fontSize, theme.font).x;
                float minSizeY = Draw.CalculateTextBoundsSize(MString.AsSpan(), theme.fontSize, theme.font).y;
                float padX = minSizeY * theme.paddingScale.x;
                float padY = minSizeY * theme.paddingScale.y;
                if (fitTextX) size.x += minSizeX + padX;
                if (fitTextY) size.y += minSizeY + padY;
            }

            Vector2 centre = CalculateCentre(pos, size, anchor, true);
            (Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);
            bool buttonPressedThisFrame = false;

            if (IsRendering)
            {
                float fontSize_ss = theme.fontSize * scale;

                // --- Handle interation ---
                bool mouseInsideMask = Draw.IsPointInsideActiveMask(InputHelper.MousePos);
                bool mouseOver = mouseInsideMask && InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size);
                bool mouseIsDown = InputHelper.IsMouseHeld(MouseButton.Left);
                bool mouseIsDownThisFrame = InputHelper.IsMouseDownThisFrame(MouseButton.Left);
                buttonPressedThisFrame = mouseOver && mouseIsDownThisFrame && enabled;

                // --- Draw ---
                Color buttonCol = theme.buttonCols.GetCol(mouseOver, mouseIsDown, enabled);
                Draw.Quad(ss.centre, ss.size, buttonCol);

                Color textCol = theme.textCols.GetCol(mouseOver, mouseIsDown, enabled);
                if (!string.IsNullOrEmpty(text))
                {
                    Draw.Text(theme.font, text, fontSize_ss, ss.centre, Anchor.TextFirstLineCentre, textCol);
                }
            }

            // Update layout and return
            OnFinishedDrawingUIElement(centre, size);
            return buttonPressedThisFrame;
        }

        public static ButtonState Button(string text, ButtonTheme theme, Vector2 pos, Vector2 size, bool enabled, bool fitTextX, bool fitTextY, Anchor anchor, UIHandle id)
        {
            ButtonState state = GetButtonState(id);
            // --- Calculate centre and size in screen space ---
            // Optionally resize button to fit text (given size is treated as padding in this case; text assumed single line)
            if (fitTextX || fitTextY)
            {
                float minSizeX = Draw.CalculateTextBoundsSize(text.AsSpan(), theme.fontSize, theme.font).x;
                float minSizeY = Draw.CalculateTextBoundsSize(MString.AsSpan(), theme.fontSize, theme.font).y;
                float padX = minSizeY * theme.paddingScale.x;
                float padY = minSizeY * theme.paddingScale.y;
                if (fitTextX) size.x += minSizeX + padX;
                if (fitTextY) size.y += minSizeY + padY;
            }

            Vector2 centre = CalculateCentre(pos, size, anchor, true);
            (Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);

            if (IsRendering)
            {
                float fontSize_ss = theme.fontSize * scale;

                // --- Handle interation ---
                bool mouseInsideMask = Draw.IsPointInsideActiveMask(InputHelper.MousePos);
                bool mouseOver = mouseInsideMask && InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size);
                bool mouseIsDown = InputHelper.IsMouseHeld(MouseButton.Left);
                if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
                {
                    if (mouseOver) state.NotifyPressed();
                    else state.NotifyCancelled();
                }
                if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
                {
                    if (mouseOver && state.isDown) state.NotifyReleased();
                    else state.NotifyCancelled();
                }

                // --- Draw ---
                Color buttonCol = theme.buttonCols.GetCol(mouseOver, mouseIsDown, enabled);
                Draw.Quad(ss.centre, ss.size, buttonCol);

                Color textCol = theme.textCols.GetCol(mouseOver, mouseIsDown, enabled);
                if (!string.IsNullOrEmpty(text))
                {
                    Draw.Text(theme.font, text, fontSize_ss, ss.centre, Anchor.Centre, textCol);
                }
            }

            // Update layout and return
            OnFinishedDrawingUIElement(centre, size);
            return state;
        }


        public static void DrawCanvasRegion(Color col)
        {
            Draw.Quad(CalculateCentre(canvasBottomLeft, canvasSize, Anchor.BottomLeft, false), canvasSize, col);
        }

        public static void DrawLetterboxes()
        {
            Vector2 canvasTopRight = canvasBottomLeft + canvasSize;
            Color col = Color.black;
            // ---- Left/right letterbox ----
            if (canvasBottomLeft.x > 0)
            {
                Vector2 size = new Vector2(canvasBottomLeft.x, screenSize.y);
                Draw.Quad(CalculateCentre(Vector2.zero, size, Anchor.BottomLeft, false), size, col);
            }
            if (canvasTopRight.x < screenSize.x)
            {
                Vector2 size = new Vector2(screenSize.x - canvasTopRight.x, screenSize.y);
                Draw.Quad(CalculateCentre(Vector2.right * canvasTopRight.x, size, Anchor.BottomLeft, false), size, col);
            }
            // ---- Top/bottom letterbox ----
            if (canvasBottomLeft.y > 0)
            {
                Vector2 size = new Vector2(screenSize.x, canvasBottomLeft.y);
                Draw.Quad(CalculateCentre(Vector2.zero, size, Anchor.BottomLeft, false), size, col);
            }
            if (canvasTopRight.y < screenSize.y)
            {
                Vector2 size = new Vector2(screenSize.x, screenSize.y - canvasTopRight.y);
                Draw.Quad(CalculateCentre(Vector2.up * canvasTopRight.y, size, Anchor.BottomLeft, false), size, col);
            }
        }

        //  --------------------------- Helper functions ---------------------------

        public static Vector2 ScreenToUISpace(Vector2 point)
        {
            return (point - canvasBottomLeft) / scale;
        }

        public static Bounds2D UIToScreenSpace(Bounds2D bounds)
        {
            return new Bounds2D(UIToScreenSpace(bounds.Min), UIToScreenSpace(bounds.Max));
        }

        public static Vector2 UIToScreenSpace(Vector2 point)
        {
            return canvasBottomLeft + point * scale;
        }

        static (Vector2 centre, Vector2 size) UIToScreenSpace(Vector2 centre, Vector2 size)
        {
            return (canvasBottomLeft + centre * scale, size * scale);
        }

        static Vector2 CalculateCentre(Vector2 pos, Vector2 size, Anchor anchor, bool applyLayout)
        {
            if (applyLayout && layoutStack.TryPeek(out LayoutScope parentLayout))
            {
                pos = parentLayout.currPos;
            }

            Vector2 centre = pos + anchor switch
            {
                Anchor.Centre => Vector2.zero,
                Anchor.CentreLeft => new Vector2(size.x, 0) / 2,
                Anchor.CentreRight => new Vector2(-size.x, 0) / 2,
                Anchor.TopLeft => new Vector2(size.x, -size.y) / 2,
                Anchor.TopRight => new Vector2(-size.x, -size.y) / 2,
                Anchor.CentreTop => new Vector2(0, -size.y) / 2,
                Anchor.BottomLeft => size / 2,
                Anchor.BottomRight => new Vector2(-size.x, size.y) / 2,
                Anchor.CentreBottom => new Vector2(0, size.y) / 2,
                _ => Vector2.zero
            };

            return centre;
        }

        public static float CalculateSizeToFitElements(float boundsSize, float spacing, int numElements)
        {
            if (numElements <= 0) return 0;
            return (boundsSize - spacing * (numElements - 1)) / numElements;
        }

        public static string LineBreakByCharCount(string text, int maxCharsPerLine)
        {
            string formatted = "";
            string currWord = "";
            string currLine = "";

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool isEndOfText = i == text.Length - 1;

                if (c is ' ' or '\n' || isEndOfText)
                {
                    // If word is short enough to fit on current line, then add it
                    if (currLine.Length + currWord.Length <= maxCharsPerLine)
                    {
                        currLine += currWord + c;
                    }
                    // If word is too long, then add the current line to the formatted text, and start a new line with this word
                    else
                    {
                        if (currWord.Length > maxCharsPerLine) throw new System.Exception("Word cannot fit on line. Todo: allow breaking with hyphens");
                        formatted += currLine + '\n';
                        currLine = currWord + c;
                    }
                    // Handle manual line break in text
                    if (c is '\n' || isEndOfText)
                    {
                        formatted += currLine;
                        currLine = "";
                    }
                    currWord = "";
                }
                else
                {
                    currWord += c;
                }
            }

            return formatted;
        }

        static T GetState<T>(UIHandle id, Dictionary<UIHandle, T> statesLookup) where T : new()
        {
            if (!statesLookup.TryGetValue(id, out T state))
            {
                state = new T();
                statesLookup.Add(id, state);
            }
            return state;
        }

        public static ColourPickerState GetColourPickerState(UIHandle id) => GetState(id, colPickerStates);
        public static InputFieldState GetInputFieldState(UIHandle id) => GetState(id, inputFieldStates);
        public static ButtonState GetButtonState(UIHandle id) => GetState(id, buttonStates);
        public static ScrollBarState GetScrollbarState(UIHandle id) => GetState(id, scrollbarStates);

        public static void ResetAllStates()
        {
            inputFieldStates.Clear();
        }

        static bool IsRendering => !boundsScopes.IsInsideScope || boundsScopes.CurrentScope.DrawUI;

        // Update bounds, etc. once element has been drawn. Given centre/size must be in UI space (not screen-space!)
        static void OnFinishedDrawingUIElement(Vector2 centre, Vector2 size)
        {
            UpdateAutoLayout(centre, size);
            SetUISpaceBoundingBox(centre, size);


            static void SetUISpaceBoundingBox(Vector2 centre, Vector2 size)
            {
                Vector2 min = centre - size / 2;
                Vector2 max = centre + size / 2;
                PrevBoundingBox = new Bounds2D(min, max);

                if (boundsScopes.TryGetCurrentScope(out BoundsScope activeBoundsScope))
                {
                    activeBoundsScope.Grow(min, max);
                }
            }

            static void UpdateAutoLayout(Vector2 centre, Vector2 size)
            {
                if (layoutStack.TryPeek(out LayoutScope activeLayout))
                {
                    activeLayout.ElementAdded(centre, size);
                }
            }
        }

        public static ButtonTheme TestButtonTheme => new()
        {
            font = FontType.FiraCodeBold,
            fontSize = 2,
            textCols = new ButtonTheme.StateCols(Color.white, Color.white, Color.white, Color.grey),
            buttonCols = new ButtonTheme.StateCols(Color.blue, Color.yellow, Color.red, Color.black)
        };



        public class LayoutScope : IDisposable
        {
            public enum Kind { None, Left, Right, Up, Down }

            public Kind kind;
            public Vector2 currPos;
            public Vector2 growDir;
            public float spacing;
            public Vector2 boundsMin;
            public Vector2 boundsMax;
            public int numElementsAdded;

            // Grow the current layout based on the
            public void ElementAdded(Vector2 boundsCentre, Vector2 boundsSize)
            {
                // Increase current pos by the bounds size (along the grow direction)
                Vector2 growAmount = Vector2.Scale(growDir, boundsSize) + growDir * spacing;
                currPos += growAmount;

                // Grow the bounding box of all elements added to this layout
                Vector2 addedMax = boundsCentre + boundsSize / 2;
                Vector2 addedMin = boundsCentre - boundsSize / 2;
                boundsMax = Vector2.Max(boundsMax, addedMax);
                boundsMin = Vector2.Min(boundsMin, addedMin);
                numElementsAdded++;
            }

            public void Dispose()
            {
                // When exiting the scope of this layout, remove it from stack, and give its bounding box
                // to the parent node so it can grow accordingly.
                layoutStack.Pop();

                if (numElementsAdded > 0 && layoutStack.TryPeek(out LayoutScope parentLayout))
                {
                    parentLayout.ElementAdded((boundsMin + boundsMax) / 2, boundsMax - boundsMin);
                }
            }

            public void Init(Kind kind, Vector2 pos, float spacing)
            {
                this.kind = kind;
                currPos = pos;
                growDir = kind switch
                {
                    Kind.Left => Vector2.left,
                    Kind.Right => Vector2.right,
                    Kind.Up => Vector2.up,
                    Kind.Down => Vector2.down,
                    _ => Vector2.zero
                };
                this.spacing = spacing;
                boundsMin = Vector2.one * float.MaxValue;
                boundsMax = Vector2.one * float.MinValue;
                numElementsAdded++;
            }
        }

        static float Remap01(float min, float max, float val)
        {
            if (max - min == 0) return 0.5f;
            if (val <= min) return 0;
            if (val >= max) return 1;
            return (val - min) / (max - min);
        }


        public class UIScope : IDisposable
        {
            public Vector2 canvasBottomLeft;
            public Vector2 canvasSize;
            public Vector2 screenSize;
            public float scale;
            public float invScale;
            public bool drawLetterboxes;
            public float aspect;

            public void Dispose()
            {
                if (drawLetterboxes) DrawLetterboxes();
                uiScopes.ExitScope();
            }
        }

        public class BoundsScope : IDisposable
        {
            public Vector2 Min;
            public Vector2 Max;
            public bool IsEmpty;
            public bool DrawUI;//

            public Bounds2D GetBounds() => IsEmpty ? new Bounds2D(Vector2.zero, Vector2.zero) : new Bounds2D(Min, Max);

            public void Init(bool draw)
            {
                Min = Vector2.one * float.MaxValue;
                Max = Vector2.one * float.MinValue;
                IsEmpty = true;
                DrawUI = draw;
            }

            public void Grow(Vector2 min, Vector2 max)
            {
                Min = Vector2.Min(min, Min);
                Max = Vector2.Max(max, Max);
                IsEmpty = false;
            }

            public void Dispose()
            {
                boundsScopes.ExitScope();

                // Grow the parent bounds
                if (boundsScopes.TryGetCurrentScope(out BoundsScope parent))
                {
                    parent.Grow(Min, Max);
                }
            }
        }



    }

}