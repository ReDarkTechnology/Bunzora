using Raylib_CsLo;

namespace Bunzora
{
    internal static class ColorUtil
    {
        public static Color FromHex(string hex)
        {
            return new Color()
            {
                a = 255,
                r = (byte)int.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                g = (byte)int.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                b = (byte)int.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)
            };
        }

        public static Color White = new Color(255, 255, 255, 255);
        public static Color Black = new Color(0, 0, 0, 255);
        public static Color Gray = new Color(128, 128, 128, 255);
        public static Color Red = new Color(255, 0, 0, 255);
        public static Color Green = new Color(0, 255, 0, 255);
        public static Color Blue = new Color(0, 0, 255, 255);
    }
}