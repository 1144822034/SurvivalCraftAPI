using Engine.Graphics;

namespace Game.IContentReader {
    public class Texture2DReader : IContentReader {
        public override string Type => "Engine.Graphics.Texture2D";
        public override string[] DefaultSuffix => ["webp", "png", "jpg", "jpeg", "astc", "astcsrgb"];

        public override object Get(ContentInfo[] contents) {
            ContentInfo contentInfo = contents[0];
            if (contentInfo.ContentPath == "Fonts/Pericles") {
                return Texture2D.Load(ContentManager.Get<Image>(contentInfo.ContentPath, contentInfo.ContentSuffix), 3);
            }
            string suffix = contentInfo.ContentSuffix.ToLower();
            return suffix switch {
#if DIRECT3D11
                ".astc" or ".astcsrgb" => throw new NotImplementedException(),
#else
                ".astc" => CompressedTexture2D.Load(contentInfo.Duplicate()),
                ".astcsrgb" => CompressedTexture2D.Load(contentInfo.Duplicate(), false),
#endif
                _ => Texture2D.Load(ContentManager.Get<Image>(contentInfo.ContentPath, suffix))
            };
        }
    }
}