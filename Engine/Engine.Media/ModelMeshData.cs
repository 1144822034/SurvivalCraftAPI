namespace Engine.Media
{
	public class ModelMeshData
	{
		public string Name;

		public int ParentBoneIndex;

		public List<ModelMeshPartData> MeshParts = [];

		public BoundingBox BoundingBox;
	}
}
