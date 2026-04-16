using System;
using UnityEngine;

namespace Seb.Helpers
{
// ---- Version 0.2 [2/Nov/2025] ----
// Extra compute helper functions
// (these are in a separate class because they require custom compute shaders, and want to keep the main helper dependency-free)

	public static class ComputeHelperExtra
	{
		static ComputeShader compute_countCopy;
		static ComputeShader compute_textureResize;

		// Create args buffer for instanced indirect rendering
		// (instance count will be taken from first element in count buffer -- integer expected)
		public static void CreateArgsBuffer(ref ComputeBuffer argsBuffer, Mesh mesh, ComputeBuffer countBuffer)
		{
			LoadShaders();
			ComputeHelper.CreateArgsBuffer(ref argsBuffer, mesh, 0);

			compute_countCopy.SetBuffer(0, "CountSource", countBuffer);
			compute_countCopy.SetBuffer(0, "CountTarget", argsBuffer);
			compute_countCopy.SetInt("ReadIndex", 0);
			compute_countCopy.SetInt("WriteIndex", 1);
			ComputeHelper.Dispatch(compute_countCopy, 1, kernelIndex: 0);
		}

		// Todo: cache/reuse intermediate textures?
		public static void ResizeTexture(Texture source, RenderTexture target, int iterations = 1)
		{
			LoadShaders();
			Texture sourceCurr = source;

			for (int i = 0; i < iterations; i++)
			{
				float t = (i + 1.0f) / iterations;
				int width = (int)Mathf.Lerp(source.width, target.width, t);
				int height = (int)Mathf.Lerp(source.height, target.height, t);

				bool isFinal = i == iterations - 1;

				RenderTexture targetCurr = isFinal ? target : ComputeHelper.CreateRenderTexture(width, height, FilterMode.Bilinear, ComputeHelper.RGBA_SFloat);

				compute_textureResize.SetTexture(0, "Source", sourceCurr);
				compute_textureResize.SetTexture(0, "Target", targetCurr);
				compute_textureResize.SetInts("targetSize", width, height);
				ComputeHelper.Dispatch(compute_textureResize, width, height, kernelIndex: 0);


				if (i > 0) ComputeHelper.Release((RenderTexture)sourceCurr);
				sourceCurr = targetCurr;
			}
		}

		static void LoadShaders()
		{
			ComputeHelper.LoadComputeShader(ref compute_countCopy, "Helper_CountCopy");
			ComputeHelper.LoadComputeShader(ref compute_textureResize, "Helper_TextureResize");
		}
	}
}