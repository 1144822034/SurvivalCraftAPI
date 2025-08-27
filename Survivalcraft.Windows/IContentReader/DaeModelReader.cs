using Engine.Graphics;

namespace Game.IContentReader {
    public class DaeModelReader : IContentReader {
        public override string Type => "Engine.Graphics.Model";
        public override string[] DefaultSuffix => ["dae"];
        public override object Get(ContentInfo[] contents) => Model.Load(contents[0].Duplicate(), true);
    }
}