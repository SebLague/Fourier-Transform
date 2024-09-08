using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb.Vis.UI
{
    public class ScrollBarState
    {
        public bool isDragging;
        public float dragInputStartY;
        public float dragScrollOffset;
        public float scrollT;

        public void Scroll(float inputAmount, float scrollRegionSize, float scrollContentSize)
        {
            if (scrollContentSize <= scrollRegionSize) return;
            float speedFac = scrollRegionSize / scrollContentSize;
            scrollT += speedFac * inputAmount;
            scrollT = Mathf.Clamp01(scrollT);
        }
    }

    public struct SliderState
    {
        public float progressT;
        public bool handleSelected;
    }

    public class ColourPickerState
    {
        public float hue;
        public float sat;
        public float val;

        public bool hueHandleSelected;
        public bool satValHandleSelected;

        public Color GetRGB()
        {
            return Color.HSVToRGB(hue, sat, val);
        }
    }


    public class ButtonState
    {
        public bool toggleState;
        public bool isDown;
        int buttonPressFrame;
        int buttonReleaseFrame;

        public bool ButtonPressedThisFrame => buttonPressFrame == Time.frameCount;
        public bool ButtonReleasedThisFrame => buttonReleaseFrame == Time.frameCount;

        public void NotifyPressed()
        {
            isDown = true;
            buttonPressFrame = Time.frameCount;
        }

        public void NotifyReleased()
        {
            isDown = false;
            buttonReleaseFrame = Time.frameCount;
        }

        public void NotifyCancelled()
        {
            isDown = false;
        }

    }

    public class InputFieldState
    {
        public string text;
        public int cursorBeforeCharIndex;
        public bool focused { get; private set; }
        public int lastGainedFocusFrame;
        public float lastInputTime;
        public TriggerState backspaceTrigger;
        public TriggerState deleteTrigger;
        public TriggerState arrowKeyTrigger;

        public struct TriggerState
        {
            public float lastManualTime;
            public float lastAutoTiggerTime;
        }

        public void SetFocus(bool focus)
        {
            focused = focus;
            lastInputTime = Time.time;
            lastGainedFocusFrame = Time.frameCount;
        }

        public void SetCursorIndex(int i)
        {
            cursorBeforeCharIndex = i;
            cursorBeforeCharIndex = Mathf.Clamp(cursorBeforeCharIndex, 0, text.Length);
            UpdateLastInputTime();
        }

        public void UpdateLastInputTime()
        {
            lastInputTime = Time.time;
        }

        public void SetStartupText(string text, bool focus = true)
        {
            this.text = text;
            SetCursorIndex(text.Length);
            SetFocus(focus);
        }

        public void IncrementCursor() => SetCursorIndex(cursorBeforeCharIndex + 1);
        public void DecrementCursor() => SetCursorIndex(cursorBeforeCharIndex - 1);
    }
}