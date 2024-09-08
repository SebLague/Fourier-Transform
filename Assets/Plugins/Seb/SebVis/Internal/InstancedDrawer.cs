using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.Rendering;

namespace Seb.Vis.Internal
{
    // Base class for drawing instanced meshes
    public class InstancedDrawer<T> : Drawer<T> where T : struct
    {
        // Buffers
        protected ComputeBuffer instanceBuf;
        protected ComputeBuffer argsBuf;
        // Other stuff
        protected readonly Pool<Material> materialPool;
        protected readonly Mesh mesh;
        protected readonly Shader shader;
        static readonly int instanceDataID = Shader.PropertyToID("InstanceData");
        static readonly int instanceOffsetID = Shader.PropertyToID("InstanceOffset");
        static readonly int transformOffsetID = Shader.PropertyToID("Offset");
        static readonly int transformScaleID = Shader.PropertyToID("Scale");
        static readonly int screenSpaceID = Shader.PropertyToID("useScreenSpace");

        // State
        protected int groupIndex;
        readonly List<uint> allArgs = new();

        bool hasSetDataThisFrame;

        public InstancedDrawer(Mesh mesh, Shader shader)
        {
            this.mesh = mesh;
            this.shader = shader;
            materialPool = new(() => new Material(shader));
        }

        protected override void InitFrame()
        {
            materialPool.ReturnAll();
            groupIndex = 0;
            hasSetDataThisFrame = false;
        }

        protected override void DrawLayer(CommandBuffer cmd, int startIndex, int count, Draw.LayerInfo layerInfo)
        {
            if (!hasSetDataThisFrame)
            {
                CreateStructuredBuffer(ref instanceBuf, allDrawData);
                InitArgs(layerSizes);
                hasSetDataThisFrame = true;
            }

            int argsByteOffset = sizeof(uint) * 5 * groupIndex;

            Material mat = materialPool.GetNextAvailableOrCreate();
            mat.SetBuffer(instanceDataID, instanceBuf);
            mat.SetInt(instanceOffsetID, startIndex);

            mat.SetVector(transformOffsetID, layerInfo.offset);
            mat.SetFloat(transformScaleID, layerInfo.scale);
            mat.SetInt(screenSpaceID, layerInfo.useScreenSpace ? 1 : 0);

            cmd.DrawMeshInstancedIndirect(mesh, 0, mat, 0, argsBuf, argsByteOffset);

            groupIndex++;
        }

        public override void Release()
        {
            base.Release();
            ReleaseBuffer(instanceBuf);
            ReleaseBuffer(argsBuf);

            materialPool.ReturnAll();
            while (materialPool.HasAvailable())
            {
                Material mat = materialPool.PurgeNextAvailable();
                if (Application.isPlaying) Object.Destroy(mat);
                else Object.DestroyImmediate(mat);//
            }
        }

        protected void InitArgs(List<uint> counts)
        {
            allArgs.Clear();
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts[i] == 0) continue;
                const int subMeshIndex = 0;
                allArgs.Add(mesh.GetIndexCount(subMeshIndex));
                allArgs.Add((uint)counts[i]);
                allArgs.Add(mesh.GetIndexStart(subMeshIndex));
                allArgs.Add(mesh.GetBaseVertex(subMeshIndex));
                // instance offset (NOTE: this apparently behaves inconsistently across different platforms
                // (i.e. not guaranteed to affect the instanceID in shader), so is best to provide an offset value manually via SetInt
                allArgs.Add(0);
            }
            CreateEmptyArgsBuffer(ref argsBuf, counts.Count);
            argsBuf.SetData(allArgs);
        }

        static void CreateEmptyArgsBuffer(ref ComputeBuffer argsBuffer, int numInstances)
        {
            const int stride = sizeof(uint);
            const int numArgsPerInstance = 5;
            int argCount = numInstances * numArgsPerInstance;

            bool createNewBuffer = argsBuffer == null || !argsBuffer.IsValid() || argsBuffer.count != argCount || argsBuffer.stride != stride;
            if (createNewBuffer)
            {
                if (argsBuffer != null)
                {
                    argsBuffer.Release();
                }
                argsBuffer = new ComputeBuffer(argCount, stride, ComputeBufferType.IndirectArguments);
            }
        }

        static void CreateStructuredBuffer(ref ComputeBuffer buffer, List<T> data)
        {
            int stride = GetStride();
            bool createNewBuffer = buffer == null || !buffer.IsValid() || buffer.count != data.Count || buffer.stride != stride;
            if (createNewBuffer)
            {
                ReleaseBuffer(buffer);
                buffer = new ComputeBuffer(data.Count, stride);
            }
            buffer.SetData(data);
        }

        static void ReleaseBuffer(ComputeBuffer buffer)
        {
            if (buffer != null) buffer.Release();
        }

        static int GetStride() => System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

    }
}