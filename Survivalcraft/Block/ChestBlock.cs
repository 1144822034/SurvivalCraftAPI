using Engine;

namespace Game
{
	public class ChestBlock : CubeBlock
	{
		public static int Index = 45;

		public override int GetFaceTextureSlot(int face, int value)
		{
			switch (face)
			{
				case 4:
					return 42;
				case 5:
					return 42;
				default:
					switch (Terrain.ExtractData(value))
					{
						case 0:
							switch (face)
							{
								case 0:
									return 27;
								case 2:
									return 26;
								default:
									return 25;
							}
						case 1:
							switch (face)
							{
								case 1:
									return 27;
								case 3:
									return 26;
								default:
									return 25;
							}
						case 2:
							switch (face)
							{
								case 2:
									return 27;
								case 0:
									return 26;
								default:
									return 25;
							}
						default:
							switch (face)
							{
								case 3:
									return 27;
								case 1:
									return 26;
								default:
									return 25;
							}
					}
			}
		}

		public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
		{
			Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
			float num = Vector3.Dot(forward, Vector3.UnitZ);
			float num2 = Vector3.Dot(forward, Vector3.UnitX);
			float num3 = Vector3.Dot(forward, -Vector3.UnitZ);
			float num4 = Vector3.Dot(forward, -Vector3.UnitX);
			int data = 0;
			if (num == MathUtils.Max(num, num2, num3, num4))
			{
				data = 2;
			}
			else if (num2 == MathUtils.Max(num, num2, num3, num4))
			{
				data = 3;
			}
			else if (num3 == MathUtils.Max(num, num2, num3, num4))
			{
				data = 0;
			}
			else if (num4 == MathUtils.Max(num, num2, num3, num4))
			{
				data = 1;
			}
			BlockPlacementData result = default;
			result.Value = Terrain.ReplaceData(Terrain.ReplaceContents( 45), data);
			result.CellFace = raycastResult.CellFace;
			return result;
		}
    }
}
