using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seb.Vis.Internal
{
    public abstract class Drawer<T> where T : struct
    {
        public readonly List<T> allDrawData = new();
        protected readonly List<uint> layerSizes = new();
        readonly List<Draw.LayerInfo> layers = new();
        int layerDrawIndex;
        bool released;
        int lastDrawFrame = -1;
        int startIndex;

        public int LayerCount => layers.Count;

        void Reset()
        {
            lastDrawFrame = Time.frameCount;
            allDrawData.Clear();
            layers.Clear();
            layerSizes.Clear();
            layerDrawIndex = 0;
            startIndex = 0;
        }

        public void StartNewLayer(Draw.LayerInfo layerInfo)
        {
            // First layer of frame: init
            if (lastDrawFrame != Time.frameCount)
            {
                Reset();
                InitFrame();
            }

            layers.Add(layerInfo);
            layerSizes.Add(0);
            OnLayerAdded(layerInfo);
        }


        public void AddToLayer(T drawData)
        {
            if (layers.Count == 0) throw new System.Exception("Layer has not yet been started.");
            layerSizes[^1]++;
            allDrawData.Add(drawData);
        }

        public void DrawNextLayer(CommandBuffer cmd)
        {
            if (released || layerSizes.Count == 0) return;
            if (layerDrawIndex >= layerSizes.Count)
            {
                Reset();
                return;
            }
            int count = (int)layerSizes[layerDrawIndex];
            Draw.LayerInfo layerInfo = layers[layerDrawIndex];
            if (count > 0) DrawLayer(cmd, startIndex, count, layerInfo);
            layerDrawIndex++;
            startIndex += count;

        }


        public int CurrDrawDataIndex => allDrawData.Count - 1;

        protected abstract void DrawLayer(CommandBuffer cmd, int startIndex, int count, Draw.LayerInfo layerInfo);

        protected abstract void InitFrame();

        protected virtual void OnLayerAdded(Draw.LayerInfo layerInfo) { }


        public virtual void Release()
        {
            released = true;
        }

    }
}