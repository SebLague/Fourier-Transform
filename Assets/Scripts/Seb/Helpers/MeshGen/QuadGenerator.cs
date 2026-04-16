using UnityEngine;

namespace Seb.Helpers
{
	public static class QuadGenerator
	{
		public static Mesh GenerateQuadMesh()
		{
			int[] indices = new int[] { 0, 1, 2, 2, 1, 3 };

			Vector3[] vertices =
			{
				new(-0.5f, 0.5f),
				new(0.5f, 0.5f),
				new(-0.5f, -0.5f),
				new(0.5f, -0.5f)
			};
			Vector2[] uvs =
			{
				new(0, 1),
				new(1, 1),
				new(0, 0),
				new(1, 0)
			};

			Mesh mesh = new Mesh();
			mesh.SetVertices(vertices);
			mesh.SetTriangles(indices, 0, true);
			mesh.SetUVs(0, uvs);
			return mesh;
		}
	}
}