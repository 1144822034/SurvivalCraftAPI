using Engine;
using Engine.Input;

public static class MultiKeyboard
{
	private class KeyboardData
	{
		public bool IsConnected;

		public bool[] KeysDownArray = new bool[Enum.GetValues(typeof(Key)).Length];

		public bool[] KeysDownOnceArray = new bool[Enum.GetValues(typeof(Key)).Length];

		public double[] KeysDownRepeatArray = new double[Enum.GetValues(typeof(Key)).Length];

		public Key? LastKey;

		public char? LastChar;
	}

	private const double KeyFirstRepeatTime = 0.3;

	private const double KeyNextRepeatTime = 0.04;

	private static KeyboardData[] _KeyboardData =
    [
        new(),
		new(),
		new(),
		new()
	];

	public static bool BackButtonQuitsApp { get; set; }

	public static event Action<int, Key> KeyDown;

	public static event Action<int, Key> KeyUp;

	public static event Action<int, char> CharacterEntered;

	public static bool IsConnected(int keyboardIndex)
	{
		return _KeyboardData[keyboardIndex].IsConnected;
	}

	public static bool IsKeyDown(int keyboardIndex, Key key)
	{
		return _KeyboardData[keyboardIndex].KeysDownArray[(int)key];
	}

	public static bool IsKeyDownOnce(int keyboardIndex, Key key)
	{
		return _KeyboardData[keyboardIndex].KeysDownOnceArray[(int)key];
	}

	public static bool IsKeyDownRepeat(int keyboardIndex, Key key)
	{
		double num = _KeyboardData[keyboardIndex].KeysDownRepeatArray[(int)key];
        return num < 0.0 || (num != 0.0 && Time.FrameStartTime >= num);
    }

    public static Key? LastKey(int keyboardIndex)
	{
		return _KeyboardData[keyboardIndex].LastKey;
	}

	public static char? LastChar(int keyboardIndex)
	{
		return _KeyboardData[keyboardIndex].LastChar;
	}

	public static void Clear()
	{
		for (int i = 0; i < 4; i++)
		{
			_KeyboardData[i].LastKey = null;
			_KeyboardData[i].LastChar = null;
			for (int j = 0; j < _KeyboardData[i].KeysDownArray.Length; j++)
			{
				_KeyboardData[i].KeysDownArray[j] = false;
				_KeyboardData[i].KeysDownOnceArray[j] = false;
				_KeyboardData[i].KeysDownRepeatArray[j] = 0.0;
			}
		}
	}

	internal static void BeforeFrame()
	{
	}

	internal static void AfterFrame()
	{
		for (int i = 0; i < 4; i++)
		{
			if (BackButtonQuitsApp && IsKeyDownOnce(i, Key.Back))
			{
				Window.Close();
			}
			_KeyboardData[i].LastKey = null;
			_KeyboardData[i].LastChar = null;
			for (int j = 0; j < _KeyboardData[i].KeysDownOnceArray.Length; j++)
			{
				_KeyboardData[i].KeysDownOnceArray[j] = false;
			}
			for (int k = 0; k < _KeyboardData[i].KeysDownRepeatArray.Length; k++)
			{
				if (_KeyboardData[i].KeysDownArray[k])
				{
					if (_KeyboardData[i].KeysDownRepeatArray[k] < 0.0)
					{
						_KeyboardData[i].KeysDownRepeatArray[k] = Time.FrameStartTime + 0.2;
					}
					else if (Time.FrameStartTime >= _KeyboardData[i].KeysDownRepeatArray[k])
					{
						_KeyboardData[i].KeysDownRepeatArray[k] = Math.Max(Time.FrameStartTime, _KeyboardData[i].KeysDownRepeatArray[k] + 0.033);
					}
				}
				else
				{
					_KeyboardData[i].KeysDownRepeatArray[k] = 0.0;
				}
			}
		}
	}

	private static void SetIsConnected(int keyboardIndex, bool value)
	{
		_KeyboardData[keyboardIndex].IsConnected = value;
	}

	private static bool ProcessKeyDown(int keyboardIndex, Key key)
	{
		if (!Window.IsActive)
		{
			return false;
		}
		_KeyboardData[keyboardIndex].LastKey = key;
		if (!_KeyboardData[keyboardIndex].KeysDownArray[(int)key])
		{
			_KeyboardData[keyboardIndex].KeysDownArray[(int)key] = true;
			_KeyboardData[keyboardIndex].KeysDownOnceArray[(int)key] = true;
			_KeyboardData[keyboardIndex].KeysDownRepeatArray[(int)key] = -1.0;
		}
		MultiKeyboard.KeyDown?.Invoke(keyboardIndex, key);
		return true;
	}

	private static bool ProcessKeyUp(int keyboardIndex, Key key)
	{
		if (!Window.IsActive)
		{
			return false;
		}
		if (_KeyboardData[keyboardIndex].KeysDownArray[(int)key])
		{
			_KeyboardData[keyboardIndex].KeysDownArray[(int)key] = false;
		}
		MultiKeyboard.KeyUp?.Invoke(keyboardIndex, key);
		return true;
	}

	private static bool ProcessCharacterEntered(int keyboardIndex, char ch)
	{
		if (!Window.IsActive)
		{
			return false;
		}
		_KeyboardData[keyboardIndex].LastChar = ch;
		MultiKeyboard.CharacterEntered?.Invoke(keyboardIndex, ch);
		return true;
	}

	internal static void Initialize()
	{
	}

	internal static void Dispose()
	{
	}
}
