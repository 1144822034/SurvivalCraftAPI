using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Engine.Graphics
{
	public class LitShader : TransformedShader
	{
		public ShaderParameter m_worldMatrixParameter;

		public ShaderParameter m_worldViewMatrixParameter;

		public ShaderParameter m_worldViewProjectionMatrixParameter;

		public ShaderParameter m_textureParameter;

		public ShaderParameter m_samplerStateParameter;

		public ShaderParameter m_materialColorParameter;

		public ShaderParameter m_emissionColorParameter;

		public ShaderParameter m_alphaThresholdParameter;

		public ShaderParameter m_ambientLightColorParameter;

		public ShaderParameter m_diffuseLightColor1Parameter;

		public ShaderParameter m_directionToLight1Parameter;

		public ShaderParameter m_diffuseLightColor2Parameter;

		public ShaderParameter m_directionToLight2Parameter;

		public ShaderParameter m_diffuseLightColor3Parameter;

		public ShaderParameter m_directionToLight3Parameter;

		public ShaderParameter m_fogStartParameter;

		public ShaderParameter m_fogLengthParameter;

		public ShaderParameter m_fogColorParameter;

		public ShaderParameter m_time;

		public int m_instancesCount;

		public int m_lightsCount;

		public bool m_useFog;

		public Texture2D Texture
		{
			set
			{
				m_textureParameter.SetValue(value);
			}
		}

		public SamplerState SamplerState
		{
			set
			{
				m_samplerStateParameter.SetValue(value);
			}
		}

		public Vector4 MaterialColor
		{
			set
			{
				m_materialColorParameter.SetValue(value);
			}
		}

		public Vector4 EmissionColor
		{
			set
			{
				m_emissionColorParameter.SetValue(value);
			}
		}

		public float AlphaThreshold
		{
			set
			{
				m_alphaThresholdParameter.SetValue(value);
			}
		}

		public Vector3 AmbientLightColor
		{
			set
			{
				m_ambientLightColorParameter.SetValue(value);
			}
		}

		public Vector3 DiffuseLightColor1
		{
			set
			{
				m_diffuseLightColor1Parameter.SetValue(value);
			}
		}

		public Vector3 DiffuseLightColor2
		{
			set
			{
				m_diffuseLightColor2Parameter.SetValue(value);
			}
		}

		public Vector3 DiffuseLightColor3
		{
			set
			{
				m_diffuseLightColor3Parameter.SetValue(value);
			}
		}

		public Vector3 LightDirection1
		{
			set
			{
				m_directionToLight1Parameter.SetValue(-value);
			}
		}

		public Vector3 LightDirection2
		{
			set
			{
				m_directionToLight2Parameter.SetValue(-value);
			}
		}

		public Vector3 LightDirection3
		{
			set
			{
				m_directionToLight3Parameter.SetValue(-value);
			}
		}

		public float FogStart
		{
			set
			{
				m_fogStartParameter.SetValue(value);
			}
		}

		public float FogLength
		{
			set
			{
				m_fogLengthParameter.SetValue(value);
			}
		}

		public Vector3 FogColor
		{
			set
			{
				m_fogColorParameter.SetValue(value);
			}
		}

		public float Time
		{
			set
			{
				m_time.SetValue(value);
			}
		}

		public int InstancesCount
		{
			get
			{
				return m_instancesCount;
			}
			set
			{
				if (value < 0 || value > Transforms.MaxWorldMatrices)
				{
					throw new InvalidOperationException("Invalid instances count.");
				}
				m_instancesCount = value;
			}
		}

		public LitShader(string vsc, string psc, int lightsCount, bool useEmissionColor, bool useVertexColor, bool useTexture, bool useFog, bool useAlphaThreshold, int maxInstancesCount = 1)
			: base(vsc, psc, maxInstancesCount, PrepareShaderMacros(lightsCount, useEmissionColor, useVertexColor, useTexture, useFog, useAlphaThreshold, maxInstancesCount))
		{
			if (lightsCount < 0 || lightsCount > 3)
			{
				throw new ArgumentException("lightsCount");
			}
			if (maxInstancesCount < 0 || maxInstancesCount > 32)
			{
				throw new ArgumentException("maxInstancesCount");
			}
			m_worldMatrixParameter = GetParameter("u_worldMatrix", allowNull: true);
			m_worldViewMatrixParameter = GetParameter("u_worldViewMatrix", allowNull: true);
			m_worldViewProjectionMatrixParameter = GetParameter("u_worldViewProjectionMatrix", allowNull: true);
			m_textureParameter = GetParameter("u_texture", allowNull: true);
			m_samplerStateParameter = GetParameter("u_samplerState", allowNull: true);
			m_materialColorParameter = GetParameter("u_materialColor", allowNull: true);
			m_emissionColorParameter = GetParameter("u_emissionColor", allowNull: true);
			m_alphaThresholdParameter = GetParameter("u_alphaThreshold", allowNull: true);
			m_ambientLightColorParameter = GetParameter("u_ambientLightColor", allowNull: true);
			m_diffuseLightColor1Parameter = GetParameter("u_diffuseLightColor1", allowNull: true);
			m_directionToLight1Parameter = GetParameter("u_directionToLight1", allowNull: true);
			m_diffuseLightColor2Parameter = GetParameter("u_diffuseLightColor2", allowNull: true);
			m_directionToLight2Parameter = GetParameter("u_directionToLight2", allowNull: true);
			m_diffuseLightColor3Parameter = GetParameter("u_diffuseLightColor3", allowNull: true);
			m_directionToLight3Parameter = GetParameter("u_directionToLight3", allowNull: true);
			m_fogStartParameter = GetParameter("u_fogStart", allowNull: true);
			m_fogLengthParameter = GetParameter("u_fogLength", allowNull: true);
			m_fogColorParameter = GetParameter("u_fogColor", allowNull: true);
			m_time = GetParameter("u_time", allowNull: true);
			m_lightsCount = lightsCount;
			m_instancesCount = 1;
			m_useFog = useFog;
			MaterialColor = Vector4.One;
			if (useEmissionColor)
			{
				EmissionColor = Vector4.Zero;
			}
			if (lightsCount >= 1)
			{
				AmbientLightColor = new Vector3(0.2f);
				DiffuseLightColor1 = new Vector3(0.8f);
				LightDirection1 = Vector3.Normalize(new Vector3(1f, -1f, 1f));
			}
			if (lightsCount >= 2)
			{
				DiffuseLightColor2 = new Vector3(0.4f);
				LightDirection2 = Vector3.Normalize(new Vector3(-1f, -0.5f, -0.25f));
			}
			if (lightsCount >= 3)
			{
				DiffuseLightColor3 = new Vector3(0.2f);
				LightDirection3 = Vector3.Normalize(new Vector3(0f, 1f, 0f));
			}
			if (useFog)
			{
				FogLength = 100f;
			}
		}

		public LitShader(int lightsCount, bool useEmissionColor, bool useVertexColor, bool useTexture, bool useFog, bool useAlphaThreshold, int maxInstancesCount = 1)
#if ANDROID
			: base(new StreamReader(Storage.OpenFile("app:Lit.vsh", OpenFileMode.Read)).ReadToEnd(), new StreamReader(Storage.OpenFile("app:Lit.psh", OpenFileMode.Read)).ReadToEnd(), maxInstancesCount, PrepareShaderMacros(lightsCount, useEmissionColor, useVertexColor, useTexture, useFog, useAlphaThreshold, maxInstancesCount))
#else
            : base(new StreamReader(typeof(Shader).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.Lit.vsh")).ReadToEnd(), new StreamReader(typeof(Shader).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.Lit.psh")).ReadToEnd(), maxInstancesCount, PrepareShaderMacros(lightsCount, useEmissionColor, useVertexColor, useTexture, useFog, useAlphaThreshold, maxInstancesCount))
#endif
		{
			if (lightsCount < 0 || lightsCount > 3)
			{
				throw new ArgumentException("lightsCount");
			}
			if (maxInstancesCount < 0 || maxInstancesCount > 32)
			{
				throw new ArgumentException("maxInstancesCount");
			}
			m_worldMatrixParameter = GetParameter("u_worldMatrix", allowNull: true);
			m_worldViewMatrixParameter = GetParameter("u_worldViewMatrix", allowNull: true);
			m_worldViewProjectionMatrixParameter = GetParameter("u_worldViewProjectionMatrix", allowNull: true);
			m_textureParameter = GetParameter("u_texture", allowNull: true);
			m_samplerStateParameter = GetParameter("u_samplerState", allowNull: true);
			m_materialColorParameter = GetParameter("u_materialColor", allowNull: true);
			m_emissionColorParameter = GetParameter("u_emissionColor", allowNull: true);
			m_alphaThresholdParameter = GetParameter("u_alphaThreshold", allowNull: true);
			m_ambientLightColorParameter = GetParameter("u_ambientLightColor", allowNull: true);
			m_diffuseLightColor1Parameter = GetParameter("u_diffuseLightColor1", allowNull: true);
			m_directionToLight1Parameter = GetParameter("u_directionToLight1", allowNull: true);
			m_diffuseLightColor2Parameter = GetParameter("u_diffuseLightColor2", allowNull: true);
			m_directionToLight2Parameter = GetParameter("u_directionToLight2", allowNull: true);
			m_diffuseLightColor3Parameter = GetParameter("u_diffuseLightColor3", allowNull: true);
			m_directionToLight3Parameter = GetParameter("u_directionToLight3", allowNull: true);
			m_fogStartParameter = GetParameter("u_fogStart", allowNull: true);
			m_fogLengthParameter = GetParameter("u_fogLength", allowNull: true);
			m_fogColorParameter = GetParameter("u_fogColor", allowNull: true);
			m_lightsCount = lightsCount;
			m_instancesCount = 1;
			m_useFog = useFog;
			MaterialColor = Vector4.One;
			if (useEmissionColor)
			{
				EmissionColor = Vector4.Zero;
			}
			if (lightsCount >= 1)
			{
				AmbientLightColor = new Vector3(0.2f);
				DiffuseLightColor1 = new Vector3(0.8f);
				LightDirection1 = Vector3.Normalize(new Vector3(1f, -1f, 1f));
			}
			if (lightsCount >= 2)
			{
				DiffuseLightColor2 = new Vector3(0.4f);
				LightDirection2 = Vector3.Normalize(new Vector3(-1f, -0.5f, -0.25f));
			}
			if (lightsCount >= 3)
			{
				DiffuseLightColor3 = new Vector3(0.2f);
				LightDirection3 = Vector3.Normalize(new Vector3(0f, 1f, 0f));
			}
			if (useFog)
			{
				FogLength = 100f;
			}
		}

        public override void PrepareForDrawingOverride()
		{
			Transforms.UpdateMatrices(m_instancesCount, m_useFog, viewProjection: false, worldViewProjection: true);
			m_worldViewProjectionMatrixParameter.SetValue(Transforms.WorldViewProjection, InstancesCount);
			if (m_lightsCount >= 1)
			{
				m_worldMatrixParameter.SetValue(Transforms.World, InstancesCount);
			}
			if (m_useFog)
			{
				m_worldViewMatrixParameter.SetValue(Transforms.WorldView, InstancesCount);
			}
		}

		public static ShaderMacro[] PrepareShaderMacros(int lightsCount, bool useEmissionColor, bool useVertexColor, bool useTexture, bool useFog, bool useAlphaThreshold, int maxInstancesCount)
		{
			List<ShaderMacro> list = [];
			if (lightsCount > 0)
			{
				list.Add(new ShaderMacro("USE_LIGHTING"));
			}
			if (lightsCount == 1)
			{
				list.Add(new ShaderMacro("ONE_LIGHT"));
			}
			if (lightsCount == 2)
			{
				list.Add(new ShaderMacro("TWO_LIGHTS"));
			}
			if (lightsCount == 3)
			{
				list.Add(new ShaderMacro("THREE_LIGHTS"));
			}
			if (useEmissionColor)
			{
				list.Add(new ShaderMacro("USE_EMISSIONCOLOR"));
			}
			if (useVertexColor)
			{
				list.Add(new ShaderMacro("USE_VERTEXCOLOR"));
			}
			if (useTexture)
			{
				list.Add(new ShaderMacro("USE_TEXTURE"));
			}
			if (useFog)
			{
				list.Add(new ShaderMacro("USE_FOG"));
			}
			if (useAlphaThreshold)
			{
				list.Add(new ShaderMacro("USE_ALPHATHRESHOLD"));
			}
			if (maxInstancesCount > 1)
			{
				list.Add(new ShaderMacro("USE_INSTANCING"));
			}
			list.Add(new ShaderMacro("MAX_INSTANCES_COUNT", maxInstancesCount.ToString(CultureInfo.InvariantCulture)));
			return list.ToArray();
		}
	}
}
