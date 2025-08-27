namespace Engine.Media {
    public class ModelData {
        public List<ModelBoneData> Bones = [];

        public List<ModelMeshData> Meshes = [];

        public List<ModelBuffersData> Buffers = [];

        public static ModelFileFormat DetermineFileFormat(Stream stream) => Collada.IsColladaStream(stream)
            ? ModelFileFormat.Collada
            : throw new InvalidOperationException("Unsupported model file format.");

        public static ModelFileFormat DetermineFileFormat(string extension) => extension.Equals(".dae", StringComparison.OrdinalIgnoreCase)
            ? ModelFileFormat.Collada
            : throw new InvalidOperationException("Unsupported model file format.");

        public static ModelData Load(Stream stream, ModelFileFormat format) => format == ModelFileFormat.Collada
            ? Collada.Load(stream)
            : throw new InvalidOperationException("Unsupported model file format.");

        public static ModelData Load(string fileName, ModelFileFormat format) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream, format);
            }
        }

        public static ModelData Load(Stream stream) {
            PeekStream peekStream = new(stream, 256);
            ModelFileFormat format = DetermineFileFormat(peekStream.GetInitialBytesStream());
            return Load(peekStream, format);
        }

        public static ModelData Load(string fileName) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream);
            }
        }

        public static void Save(ModelData modelData, Stream stream, ModelFileFormat format) {
            if (format == ModelFileFormat.Collada) {
                Collada.Save(modelData, stream);
                return;
            }
            throw new InvalidOperationException("Unsupported model file format.");
        }

        public static void Save(ModelData modelData, string fileName, ModelFileFormat format) {
            using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Create);
            Save(modelData, stream, format);
        }
    }
}