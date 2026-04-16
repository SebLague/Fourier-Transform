using UnityEngine;

namespace Seb.Visualization.UI.Examples
{
	[ExecuteAlways]
	public class CheckboxExample : MonoBehaviour
	{
		public ThemeSelector themeSelector;
		public Vector2 pos;
		public float size;
		public Color col;
		public Anchor anchor;

		void Update()
		{
			using (UI.CreateFixedAspectUIScope())
			{
				DrawToggle();
			}
		}

		void DrawToggle()
		{
			UITheme theme = themeSelector.ActiveTheme;

			UIHandle id = new("toggletest");
			UI.DrawToggle(id, pos, size, theme.checkboxTheme, anchor);
		}
	}
}