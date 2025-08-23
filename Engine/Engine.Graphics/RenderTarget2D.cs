#if DIRECT3D11
using SharpDX.DXGI;
using SharpDX;
using SharpDX.Direct3D11;
#else
using Silk.NET.OpenGLES;
#endif
using System.Runtime.InteropServices;
using Engine.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphics
{
    public  class RenderTarget2D : Texture2D
    {
        private DepthFormat m_depthFormat;

        #if DIRECT3D11
        public RenderTargetView m_colorTextureView;

        public SharpDX.Direct3D11.Texture2D m_depthTexture;
        
        public DepthStencilView m_depthTextureView;
#else
        public int m_frameBuffer;

        public int m_depthBuffer;
#endif

        public DepthFormat DepthFormat
        {
            get
            {
                return m_depthFormat;
            }
            set
            {
                m_depthFormat = value;
            }
        }

        public RenderTarget2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat, DepthFormat depthFormat)
            : base(width, height, mipLevelsCount, colorFormat)
        {
            try
            {
                InitializeRenderTarget2D(width, height, mipLevelsCount, colorFormat, depthFormat);
                AllocateRenderTarget();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            DeleteRenderTarget();
        }

        public void GetData<T>(T[] target, int targetStartIndex, Rectangle sourceRectangle) where T : struct
        {
            VerifyParametersGetData(target, targetStartIndex, sourceRectangle);
            var gCHandle = GCHandle.Alloc(target, GCHandleType.Pinned);
            try
            {
                int num = Utilities.SizeOf<T>();
                GetDataInternal(gCHandle.AddrOfPinnedObject() + targetStartIndex * num, sourceRectangle);
            }
            finally
            {
                gCHandle.Free();
            }
        }
        public unsafe Image GetData(Rectangle sourceRectangle)
        {
            VerifyNotDisposed();
            Image<Rgba32> image = new(Image.DefaultImageSharpConfiguration, sourceRectangle.Width, sourceRectangle.Height);
            image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory);
            GetDataInternal((nint)memory.Pin().Pointer, sourceRectangle);
            return new Image(image);
        }

        public void GetData(nint target, Rectangle sourceRectangle)
        {
            VerifyParametersGetData(target, sourceRectangle);
            GetDataInternal(target, sourceRectangle);
        }

        public unsafe void GetDataInternal(nint target, Rectangle sourceRectangle)
        {
#if DIRECT3D11
            int size = ColorFormat.GetSize();
            Texture2DDescription texture2DDescription = new()
            {
                ArraySize = 1,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = DXWrapper.TranslateColorFormat(ColorFormat),
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                Width = sourceRectangle.Width,
                Height = sourceRectangle.Height
            };
            using (SharpDX.Direct3D11.Texture2D texture2D = new(DXWrapper.Device, texture2DDescription))
            {
                ResourceRegion resourceRegion = new ResourceRegion(sourceRectangle.Left, sourceRectangle.Top, 0, sourceRectangle.Left + sourceRectangle.Width, sourceRectangle.Top + sourceRectangle.Height, 1);
                DXWrapper.Context.CopySubresourceRegion(m_texture, 0, resourceRegion, texture2D, 0);
                DataStream dataStream = null;
                try
                {
                    DataBox dataBox = DXWrapper.Context.MapSubresource(texture2D, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                    int num = 0;
                    for (int i = 0; i < sourceRectangle.Height; i++)
                    {
                        dataStream.Position = i * dataBox.RowPitch;
                        dataStream.Read(target, num * size, sourceRectangle.Width * size);
                        num += sourceRectangle.Width;
                    }
                }
                finally
                {
                    dataStream?.Dispose();
                }
            }
#else
            GLWrapper.BindFramebuffer(m_frameBuffer);
            GLWrapper.GL.ReadPixels(sourceRectangle.Left, sourceRectangle.Top, (uint)sourceRectangle.Width, (uint)sourceRectangle.Height, PixelFormat.Rgba, PixelType.UnsignedByte, target.ToPointer());
#endif
        }

        public void GenerateMipMaps()
        {
#if DIRECT3D11
            DXWrapper.Context.GenerateMips(m_textureView);
#else
            GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, forceBind: false);
            GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
#endif
        }

        public override void HandleDeviceLost()
        {
            DeleteRenderTarget();
        }

        public override void HandleDeviceReset()
        {
#if DIRECT3D11
            base.HandleDeviceReset();
#endif
            AllocateRenderTarget();
        }

        public void AllocateRenderTarget()
        {
#if DIRECT3D11
            m_colorTextureView = new RenderTargetView(DXWrapper.Device, m_texture);
            if (DepthFormat != DepthFormat.None)
            {
                Texture2DDescription texture2DDescription = new Texture2DDescription
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.DepthStencil,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = DXWrapper.TranslateDepthFormat(DepthFormat),
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    Width = Width,
                    Height = Height
                };
                m_depthTexture = new SharpDX.Direct3D11.Texture2D(DXWrapper.Device, texture2DDescription);
                m_depthTextureView = new DepthStencilView(DXWrapper.Device, m_depthTexture);
            }
#else
            GLWrapper.GL.GenFramebuffers(1u, out uint frameBuffer);
            m_frameBuffer = (int)frameBuffer;
            GLWrapper.BindFramebuffer(m_frameBuffer);
            GLWrapper.GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                (uint)m_texture,
                0
            );
            if (DepthFormat != 0)
            {
                GLWrapper.GL.GenRenderbuffers(1u, out uint depthBuffer);
                m_depthBuffer = (int)depthBuffer;
                GLWrapper.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
                GLWrapper.GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, GLWrapper.TranslateDepthFormat(DepthFormat), (uint)Width, (uint)Height);
                GLWrapper.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);
                GLWrapper.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, 0);
            }
            else
            {
                GLWrapper.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
                GLWrapper.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, 0);
            }
            GLEnum framebufferErrorCode = GLWrapper.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (framebufferErrorCode != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException($"Error creating framebuffer ({framebufferErrorCode.ToString()}).");
            }
#endif
        }

        public void DeleteRenderTarget()
        {
#if DIRECT3D11
            Utilities.Dispose(ref m_colorTextureView);
            Utilities.Dispose(ref m_depthTexture);
            Utilities.Dispose(ref m_depthTextureView);
#else
            if (m_depthBuffer != 0)
            {
                uint depthBuffer = (uint)m_depthBuffer;
                GLWrapper.GL.DeleteRenderbuffers(1, in depthBuffer);
                m_depthBuffer = 0;
            }
            if (m_frameBuffer != 0)
            {
                GLWrapper.DeleteFramebuffer(m_frameBuffer);
                m_frameBuffer = 0;
            }
#endif
        }

        public new static RenderTarget2D Load(Color color, int width, int height)
        {
            RenderTarget2D renderTarget2D = new RenderTarget2D(width, height, 1, ColorFormat.Rgba8888, DepthFormat.None);
            Color[] array = new Color[width * height];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = color;
            }
            renderTarget2D.SetData(0, array);
            return renderTarget2D;
        }

        public new static RenderTarget2D Load(Image image, int mipLevelsCount = 1)
        {
            RenderTarget2D renderTarget2D = new RenderTarget2D(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888, DepthFormat.None);
            renderTarget2D.SetData(image.m_trueImage);
            if (mipLevelsCount > 1)
            {
#if DIRECT3D11
                DXWrapper.Context.GenerateMips(renderTarget2D.m_textureView);
#else
                GLWrapper.BindTexture(TextureTarget.Texture2D, renderTarget2D.m_texture, forceBind: false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
#endif
            }
            return renderTarget2D;
        }

        public new static RenderTarget2D Load(Stream stream, bool premultiplyAlpha = false, int mipLevelsCount = 1)
        {
            Image image = Image.Load(stream);
            if (premultiplyAlpha)
            {
                Image.PremultiplyAlpha(image);
            }
            return Load(image, mipLevelsCount);
        }

        public new static RenderTarget2D Load(string fileName, bool premultiplyAlpha = false, int mipLevelsCount = 1)
        {
            using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read);
            return Load(stream, premultiplyAlpha, mipLevelsCount);
        }

        public static Image Save(RenderTarget2D renderTarget)
        {
            if (renderTarget.ColorFormat != ColorFormat.Rgba8888)
            {
                throw new InvalidOperationException("Unsupported color format.");
            }
            return renderTarget.GetData(new Rectangle(0, 0, renderTarget.Width, renderTarget.Height));
        }

		public static void Save(RenderTarget2D renderTarget, Stream stream, ImageFileFormat format, bool saveAlpha)
		{
			if (renderTarget.ColorFormat != ColorFormat.Rgba8888)
			{
				throw new InvalidOperationException("Unsupported color format.");
			}
			Image.Save(renderTarget.GetData(new Rectangle(0, 0, renderTarget.Width, renderTarget.Height)), stream, format, saveAlpha);
		}

        public static void Save(RenderTarget2D renderTarget, string fileName, ImageFileFormat format, bool saveAlpha)
        {
            using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Create);
            Save(renderTarget, stream, format, saveAlpha);
        }

		public override int GetGpuMemoryUsage()
		{
			return base.GetGpuMemoryUsage() + (DepthFormat.GetSize() * Width * Height);
		}

		private void InitializeRenderTarget2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat, DepthFormat depthFormat)
		{
			DepthFormat = depthFormat;
		}

		private void VerifyParametersGetData<T>(T[] target, int targetStartIndex, Rectangle sourceRectangle) where T : struct
		{
			VerifyNotDisposed();
			int size = ColorFormat.GetSize();
			int num = Utilities.SizeOf<T>();
			ArgumentNullException.ThrowIfNull(target);
			if (num > size)
			{
				throw new ArgumentException("Target array element size is larger than pixel size.");
			}
			if (size % num != 0)
			{
				throw new ArgumentException("Pixel size is not an integer multiple of target array element size.");
			}
			if (sourceRectangle.Left < 0 || sourceRectangle.Width <= 0 || sourceRectangle.Top < 0 || sourceRectangle.Height <= 0 || sourceRectangle.Left + sourceRectangle.Width > Width || sourceRectangle.Top + sourceRectangle.Height > Height)
			{
				throw new ArgumentOutOfRangeException("sourceRectangle");
			}
			if (targetStartIndex < 0 || targetStartIndex >= target.Length)
			{
				throw new ArgumentOutOfRangeException("targetStartIndex");
			}
			if ((target.Length - targetStartIndex) * num < sourceRectangle.Width * sourceRectangle.Height * size)
			{
				throw new InvalidOperationException("Not enough space in target array.");
			}
		}

        public void VerifyParametersGetData(nint target, Rectangle sourceRectangle)
        {
            VerifyNotDisposed();
            if (target == IntPtr.Zero)
            {
                throw new ArgumentNullException("target");
            }
            if (sourceRectangle.Left < 0 || sourceRectangle.Width <= 0 || sourceRectangle.Top < 0 || sourceRectangle.Height <= 0 || sourceRectangle.Left + sourceRectangle.Width > Width || sourceRectangle.Top + sourceRectangle.Height > Height)
            {
                throw new ArgumentOutOfRangeException("sourceRectangle");
            }
        }

        public static void VerifyParametersSwap(RenderTarget2D renderTarget1, RenderTarget2D renderTarget2)
        {
            if (renderTarget1 == null)
            {
                throw new ArgumentNullException("renderTarget1");
            }
            if (renderTarget2 == null)
            {
                throw new ArgumentNullException("renderTarget2");
            }
            renderTarget1.VerifyNotDisposed();
            renderTarget2.VerifyNotDisposed();
        }
    }
}
