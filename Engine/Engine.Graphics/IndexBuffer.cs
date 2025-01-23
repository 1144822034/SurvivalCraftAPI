using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;
using Buffer = System.Buffer;

namespace Engine.Graphics
{
	public  class IndexBuffer : GraphicsResource
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

		public IndexFormat IndexFormat
		{
			get;
			set;
		}

		public int IndicesCount
		{
			get;
			set;
		}

		public object Tag
		{
			get;
			set;
		}

		public IndexBuffer(IndexFormat indexFormat, int indicesCount)
		{
			InitializeIndexBuffer(indexFormat, indicesCount);
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
			var gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
			try
			{
				int num = Utilities.SizeOf<T>();
				int size = IndexFormat.GetSize();
				GLWrapper.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
				GLWrapper.GL.BufferSubData(BufferTargetARB.ElementArrayBuffer, new IntPtr(targetStartIndex * size), new UIntPtr((uint)(num * sourceCount)), (gCHandle.AddrOfPinnedObject() + (sourceStartIndex * num)).ToPointer());
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
			GLWrapper.GL.GenBuffers(1, out uint buffer);
            m_buffer = (int)buffer;
			GLWrapper.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
			GLWrapper.GL.BufferData(BufferTargetARB.ElementArrayBuffer, new UIntPtr((uint)(IndexFormat.GetSize() * IndicesCount)), null, BufferUsageARB.StaticDraw);
		}

        public void DeleteBuffer()
		{
			if (m_buffer != 0)
			{
				GLWrapper.DeleteBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
				m_buffer = 0;
			}
		}

		public override int GetGpuMemoryUsage()
		{
			return IndicesCount * IndexFormat.GetSize();
		}

		private void InitializeIndexBuffer(IndexFormat indexFormat, int indicesCount)
		{
			if (indicesCount <= 0)
			{
				throw new ArgumentException("Indices count must be greater than 0.");
			}
			IndexFormat = indexFormat;
			IndicesCount = indicesCount;
		}

		private void VerifyParametersSetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct
		{
			VerifyNotDisposed();
			int num = Utilities.SizeOf<T>();
			int size = IndexFormat.GetSize();
			ArgumentNullException.ThrowIfNull(source);
			if (sourceStartIndex < 0 || sourceCount < 0 || sourceStartIndex + sourceCount > source.Length)
			{
				throw new ArgumentException("Range is out of source bounds.");
			}
			if (targetStartIndex < 0 || (targetStartIndex * size) + (sourceCount * num) > IndicesCount * size)
			{
				throw new ArgumentException("Range is out of target bounds.");
			}
		}
	}
}
