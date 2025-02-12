
using System.Diagnostics;
#if ANDROID
using System.Collections.Concurrent;
using Android.App;
using Android.Views;
using Android.Widget;
#else
using Silk.NET.Input;
#endif

namespace Engine.Input
{
    public static class Keyboard
    {
#if ANDROID
        public struct KeyInfo
        {
            public Keycode KeyCode;
            public KeyEventActions Action;
            public int? UnicodeChar;
            public KeyInfo(Keycode keyCode, KeyEventActions action, int? unicodeChar)
            {
                KeyCode = keyCode;
                Action = action;
                UnicodeChar = unicodeChar;
            }
        }

        public static ConcurrentQueue<KeyInfo> m_cachedKeyEvents = [];
#else
        public static IKeyboard m_keyboard;
#endif

        public static double m_keyFirstRepeatTime = 0.3;

        public static double m_keyNextRepeatTime = 0.04;

        private static bool[] m_keysDownArray = new bool[Enum.GetValues(typeof(Key)).Length];

        private static bool[] m_keysDownOnceArray = new bool[Enum.GetValues(typeof(Key)).Length];

        private static double[] m_keysDownRepeatArray = new double[Enum.GetValues(typeof(Key)).Length];

        private static Key? m_lastKey;

        public static string LastString;

        private static char? m_lastChar;

        public static Key? LastKey => m_lastKey;

        public static char? LastChar => m_lastChar;

        public static bool IsKeyboardVisible
        {
            get;
            private set;
        }

        public static bool BackButtonQuitsApp
        {
            get;
            set;
        }

        public static event Action<Key> KeyDown;

        public static event Action<Key> KeyUp;

        public static event Action<char> CharacterEntered;

        public static bool IsKeyDown(Key key)
        {
            return m_keysDownArray[(int)key];
        }

        public static bool IsKeyDownOnce(Key key)
        {
            return m_keysDownOnceArray[(int)key];
        }

        public static bool IsKeyDownRepeat(Key key)
        {
            double num = m_keysDownRepeatArray[(int)key];
            return num < 0.0 || num != 0.0 && Time.FrameStartTime >= num;
        }

        public static void ShowKeyboard(string title, string description, string defaultText, bool passwordMode, Action<string> enter, Action cancel)
        {
            ArgumentNullException.ThrowIfNull(title);
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(defaultText);
            if (!IsKeyboardVisible)
            {
                Clear();
                Touch.Clear();
                Mouse.Clear();
                IsKeyboardVisible = true;
                try
                {
                    ShowKeyboardInternal(title, description, defaultText, passwordMode, delegate (string text)
                    {
                        Dispatcher.Dispatch(delegate
                        {
                            IsKeyboardVisible = false;
                            enter?.Invoke(text ?? string.Empty);
                        });
                    }, delegate
                    {
                        Dispatcher.Dispatch(delegate
                        {
                            IsKeyboardVisible = false;
                            cancel?.Invoke();
                        });
                    });
                }
                catch
                {
                    IsKeyboardVisible = false;
                    throw;
                }
            }
        }

        public static void Clear()
        {
            m_lastKey = null;
            m_lastChar = null;
            for (int i = 0; i < m_keysDownArray.Length; i++)
            {
                m_keysDownArray[i] = false;
                m_keysDownOnceArray[i] = false;
                m_keysDownRepeatArray[i] = 0.0;
            }
        }

        internal static void BeforeFrame()
        {
#if ANDROID
            while (!m_cachedKeyEvents.IsEmpty)
            {
                if (m_cachedKeyEvents.TryDequeue(out KeyInfo keyInfo))
                {
                    switch (keyInfo.Action)
                    {
                        case KeyEventActions.Down:
                            HandleKeyDown(keyInfo.KeyCode);
                            if (keyInfo.UnicodeChar.HasValue)
                            {
                                HandleKeyPress(keyInfo.UnicodeChar.Value);
                            }
                            break;
                        case KeyEventActions.Up: HandleKeyUp(keyInfo.KeyCode); break;
                    }
                }
                else
                {
                    Thread.Yield();
                }
            }
#endif
        }

        internal static void AfterFrame()
        {
            if (BackButtonQuitsApp && IsKeyDownOnce(Key.Back))
            {
                Window.Close();
            }
            m_lastKey = null;
            m_lastChar = null;
            for (int i = 0; i < m_keysDownOnceArray.Length; i++)
            {
                m_keysDownOnceArray[i] = false;
            }
            for (int j = 0; j < m_keysDownRepeatArray.Length; j++)
            {
                if (m_keysDownArray[j])
                {
                    if (m_keysDownRepeatArray[j] < 0.0)
                    {
                        m_keysDownRepeatArray[j] = Time.FrameStartTime + m_keyFirstRepeatTime;
                    }
                    else if (Time.FrameStartTime >= m_keysDownRepeatArray[j])
                    {
                        m_keysDownRepeatArray[j] = Math.Max(Time.FrameStartTime, m_keysDownRepeatArray[j] + m_keyNextRepeatTime);
                    }
                }
                else
                {
                    m_keysDownRepeatArray[j] = 0.0;
                }
            }
        }

        public static bool ProcessKeyDown(Key key)
        {
            if (!Window.IsActive || IsKeyboardVisible)
            {
                return false;
            }
            m_lastKey = key;
            if (!m_keysDownArray[(int)key])
            {
                m_keysDownArray[(int)key] = true;
                m_keysDownOnceArray[(int)key] = true;
                m_keysDownRepeatArray[(int)key] = -1.0;
            }
            KeyDown?.Invoke(key);
            return true;
        }

        public static bool ProcessKeyUp(Key key)
        {
            if (!Window.IsActive || IsKeyboardVisible)
            {
                return false;
            }
            if (m_keysDownArray[(int)key])
            {
                m_keysDownArray[(int)key] = false;
            }
            KeyUp?.Invoke(key);
            return true;
        }

        public static bool ProcessCharacterEntered(char ch)
        {
            if (!Window.IsActive || IsKeyboardVisible)
            {
                return false;
            }
            m_lastChar = ch;
            CharacterEntered?.Invoke(ch);
            return true;
        }

        internal static void Initialize()
        {
#if !ANDROID
            m_keyboard = Window.m_inputContext.Keyboards[0];
            m_keyboard.KeyDown += KeyDownHandler;
            m_keyboard.KeyUp += KeyUpHandler;
            m_keyboard.KeyChar += KeyPressHandler;
#endif
        }

        internal static void Dispose()
        {
        }
#if !ANDROID
        private static void ShowKeyboardInternal(string title, string description, string defaultText, bool passwordMode, Action<string> enter, Action cancel)
        {
            cancel();
        }

        private static void KeyDownHandler(IKeyboard keyboard, Silk.NET.Input.Key key, int scancode)
        {
            if (scancode == 270 || key == Silk.NET.Input.Key.Delete) KeyboardInput.DeletePressed = true;
            Key translatedKey = TranslateKey(key);
            if (translatedKey != (Key)(-1))
            {
                ProcessKeyDown(translatedKey);
            }
            else if (scancode == 270)
            {
                ProcessKeyDown(Key.Back);
            }
        }

        private static void KeyUpHandler(IKeyboard keyboard, Silk.NET.Input.Key key, int scancode)
        {
            Key translatedKey = TranslateKey(key);
            if (translatedKey != (Key)(-1))
            {
                ProcessKeyUp(translatedKey);
            }
            else if (scancode == 270)
            {
                ProcessKeyUp(Key.Back);
            }
        }

        private static void KeyPressHandler(IKeyboard keyboard, char c)
        {
            KeyboardInput.Chars.Add(c);
            ProcessCharacterEntered(c);
            LastString += c;
        }
#else
        public static void HandleKeyEvent(KeyEvent keyEvent)
        {
            m_cachedKeyEvents.Enqueue(new KeyInfo(keyEvent.KeyCode, keyEvent.Action, keyEvent.UnicodeChar));
        }

        internal static void HandleKeyDown(Keycode keyCode)
        {
            Key key = TranslateKey(keyCode);
            if (key != (Key)(-1))
            {
                ProcessKeyDown(key);
            }
        }

        internal static void HandleKeyUp(Keycode keyCode)
        {
            Key key = TranslateKey(keyCode);
            if (key != (Key)(-1))
            {
                ProcessKeyUp(key);
            }
        }

        internal static void HandleKeyPress(int unicodeCharacter)
        {
            ProcessCharacterEntered((char)unicodeCharacter);
        }

#endif
#if !ANDROID
        public static Key TranslateKey(Silk.NET.Input.Key key)
        {
            switch (key)
            {
                case Silk.NET.Input.Key.ShiftLeft:
                    return Key.Shift;
                case Silk.NET.Input.Key.ShiftRight:
                    return Key.Shift;
                case Silk.NET.Input.Key.ControlLeft:
                    return Key.Control;
                case Silk.NET.Input.Key.ControlRight:
                    return Key.Control;
                case Silk.NET.Input.Key.F1:
                    return Key.F1;
                case Silk.NET.Input.Key.F2:
                    return Key.F2;
                case Silk.NET.Input.Key.F3:
                    return Key.F3;
                case Silk.NET.Input.Key.F4:
                    return Key.F4;
                case Silk.NET.Input.Key.F5:
                    return Key.F5;
                case Silk.NET.Input.Key.F6:
                    return Key.F6;
                case Silk.NET.Input.Key.F7:
                    return Key.F7;
                case Silk.NET.Input.Key.F8:
                    return Key.F8;
                case Silk.NET.Input.Key.F9:
                    return Key.F9;
                case Silk.NET.Input.Key.F10:
                    return Key.F10;
                case Silk.NET.Input.Key.F11:
                    return Key.F11;
                case Silk.NET.Input.Key.F12:
                    return Key.F12;
                case Silk.NET.Input.Key.Up:
                    return Key.UpArrow;
                case Silk.NET.Input.Key.Down:
                    return Key.DownArrow;
                case Silk.NET.Input.Key.Left:
                    return Key.LeftArrow;
                case Silk.NET.Input.Key.Right:
                    return Key.RightArrow;
                case Silk.NET.Input.Key.Enter:
                    return Key.Enter;
                case Silk.NET.Input.Key.KeypadEnter:
                    return Key.Enter;
                case Silk.NET.Input.Key.Escape:
                    return Key.Escape;
                case Silk.NET.Input.Key.Space:
                    return Key.Space;
                case Silk.NET.Input.Key.Tab:
                    return Key.Tab;
                case Silk.NET.Input.Key.Backspace:
                    return Key.BackSpace;
                case Silk.NET.Input.Key.Insert:
                    return Key.Insert;
                case Silk.NET.Input.Key.Delete:
                    return Key.Delete;
                case Silk.NET.Input.Key.PageUp:
                    return Key.PageUp;
                case Silk.NET.Input.Key.PageDown:
                    return Key.PageDown;
                case Silk.NET.Input.Key.Home:
                    return Key.Home;
                case Silk.NET.Input.Key.End:
                    return Key.End;
                case Silk.NET.Input.Key.CapsLock:
                    return Key.CapsLock;
                case Silk.NET.Input.Key.A:
                    return Key.A;
                case Silk.NET.Input.Key.B:
                    return Key.B;
                case Silk.NET.Input.Key.C:
                    return Key.C;
                case Silk.NET.Input.Key.D:
                    return Key.D;
                case Silk.NET.Input.Key.E:
                    return Key.E;
                case Silk.NET.Input.Key.F:
                    return Key.F;
                case Silk.NET.Input.Key.G:
                    return Key.G;
                case Silk.NET.Input.Key.H:
                    return Key.H;
                case Silk.NET.Input.Key.I:
                    return Key.I;
                case Silk.NET.Input.Key.J:
                    return Key.J;
                case Silk.NET.Input.Key.K:
                    return Key.K;
                case Silk.NET.Input.Key.L:
                    return Key.L;
                case Silk.NET.Input.Key.M:
                    return Key.M;
                case Silk.NET.Input.Key.N:
                    return Key.N;
                case Silk.NET.Input.Key.O:
                    return Key.O;
                case Silk.NET.Input.Key.P:
                    return Key.P;
                case Silk.NET.Input.Key.Q:
                    return Key.Q;
                case Silk.NET.Input.Key.R:
                    return Key.R;
                case Silk.NET.Input.Key.S:
                    return Key.S;
                case Silk.NET.Input.Key.T:
                    return Key.T;
                case Silk.NET.Input.Key.U:
                    return Key.U;
                case Silk.NET.Input.Key.V:
                    return Key.V;
                case Silk.NET.Input.Key.W:
                    return Key.W;
                case Silk.NET.Input.Key.X:
                    return Key.X;
                case Silk.NET.Input.Key.Y:
                    return Key.Y;
                case Silk.NET.Input.Key.Z:
                    return Key.Z;
                case Silk.NET.Input.Key.Number0:
                    return Key.Number0;
                case Silk.NET.Input.Key.Number1:
                    return Key.Number1;
                case Silk.NET.Input.Key.Number2:
                    return Key.Number2;
                case Silk.NET.Input.Key.Number3:
                    return Key.Number3;
                case Silk.NET.Input.Key.Number4:
                    return Key.Number4;
                case Silk.NET.Input.Key.Number5:
                    return Key.Number5;
                case Silk.NET.Input.Key.Number6:
                    return Key.Number6;
                case Silk.NET.Input.Key.Number7:
                    return Key.Number7;
                case Silk.NET.Input.Key.Number8:
                    return Key.Number8;
                case Silk.NET.Input.Key.Number9:
                    return Key.Number9;
                case Silk.NET.Input.Key.GraveAccent:
                    return Key.Tilde;
                case Silk.NET.Input.Key.Minus:
                    return Key.Minus;
                case Silk.NET.Input.Key.Equal:
                    return Key.Plus;
                case Silk.NET.Input.Key.LeftBracket:
                    return Key.LeftBracket;
                case Silk.NET.Input.Key.RightBracket:
                    return Key.RightBracket;
                case Silk.NET.Input.Key.Semicolon:
                    return Key.Semicolon;
                case Silk.NET.Input.Key.Apostrophe:
                    return Key.Quote;
                case Silk.NET.Input.Key.Comma:
                    return Key.Comma;
                case Silk.NET.Input.Key.Period:
                    return Key.Period;
                case Silk.NET.Input.Key.Slash:
                    return Key.Slash;
                case Silk.NET.Input.Key.AltLeft:
                case Silk.NET.Input.Key.AltRight:
                    return Key.Alt;
                default:
                    return (Key)(-1);
            }
        }
#else
        public static Key TranslateKey(Keycode keyCode)
        {
            switch (keyCode)
            {
                case Keycode.Home:
                    return Key.Home;
                case Keycode.Back:
                    return Key.Back;
                case Keycode.Num0:
                    return Key.Number0;
                case Keycode.Num1:
                    return Key.Number1;
                case Keycode.Num2:
                    return Key.Number2;
                case Keycode.Num3:
                    return Key.Number3;
                case Keycode.Num4:
                    return Key.Number4;
                case Keycode.Num5:
                    return Key.Number5;
                case Keycode.Num6:
                    return Key.Number6;
                case Keycode.Num7:
                    return Key.Number7;
                case Keycode.Num8:
                    return Key.Number8;
                case Keycode.Num9:
                    return Key.Number9;
                case Keycode.A:
                    return Key.A;
                case Keycode.B:
                    return Key.B;
                case Keycode.C:
                    return Key.C;
                case Keycode.D:
                    return Key.D;
                case Keycode.E:
                    return Key.E;
                case Keycode.F:
                    return Key.F;
                case Keycode.G:
                    return Key.G;
                case Keycode.H:
                    return Key.H;
                case Keycode.I:
                    return Key.I;
                case Keycode.J:
                    return Key.J;
                case Keycode.K:
                    return Key.K;
                case Keycode.L:
                    return Key.L;
                case Keycode.M:
                    return Key.M;
                case Keycode.N:
                    return Key.N;
                case Keycode.O:
                    return Key.O;
                case Keycode.P:
                    return Key.P;
                case Keycode.Q:
                    return Key.Q;
                case Keycode.R:
                    return Key.R;
                case Keycode.S:
                    return Key.S;
                case Keycode.T:
                    return Key.T;
                case Keycode.U:
                    return Key.U;
                case Keycode.V:
                    return Key.V;
                case Keycode.W:
                    return Key.W;
                case Keycode.X:
                    return Key.X;
                case Keycode.Y:
                    return Key.Y;
                case Keycode.Z:
                    return Key.Z;
                case Keycode.Comma:
                    return Key.Comma;
                case Keycode.Period:
                    return Key.Period;
                case Keycode.ShiftLeft:
                    return Key.Shift;
                case Keycode.ShiftRight:
                    return Key.Shift;
                case Keycode.Tab:
                    return Key.Tab;
                case Keycode.Space:
                    return Key.Space;
                case Keycode.Enter:
                    return Key.Enter;
                case Keycode.Del:
                    return Key.Delete;
                case Keycode.Minus:
                    return Key.Minus;
                case Keycode.LeftBracket:
                    return Key.LeftBracket;
                case Keycode.RightBracket:
                    return Key.RightBracket;
                case Keycode.Semicolon:
                    return Key.Semicolon;
                case Keycode.Slash:
                    return Key.Slash;
                case Keycode.Plus:
                    return Key.Plus;
                case Keycode.PageUp:
                    return Key.PageUp;
                case Keycode.PageDown:
                    return Key.PageDown;
                case Keycode.Escape:
                    return Key.Escape;
                case Keycode.ForwardDel:
                    return Key.Delete;
                case Keycode.CtrlLeft:
                    return Key.Control;
                case Keycode.CtrlRight:
                    return Key.Control;
                case Keycode.CapsLock:
                    return Key.CapsLock;
                case Keycode.Insert:
                    return Key.Insert;
                case Keycode.F1:
                    return Key.F1;
                case Keycode.F2:
                    return Key.F2;
                case Keycode.F3:
                    return Key.F3;
                case Keycode.F4:
                    return Key.F4;
                case Keycode.F5:
                    return Key.F5;
                case Keycode.F6:
                    return Key.F6;
                case Keycode.F7:
                    return Key.F7;
                case Keycode.F8:
                    return Key.F8;
                case Keycode.F9:
                    return Key.F9;
                case Keycode.F10:
                    return Key.F10;
                case Keycode.F11:
                    return Key.F11;
                case Keycode.F12:
                    return Key.F12;
                case Keycode.AltLeft:
                case Keycode.AltRight:
                        return Key.Alt;
                default:
                    return (Key)(-1);
            }
        }

        public static void ShowKeyboardInternal(string title, string description, string defaultText, bool passwordMode, Action<string> enter, Action cancel)
        {
            AlertDialog.Builder builder = new(Window.Activity);
            builder.SetTitle(title);
            builder.SetMessage(description);
            EditText editText = new(Window.Activity);
            editText.Text = defaultText;
            builder.SetView(editText);
            builder.SetPositiveButton("Ok", delegate
            {
                enter(editText.Text);
            });
            builder.SetNegativeButton("Cancel", delegate
            {
                cancel();
            });
            Window.Activity.RunOnUiThread(
                () =>
                {
                    AlertDialog alertDialog = builder.Create();
                    alertDialog.DismissEvent += delegate
                    {
                        cancel();
                    };
                    alertDialog.CancelEvent += delegate
                    {
                        cancel();
                    };
                    alertDialog.Window.Attributes.Gravity = GravityFlags.Center;
                    alertDialog.Show();
                }
            );
        }
#endif
		}
	}