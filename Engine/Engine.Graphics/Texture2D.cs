using System.Runtime.InteropServices;
using Engine.Media;
using Silk.NET.OpenGLES;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphics
{
	public class Texture2D : GraphicsResource
	{
        private int m_width;
        private int m_height;
        private ColorFormat m_colorFormat;
        private int m_mipLevelsCount;
        private object m_tag;
		public int m_texture;
		public PixelFormat m_pixelFormat;
		public PixelType m_pixelType;
        private string m_debugName;

		public IntPtr NativeHandle => m_texture;

		public string DebugName
		{
			get
			{
				return m_debugName;
			}
			set
			{
                m_debugName = value;
			}
		}

        public int Width
        {
            get
            {
                return m_width;
            }
            private set
            {
                m_width = value;
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
            private set
            {
                m_height = value;
            }
        }

        public ColorFormat ColorFormat
        {
            get
            {
                return m_colorFormat;
            }
            private set
            {
                m_colorFormat = value;
            }
        }

        public int MipLevelsCount
        {
            get
            {
                return m_mipLevelsCount;
            }
            private set
            {
                m_mipLevelsCount = value;
            }
        }

        public object Tag
        {
            get
            {
                return m_tag;
            }
            set
            {
                m_tag = value;
            }
        }

		public Texture2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat)
		{
			InitializeTexture2D(width, height, mipLevelsCount, colorFormat);
			switch (ColorFormat)
			{
				case ColorFormat.Rgba8888:
					m_pixelFormat = PixelFormat.Rgba;
					m_pixelType = PixelType.UnsignedByte;
					break;
				case ColorFormat.Rgb565:
					m_pixelFormat = PixelFormat.Rgb;
					m_pixelType = PixelType.UnsignedShort565;
					break;
				case ColorFormat.Rgba5551:
					m_pixelFormat = PixelFormat.Rgba;
					m_pixelType = PixelType.UnsignedShort5551;
					break;
				case ColorFormat.R8:
					m_pixelFormat = (PixelFormat)6409;// GL_LUMINANCE
					m_pixelType = PixelType.UnsignedByte;
					break;
				default:
					throw new InvalidOperationException("Unsupported surface format.");
			}
			AllocateTexture();
		}

		public override void Dispose()
		{
			base.Dispose();
			DeleteTexture();
		}

		public void SetData<T>(int mipLevel, T[] source, int sourceStartIndex = 0) where T : struct
		{
			VerifyParametersSetData(mipLevel, source, sourceStartIndex);
			var gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
			try
			{
                int num = Utilities.SizeOf<T>();
                SetDataInternal(mipLevel, gCHandle.AddrOfPinnedObject() + sourceStartIndex * num);
			}
			finally
			{
				gCHandle.Free();
			}
		}
        public void SetData(int mipLevel, nint source)
        {
            VerifyParametersSetData(mipLevel, source);
            SetDataInternal(mipLevel, source);
        }

        public void SetDataInternal(int mipLevel, nint source)
        {
            int width = MathUtils.Max(Width >> mipLevel, 1);
            int height = MathUtils.Max(Height >> mipLevel, 1);
            GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, forceBind: false);
            GLWrapper.GL.TexImage2D(TextureTarget.Texture2D, mipLevel, (InternalFormat)m_pixelFormat, (uint)width, (uint)height, 0, m_pixelFormat, m_pixelType, source);
        }

		public unsafe void SetData(SixLabors.ImageSharp.Image<Rgba32> source)
		{
			VerifyParametersSetData(source);
			source.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory);
			GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, false);
			GLWrapper.GL.TexImage2D(
                TextureTarget.Texture2D,
				0,
				(InternalFormat)m_pixelFormat,
				(uint)source.Width,
                (uint)source.Height,
				0,
				m_pixelFormat,
				m_pixelType,
				memory.Pin().Pointer
			);
		}

        public static void Swap(Texture2D texture1, Texture2D texture2)
        {
            VerifyParametersSwap(texture1, texture2);
            SwapTexture2D(texture1, texture2);
            Utilities.Swap(ref texture1.m_texture, ref texture2.m_texture);
            Utilities.Swap(ref texture1.m_pixelFormat, ref texture2.m_pixelFormat);
            Utilities.Swap(ref texture1.m_pixelType, ref texture2.m_pixelType);
            Utilities.Swap(ref texture1.m_debugName, ref texture2.m_debugName);
        }

        public static void SwapTexture2D(Texture2D texture1, Texture2D texture2)
        {
            Utilities.Swap(ref texture1.m_width, ref texture2.m_width);
            Utilities.Swap(ref texture1.m_height, ref texture2.m_height);
            Utilities.Swap(ref texture1.m_colorFormat, ref texture2.m_colorFormat);
            Utilities.Swap(ref texture1.m_mipLevelsCount, ref texture2.m_mipLevelsCount);
            Utilities.Swap(ref texture1.m_tag, ref texture2.m_tag);
        }

        public override void HandleDeviceLost()
		{
			DeleteTexture();
		}

        public override void HandleDeviceReset()
		{
			AllocateTexture();
		}

		public unsafe void AllocateTexture()
		{
			GLWrapper.GL.GenTextures(1, out uint texture);
            m_texture = (int)texture;
			GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, forceBind: false);
			for (int i = 0; i < MipLevelsCount; i++)
			{
				int width = MathUtils.Max(Width >> i, 1);
				int height = MathUtils.Max(Height >> i, 1);
				GLWrapper.GL.TexImage2D(TextureTarget.Texture2D, i, (InternalFormat)m_pixelFormat, (uint)width, (uint)height, 0, m_pixelFormat, m_pixelType, null);
			}
		}

		public void DeleteTexture()
		{
			if (m_texture != 0)
			{
				GLWrapper.DeleteTexture(m_texture);
				m_texture = 0;
			}
		}
		
		public override int GetGpuMemoryUsage()
		{
			int num = 0;
			for (int i = 0; i < MipLevelsCount; i++)
			{
				int num2 = MathUtils.Max(Width >> i, 1);
				int num3 = MathUtils.Max(Height >> i, 1);
				num += ColorFormat.GetSize() * num2 * num3;
			}
			return num;
		}

		public static Texture2D Load(LegacyImage image, int mipLevelsCount = 1)
		{
			var texture2D = new Texture2D(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888);
			if (mipLevelsCount > 1)
			{
                LegacyImage[] array = LegacyImage.GenerateMipmaps(image, mipLevelsCount).ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					texture2D.SetData(i, array[i].Pixels);
				}
			}
			else
			{
			    texture2D.SetData(0, image.Pixels);
            }
            texture2D.Tag = image;
			return texture2D;
		}

		public static Texture2D Load(Image image, int mipLevelsCount = 1)
		{
			var texture2D = new Texture2D(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888);
			texture2D.SetData(image.m_trueImage);
            if(mipLevelsCount > 1)
            {
                GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, forceBind: false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            texture2D.Tag = image;
			return texture2D;
		}

        public static Texture2D Load(SixLabors.ImageSharp.Image<Rgba32> image, int mipLevelsCount = 1)
        {
            var texture2D = new Texture2D(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888);
            texture2D.SetData(image);
            if (mipLevelsCount > 1)
            {
                GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, forceBind: false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            texture2D.Tag = new Image(image);
            return texture2D;
        }

        public static Texture2D Load(Stream stream, bool premultiplyAlpha = false, int mipLevelsCount = 1)
		{
			var image = Image.Load(stream);
			if (premultiplyAlpha)
			{
				Image.PremultiplyAlpha(image);
			}
			return Load(image, mipLevelsCount);
		}

		public static Texture2D Load(string fileName, bool premultiplyAlpha = false, int mipLevelsCount = 1)
		{
			using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read))
			{
				return Load(stream, premultiplyAlpha, mipLevelsCount);
			}
		}

		internal void InitializeTexture2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat)
		{
			if (width < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(width));
			}
			if (height < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(height));
			}
			if (mipLevelsCount < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(mipLevelsCount));
			}
			Width = width;
			Height = height;
			ColorFormat = colorFormat;
			if (mipLevelsCount > 1)
			{
				int num = 0;
				for (int num2 = MathUtils.Max(width, height); num2 >= 1; num2 /= 2)
				{
					num++;
				}
				MipLevelsCount = MathUtils.Min(num, mipLevelsCount);
			}
			else
			{
				MipLevelsCount = 1;
			}
		}

		private void VerifyParametersSetData<T>(int mipLevel, T[] source, int sourceStartIndex = 0) where T : struct
		{
			VerifyNotDisposed();
			int num = Utilities.SizeOf<T>();
			int size = ColorFormat.GetSize();
			int num2 = MathUtils.Max(Width >> mipLevel, 1);
			int num3 = MathUtils.Max(Height >> mipLevel, 1);
			int num4 = size * num2 * num3;
			ArgumentNullException.ThrowIfNull(source);
						if (mipLevel < 0 || mipLevel >= MipLevelsCount)
			{
				throw new ArgumentOutOfRangeException(nameof(mipLevel));
			}
			if (num > size)
			{
				throw new ArgumentNullException("Source array element size is larger than pixel size.");
			}
			if (size % num != 0)
			{
				throw new ArgumentNullException("Pixel size is not an integer multiple of source array element size.");
			}
			if (sourceStartIndex < 0 || (source.Length - sourceStartIndex) * num < num4)
			{
				throw new InvalidOperationException("Not enough data in source array.");
			}
		}

        public void VerifyParametersSetData(int mipLevel, nint source)
        {
            VerifyNotDisposed();
            if (source == IntPtr.Zero)
            {
                throw new ArgumentNullException("source");
            }
            if (mipLevel < 0 || mipLevel >= MipLevelsCount)
            {
                throw new ArgumentOutOfRangeException("mipLevel");
            }
        }

        private static void VerifyParametersSwap(Texture2D texture1, Texture2D texture2)
        {
            if (texture1 == null)
            {
                throw new ArgumentNullException("texture1");
            }
            if (texture2 == null)
            {
                throw new ArgumentNullException("texture2");
            }
            if (texture1.GetType() != typeof(Texture2D))
            {
                throw new ArgumentException("texture1");
            }
            if (texture2.GetType() != typeof(Texture2D))
            {
                throw new ArgumentException("texture2");
            }
            texture1.VerifyNotDisposed();
            texture2.VerifyNotDisposed();
        }

		private void VerifyParametersSetData(SixLabors.ImageSharp.Image<Rgba32> source)
		{
			VerifyNotDisposed();
			ArgumentNullException.ThrowIfNull(source);
		}
	}
}
