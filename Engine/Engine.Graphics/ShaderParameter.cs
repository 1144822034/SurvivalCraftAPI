namespace Engine.Graphics
{
	public  class ShaderParameter
	{
		public object Resource;
		public bool IsChanged = true;
		public readonly Shader Shader;
		public readonly string Name;
		public readonly ShaderParameterType Type;
		public readonly int Count;
#if DIRECT3D11
        public int VsResourceBindingSlot = -1;
        public int PsResourceBindingSlot = -1;
        public int VsBufferIndex = -1;
        public IntPtr VsBufferPtr;
        public int PsBufferIndex = -1;
        public IntPtr PsBufferPtr;
#else
		public int Location;
		public float[] Value;
#endif

		public ShaderParameter(string name, ShaderParameterType type)
		{
			Name = name;
			Type = type;
		}

		public ShaderParameter(Shader shader, string name, ShaderParameterType type, int count)
		{
			Shader = shader;
			Name = name;
			Type = type;
			Count = count;
#if !DIRECT3D11
			switch (type)
			{
				case ShaderParameterType.Texture2D:
				case ShaderParameterType.Sampler2D:
					break;
				case ShaderParameterType.Float:
					Value = new float[count];
					break;
				case ShaderParameterType.Vector2:
					Value = new float[2 * count];
					break;
				case ShaderParameterType.Vector3:
					Value = new float[3 * count];
					break;
				case ShaderParameterType.Vector4:
					Value = new float[4 * count];
					break;
				case ShaderParameterType.Matrix:
					Value = new float[16 * count];
					break;
				default:
					throw new ArgumentException("type");
			}
#endif
		}

		public void SetValue(float value)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Float || Count != 1)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (VsBufferIndex != -1 && *(float*)(void*)VsBufferPtr != value)
            {
                *(float*)(void*)VsBufferPtr = value;
                IsChanged = true;
            }
            if (PsBufferIndex != -1 && *(float*)(void*)PsBufferPtr != value)
            {
                *(float*)(void*)PsBufferPtr = value;
                IsChanged = true;
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Float || Count != 1)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (value != Value[0])
			{
				Value[0] = value;
				IsChanged = true;
			}
#endif
		}

		public void SetValue(float[] value, int count)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Float)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0 || count > value.Length || count > Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            for (int i = 0; i < count; i++)
            {
                if (VsBufferIndex != -1)
                {
                    float* ptr = (float*)((byte*)(void*)VsBufferPtr + ((IntPtr)i * 4));
                    if (*ptr != value[i])
                    {
                        *ptr = value[i];
                        IsChanged = true;
                    }
                }
                if (PsBufferIndex != -1)
                {
                    float* ptr2 = (float*)((byte*)(void*)PsBufferPtr + ((IntPtr)i * 4));
                    if (*ptr2 != value[i])
                    {
                        *ptr2 = value[i];
                        IsChanged = true;
                    }
                }
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Float)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (count < 0 || count > value.Length || count > Count)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			if (!IsChanged)
			{
				for (int i = 0; i < count; i++)
				{
					if (Value[i] != value[i])
					{
						IsChanged = true;
						break;
					}
				}
			}
			if (IsChanged)
			{
				for (int j = 0; j < count; j++)
				{
					Value[j] = value[j];
				}
				IsChanged = true;
			}
#endif
		}

		public void SetValue(Vector2 value)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Vector2 || Count != 1)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (VsBufferIndex != -1 && (IsChanged || !Compare((Vector2*)(void*)VsBufferPtr, &value)))
            {
                *(Vector2*)(void*)VsBufferPtr = value;
                IsChanged = true;
            }
            if (PsBufferIndex != -1 && (IsChanged || !Compare((Vector2*)(void*)PsBufferPtr, &value)))
            {
                *(Vector2*)(void*)PsBufferPtr = value;
                IsChanged = true;
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Vector2 || Count != 1)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (IsChanged || value.X != Value[0] || value.Y != Value[1])
			{
				Value[0] = value.X;
				Value[1] = value.Y;
				IsChanged = true;
			}
#endif
		}

		public void SetValue(Vector2[] value, int count)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Vector2)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0 || count > value.Length || count > Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            fixed (Vector2* ptr = value)
            {
                if (VsBufferIndex != -1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (IsChanged || !Compare((Vector2*)((byte*)(void*)VsBufferPtr + (i * (IntPtr)sizeof(Vector2))), ptr + i))
                        {
                            *(Vector2*)((byte*)(void*)VsBufferPtr + (i * (IntPtr)sizeof(Vector2))) = ptr[i];
                            IsChanged = true;
                        }
                    }
                }
                if (PsBufferIndex != -1)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (IsChanged || !Compare((Vector2*)((byte*)(void*)PsBufferPtr + (j * (IntPtr)sizeof(Vector2))), ptr + j))
                        {
                            *(Vector2*)((byte*)(void*)PsBufferPtr + (j * (IntPtr)sizeof(Vector2))) = ptr[j];
                            IsChanged = true;
                        }
                    }
                }
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Vector2)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (count < 0 || count > value.Length || count > Count)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			if (!IsChanged)
			{
				int i = 0;
				int num = 0;
				for (; i < count; i++)
				{
					if (Value[num++] != value[i].X || Value[num++] != value[i].Y)
					{
						IsChanged = true;
						break;
					}
				}
			}
			if (IsChanged)
			{
				int j = 0;
				int num2 = 0;
				for (; j < count; j++)
				{
					Value[num2++] = value[j].X;
					Value[num2++] = value[j].Y;
				}
			}
#endif
		}

		public void SetValue(Vector3 value)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Vector3 || Count != 1)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (VsBufferIndex != -1 && (IsChanged || !Compare((Vector3*)(void*)VsBufferPtr, &value)))
            {
                *(Vector3*)(void*)VsBufferPtr = value;
                IsChanged = true;
            }
            if (PsBufferIndex != -1 && (IsChanged || !Compare((Vector3*)(void*)PsBufferPtr, &value)))
            {
                *(Vector3*)(void*)PsBufferPtr = value;
                IsChanged = true;
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Vector3 || Count != 1)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (IsChanged || value.X != Value[0] || value.Y != Value[1] || value.Z != Value[2])
			{
				Value[0] = value.X;
				Value[1] = value.Y;
				Value[2] = value.Z;
				IsChanged = true;
			}
#endif
		}

		public void SetValue(Vector3[] value, int count)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Vector3)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0 || count > value.Length || count > Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            fixed (Vector3* ptr = value)
            {
                if (VsBufferIndex != -1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (IsChanged || !Compare((Vector3*)((byte*)(void*)VsBufferPtr + (i * (IntPtr)sizeof(Vector3))), ptr + i))
                        {
                            *(Vector3*)((byte*)(void*)VsBufferPtr + (i * (IntPtr)sizeof(Vector3))) = ptr[i];
                            IsChanged = true;
                        }
                    }
                }
                if (PsBufferIndex != -1)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (IsChanged || !Compare((Vector3*)((byte*)(void*)PsBufferPtr + (j * (IntPtr)sizeof(Vector3))), ptr + j))
                        {
                            *(Vector3*)((byte*)(void*)PsBufferPtr + (j * (IntPtr)sizeof(Vector3))) = ptr[j];
                            IsChanged = true;
                        }
                    }
                }
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Vector3)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (count < 0 || count > value.Length || count > Count)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			if (!IsChanged)
			{
				int i = 0;
				int num = 0;
				for (; i < count; i++)
				{
					if (Value[num++] != value[i].X || Value[num++] != value[i].Y || Value[num++] != value[i].Z)
					{
						IsChanged = true;
						break;
					}
				}
			}
			if (IsChanged)
			{
				int j = 0;
				int num2 = 0;
				for (; j < count; j++)
				{
					Value[num2++] = value[j].X;
					Value[num2++] = value[j].Y;
					Value[num2++] = value[j].Z;
				}
			}
#endif
		}

		public void SetValue(Vector4 value)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Vector4 || Count != 1)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (VsBufferIndex != -1 && (IsChanged || !Compare((Vector4*)(void*)VsBufferPtr, &value)))
            {
                *(Vector4*)(void*)VsBufferPtr = value;
                IsChanged = true;
            }
            if (PsBufferIndex != -1 && (IsChanged || !Compare((Vector4*)(void*)PsBufferPtr, &value)))
            {
                *(Vector4*)(void*)PsBufferPtr = value;
                IsChanged = true;
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Vector4 || Count != 1)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (IsChanged || value.X != Value[0] || value.Y != Value[1] || value.Z != Value[2] || value.W != Value[3])
			{
				Value[0] = value.X;
				Value[1] = value.Y;
				Value[2] = value.Z;
				Value[3] = value.W;
				IsChanged = true;
			}
#endif
		}

		public void SetValue(Vector4[] value, int count)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Vector4)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0 || count > value.Length || count > Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            fixed (Vector4* ptr = value)
            {
                if (VsBufferIndex != -1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (IsChanged || !Compare((Vector4*)((byte*)(void*)VsBufferPtr + (i * (IntPtr)sizeof(Vector4))), ptr + i))
                        {
                            *(Vector4*)((byte*)(void*)VsBufferPtr + (i * (IntPtr)sizeof(Vector4))) = ptr[i];
                            IsChanged = true;
                        }
                    }
                }
                if (PsBufferIndex != -1)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (IsChanged || !Compare((Vector4*)((byte*)(void*)PsBufferPtr + (j * (IntPtr)sizeof(Vector4))), ptr + j))
                        {
                            *(Vector4*)((byte*)(void*)PsBufferPtr + (j * (IntPtr)sizeof(Vector4))) = ptr[j];
                            IsChanged = true;
                        }
                    }
                }
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Vector4)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (count < 0 || count > value.Length || count > Count)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			if (!IsChanged)
			{
				int i = 0;
				int num = 0;
				for (; i < count; i++)
				{
					if (Value[num++] != value[i].X || Value[num++] != value[i].Y || Value[num++] != value[i].Z || Value[num++] != value[i].W)
					{
						IsChanged = true;
						break;
					}
				}
			}
			if (IsChanged)
			{
				int j = 0;
				int num2 = 0;
				for (; j < count; j++)
				{
					Value[num2++] = value[j].X;
					Value[num2++] = value[j].Y;
					Value[num2++] = value[j].Z;
					Value[num2++] = value[j].W;
				}
			}
#endif
		}

		public void SetValue(Matrix value)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Matrix || Count != 1)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (VsBufferIndex != -1 && (IsChanged || !Compare((Matrix*)(void*)VsBufferPtr, &value)))
            {
                *(Matrix*)(void*)VsBufferPtr = value;
                IsChanged = true;
            }
            if (PsBufferIndex != -1 && (IsChanged || !Compare((Matrix*)(void*)PsBufferPtr, &value)))
            {
                *(Matrix*)(void*)PsBufferPtr = value;
                IsChanged = true;
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Matrix || Count != 1)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (IsChanged || value.M11 != Value[0] || value.M12 != Value[1] || value.M13 != Value[2] || value.M14 != Value[3] || value.M21 != Value[4] || value.M22 != Value[5] || value.M23 != Value[6] || value.M24 != Value[7] || value.M31 != Value[8] || value.M32 != Value[9] || value.M33 != Value[10] || value.M34 != Value[11] || value.M41 != Value[12] || value.M42 != Value[13] || value.M43 != Value[14] || value.M44 != Value[15])
			{
				Value[0] = value.M11;
				Value[1] = value.M12;
				Value[2] = value.M13;
				Value[3] = value.M14;
				Value[4] = value.M21;
				Value[5] = value.M22;
				Value[6] = value.M23;
				Value[7] = value.M24;
				Value[8] = value.M31;
				Value[9] = value.M32;
				Value[10] = value.M33;
				Value[11] = value.M34;
				Value[12] = value.M41;
				Value[13] = value.M42;
				Value[14] = value.M43;
				Value[15] = value.M44;
				IsChanged = true;
			}
#endif
		}

		public void SetValue(Matrix[] value, int count)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Matrix)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0 || count > value.Length || count > Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            fixed (Matrix* ptr = value)
            {
                if (VsBufferIndex != -1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (IsChanged || !Compare((Matrix*)((byte*)(void*)VsBufferPtr + (i * (IntPtr)sizeof(Matrix))), ptr + i))
                        {
                            *(Matrix*)((byte*)(void*)VsBufferPtr + (i * (IntPtr)sizeof(Matrix))) = ptr[i];
                            IsChanged = true;
                        }
                    }
                }
                if (PsBufferIndex != -1)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (IsChanged || !Compare((Matrix*)((byte*)(void*)PsBufferPtr + (j * (IntPtr)sizeof(Matrix))), ptr + j))
                        {
                            *(Matrix*)((byte*)(void*)PsBufferPtr + (j * (IntPtr)sizeof(Matrix))) = ptr[j];
                            IsChanged = true;
                        }
                    }
                }
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Matrix)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (count < 0 || count > value.Length || count > Count)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			if (!IsChanged)
			{
				int i = 0;
				int num = 0;
				for (; i < count; i++)
				{
					if (Value[num++] != value[i].M11 || Value[num++] != value[i].M12 || Value[num++] != value[i].M13 || Value[num++] != value[i].M14 || Value[num++] != value[i].M21 || Value[num++] != value[i].M22 || Value[num++] != value[i].M23 || Value[num++] != value[i].M24 || Value[num++] != value[i].M31 || Value[num++] != value[i].M32 || Value[num++] != value[i].M33 || Value[num++] != value[i].M34 || Value[num++] != value[i].M41 || Value[num++] != value[i].M42 || Value[num++] != value[i].M43 || Value[num++] != value[i].M44)
					{
						IsChanged = true;
						break;
					}
				}
			}
			if (IsChanged)
			{
				int j = 0;
				int num2 = 0;
				for (; j < count; j++)
				{
					Value[num2++] = value[j].M11;
					Value[num2++] = value[j].M12;
					Value[num2++] = value[j].M13;
					Value[num2++] = value[j].M14;
					Value[num2++] = value[j].M21;
					Value[num2++] = value[j].M22;
					Value[num2++] = value[j].M23;
					Value[num2++] = value[j].M24;
					Value[num2++] = value[j].M31;
					Value[num2++] = value[j].M32;
					Value[num2++] = value[j].M33;
					Value[num2++] = value[j].M34;
					Value[num2++] = value[j].M41;
					Value[num2++] = value[j].M42;
					Value[num2++] = value[j].M43;
					Value[num2++] = value[j].M44;
				}
			}
#endif
		}

		public void SetValue(Texture2D value)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Texture2D || Count != 1)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (value != Resource)
            {
                Resource = value;
                IsChanged = true;
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Texture2D || Count != 1)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (value != Resource)
			{
				Resource = value;
				IsChanged = true;
			}
#endif
		}

		public void SetValue(SamplerState value)
		{
#if DIRECT3D11
            if (Type != ShaderParameterType.Sampler2D || Count != 1)
            {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (value != Resource)
            {
                Resource = value;
                IsChanged = true;
            }
#else
			if (Type == ShaderParameterType.Null) return;
			if (Type != ShaderParameterType.Sampler2D || Count != 1)
			{
				throw new InvalidOperationException("Shader parameter type mismatch.");
			}
			if (value != Resource)
			{
				Resource = value;
				IsChanged = true;
			}
#endif
		}

        public static unsafe bool Compare(Vector2* a, Vector2* b)
        {
            return *(long*)a == *(long*)b;
        }

        public static unsafe bool Compare(Vector3* a, Vector3* b)
        {
            if (*(long*)a != *(long*)b)
            {
                return false;
            }
            if (*(int*)((byte*)a + (2 * (nint)4)) != *(int*)((byte*)b + (2 * (nint)4)))
            {
                return false;
            }
            return true;
        }

        public static unsafe bool Compare(Vector4* a, Vector4* b)
        {
            if (*(long*)a != *(long*)b)
            {
                return false;
            }
            if (*(long*)((byte*)a + 8) != *(long*)((byte*)b + 8))
            {
                return false;
            }
            return true;
        }

        public static unsafe bool Compare(Matrix* a, Matrix* b)
        {
            if (*(long*)a != *(long*)b)
            {
                return false;
            }
            if (*(long*)((byte*)a + 8) != *(long*)((byte*)b + 8))
            {
                return false;
            }
            if (*(long*)((byte*)a + (2 * (nint)8)) != *(long*)((byte*)b + (2 * (nint)8)))
            {
                return false;
            }
            if (*(long*)((byte*)a + (3 * (nint)8)) != *(long*)((byte*)b + (3 * (nint)8)))
            {
                return false;
            }
            if (*(long*)((byte*)a + (4 * (nint)8)) != *(long*)((byte*)b + (4 * (nint)8)))
            {
                return false;
            }
            if (*(long*)((byte*)a + (5 * (nint)8)) != *(long*)((byte*)b + (5 * (nint)8)))
            {
                return false;
            }
            if (*(long*)((byte*)a + (6 * (nint)8)) != *(long*)((byte*)b + (6 * (nint)8)))
            {
                return false;
            }
            if (*(long*)((byte*)a + (7 * (nint)8)) != *(long*)((byte*)b + (7 * (nint)8)))
            {
                return false;
            }
            return true;
        }
	}
}
