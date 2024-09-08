using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Seb.Vis.Internal;
using System;
using Seb.Vis.Text.FontLoading;

namespace Seb.Vis
{
    // (Partial) Draw class: contains the management code
    public static partial class Draw
    {
        const CameraEvent drawCameraEvent = CameraEvent.BeforeImageEffects;

        // Drawing helpers
        static Drawer<ShapeData> shapeDrawer;
        static Drawer<SphereData> sphereDrawer;
        static Drawer<TextDrawData> textDrawer;

        static FontData[] defaultFontsData;

        // Command buffer stuff
        static CommandBuffer cmd;
        static int lastDispatchFrame;
        static int initFrame = -1;
        static int camInitFrame = -1;

        const string cmdBufferName = "Vis Draw Commands";

        static bool isInitSinceCleanup;
        static Mesh quadMesh;
        static Mesh sphereMesh;

        static Scopes<MaskScope> maskScopes = new();
        static Vector2 activeMaskMin;
        static Vector2 activeMaskMax;

        static (int lastInitFrame, bool lastInitPlayMode, bool initialized) initState;
        static Stack<LayerInfo> layers = new();


        static void DispatchAll()
        {
            if (!isInitSinceCleanup) return;
            if (quadMesh == null)
            {
                // Stuff becomes null a few frames after editor starts up first time.
                // Not sure why, so just a hack for now...
                Init();
                return;
            }

            // Ensure command buffer is initialized
            if (cmd == null)
            {
                cmd = new CommandBuffer();
                cmd.name = cmdBufferName;
            }
            cmd.Clear();

            for (int i = 0; i < layers.Count; i++)
            {
                sphereDrawer.DrawNextLayer(cmd);
                shapeDrawer.DrawNextLayer(cmd);
                textDrawer.DrawNextLayer(cmd);
            }

            layers.Clear();
        }

        // Called for each active camera
        static void OnPreRender(Camera cam)
        {
            // Add draw commands to cmdbuffer once per frame
            if (lastDispatchFrame != Time.frameCount)
            {
                lastDispatchFrame = Time.frameCount;
                DispatchAll();
            }

            // Set camera-projection matrix and screenspace matrix
            Matrix4x4 vp = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            Shader.SetGlobalMatrix("WorldToClipSpace", vp);
            Shader.SetGlobalVector("ScreenSize", new Vector2(cam.pixelWidth, cam.pixelHeight));

            // -- Make sure cmdbuffer is attached just once to each camera --

            if (camInitFrame < initState.lastInitFrame || camInitFrame == Time.frameCount)
            {
                camInitFrame = Time.frameCount;
                // Remove buffer by name.
                // This is done because there are situations in which the buffer can be
                // null (but still attached to camera), and I don't want to think about it.
                var allBuffers = cam.GetCommandBuffers(drawCameraEvent);
                foreach (var b in allBuffers)
                {
                    if (string.Equals(b.name, cmdBufferName, System.StringComparison.Ordinal))
                    {
                        cam.RemoveCommandBuffer(drawCameraEvent, b);
                    }
                }
            }

            if (cmd != null) cam.RemoveCommandBuffer(drawCameraEvent, cmd);

            if (lastDispatchFrame == Time.frameCount && cmd != null)
            {
                cam.AddCommandBuffer(drawCameraEvent, cmd);
            }

        }

        static void CleanupBeforeAssemblyReload()
        {
            Cleanup();

        }

        static void Cleanup()
        {
            isInitSinceCleanup = false;
            ReleaseDrawer(shapeDrawer);
            ReleaseDrawer(sphereDrawer);
            ReleaseDrawer(textDrawer);
            shapeDrawer = null;
            sphereDrawer = null;
            textDrawer = null;
            layers.Clear();

            static void ReleaseDrawer<T>(Drawer<T> drawer) where T : struct
            {
                if (drawer != null)
                {
                    drawer.Release();
                }
            }
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {

            if (state is UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Cleanup();
            }
            if (state is UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                RegisterCallbacks();
                Init();
            }

        }
#endif

        static void Init()
        {
            initState = (Time.frameCount, Application.isPlaying, true);
            //Debug.Log("Init! Frame = " + Time.frameCount + "  isplaying = " + Application.isPlaying);
            Cleanup();
            isInitSinceCleanup = true;
            lastDispatchFrame = -1;
            initFrame = Time.frameCount;

            quadMesh = QuadGenerator.GenerateQuadMesh();
            sphereMesh = SphereGenerator.GenerateSphereMesh(3);

            CreateShapeDrawers();
            LoadDefaultFonts();
            // Set active mask to 'infinite' bounds. Note: dividing by 2 so that translating layer doesn't cause immediate over/underflow
            activeMaskMin = Vector2.one * float.MinValue / 2;
            activeMaskMax = Vector2.one * float.MaxValue / 2;

            static void CreateShapeDrawers()
            {
                shapeDrawer = new InstancedDrawer<ShapeData>(quadMesh, Shader.Find("Vis/Shapes"));
                sphereDrawer = new InstancedDrawer<SphereData>(sphereMesh, Shader.Find("Vis/UnlitSphere"));
                textDrawer = new TextDrawManager();
            }

            static void LoadDefaultFonts()
            {
                if (defaultFontsData == null || defaultFontsData.Length != FontMap.map.Length)
                {
                    defaultFontsData = new FontData[FontMap.map.Length];
                    HashSet<FontType> fontCoverageTest = new((FontType[])Enum.GetValues(typeof(FontType)));

                    for (int i = 0; i < FontMap.map.Length; i++)
                    {
                        (FontType font, string path) = FontMap.map[i];

                        byte[] raw = Resources.Load<TextAsset>(FontMap.FontFolderPath + "/" + path).bytes;
                        defaultFontsData[(int)font] = FontParser.Parse(raw);
                        fontCoverageTest.Remove(font);
                    }

                    if (fontCoverageTest.Count != 0)
                    {
                        foreach (var f in fontCoverageTest)
                        {
                            Debug.LogError("Font not found in map: " + f);
                        }
                    }

                }
            }
        }


        static void RegisterCallbacks()
        {
            Camera.onPreRender -= OnPreRender;
            Camera.onPreRender += OnPreRender;

#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= CleanupBeforeAssemblyReload;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += CleanupBeforeAssemblyReload;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
#endif
        // Callback invoked when starting up the runtime. Called before the first scene is loaded.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeOnLoad()
        {
            RegisterCallbacks();
            Init();
        }

        public readonly struct LayerInfo
        {
            public readonly Vector2 offset;
            public readonly float scale;
            public readonly bool useScreenSpace;

            public LayerInfo(Vector2 offset, float scale, bool useScreenSpace)
            {
                this.offset = offset;
                this.scale = scale;
                this.useScreenSpace = useScreenSpace;
            }
        }


        public class MaskScope : IDisposable
        {
            public Vector2 boundsMin;
            public Vector2 boundsMax;

            public void Init(Vector2 min, Vector2 max)
            {
                boundsMin = activeMaskMin = min;
                boundsMax = activeMaskMax = max;
            }

            public void Dispose()
            {
                maskScopes.ExitScope();
                if (maskScopes.TryGetCurrentScope(out MaskScope parent))
                {
                    activeMaskMin = parent.boundsMin;
                    activeMaskMax = parent.boundsMax;
                }
                else
                {
                    activeMaskMin = Vector2.one * float.MinValue;
                    activeMaskMax = Vector2.one * float.MaxValue;
                }
            }
        }
    }
}