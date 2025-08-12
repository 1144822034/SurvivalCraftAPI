using System.Collections.Generic;

namespace Game;

public class DirtSlabBlock : SlabBlock
{
	public const int Index = 259;

	public DirtSlabBlock()
		: base(2, 2)
	{
	}

	public override int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		return value;
	}

	public override int? GetPaintColor(int value)
	{
		return null;
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SlabBlock.SetColor(0, null));
	}
}
