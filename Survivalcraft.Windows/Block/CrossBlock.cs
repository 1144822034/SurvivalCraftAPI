using Engine;
using Engine.Graphics;

namespace Game
{
	public abstract class CrossBlock : Block
	{
		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			generator.GenerateCrossfaceVertices(this, value, x, y, z, Color.White, GetFaceTextureSlot(0, value), geometry.SubsetAlphaTest);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawFlatOrImageExtrusionBlock(primitivesRenderer, value, size, ref matrix, null, color, isEmissive: false, environmentData);
		}
	}
}
