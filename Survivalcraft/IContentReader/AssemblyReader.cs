using System.Reflection;

namespace Game.IContentReader
{
    public class AssemblyReader : IContentReader
    {
        public override string Type => "System.Reflection.Assembly";
        public override string[] DefaultSuffix => ["dll","Exp"];
        public override object Get(ContentInfo[] contents)
        {
            return Assembly.Load(ModsManager.StreamToBytes(contents[0].Duplicate()));
        }
    }
}