using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
namespace Game
{
	public class TerrainChunk : IDisposable
	{
		public struct BrushPaint
		{
			public Point3 Position;

			public TerrainBrush Brush;
		}

		public const int SizeBits = 4;

		public const int Size = 16;

		public const int HeightBits = 8;

		public const int Height = 256;

		public const int SizeMinusOne = 15;

		public const int HeightMinusOne = 255;

		public const int SliceHeight = 16;

		public const int SlicesCount = 16;

		public Terrain Terrain;

		public Point2 Coords;

		public Point2 Origin;

		public BoundingBox BoundingBox;

		public Vector2 Center;

		public TerrainChunkState State;

		public TerrainChunkState ThreadState;

		public bool WasDowngraded;

		public TerrainChunkState? DowngradedState;

		public bool WasUpgraded;

		public TerrainChunkState? UpgradedState;

		public int ModificationCounter;

		public float[] HazeEnds = new float[4];

		public bool AreBehaviorsNotified;

		public bool IsLoaded;

		public volatile bool NewGeometryData;

		public TerrainChunkGeometry Geometry = new();

		public int[] Cells;

		public int[] Shafts;

        public static ArrayCache<int> m_cellsCache = new ArrayCache<int>((IEnumerable<int>)new int[1] { 65536 }, 0.66f, 60f, 0.33f, 5f);

        public static ArrayCache<int> m_shaftsCache = new ArrayCache<int>((IEnumerable<int>)new int[1] { 256 }, 0.66f, 60f, 0.33f, 5f);

		public DynamicArray<BrushPaint> m_brushPaints = [];
		
		public TerrainGeometry[] ChunkSliceGeometries = new TerrainGeometry[16];

		public DynamicArray<TerrainChunkGeometry.Buffer> Buffers = new DynamicArray<TerrainChunkGeometry.Buffer>();

		public int[] SliceContentsHashes = new int[16];

		public int[] GeneratedSliceContentsHashes = new int[16];

		public TerrainChunk(Terrain terrain, int x, int z)
		{
			Terrain = terrain;
			Coords = new Point2(x, z);
			Origin = new Point2(x * 16, z * 16);
			BoundingBox = new BoundingBox(new Vector3(Origin.X, 0f, Origin.Y), new Vector3(Origin.X + 16, 256f, Origin.Y + 16));
			Center = new Vector2((float)Origin.X + 8f, (float)Origin.Y + 8f);
            Cells = TerrainChunk.m_cellsCache.Rent(65536, true);
            Shafts = TerrainChunk.m_shaftsCache.Rent(256, true);
        }

		public virtual void DisposeVertexIndexBuffers()
		{
			foreach (var b in Buffers)
			{
				b.Dispose();
			}
			Buffers.Clear();
		}

		public virtual void InvalidateSliceContentsHashes()
		{
			for (int i = 0; i < GeneratedSliceContentsHashes.Length; i++)
			{
				GeneratedSliceContentsHashes[i] = 0;
			}
		}
		public virtual void CopySliceContentsHashes() {
			for (int i = 0; i < GeneratedSliceContentsHashes.Length; i++)
			{
				GeneratedSliceContentsHashes[i] = SliceContentsHashes[i];
			}
		}
		public virtual void Dispose()
		{
			DisposeVertexIndexBuffers();
            if (this.Geometry == null)
                throw new InvalidOperationException();
            this.Geometry = null;
            TerrainChunk.m_cellsCache.Return(this.Cells);
            TerrainChunk.m_shaftsCache.Return(this.Shafts);
        }

		public static bool IsCellValid(int x, int y, int z)
		{
			if (x >= 0 && x < 16 && y >= 0 && y < 256 && z >= 0)
			{
				return z < 16;
			}
			return false;
		}

		public static bool IsShaftValid(int x, int z)
		{
			if (x >= 0 && x < 16 && z >= 0)
			{
				return z < 16;
			}
			return false;
		}

		public static int CalculateCellIndex(int x, int y, int z)
		{
			if(y is >= 0 and < 256)
			{
				return y | (x << 8) | (z << 12);
			}
			else
			{
				int absY = Math.Abs(y);
				int yUpperBits = absY >> 8;
				if (yUpperBits > 0x7FFF)
				{
					throw new ArgumentOutOfRangeException(nameof(y), "Height is too large.");
				}
				int yLower8Bits = absY & 0xFF;
				yUpperBits = yUpperBits & 0x7FFF;
				return (((y < 0) ? 1 : 0) << 31) | (yUpperBits << 16) | (z << 12) | (x << 8) | yLower8Bits;
			}
		}

		public virtual int CalculateTopmostCellHeight(int x, int z)
		{
			int num = CalculateCellIndex(x, 255, z);
			int num2 = 255;
			while (num2 >= 0)
			{
				if (Terrain.ExtractContents(GetCellValueFast(num)) != 0)
				{
					return num2;
				}
				num2--;
				num--;
			}
			return 0;
		}

		public virtual int GetCellValueFast(int index)
		{
			return Cells[index];
		}

		public virtual int GetCellValueFast(int x, int y, int z)
		{
			return Cells[y + (x * 256) + (z * 256 * 16)];
		}

		public virtual void SetCellValueFast(int x, int y, int z, int value)
		{
			Cells[y + (x * 256) + (z * 256 * 16)] = value;
		}

		public virtual void SetCellValueFast(int index, int value)
		{
			Cells[index] = value;
		}

		public virtual int GetCellContentsFast(int x, int y, int z)
		{
			return Terrain.ExtractContents(GetCellValueFast(x, y, z));
		}

		public virtual int GetCellLightFast(int x, int y, int z)
		{
			return Terrain.ExtractLight(GetCellValueFast(x, y, z));
		}

		public virtual int GetShaftValueFast(int x, int z)
		{
			return Shafts[x + (z * 16)];
		}

		public virtual void SetShaftValueFast(int x, int z, int value)
		{
			Shafts[x + (z * 16)] = value;
		}

		public virtual int GetTemperatureFast(int x, int z)
		{
			return Terrain.ExtractTemperature(GetShaftValueFast(x, z));
		}

		public virtual void SetTemperatureFast(int x, int z, int temperature)
		{
			SetShaftValueFast(x, z, Terrain.ReplaceTemperature(GetShaftValueFast(x, z), temperature));
		}

		public virtual int GetHumidityFast(int x, int z)
		{
			return Terrain.ExtractHumidity(GetShaftValueFast(x, z));
		}

		public virtual void SetHumidityFast(int x, int z, int humidity)
		{
			SetShaftValueFast(x, z, Terrain.ReplaceHumidity(GetShaftValueFast(x, z), humidity));
		}

		public virtual int GetTopHeightFast(int x, int z)
		{
			return Terrain.ExtractTopHeight(GetShaftValueFast(x, z));
		}

		public virtual void SetTopHeightFast(int x, int z, int topHeight)
		{
			SetShaftValueFast(x, z, Terrain.ReplaceTopHeight(GetShaftValueFast(x, z), topHeight));
		}

		public virtual int GetBottomHeightFast(int x, int z)
		{
			return Terrain.ExtractBottomHeight(GetShaftValueFast(x, z));
		}

		public virtual void SetBottomHeightFast(int x, int z, int bottomHeight)
		{
			SetShaftValueFast(x, z, Terrain.ReplaceBottomHeight(GetShaftValueFast(x, z), bottomHeight));
		}

		public virtual int GetSunlightHeightFast(int x, int z)
		{
			return Terrain.ExtractSunlightHeight(GetShaftValueFast(x, z));
		}

		public virtual void SetSunlightHeightFast(int x, int z, int sunlightHeight)
		{
			SetShaftValueFast(x, z, Terrain.ReplaceSunlightHeight(GetShaftValueFast(x, z), sunlightHeight));
		}

		public virtual void AddBrushPaint(int x, int y, int z, TerrainBrush brush)
		{
			m_brushPaints.Add(new BrushPaint
			{
				Position = new Point3(x, y, z),
				Brush = brush
			});
		}

		public virtual void ApplyBrushPaints(TerrainChunk chunk)
		{
			foreach (BrushPaint brushPaint in m_brushPaints)
			{
				brushPaint.Brush.PaintFast(chunk, brushPaint.Position.X, brushPaint.Position.Y, brushPaint.Position.Z);
			}
		}
	}
}
