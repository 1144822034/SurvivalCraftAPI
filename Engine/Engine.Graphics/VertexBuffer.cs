#if DIRECT3D11
using SharpDX;
using SharpDX.Direct3D11;
#else
using Silk.NET.OpenGLES;
#endif
using System.Runtime.InteropServices;

namespace Engine.Graphics
{
	public  class VertexBuffer : GraphicsResource
	{
#if DIRECT3D11
        public SharpDX.Direct3D11.Buffer m_buffer;
#else
        public int m_buffer;
#endif

		public string DebugName
		{
			get
			{
#if DIRECT3D11
                return m_buffer.DebugName;
#else
				return string.Empty;
#endif
			}
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
#if DIRECT3D11
                m_buffer.DebugName = value;
#endif
			}
		}

		public VertexDeclaration VertexDeclaration
		{
			get;
			set;
		}

		public int VerticesCount
		{
			get;
			set;
		}

		public object Tag
		{
			get;
			set;
		}

		public VertexBuffer(VertexDeclaration vertexDeclaration, int verticesCount)
		{
			InitializeVertexBuffer(vertexDeclaration, verticesCount);
			AllocateBuffer();
		}

		public override void Dispose()
		{
			base.Dispose();
			DeleteBuffer();
		}

		public unsafe void SetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct
		{
			VerifyParametersSetData(source, sourceStartIndex, sourceCount, targetStartIndex);
			GCHandle gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
			try
			{
				int num = Utilities.SizeOf<T>();
				int vertexStride = VertexDeclaration.VertexStride;
#if DIRECT3D11
                DataBox dataBox = new (gCHandle.AddrOfPinnedObject() + (sourceStartIndex * num), 1, 0);
                ResourceRegion resourceRegion = new (targetStartIndex * VertexDeclaration.VertexStride, 0, 0, (targetStartIndex * VertexDeclaration.VertexStride) + (sourceCount * num), 1, 1);
                DXWrapper.Context.UpdateSubresource(dataBox, m_buffer, 0, resourceRegion);
#else
				GLWrapper.BindBuffer(BufferTargetARB.ArrayBuffer, m_buffer);
				GLWrapper.GL.BufferSubData(BufferTargetARB.ArrayBuffer, new IntPtr(targetStartIndex * vertexStride), new UIntPtr((uint)(num * sourceCount)), (gCHandle.AddrOfPinnedObject() + (sourceStartIndex * num)).ToPointer());
#endif
			}
			finally
			{
				gCHandle.Free();
			}
		}

		public override void HandleDeviceLost()
		{
			DeleteBuffer();
		}

        public override void HandleDeviceReset()
		{
			AllocateBuffer();
		}

        public unsafe void AllocateBuffer()
		{
#if DIRECT3D11
            m_buffer = new SharpDX.Direct3D11.Buffer(DXWrapper.Device, VertexDeclaration.VertexStride * VerticesCount, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
#else
			GLWrapper.GL.GenBuffers(1, out uint buffer);
            m_buffer = (int)buffer;
			GLWrapper.BindBuffer(BufferTargetARB.ArrayBuffer, m_buffer);
            GLWrapper.GL.BufferData(BufferTargetARB.ArrayBuffer, new UIntPtr((uint)(VertexDeclaration.VertexStride * VerticesCount)), null, BufferUsageARB.StaticDraw);
#endif
		}

        public void DeleteBuffer()
		{
#if DIRECT3D11
            Utilities.Dispose(ref m_buffer);
#else
			if (m_buffer != 0)
			{
				GLWrapper.DeleteBuffer(BufferTargetARB.ArrayBuffer, m_buffer);
				m_buffer = 0;
			}
#endif
		}

		public override int GetGpuMemoryUsage()
		{
			return VertexDeclaration.VertexStride * VerticesCount;
		}

        public void InitializeVertexBuffer(VertexDeclaration vertexDeclaration, int verticesCount)
		{
			ArgumentNullException.ThrowIfNull(vertexDeclaration);
			if (verticesCount <= 0)
			{
				throw new ArgumentException("verticesCount must be greater than 0.");
			}
			VertexDeclaration = vertexDeclaration;
			VerticesCount = verticesCount;
		}

		public void VerifyParametersSetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct
		{
			VerifyNotDisposed();
			int num = Utilities.SizeOf<T>();
			int vertexStride = VertexDeclaration.VertexStride;
			ArgumentNullException.ThrowIfNull(source);
			if (sourceStartIndex < 0 || sourceCount < 0 || sourceStartIndex + sourceCount > source.Length)
			{
				throw new ArgumentException("Range is out of source bounds.");
			}
			if (targetStartIndex < 0 || (targetStartIndex * vertexStride) + (sourceCount * num) > VerticesCount * vertexStride)
			{
				throw new ArgumentException("Range is out of target bounds.");
			}
		}
	}
}
