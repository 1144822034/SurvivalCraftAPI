using System;
using System.Runtime.InteropServices;
#if DIRECT3D11
using SharpDX;
using SharpDX.Direct3D11;
#else
using Silk.NET.OpenGLES;
#endif
using Buffer = System.Buffer;

namespace Engine.Graphics
{
	public  class IndexBuffer : GraphicsResource
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
#if DIRECT3D11
                DataBox dataBox = new (gCHandle.AddrOfPinnedObject() + (sourceStartIndex * num), 1, 0);
                ResourceRegion resourceRegion = new (targetStartIndex * size, 0, 0, (targetStartIndex * size) + (sourceCount * num), 1, 1);
                DXWrapper.Context.UpdateSubresource(dataBox, m_buffer, 0, resourceRegion);
#else
				GLWrapper.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
				GLWrapper.GL.BufferSubData(BufferTargetARB.ElementArrayBuffer, new IntPtr(targetStartIndex * size), new UIntPtr((uint)(num * sourceCount)), (gCHandle.AddrOfPinnedObject() + (sourceStartIndex * num)).ToPointer());
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
            m_buffer = new SharpDX.Direct3D11.Buffer(DXWrapper.Device, IndexFormat.GetSize() * IndicesCount, ResourceUsage.Default, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
#else
			GLWrapper.GL.GenBuffers(1, out uint buffer);
            m_buffer = (int)buffer;
			GLWrapper.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
			GLWrapper.GL.BufferData(BufferTargetARB.ElementArrayBuffer, new UIntPtr((uint)(IndexFormat.GetSize() * IndicesCount)), null, BufferUsageARB.StaticDraw);
#endif
		}

        public void DeleteBuffer()
		{
#if DIRECT3D11
            Utilities.Dispose(ref m_buffer);
#else
			if (m_buffer != 0)
			{
				GLWrapper.DeleteBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
				m_buffer = 0;
			}
#endif
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
