using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentVrHandsModel : Component,IDrawable,IUpdateable
	{
		public SubsystemTerrain m_subsystemTerrain;

		public ComponentPlayer m_componentPlayer;

		public ComponentMiner m_componentMiner;

		public Model m_vrHandModel;

		public Vector3 m_itemOffset;

		public Vector3 m_itemRotation;

		public float m_pokeAnimationTime;

		public double m_nextHandLightTime;

		public float m_handLight;

		public int m_itemLight;

		public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new DrawBlockEnvironmentData();

		public PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

		public static LitShader m_shader = new LitShader(
			2,
			useEmissionColor: false,
			useVertexColor: false,
			useTexture: true,
			useFog: false,
			useAlphaThreshold: false
		);

		public static int[] m_drawOrders = new int[1] { 1 };

		public Vector3 ItemOffsetOrder { get; set; }

		public Vector3 ItemRotationOrder { get; set; }

		public int[] DrawOrders => m_drawOrders;

		public UpdateOrder UpdateOrder => UpdateOrder.FirstPersonModels;

		public void Draw(Camera camera,int drawOrder)
		{
			if(!(m_componentPlayer.ComponentHealth.Health > 0f)
				|| !camera.GameWidget.IsEntityFirstPersonTarget(base.Entity)
				|| !m_componentPlayer.ComponentInput.IsControlledByVr)
			{
				return;
			}
			Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
			int x = Terrain.ToCell(eyePosition.X);
			int num = Terrain.ToCell(eyePosition.Y);
			int z = Terrain.ToCell(eyePosition.Z);
			int activeBlockValue = m_componentMiner.ActiveBlockValue;
			if(Time.FrameStartTime >= m_nextHandLightTime)
			{
				float? num2 = LightingManager.CalculateSmoothLight(m_subsystemTerrain,eyePosition);
				if(num2.HasValue)
				{
					m_nextHandLightTime = Time.FrameStartTime + 0.1;
					m_handLight = num2.Value;
				}
			}
			Matrix identity = Matrix.Identity;
			if(m_pokeAnimationTime > 0f)
			{
				float num3 = MathF.Sin(MathF.Sqrt(m_pokeAnimationTime) * (float)Math.PI);
				if(activeBlockValue != 0)
				{
					identity *= Matrix.CreateRotationX((0f - MathUtils.DegToRad(90f)) * num3);
				}
				else
				{
					identity *= Matrix.CreateRotationX((0f - MathUtils.DegToRad(45f)) * num3);
				}
			}
			if(!VrManager.IsControllerPresent(VrController.Right))
			{
				return;
			}
			Matrix matrix = VrManager.HmdMatrixInverted * Matrix.CreateWorld(camera.ViewPosition,camera.ViewDirection,camera.ViewUp) * camera.ViewMatrix;
			Matrix controllerMatrix = VrManager.GetControllerMatrix(VrController.Right);
			if(activeBlockValue == 0)
			{
				Display.DepthStencilState = DepthStencilState.Default;
				Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
				m_shader.Texture = m_componentPlayer.ComponentCreatureModel.TextureOverride;
				m_shader.SamplerState = SamplerState.PointClamp;
				m_shader.MaterialColor = Vector4.One;
				m_shader.AmbientLightColor = new Vector3(m_handLight * LightingManager.LightAmbient);
				m_shader.DiffuseLightColor1 = new Vector3(m_handLight);
				m_shader.DiffuseLightColor2 = new Vector3(m_handLight);
				m_shader.LightDirection1 = -Vector3.TransformNormal(LightingManager.DirectionToLight1,camera.ViewMatrix);
				m_shader.LightDirection2 = -Vector3.TransformNormal(LightingManager.DirectionToLight2,camera.ViewMatrix);
				m_shader.Transforms.View = Matrix.Identity;
				m_shader.Transforms.Projection = camera.ProjectionMatrix;
				m_shader.Transforms.World[0] = Matrix.CreateScale(0.01f) * identity * controllerMatrix * matrix;
				foreach(ModelMesh mesh in m_vrHandModel.Meshes)
				{
					foreach(ModelMeshPart meshPart in mesh.MeshParts)
					{
						Display.DrawIndexed(
							PrimitiveType.TriangleList,
							m_shader,
							meshPart.VertexBuffer,
							meshPart.IndexBuffer,
							meshPart.StartIndex,
							meshPart.IndicesCount
						);
					}
				}
			}
			else
			{
				if(num >= 0
					&& num <= 255)
				{
					TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x,z);
					if(chunkAtCell != null
						&& chunkAtCell.State >= TerrainChunkState.InvalidVertices1)
					{
						m_itemLight = m_subsystemTerrain.Terrain.GetCellLightFast(x,num,z);
					}
				}
				int num4 = Terrain.ExtractContents(activeBlockValue);
				Block block = BlocksManager.Blocks[num4];
				Vector3 vector = block.InHandRotation * ((float)Math.PI / 180f) + m_itemRotation;
				Matrix matrix2 = Matrix.CreateFromYawPitchRoll(vector.Y,vector.X,vector.Z) * Matrix.CreateTranslation(block.InHandOffset) * identity * Matrix.CreateTranslation(m_itemOffset) * controllerMatrix * matrix;
				m_drawBlockEnvironmentData.DrawBlockMode = DrawBlockMode.FirstPerson;
				m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
				m_drawBlockEnvironmentData.InWorldMatrix = matrix2;
				m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetHumidity(x,z);
				m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x,z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num);
				m_drawBlockEnvironmentData.Light = m_itemLight;
				m_drawBlockEnvironmentData.EnvironmentTemperature = m_componentPlayer.ComponentVitalStats.EnvironmentTemperature;
				block.DrawBlock(
					m_primitivesRenderer,
					activeBlockValue,
					Color.White,
					block.InHandScale,
					ref matrix2,
					m_drawBlockEnvironmentData
				);
			}
			m_primitivesRenderer.Flush(camera.ProjectionMatrix);
		}

		public void Update(float dt)
		{
			m_pokeAnimationTime = m_componentMiner.PokingPhase;
			m_itemOffset = Vector3.Lerp(m_itemOffset,ItemOffsetOrder,MathUtils.Saturate(10f * dt));
			m_itemRotation = Vector3.Lerp(m_itemRotation,ItemRotationOrder,MathUtils.Saturate(10f * dt));
			ItemOffsetOrder = Vector3.Zero;
			ItemRotationOrder = Vector3.Zero;
		}

		public override void Load(ValuesDictionary valuesDictionary,IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			m_componentMiner = base.Entity.FindComponent<ComponentMiner>(throwOnError: true);
			m_vrHandModel = ContentManager.Get<Model>(valuesDictionary.GetValue<string>("VrHandModelName"));
		}
	}
}