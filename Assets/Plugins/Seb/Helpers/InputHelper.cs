using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Types;

namespace Seb.Helpers
{
    public enum MouseButton { Left = 0, Right = 1, Middle = 2 }

    public static class InputHelper
    {

        // Screen-space mouse position
        public static Vector2 MousePos => Input.mousePosition;
        public static bool BackspacePressedThisFrame => Input.GetKeyDown(KeyCode.Backspace);
        public static bool BackspaceIsHeld => Input.GetKey(KeyCode.Backspace);
        public static bool LeftArrowPressedThisFrame => Input.GetKeyDown(KeyCode.LeftArrow);
        public static bool RightArrowPressedThisFrame => Input.GetKeyDown(KeyCode.RightArrow);
        public static string InputStringThisFrame => Input.inputString;

        public static bool KeyPressedThisFrame(KeyCode key) => Input.GetKeyDown(key);
        public static bool KeyIsHeld(KeyCode key) => Input.GetKey(key);

        static Camera _worldCam;
        static Vector2 prevWorldMousePos;
        static int prevWorldMouseFrame = -1;

        public static Camera WorldCam
        {
            get
            {
                if (_worldCam == null) _worldCam = Camera.main;
                return _worldCam;
            }
        }

        public static Vector2 MousePosWorld
        {
            get
            {
                if (Time.frameCount != prevWorldMouseFrame)
                {
                    prevWorldMousePos = WorldCam.ScreenToWorldPoint(Input.mousePosition);
                    prevWorldMouseFrame = Time.frameCount;
                }
                return prevWorldMousePos;
            }
        }

        public static InputState GetKeyState(KeyCode key)
        {
            return new InputState(Input.GetKeyDown(key), Input.GetKeyUp(key), Input.GetKey(key));
        }

        public static bool MouseInBounds_ScreenSpace(Vector2 centre, Vector2 size)
        {
            if (!Application.isPlaying) return false;
            Vector2 offset = MousePos - centre;
            return Mathf.Abs(offset.x) < size.x / 2 && Mathf.Abs(offset.y) < size.y / 2;
        }

        public static bool MouseInBounds_ScreenSpace(Bounds2D bounds)
        {
            if (!Application.isPlaying) return false;
            return bounds.PointInBounds(MousePos);
        }

        public static bool MouseInPoint_ScreenSpace(Vector2 centre, float radius)
        {
            if (!Application.isPlaying) return false;
            Vector2 offset = MousePos - centre;
            return offset.sqrMagnitude < radius * radius;
        }

        public static bool MouseInsidePoint_World(Vector2 centre, float radius)
        {
            if (!Application.isPlaying) return false;
            Vector2 offset = MousePosWorld - centre;
            return offset.sqrMagnitude < radius * radius;
        }

        public static bool MouseInsideBounds_World(Vector2 centre, Vector2 size)
        {
            if (!Application.isPlaying) return false;
            Vector2 offset = MousePosWorld - centre;
            return Mathf.Abs(offset.x) < size.x / 2 && Mathf.Abs(offset.y) < size.y / 2;
        }

        public static bool IsMouseHeld(MouseButton button)
        {
            if (!Application.isPlaying) return false;
            return Input.GetMouseButton((int)button);
        }

        public static bool IsMouseDownThisFrame(MouseButton button)
        {
            if (!Application.isPlaying) return false;
            return Input.GetMouseButtonDown((int)button);
        }

        public static bool IsMouseUpThisFrame(MouseButton button)
        {
            if (!Application.isPlaying) return false;
            return Input.GetMouseButtonUp((int)button);
        }

        public static float GetMouseScrollDelta() => Input.mouseScrollDelta.y;

        public readonly struct InputState
        {
            public readonly bool PressedThisFrame;
            public readonly bool ReleasedThisFrame;
            public readonly bool IsHeld;

            public InputState(bool pressedThisFrame, bool releasedThisFrame, bool isHeld)
            {
                PressedThisFrame = pressedThisFrame;
                ReleasedThisFrame = releasedThisFrame;
                IsHeld = isHeld;
            }
        }
    }
}