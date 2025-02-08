using Engine.Graphics;
using System.Collections.Generic;

namespace Game
{
    public class TerrainGeometry
    {
        public TerrainGeometrySubset SubsetOpaque;

        public TerrainGeometrySubset SubsetAlphaTest;

        public TerrainGeometrySubset SubsetTransparent;

        public TerrainGeometrySubset[] OpaqueSubsetsByFace;

        public TerrainGeometrySubset[] AlphaTestSubsetsByFace;

        public TerrainGeometrySubset[] TransparentSubsetsByFace;

        public TerrainGeometrySubset[] Subsets;

        public Dictionary<Texture2D, TerrainGeometry> Draws = null;

        public Texture2D DefaultTexture;
        
        [Obsolete("此方法将弃用")]
        public TerrainGeometry()
        {
            InitSubsets();
        }
        public TerrainGeometry(Texture2D texture2D)
        {
	        InitSubsets();
	        DefaultTexture = texture2D;
	        //添加到默认纹理区
	        Draws = new();
	        Draws.Add(DefaultTexture,this);
        }

        public virtual void InitSubsets()
        {
            Subsets = new TerrainGeometrySubset[7];
            for(int i = 0; i < 7; i++)
            {
	            Subsets[i] = new TerrainGeometrySubset();
            }
            SubsetOpaque = Subsets[4];
            SubsetAlphaTest = Subsets[5];
            SubsetTransparent = Subsets[6];
            OpaqueSubsetsByFace = new TerrainGeometrySubset[6]
            {
                Subsets[0],
                Subsets[1],
                Subsets[2],
                Subsets[3],
                Subsets[4],
                Subsets[4]
            };
            AlphaTestSubsetsByFace = new TerrainGeometrySubset[6]
            {
                Subsets[5],
                Subsets[5],
                Subsets[5],
                Subsets[5],
                Subsets[5],
                Subsets[5]
            };
            TransparentSubsetsByFace = new TerrainGeometrySubset[6]
            {
                Subsets[6],
                Subsets[6],
                Subsets[6],
                Subsets[6],
                Subsets[6],
                Subsets[6]
            };
        }

        public virtual TerrainGeometry GetGeometry(Texture2D texture)
        {
            Draws ??= [];
            if (Draws.TryGetValue(texture, out var geometries)) return geometries;
            else
            {
                var geometry = new TerrainGeometry();
                Draws.Add(texture, geometry);
                return geometry;
            }
        }

        public virtual void ClearGeometry()
        {
	        foreach(var subset in Subsets)
	        {
		        subset.Indices.Clear();
		        subset.Vertices.Clear();
	        }
	        if(Draws==null) return;
	        foreach(var drawItem in Draws)
	        {
		        if(drawItem.Value!=this) drawItem.Value.ClearGeometry();   
	        }
	        Draws.Clear();
	        if(DefaultTexture!=null)Draws.Add(DefaultTexture,this);
        }
    }
}