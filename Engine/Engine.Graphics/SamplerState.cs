namespace Engine.Graphics
{
	public  class SamplerState : LockOnFirstUse
	{
		private TextureFilterMode m_filterMode;

		private TextureAddressMode m_addressModeU;

		private TextureAddressMode m_addressModeV;

		private int m_maxAnisotropy = 1;

		private float m_minLod = -1000f;

		private float m_maxLod = 1000f;

		private float m_mipLodBias;

		public static SamplerState PointClamp = new()
		{
			FilterMode = TextureFilterMode.Point,
			AddressModeU = TextureAddressMode.Clamp,
			AddressModeV = TextureAddressMode.Clamp,
			IsLocked = true
		};

		public static SamplerState PointWrap = new()
		{
			FilterMode = TextureFilterMode.Point,
			AddressModeU = TextureAddressMode.Wrap,
			AddressModeV = TextureAddressMode.Wrap,
			IsLocked = true
		};

		public static SamplerState LinearClamp = new()
		{
			FilterMode = TextureFilterMode.Linear,
			AddressModeU = TextureAddressMode.Clamp,
			AddressModeV = TextureAddressMode.Clamp,
			IsLocked = true
		};

		public static SamplerState LinearWrap = new()
		{
			FilterMode = TextureFilterMode.Linear,
			AddressModeU = TextureAddressMode.Wrap,
			AddressModeV = TextureAddressMode.Wrap,
			IsLocked = true
		};

		public static SamplerState AnisotropicClamp = new()
		{
			FilterMode = TextureFilterMode.Anisotropic,
			AddressModeU = TextureAddressMode.Clamp,
			AddressModeV = TextureAddressMode.Clamp,
			MaxAnisotropy = 16,
			IsLocked = true
		};

		public static SamplerState AnisotropicWrap = new()
		{
			FilterMode = TextureFilterMode.Anisotropic,
			AddressModeU = TextureAddressMode.Wrap,
			AddressModeV = TextureAddressMode.Wrap,
			MaxAnisotropy = 16,
			IsLocked = true
		};

		public TextureFilterMode FilterMode
		{
			get
			{
				return m_filterMode;
			}
			set
			{
				ThrowIfLocked();
				m_filterMode = value;
			}
		}

		public TextureAddressMode AddressModeU
		{
			get
			{
				return m_addressModeU;
			}
			set
			{
				ThrowIfLocked();
				m_addressModeU = value;
			}
		}

		public TextureAddressMode AddressModeV
		{
			get
			{
				return m_addressModeV;
			}
			set
			{
				ThrowIfLocked();
				m_addressModeV = value;
			}
		}

		public int MaxAnisotropy
		{
			get
			{
				return m_maxAnisotropy;
			}
			set
			{
				ThrowIfLocked();
				m_maxAnisotropy = MathUtils.Max(value, 1);
			}
		}

		public float MinLod
		{
			get
			{
				return m_minLod;
			}
			set
			{
				ThrowIfLocked();
				m_minLod = value;
			}
		}

		public float MaxLod
		{
			get
			{
				return m_maxLod;
			}
			set
			{
				ThrowIfLocked();
				m_maxLod = value;
			}
		}

		public float MipLodBias
		{
			get
			{
				return m_mipLodBias;
			}
			set
			{
				ThrowIfLocked();
				m_mipLodBias = value;
			}
		}
	}
}