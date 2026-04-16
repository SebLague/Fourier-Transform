using UnityEngine;

namespace Seb.Visualization.UI.Examples
{
    public class ThemeSelector : MonoBehaviour
    {
        public UIThemeLibrary.ThemeName themeName;
        UITheme activeTheme;

        public UITheme ActiveTheme
        {
            get { return UIThemeLibrary.CreateTheme(themeName); }
            /*
            get
            {
                if (activeTheme == null || activeTheme.ThemeName != themeName)
                {
                    activeTheme = UIThemeLibrary.CreateTheme(themeName);
                }

                return activeTheme;
            }
            */
        }
    }
}