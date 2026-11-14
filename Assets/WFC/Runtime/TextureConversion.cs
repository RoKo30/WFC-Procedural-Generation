using System;
using ProceduralGen.Wfc.Core;
using UnityEngine;

namespace ProceduralGen.Wfc
{
    // Convert Unity textures to ImageData
    public static class TextureConversion
    {
        public static ImageData ToImageData(Texture2D texture)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (!texture.isReadable)
                throw new ArgumentException(
                    $"Input texture '{texture.name}' is not readable. Enable Read/Write in its import settings " +
                    "(the generator inspector has a button that applies the right settings).");

            Color32[] pixels = texture.GetPixels32();
            var packed = new int[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                packed[i] = Pack(pixels[i]);

            return ImageData.FromRgba(texture.width, texture.height, packed);
        }

        public static int Pack(Color32 c) => ImageData.PackRgba(c.r, c.g, c.b, c.a);

        public static Color32 Unpack(int rgba)
        {
            ImageData.UnpackRgba(rgba, out byte r, out byte g, out byte b, out byte a);
            return new Color32(r, g, b, a);
        }

        public static Color32[] ToColors(int[] palette)
        {
            var colors = new Color32[palette.Length];
            for (int i = 0; i < palette.Length; i++) colors[i] = Unpack(palette[i]);
            return colors;
        }
    }
}
