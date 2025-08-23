using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentBlockHighlight : Component, IDrawable, IUpdateable
	{

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

		public SubsystemSky m_subsystemSky;

		public ComponentPlayer m_componentPlayer;

		public PrimitivesRenderer3D m_primitivesRenderer3D = new();

		public Shader m_shader;

		public CellFace m_cellFace;

		public int m_value;

		public object m_highlightRaycastResult;

		public static int[] m_drawOrders =
		[
			1,
			2000
		];

		public Point3? NearbyEditableCell
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder => UpdateOrder.BlockHighlight;

		public int[] DrawOrders => m_drawOrders;

		public virtual void Update(float dt)
		{
			Camera activeCamera = m_componentPlayer.GameWidget.ActiveCamera;
			var ray = new Ray3?(new Ray3(activeCamera.ViewPosition, activeCamera.ViewDirection));
			NearbyEditableCell = null;
			if (ray.HasValue)
			{
				m_highlightRaycastResult = m_componentPlayer.ComponentMiner.Raycast(ray.Value, RaycastMode.Digging);
				if (!(m_highlightRaycastResult is TerrainRaycastResult))
				{
					return;
				}
				var terrainRaycastResult = (TerrainRaycastResult)m_highlightRaycastResult;
				if (terrainRaycastResult.Distance < 3f)
				{
					Point3 point = terrainRaycastResult.CellFace.Point;
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
					Block obj = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
					if (obj is CrossBlock)
					{
						terrainRaycastResult.Distance = MathUtils.Max(terrainRaycastResult.Distance, 0.1f);
						m_highlightRaycastResult = terrainRaycastResult;
					}
					if (obj.IsEditable_(cellValue))
					{
						NearbyEditableCell = terrainRaycastResult.CellFace.Point;
					}
				}
			}
			else
			{
				m_highlightRaycastResult = null;
			}
		}

		public virtual void Draw(Camera camera, int drawOrder)
		{
			if (camera.GameWidget.PlayerData == m_componentPlayer.PlayerData)
			{
				if (drawOrder == m_drawOrders[0])
				{
					DrawFillHighlight(camera);
					DrawOutlineHighlight(camera);
					DrawReticleHighlight(camera);
				}
				else
				{
					DrawRayHighlight(camera);
				}
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemAnimatedTextures = Project.FindSubsystem<SubsystemAnimatedTextures>(throwOnError: true);
			m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			m_shader = new Shader(ModsManager.GetInPakOrStorageFile<string>("Shaders/Highlight", "vsh"), ModsManager.GetInPakOrStorageFile<string>("Shaders/Highlight", "psh"),[new("ShadowShader")]);
		}

		public virtual void DrawRayHighlight(Camera camera)
		{
			if (!camera.Eye.HasValue)
			{
				return;
			}
			Ray3 ray;
			float num;
			if (m_highlightRaycastResult is TerrainRaycastResult)
			{
				var obj = (TerrainRaycastResult)m_highlightRaycastResult;
				ray = obj.Ray;
				num = MathUtils.Min(obj.Distance, 2f);
			}
			else if (m_highlightRaycastResult is BodyRaycastResult)
			{
				var obj2 = (BodyRaycastResult)m_highlightRaycastResult;
				ray = obj2.Ray;
				num = MathUtils.Min(obj2.Distance, 2f);
			}
			else if (m_highlightRaycastResult is MovingBlocksRaycastResult)
			{
				var obj3 = (MovingBlocksRaycastResult)m_highlightRaycastResult;
				ray = obj3.Ray;
				num = MathUtils.Min(obj3.Distance, 2f);
			}
			else
			{
				if (!(m_highlightRaycastResult is Ray3))
				{
					return;
				}
				ray = (Ray3)m_highlightRaycastResult;
				num = 2f;
			}
			Color color = Color.White * 0.5f;
			var color2 = Color.Lerp(color, Color.Transparent, MathUtils.Saturate(num / 2f));
			FlatBatch3D flatBatch3D = m_primitivesRenderer3D.FlatBatch();
			flatBatch3D.QueueLine(ray.Position, ray.Position + (ray.Direction * num), color, color2);
			flatBatch3D.Flush(camera.ViewProjectionMatrix);
		}

		public virtual void DrawReticleHighlight(Camera camera)
		{
			// TODO: 加上？
		}

		public virtual void DrawFillHighlight(Camera camera)
		{
			// TODO: 加上？
		}

		public virtual void DrawOutlineHighlight(Camera camera)
		{
			if (camera.UsesMovementControls || !(m_componentPlayer.ComponentHealth.Health > 0f) || !m_componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible)
			{
				return;
			}
			if (m_componentPlayer.ComponentMiner.DigCellFace.HasValue)
			{
				CellFace value = m_componentPlayer.ComponentMiner.DigCellFace.Value;
				BoundingBox cellFaceBoundingBox = GetCellFaceBoundingBox(value.Point);
				float num = m_subsystemSky.CalculateFog(camera.ViewPosition, cellFaceBoundingBox.Center());
				Color color = Color.MultiplyNotSaturated(Color.Black, 1f - num);
				DrawBoundingBoxFace(m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None), value.Face, cellFaceBoundingBox.Min, cellFaceBoundingBox.Max, color);
			}
			else
			{
				if (!m_componentPlayer.ComponentAimingSights.IsSightsVisible && (SettingsManager.LookControlMode == LookControlMode.SplitTouch || !m_componentPlayer.ComponentInput.IsControlledByTouch) && m_highlightRaycastResult is TerrainRaycastResult)
				{
					CellFace cellFace = ((TerrainRaycastResult)m_highlightRaycastResult).CellFace;
					BoundingBox cellFaceBoundingBox2 = GetCellFaceBoundingBox(cellFace.Point);
					float num2 = m_subsystemSky.CalculateFog(camera.ViewPosition, cellFaceBoundingBox2.Center());
					Color color2 = Color.MultiplyNotSaturated(Color.Black, 1f - num2);
					DrawBoundingBoxFace(m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None), cellFace.Face, cellFaceBoundingBox2.Min, cellFaceBoundingBox2.Max, color2);
				}
				if (NearbyEditableCell.HasValue)
				{
					BoundingBox cellFaceBoundingBox3 = GetCellFaceBoundingBox(NearbyEditableCell.Value);
					float num3 = m_subsystemSky.CalculateFog(camera.ViewPosition, cellFaceBoundingBox3.Center());
					Color color3 = Color.MultiplyNotSaturated(Color.Black, 1f - num3);
					m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None).QueueBoundingBox(cellFaceBoundingBox3, color3);
				}
			}
			m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
		}

		public static void DrawBoundingBoxFace(FlatBatch3D batch, int face, Vector3 c1, Vector3 c2, Color color)
		{
			switch (face)
			{
				case 0:
					batch.QueueLine(new Vector3(c1.X, c1.Y, c2.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c2.X, c2.Y, c2.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c2.X, c1.Y, c2.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c1.X, c2.Y, c2.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
					break;
				case 1:
					batch.QueueLine(new Vector3(c2.X, c1.Y, c2.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c2.Y, c1.Z), color);
					batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
					break;
				case 2:
					batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c1.Z), color);
					batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c2.Y, c1.Z), color);
					batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c1.Z), color);
					batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c1.Y, c1.Z), color);
					break;
				case 3:
					batch.QueueLine(new Vector3(c1.X, c2.Y, c2.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c1.Y, c1.Z), color);
					batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
					break;
				case 4:
					batch.QueueLine(new Vector3(c2.X, c2.Y, c2.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c1.Z), color);
					batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
					break;
				case 5:
					batch.QueueLine(new Vector3(c1.X, c1.Y, c2.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c1.Z), color);
					batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
					batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
					break;
			}
		}

		public BoundingBox GetCellFaceBoundingBox(Point3 point)
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
			BoundingBox[] customCollisionBoxes = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].GetCustomCollisionBoxes(m_subsystemTerrain, cellValue);
			var vector = new Vector3(point.X, point.Y, point.Z);
			if (customCollisionBoxes.Length != 0)
			{
				BoundingBox? boundingBox = null;
				for (int i = 0; i < customCollisionBoxes.Length; i++)
				{
					if (customCollisionBoxes[i] != default)
					{
						boundingBox = boundingBox.HasValue ? BoundingBox.Union(boundingBox.Value, customCollisionBoxes[i]) : customCollisionBoxes[i];
					}
				}
				if (!boundingBox.HasValue)
				{
					boundingBox = new BoundingBox(Vector3.Zero, Vector3.One);
				}
				return new BoundingBox(boundingBox.Value.Min + vector, boundingBox.Value.Max + vector);
			}
			return new BoundingBox(vector, vector + Vector3.One);
		}
	}
}
