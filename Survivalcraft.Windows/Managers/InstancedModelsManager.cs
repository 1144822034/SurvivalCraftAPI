using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;

namespace Game
{
	public static class InstancedModelsManager
	{
		public struct SourceModelVertex
		{
			public float X;

			public float Y;

			public float Z;

			public float Nx;

			public float Ny;

			public float Nz;

			public float Tx;

			public float Ty;
		}

		public struct InstancedVertex
		{
			public float X;

			public float Y;

			public float Z;

			public float Nx;

			public float Ny;

			public float Nz;

			public float Tx;

			public float Ty;

			public float Instance;
		}

		public static Dictionary<Model, InstancedModelData> m_cache;

		static InstancedModelsManager()
		{
			m_cache = [];
			Display.DeviceReset += delegate
			{
				foreach (InstancedModelData value in m_cache.Values)
				{
					value.VertexBuffer.Dispose();
					value.IndexBuffer.Dispose();
				}
				m_cache.Clear();
			};
		}

		public static InstancedModelData GetInstancedModelData(Model model, int[] meshDrawOrders)
		{
			if (!m_cache.TryGetValue(model, out InstancedModelData value))
			{
				value = CreateInstancedModelData(model, meshDrawOrders);
				m_cache.Add(model, value);
			}
			return value;
		}

		public static InstancedModelData CreateInstancedModelData(Model model, int[] meshDrawOrders)
		{
			var dynamicArray = new DynamicArray<InstancedVertex>();
			var dynamicArray2 = new DynamicArray<int>();
			for (int i = 0; i < meshDrawOrders.Length; i++)
			{
				ModelMesh modelMesh = model.Meshes[meshDrawOrders[i]];
				foreach (ModelMeshPart meshPart in modelMesh.MeshParts)
				{
					_ = dynamicArray.Count;
					VertexBuffer vertexBuffer = meshPart.VertexBuffer;
					IndexBuffer indexBuffer = meshPart.IndexBuffer;
					ReadOnlyList<VertexElement> vertexElements = vertexBuffer.VertexDeclaration.VertexElements;
					int[] indexData = BlockMesh.GetIndexData<int>(indexBuffer);
					var dictionary = new Dictionary<int, int>();
					if (vertexElements.Count != 3 || vertexElements[0].Offset != 0 || !(vertexElements[0].Semantic == VertexElementSemantic.Position.GetSemanticString()) || vertexElements[1].Offset != 12 || !(vertexElements[1].Semantic == VertexElementSemantic.Normal.GetSemanticString()) || vertexElements[2].Offset != 24 || !(vertexElements[2].Semantic == VertexElementSemantic.TextureCoordinate.GetSemanticString()))
					{
						throw new InvalidOperationException("Unsupported vertex format.");
					}
					SourceModelVertex[] vertexData = BlockMesh.GetVertexData<SourceModelVertex>(vertexBuffer);
					for (int j = meshPart.StartIndex; j < meshPart.StartIndex + meshPart.IndicesCount; j++)
					{
						int num = indexData[j];
						if (!dictionary.ContainsKey(num))
						{
							dictionary.Add(num, dynamicArray.Count);
							InstancedVertex item = default;
							SourceModelVertex sourceModelVertex = vertexData[num];
							item.X = sourceModelVertex.X;
							item.Y = sourceModelVertex.Y;
							item.Z = sourceModelVertex.Z;
							item.Nx = sourceModelVertex.Nx;
							item.Ny = sourceModelVertex.Ny;
							item.Nz = sourceModelVertex.Nz;
							item.Tx = sourceModelVertex.Tx;
							item.Ty = sourceModelVertex.Ty;
							item.Instance = modelMesh.ParentBone.Index;
							dynamicArray.Add(item);
						}
					}
					for (int k = 0; k < meshPart.IndicesCount / 3; k++)
					{
						dynamicArray2.Add(dictionary[indexData[meshPart.StartIndex + (3 * k)]]);
						dynamicArray2.Add(dictionary[indexData[meshPart.StartIndex + (3 * k) + 1]]);
						dynamicArray2.Add(dictionary[indexData[meshPart.StartIndex + (3 * k) + 2]]);
					}
				}
			}
			var instancedModelData = new InstancedModelData();
			instancedModelData.VertexBuffer = new VertexBuffer(InstancedModelData.VertexDeclaration, dynamicArray.Count);
			instancedModelData.IndexBuffer = new IndexBuffer(IndexFormat.ThirtyTwoBits, dynamicArray2.Count);
			instancedModelData.VertexBuffer.SetData(dynamicArray.Array, 0, dynamicArray.Count);
			instancedModelData.IndexBuffer.SetData(dynamicArray2.Array, 0, dynamicArray2.Count);
			return instancedModelData;
		}
	}
}
