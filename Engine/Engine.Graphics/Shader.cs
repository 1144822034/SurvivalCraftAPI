using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
#if DIRECT3D11
using SharpDX;
using System.Runtime.InteropServices;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
#else
using Silk.NET.OpenGLES;
#endif

namespace Engine.Graphics
{
	public class Shader : GraphicsResource
	{
        public Dictionary<string, ShaderParameter> m_parametersByName;
        public ShaderParameter[] m_parameters;
        public string m_vertexShaderCode;
        public string m_pixelShaderCode;
        public ShaderMacro[] m_shaderMacros;
        #if DIRECT3D11
        public VertexShader m_vertexShader;
        public PixelShader m_pixelShader;
        public SharpDX.Direct3D11.Buffer[] m_allConstantBuffers;
        public int[] m_allConstantBuffersSizes;
        public IntPtr[] m_allConstantBuffersCpu;
        public SharpDX.Direct3D11.Buffer[] m_vertexShaderConstantBuffers;
        public SharpDX.Direct3D11.Buffer[] m_pixelShaderConstantBuffers;
        public byte[] m_pixelShaderBytecode;
        public byte[] m_vertexShaderBytecode;
        public Dictionary<VertexDeclaration, InputLayout> m_inputLayouts = new Dictionary<VertexDeclaration, InputLayout>();
        public VertexDeclaration m_lastVertexDeclaration;
        public InputLayout m_lastInputLayout;
        #else
        public struct ShaderAttributeData
		{
			public string Semantic;

			public int Location;
		}

        public struct VertexAttributeData
		{
			public int Size;

			public VertexAttribPointerType Type;

			public bool Normalize;

			public int Offset;
		}

        public int m_program;
        public int m_vertexShader;
        public int m_pixelShader;
        public Dictionary<VertexDeclaration, VertexAttributeData[]> m_vertexAttributeDataByDeclaration = [];
        public List<ShaderAttributeData> m_shaderAttributeData = [];
        public ShaderParameter m_glymulParameter;
#endif

		public string DebugName
		{
			get
			{
				return string.Empty;
			}
			set
			{
#if DIRECT3D11
                m_vertexShader.DebugName = value;
                m_pixelShader.DebugName = value;
#endif
			}
		}

		public ShaderParameter GetParameter(string name, bool allowNull = false)
		{
            return m_parametersByName.TryGetValue(name, out ShaderParameter value)
                ? value
                : allowNull
                    ? null
                    : throw new InvalidOperationException($"Parameter \"{name}\" not found.");
        }

		public override int GetGpuMemoryUsage()
		{
			return 16384;
		}

        public virtual void PrepareForDrawingOverride()
		{
		}

        public virtual void InitializeShader(string vertexShaderCode, string pixelShaderCode, ShaderMacro[] shaderMacros)
		{
			ArgumentNullException.ThrowIfNull(vertexShaderCode);
			ArgumentNullException.ThrowIfNull(pixelShaderCode);
			ArgumentNullException.ThrowIfNull(shaderMacros);
			m_vertexShaderCode = vertexShaderCode;
			m_pixelShaderCode = pixelShaderCode;
			m_shaderMacros = (ShaderMacro[])shaderMacros.Clone();
		}
		public object Tag
		{
			get;
			set;
		}

		public ReadOnlyList<ShaderParameter> Parameters => new(m_parameters);
		public virtual void Construct(string vertexShaderCode, string pixelShaderCode, params ShaderMacro[] shaderMacros)
		{
			try
			{
				InitializeShader(vertexShaderCode, pixelShaderCode, shaderMacros);
				CompileShaders();
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public Shader(string vertexShaderCode, string pixelShaderCode, params ShaderMacro[] shaderMacros)
        {
			Construct(vertexShaderCode, pixelShaderCode, shaderMacros);
		}
		public override void Dispose()
		{
			base.Dispose();
			DeleteShaders();
		}

        public virtual void PrepareForDrawing()
		{
#if !DIRECT3D11
			m_glymulParameter.SetValue((Display.RenderTarget != null) ? (-1f) : 1f);
#endif
			PrepareForDrawingOverride();
		}

#if !DIRECT3D11
        public virtual VertexAttributeData[] GetVertexAttribData(VertexDeclaration vertexDeclaration)
		{
			if (!m_vertexAttributeDataByDeclaration.TryGetValue(vertexDeclaration, out VertexAttributeData[] value))
			{
				value = new VertexAttributeData[8];
				foreach (ShaderAttributeData shaderAttributeDatum in m_shaderAttributeData)
				{
					VertexElement vertexElement = null;
					for (int i = 0; i < vertexDeclaration.m_elements.Length; i++)
					{
						if (vertexDeclaration.m_elements[i].Semantic == shaderAttributeDatum.Semantic)
						{
							vertexElement = vertexDeclaration.m_elements[i];
							break;
						}
					}
					if (!(vertexElement != null))
					{
						throw new InvalidOperationException($"VertexElement not found for shader attribute \"{shaderAttributeDatum.Semantic}\".");
					}
					value[shaderAttributeDatum.Location] = new VertexAttributeData
					{
						Size = vertexElement.Format.GetElementsCount(),
						Offset = vertexElement.Offset
					};
					GLWrapper.TranslateVertexElementFormat(vertexElement.Format, out value[shaderAttributeDatum.Location].Type, out value[shaderAttributeDatum.Location].Normalize);

                }
				m_vertexAttributeDataByDeclaration.Add(vertexDeclaration, value);
			}
			return value;
		}

        public static void ParseShaderMetadata(string shaderCode, Dictionary<string, string> semanticsByAttribute, Dictionary<string, string> samplersByTexture)
		{
			string[] array = shaderCode.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				try
				{
					string text = array[i];
					text = text.Trim();
					if (text.StartsWith("//"))
					{
						text = text.Substring(2).TrimStart();
						if (text.StartsWith('<') && text.EndsWith("/>"))
						{
							var xElement = XElement.Parse(text);
							if (xElement.Name == "Semantic")
							{
								if (xElement.Attribute("Attribute") == null)
								{
									throw new InvalidOperationException("Missing \"Attribute\" attribute in shader metadata.");
								}
								if (xElement.Attribute("Name") == null)
								{
									throw new InvalidOperationException("Missing \"Name\" attribute in shader metadata.");
								}
								semanticsByAttribute.Add(xElement.Attribute("Attribute").Value, xElement.Attribute("Name").Value);
							}
							else
							{
								if (!(xElement.Name == "Sampler"))
								{
									throw new InvalidOperationException("Unrecognized shader metadata node.");
								}
								if (xElement.Attribute("Texture") == null)
								{
									throw new InvalidOperationException("Missing \"Texture\" attribute in shader metadata.");
								}
								if (xElement.Attribute("Name") == null)
								{
									throw new InvalidOperationException("Missing \"Name\" attribute in shader metadata.");
								}
								samplersByTexture.Add(xElement.Attribute("Texture").Value, xElement.Attribute("Name").Value);
							}
						}
					}
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Error in shader metadata, line {i + 1}. {ex.Message}");
				}
			}
		}

        public virtual string PrependShaderMacros(string shaderCode, ShaderMacro[] shaderMacros, bool isVertexShader)
		{
			string str = "";

			if (shaderCode.StartsWith("#version "))
			{
				string versioncode = shaderCode.Split(new char[] { '\n' })[0];
				string versionnum = versioncode.Split(new char[] { ' ' })[1];

                if (int.Parse(versionnum) >= 300 || versioncode.EndsWith("es"))
                    str += $"#version {versionnum} es" + Environment.NewLine;
                else

				str += $"#version {versionnum}" + Environment.NewLine;
				shaderCode = "//" + shaderCode;
			}
            else
            {
                //[WARN] 未指定版本时，会主动加上最低的版本号
                str += "#version 100" + Environment.NewLine;
            }

			str = str + "#define GLSL" + Environment.NewLine;
			if (isVertexShader)
			{
				str = (!Display.UseReducedZRange) ? (str + "#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul; gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;" + Environment.NewLine) : (str + "#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul;" + Environment.NewLine);
				str = str + "uniform float u_glymul;" + Environment.NewLine;
			}
			foreach (ShaderMacro shaderMacro in shaderMacros)
			{
				str = str + "#define " + shaderMacro.Name + " " + shaderMacro.Value + Environment.NewLine;
			}
			str = str + "#line 1" + Environment.NewLine;
			return str + shaderCode;
		}
#endif

        public override void HandleDeviceLost()
		{
			DeleteShaders();
		}

        public override void HandleDeviceReset()
		{
			CompileShaders();
		}

        public virtual void CompileShaders()
		{
#if DIRECT3D11
            m_parametersByName = new Dictionary<string, ShaderParameter>();
            List<SharpDX.Direct3D11.Buffer> list = new List<SharpDX.Direct3D11.Buffer>();
            List<int> list2 = new List<int>();
            List<IntPtr> list3 = new List<IntPtr>();
            List<SharpDX.Direct3D11.Buffer> list4 = new List<SharpDX.Direct3D11.Buffer>();
            List<SharpDX.Direct3D11.Buffer> list5 = new List<SharpDX.Direct3D11.Buffer>();
            m_vertexShaderBytecode = CompileShader(m_vertexShaderCode, true, list, list2, list3, list4);
            m_pixelShaderBytecode = CompileShader(m_pixelShaderCode, false, list, list2, list3, list5);
            if (m_parameters == null)
            {
                m_parameters = Enumerable.ToArray<ShaderParameter>(m_parametersByName.Values);
            }
            else
            {
                ShaderParameter[] parameters = m_parameters;
                foreach (ShaderParameter t in parameters) {
                    t.IsChanged = true;
                }
            }
            m_vertexShaderConstantBuffers = list4.ToArray();
            m_pixelShaderConstantBuffers = list5.ToArray();
            m_allConstantBuffers = list.ToArray();
            m_allConstantBuffersSizes = list2.ToArray();
            m_allConstantBuffersCpu = list3.ToArray();
            m_vertexShader = new VertexShader(DXWrapper.Device, m_vertexShaderBytecode, null);
            m_pixelShader = new PixelShader(DXWrapper.Device, m_pixelShaderBytecode, null);
#else
			DeleteShaders();
			Dictionary<string, string> dictionary = [];
			Dictionary<string, string> dictionary2 = [];
			ParseShaderMetadata(m_vertexShaderCode, dictionary, dictionary2);
			ParseShaderMetadata(m_pixelShaderCode, dictionary, dictionary2);
			string @string = PrependShaderMacros(m_vertexShaderCode, m_shaderMacros, isVertexShader: true);
            string string2 = PrependShaderMacros(m_pixelShaderCode, m_shaderMacros, isVertexShader: false);
			uint vertexShader = GLWrapper.GL.CreateShader(ShaderType.VertexShader);
            m_vertexShader = (int)vertexShader;
			GLWrapper.GL.ShaderSource(vertexShader, @string);
            GLWrapper.GL.CompileShader(vertexShader);
            GLWrapper.GL.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int @params);
			if (@params != 1)
			{
				string shaderInfoLog = GLWrapper.GL.GetShaderInfoLog(vertexShader);
				throw new InvalidOperationException($"Error compiling vertex shader.\n{shaderInfoLog}");
			}
			uint pixelShader = GLWrapper.GL.CreateShader(ShaderType.FragmentShader);
            m_pixelShader = (int)pixelShader;
			GLWrapper.GL.ShaderSource(pixelShader, string2);
			GLWrapper.GL.CompileShader(pixelShader);
			GLWrapper.GL.GetShader(pixelShader, ShaderParameterName.CompileStatus, out int params2);
			if (params2 != 1)
			{
				string shaderInfoLog2 = GLWrapper.GL.GetShaderInfoLog(pixelShader);
				throw new InvalidOperationException($"Error compiling pixel shader.\n{shaderInfoLog2}");
			}
			uint program = GLWrapper.GL.CreateProgram();
            m_program = (int)program;
			GLWrapper.GL.AttachShader(program, vertexShader);
			GLWrapper.GL.AttachShader(program, pixelShader);
			GLWrapper.GL.LinkProgram(program);
			GLWrapper.GL.GetProgram(program, ProgramPropertyARB.LinkStatus, out int params3);
            if (params3 != 1)
			{
				string programInfoLog = GLWrapper.GL.GetProgramInfoLog(program);
				throw new InvalidOperationException($"Error linking program.\n{programInfoLog}");
			}
			GLWrapper.GL.GetProgram(program, ProgramPropertyARB.ActiveAttributes, out int params4);
			for (int i = 0; i < params4; i++)
			{
				GLWrapper.GL.GetActiveAttrib(program, (uint)i, 256u, out uint _, out int _, out AttributeType _, out string stringBuilder);
				int attribLocation = GLWrapper.GL.GetAttribLocation(program, stringBuilder.ToString());
				if (!dictionary.TryGetValue(stringBuilder.ToString(), out string value))
				{
					throw new InvalidOperationException($"Attribute \"{stringBuilder.ToString()}\" has no semantic defined in shader metadata.");
				}
				m_shaderAttributeData.Add(new ShaderAttributeData
				{
					Location = attribLocation,
					Semantic = value
				});
			}
			GLWrapper.GL.GetProgram(program,ProgramPropertyARB.ActiveUniforms, out int params5);
			List<ShaderParameter> list = [];
			Dictionary<string, ShaderParameter> dictionary3 = [];
			for (int j = 0; j < params5; j++)
			{

				GLWrapper.GL.GetActiveUniform(program, (uint)j, 256u, out uint _, out int size2, out UniformType type2, out string stringBuilder2);
				int uniformLocation = GLWrapper.GL.GetUniformLocation(program, stringBuilder2.ToString());
				ShaderParameterType shaderParameterType = GLWrapper.TranslateActiveUniformType(type2);
				int num = stringBuilder2.ToString().IndexOf('[');
				if (num >= 0)
				{
					stringBuilder2 = stringBuilder2.Remove(num, stringBuilder2.Length - num);
				}

				ShaderParameter shaderParameter = new(this, stringBuilder2.ToString(), shaderParameterType, size2);
				shaderParameter.Location = uniformLocation;
				dictionary3.Add(shaderParameter.Name, shaderParameter);
				list.Add(shaderParameter);
				if (shaderParameterType == ShaderParameterType.Texture2D)
				{
					if (!dictionary2.TryGetValue(shaderParameter.Name, out string value2))
					{
						throw new InvalidOperationException($"Texture \"{shaderParameter.Name}\" has no sampler defined in shader metadata.");
					}
					ShaderParameter shaderParameter2 = new(this, value2, ShaderParameterType.Sampler2D, 1);
					shaderParameter2.Location = int.MaxValue;
					dictionary3.Add(value2, shaderParameter2);
					list.Add(shaderParameter2);
				}
			}
			if (m_parameters != null)
			{
				foreach (KeyValuePair<string, ShaderParameter> item in dictionary3)
				{
					if (m_parametersByName.TryGetValue(item.Key, out ShaderParameter value3))
					{
						value3.Location = item.Value.Location;
					}
				}
				ShaderParameter[] parameters = m_parameters;
				for (int k = 0; k < parameters.Length; k++)
				{
					parameters[k].IsChanged = true;
				}
			}
			else
			{
				m_parameters = list.ToArray();
				m_parametersByName = dictionary3;
			}
			m_glymulParameter = GetParameter("u_glymul");
			if (m_glymulParameter.Type != 0)
			{
				throw new InvalidOperationException("u_glymul parameter has invalid type.");
			}
#endif
		}

        #if DIRECT3D11
        public virtual byte[] CompileShader(string code,
            bool isVertexShader,
            List<SharpDX.Direct3D11.Buffer> allConstantBuffers,
            List<int> allConstantBuffersSizes,
            List<IntPtr> allConstantBuffersCpu,
            List<SharpDX.Direct3D11.Buffer> shaderConstantBuffers)
        {
            string text = (isVertexShader ? "vs_4_0_level_9_1" : "ps_4_0_level_9_1");
            List<SharpDX.Direct3D.ShaderMacro> list = new List<SharpDX.Direct3D.ShaderMacro>();
            list.Add(new SharpDX.Direct3D.ShaderMacro("HLSL", string.Empty));
            list.AddRange(Enumerable.Select<ShaderMacro, SharpDX.Direct3D.ShaderMacro>(m_shaderMacros, (ShaderMacro s) => new SharpDX.Direct3D.ShaderMacro(s.Name, s.Value)));
            ShaderFlags shaderFlags = ShaderFlags.OptimizationLevel1;
            byte[] array;
            using (CompilationResult compilationResult = ShaderBytecode.Compile(
                code,
                "main",
                text,
                shaderFlags,
                EffectFlags.None,
                list.ToArray(),
                null,
                string.Empty,
                SecondaryDataFlags.None
            ))
            {
                if (compilationResult.HasErrors)
                {
                    throw new InvalidOperationException(compilationResult.Message);
                }
                if (!string.IsNullOrWhiteSpace(compilationResult.Message))
                {
                    Log.Warning(compilationResult.Message);
                }
                array = compilationResult.Bytecode;
            }
            using (ShaderReflection shaderReflection = new ShaderReflection(array))
            {
                for (int i = 0; i < shaderReflection.Description.ConstantBuffers; i++)
                {
                    int count = allConstantBuffers.Count;
                    ConstantBuffer constantBuffer = shaderReflection.GetConstantBuffer(i);
                    SharpDX.Direct3D11.Buffer buffer = new(
                        DXWrapper.Device,
                        constantBuffer.Description.Size,
                        ResourceUsage.Dynamic,
                        BindFlags.ConstantBuffer,
                        CpuAccessFlags.Write,
                        ResourceOptionFlags.None,
                        0
                    );
                    allConstantBuffers.Add(buffer);
                    allConstantBuffersSizes.Add(buffer.Description.SizeInBytes);
                    shaderConstantBuffers.Add(buffer);
                    allConstantBuffersCpu.Add(Marshal.AllocHGlobal(constantBuffer.Description.Size));
                    for (int j = 0; j < constantBuffer.Description.VariableCount; j++)
                    {
                        ShaderReflectionVariable variable = constantBuffer.GetVariable(j);
                        ShaderReflectionType variableType = variable.GetVariableType();
                        ShaderParameterType shaderParameterType = DXWrapper.TranslateShaderTypeDescription(variableType.Description);
                        if (!m_parametersByName.TryGetValue(variable.Description.Name, out ShaderParameter shaderParameter))
                        {
                            shaderParameter = new ShaderParameter(this, variable.Description.Name, shaderParameterType, MathUtils.Max(variableType.Description.ElementCount, 1));
                            m_parametersByName.Add(shaderParameter.Name, shaderParameter);
                        }
                        if (isVertexShader)
                        {
                            shaderParameter.VsBufferIndex = count;
                            shaderParameter.VsBufferPtr = allConstantBuffersCpu[count] + variable.Description.StartOffset;
                        }
                        else
                        {
                            shaderParameter.PsBufferIndex = count;
                            shaderParameter.PsBufferPtr = allConstantBuffersCpu[count] + variable.Description.StartOffset;
                        }
                    }
                }
                for (int k = 0; k < shaderReflection.Description.BoundResources; k++)
                {
                    InputBindingDescription resourceBindingDescription = shaderReflection.GetResourceBindingDescription(k);
                    if (resourceBindingDescription.Type != ShaderInputType.ConstantBuffer)
                    {
                        ShaderParameterType shaderParameterType2 = DXWrapper.TranslateInputBindingDescription(resourceBindingDescription);
                        if (!m_parametersByName.TryGetValue(resourceBindingDescription.Name, out ShaderParameter shaderParameter2))
                        {
                            shaderParameter2 = new ShaderParameter(this, resourceBindingDescription.Name, shaderParameterType2, 1);
                            m_parametersByName.Add(shaderParameter2.Name, shaderParameter2);
                        }
                        if (isVertexShader)
                        {
                            shaderParameter2.VsResourceBindingSlot = resourceBindingDescription.BindPoint;
                        }
                        else
                        {
                            shaderParameter2.PsResourceBindingSlot = resourceBindingDescription.BindPoint;
                        }
                    }
                }
            }
            return array;
        }
#endif

        public virtual void DeleteShaders()
		{
#if DIRECT3D11
            Utilities.Dispose(ref m_vertexShader);
            Utilities.Dispose(ref m_pixelShader);
            if (m_allConstantBuffers != null)
            {
                Utilities.DisposeCollection(m_allConstantBuffers);
                m_allConstantBuffers = null;
            }
            if (m_inputLayouts != null)
            {
                Utilities.DisposeCollection<InputLayout>(m_inputLayouts.Values);
                m_inputLayouts = null;
            }
            if (m_allConstantBuffersCpu != null)
            {
                IntPtr[] allConstantBuffersCpu = m_allConstantBuffersCpu;
                foreach (IntPtr t in allConstantBuffersCpu) {
                    Marshal.FreeHGlobal(t);
                }
                m_allConstantBuffersCpu = null;
            }
#else
            uint vertexShader = (uint)m_vertexShader;
            uint pixelShader = (uint)m_pixelShader;
			if (m_program != 0)
			{
                uint program = (uint)m_program;
				if (m_vertexShader != 0)
				{
					GLWrapper.GL.DetachShader(program, vertexShader);
				}
				if (m_pixelShader != 0)
				{
					GLWrapper.GL.DetachShader(program, pixelShader);
				}
				GLWrapper.DeleteProgram(m_program);
				m_program = 0;
			}
			if (m_vertexShader != 0)
			{
				GLWrapper.GL.DeleteShader(vertexShader);
				m_vertexShader = 0;
			}
			if (m_pixelShader != 0)
			{
				GLWrapper.GL.DeleteShader(pixelShader);
				m_pixelShader = 0;
			}
#endif
		}
	}
}
