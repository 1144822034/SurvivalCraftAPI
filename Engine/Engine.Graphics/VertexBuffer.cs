using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.ES30;

namespace Engine.Graphics
{
	public  class VertexBuffer : GraphicsResource
	{
        public int m_buffer;

		public string DebugName
		{
			get
			{
				return string.Empty;
			}
			set
			{
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

		public void SetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct
		{
			VerifyParametersSetData(source, sourceStartIndex, sourceCount, targetStartIndex);
			var gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
			try
			{
				int num = Utilities.SizeOf<T>();
				int vertexStride = VertexDeclaration.VertexStride;
				GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, m_buffer);
				GL.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(targetStartIndex * vertexStride), new IntPtr(num * sourceCount), gCHandle.AddrOfPinnedObject() + (sourceStartIndex * num));
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

        public void AllocateBuffer()
		{
			GL.GenBuffers(1, out m_buffer);
			GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, m_buffer);
			GL.BufferData(All.ArrayBuffer, new IntPtr(VertexDeclaration.VertexStride * VerticesCount), IntPtr.Zero, All.StaticDraw);
		}

        public void DeleteBuffer()
		{
			if (m_buffer != 0)
			{
				GLWrapper.DeleteBuffer(All.ArrayBuffer, m_buffer);
				m_buffer = 0;
			}
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
