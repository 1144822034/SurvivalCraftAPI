using System;

namespace Game
{
    public class TerrainChunkSliceGeometry : TerrainGeometry
    {
        public TerrainChunkSliceGeometry()
        {
            this.Subsets = new TerrainGeometrySubset[7];
            for (int i = 0; i < this.Subsets.Length; i++)
            {
                this.Subsets[i] = new TerrainGeometrySubset();
            }
            this.SubsetOpaque = this.Subsets[4];
            this.SubsetAlphaTest = this.Subsets[5];
            this.SubsetTransparent = this.Subsets[6];
            this.OpaqueSubsetsByFace = new TerrainGeometrySubset[]
            {
                this.Subsets[0],
                this.Subsets[1],
                this.Subsets[2],
                this.Subsets[3],
                this.Subsets[4],
                this.Subsets[4]
            };
            this.AlphaTestSubsetsByFace = new TerrainGeometrySubset[]
            {
                this.Subsets[5],
                this.Subsets[5],
                this.Subsets[5],
                this.Subsets[5],
                this.Subsets[5],
                this.Subsets[5]
            };
            this.TransparentSubsetsByFace = new TerrainGeometrySubset[]
            {
                this.Subsets[6],
                this.Subsets[6],
                this.Subsets[6],
                this.Subsets[6],
                this.Subsets[6],
                this.Subsets[6]
            };
        }

        public const int OpaqueFace0Index = 0;

        public const int OpaqueFace1Index = 1;

        public const int OpaqueFace2Index = 2;

        public const int OpaqueFace3Index = 3;

        public const int OpaqueIndex = 4;

        public const int AlphaTestIndex = 5;

        public const int TransparentIndex = 6;

        public TerrainGeometrySubset[] Subsets = new TerrainGeometrySubset[7];

        public int ContentsHash;

        public int GeometryHash;
    }
}