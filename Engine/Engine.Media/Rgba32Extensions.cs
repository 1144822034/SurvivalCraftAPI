using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Media
{
    public static class Rgba32Extensions
    {
        public static Rgba32 PremultiplyAlpha(this Rgba32 pixel)
        {
            return new Rgba32((byte)(pixel.R * (uint)pixel.A / 255u), (byte)(pixel.G * (uint)pixel.A / 255u), (byte)(pixel.B * (uint)pixel.A / 255u), pixel.A);
        }
        public static bool IsMagenta(this Rgba32 pixel)
        {
            return pixel.R == 255 && pixel.G == 0 && pixel.B == 255 && pixel.A == 255;
        }
        public static bool IsCompletelyTransparent(this Rgba32 pixel)
        {
            return pixel.R == 0 && pixel.G == 0 && pixel.B == 0 && pixel.A == 0;
        }
    }
}
