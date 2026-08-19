using UnityEngine;

namespace Backend.Util
{
    public static class ColorUtil
    {
        public static Color GetUnitTypeColor(UnitType type)
        {
            return type switch
            {
                UnitType.fire  => new Color(1f,   0.3f, 0.1f),
                UnitType.light => new Color(1f,   1f,   0.2f),
                UnitType.water => new Color(0.2f, 0.5f, 1f),
                UnitType.grass => new Color(0.2f, 0.8f, 0.2f),
                _              => Color.white,
            };
        }

        public static Color GetPanelTypeColor(PanelType type)
        {
            return type switch
            {
                PanelType.fire  => new Color32(0xC0, 0x40, 0x18, 255),
                PanelType.light => new Color32(0xD2, 0xC6, 0x3E, 255),
                PanelType.water => new Color32(0x36, 0x7D, 0xC8, 255),
                PanelType.grass => new Color32(0x59, 0xC4, 0x68, 255),
                PanelType.heart => new Color32(0xCA, 0x8B, 0xC5, 255),
                _               => Color.white,
            };
        }

        public static Color LerpHSV(Color from, Color to, float t)
        {
            Color.RGBToHSV(from, out float h1, out float s1, out float v1);
            Color.RGBToHSV(to, out float h2, out float s2, out float v2);

            float h = LerpHue(h1, h2, t);
            float s = Mathf.Lerp(s1, s2, t);
            float v = Mathf.Lerp(v1, v2, t);
            var color = Color.HSVToRGB(h, s, v);
            color.a = Mathf.Lerp(from.a, to.a, t);
            return color;
        }

        private static float LerpHue(float from, float to, float t)
        {
            float delta = to - from;
            if (delta > 0.5f)
                delta -= 1f;
            else if (delta < -0.5f)
                delta += 1f;

            float hue = from + delta * t;
            if (hue < 0f)
                hue += 1f;
            else if (hue >= 1f)
                hue -= 1f;
            return hue;
        }
    }
}
