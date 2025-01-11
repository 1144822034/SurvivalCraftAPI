using System.Diagnostics;
using Engine;
using Engine.Graphics;
using Engine.Input;
using Engine.Media;
using Window = Engine.Window;

#if WINDOWS
using ImeSharp;
#endif

namespace Game;

/// <summary>
/// <para>
/// Survivalcraft Api 所添加的文本框
/// （支持 Windows IME 输入法, Android IME 适配待开发）。
/// </para>
/// <para>
/// A text box widget that added by Survivalcraft Api
/// (Support Windows IME, and Android IME support is pending development).
/// </para>
/// </summary>
public class TextBoxWidget : Widget
{
    // 已过时成员

    #region Obsolete Members

    /// <summary>
    /// <para>
    /// 已弃用。
    /// </para>
    /// <para>
    /// Deprecated.
    /// </para>
    /// </summary>
    [Obsolete("TextBoxWidget.JustOpened is deprecated.", error: true)]
    public bool JustOpened;

    [Obsolete("TextBoxWidget.MoveNextFlag is deprecated.", error: true)]
    public bool MoveNextFlag;

    [Obsolete($"TextBoxWidget.m_scroll is deprecated, use {nameof(TextBoxWidget)}.{nameof(Scroll)} instead.")]
    public float m_scroll;

    /// <summary>
    /// <para>
    /// Android 文本框的 Description。
    /// </para>
    /// <para>
    /// This is the description of android text box.
    /// </para>
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// <para>
    /// Android 文本框的 Title。
    /// </para>
    /// <para>
    /// This is the title of android text box.
    /// </para>
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// <para>
    /// 旧版文本框的光标位置，现版本不再使用。
    /// </para>
    /// <para>
    /// Old caret position, now is deprecated.
    /// </para>
    /// </summary>
    [Obsolete($"TextBoxWidget.m_caretPosition is deprecated, use {nameof(TextBoxWidget)}.{nameof(Caret)} instead.",
        error: true)]
    public int m_caretPosition;

    /// <summary>
    /// <para>
    /// <see cref="LegacyTextBoxWidget"/> 里的旧版 <see cref="Caret"/> 属性，现版本不再使用。
    /// </para>
    /// <para>
    /// Old property of <see cref="Caret"/> in <see cref="LegacyTextBoxWidget"/>, now is deprecated.
    /// </para>
    /// </summary>
    [Obsolete($"TextBoxWidget.CaretPosition is deprecated, use {nameof(TextBoxWidget)}.{nameof(Caret)} instead.",
        error: true)]
    public int CaretPosition
    {
        get => Caret;
        set => Caret = value;
    }

    /// <summary>
    /// <para>
    /// <see cref="LegacyTextBoxWidget.HasFocus"/> 的后台字段，现版本不再使用，请用 <see cref="HasFocus"/> 替代。
    /// </para>
    /// <para>
    /// The backend field of <see cref="LegacyTextBoxWidget.HasFocus"/>, now is deprecated, use <see cref="HasFocus"/> instead.
    /// </para>
    /// </summary>
    [Obsolete($"TextBoxWidget.m_hasFocus is deprecated, use {nameof(TextBoxWidget)}.{nameof(HasFocus)} instead.",
        error: true)]
    public bool m_hasFocus;

    /// <summary>
    /// <para>
    /// <see cref="LegacyTextBoxWidget.Size"/> 的后台字段，现版本不再使用。
    /// </para>
    /// <para>
    /// The backend field of old <see cref="LegacyTextBoxWidget.Size"/>, now is deprecated.
    /// </para>
    /// </summary>
    [Obsolete($"TextBoxWidget.m_size is deprecated, use {nameof(TextBoxWidget)}.{nameof(Size)} property instead.",
        error: true)]
    public Vector2? m_size;

    /// <summary>
    /// <para>
    /// 等效于 <see cref="FocusStartTime"/>，
    /// 现版本不再使用，请用 <see cref="FocusStartTime"/> 替代。
    /// </para>
    /// <para>
    /// Is the same as <see cref="FocusStartTime"/>,
    /// now is deprecated, use <see cref="FocusStartTime"/> instead.
    /// </para>
    /// </summary>
    [Obsolete(
        $"TextBoxWidget.m_focusStartTime is deprecated, use {nameof(TextBoxWidget)}.{nameof(FocusStartTime)} instead.",
        error: true)]
    public float m_focusStartTime = 0;

    #endregion

    // 文本存储

    #region Texts

    /// <summary>
    /// <para>
    /// 选中的字符，当 <see cref="SelectionLength"/> 为 0 时返回 null。
    /// </para>
    /// </summary>
    public string SelectionString
    {
        get
        {
            if (SelectionLength == 0)
            {
                return null;
            }

            int caret = Caret;
            int selectionLength = SelectionLength;
            if (SelectionLength < 0)
            {
                caret += selectionLength;
                selectionLength = -selectionLength;
            }

            return Text.Substring(caret, selectionLength);
        }
    }

    /// <summary>
    /// <para>
    /// 文本框里包括 <see cref="Text"/> 和 <see cref="CompositionText"/> 的完整文本。
    /// </para>
    /// <para>
    /// The full text of this text box, including <see cref="CompositionText"/> and <see cref="Text"/>.
    /// </para>
    /// </summary>
    public string FullText => Text.Insert(Caret, CompositionText ?? "");

    /// <summary>
    /// <para>
    /// <see cref="Text"/> 的后台字段，
    /// 不推荐直接使用，请使用 <see cref="Text"/>。
    /// </para>
    /// <para>
    /// The backend field of <see cref="Text"/>,
    /// it's not safe use it directly, please use <see cref="Text"/> instead.
    /// </para>
    /// </summary>
    public string m_text = "";

    /// <summary>
    /// <para>
    /// 文本框里已经输入的文本。
    /// </para>
    /// <para>
    /// Text already inputted of this text box.
    /// </para>
    /// </summary>
    public string Text
    {
        get => m_text;
        set
        {
	        string text = value == null ? string.Empty : ((value.Length > MaximumLength) ? value.Substring(0, MaximumLength) : value);
	        if(text != m_text)
	        {
		        m_text = text;
		        Caret = Math.Clamp(Caret,0,m_text.Length);
		        TextChanged?.Invoke(this);
		        Scroll = Math.Clamp(Scroll,-Font.MeasureText(FullText,new Vector2(FontScale),FontSpacing).X,Font.MeasureText(FullText,new Vector2(FontScale),FontSpacing).X);
	        }
        }
    }

    /// <summary>
    /// <para>
    /// 输入法 “组合窗” 的文本， 可能为 null。
    /// </para>
    /// <para>
    /// 仅在 Windows 平台下可用（Android 平台始终为null）。
    /// </para>
    /// <para>
    /// IME composition text, may be null.
    ///</para>
    /// <para>
    /// Windows only (Always null on Android).
    /// </para>
    /// </summary>
    public string CompositionText { get; set; }

    /// <summary>
    /// <para>
    /// “组合窗” 中光标的位置，可被视为 <see cref="CompositionText"/> 的索引，
    /// 等效于 <see cref="Caret"/>，但是对应的字符串从 <see cref="Text"/> 变为了 <see cref="CompositionText"/>，
    /// 若需要使用相对于 <see cref="FullText"/> 的光标（字符索引），请将此值与 <see cref="Caret"/> 相加。
    /// </para>
    /// <para>
    /// 仅在 Windows 平台下可用（Android 平台始终为 0）。
    /// </para>
    /// <para>
    /// IME composition text caret position, can be considered as the index of <see cref="CompositionText"/>,
    /// Is the same as <see cref="Caret"/>, but replaces <see cref="Text"/> to <see cref="CompositionText"/>,
    /// To get the index to <see cref="FullText"/>, add this value and <see cref="Caret"/>.
    /// </para>
    /// <para>
    /// Windows only.
    /// </para>
    /// </summary>
    public int CompositionTextCaret { get; set; }

    /// <summary>
    /// <para>
    /// 光标的位置，可被视为 <see cref="Text"/> 的索引。
    /// 绘制时位置为索引对应的字符的左侧，可以与 <see cref="Text"/> 的长度相等（表示光标在文本末尾）。
    /// </para>
    /// <para>
    /// Text caret, can be considered as the index of <see cref="Text"/>.
    /// The caret is drawn at the left of the character that corresponds to the index.
    /// </para>
    /// </summary>
    public int Caret { get; set; }

    #endregion

    // 交互

    #region Interacting

    /// <summary>
    /// <para>
    /// 当前选中的文本长度，为 0 则表示不选择任何文本，可以小于 0.
    /// </para>
    /// <para>
    /// The length of the selected text, 0 means no text is selected, can be less than 0.
    /// </para>
    /// </summary>
    public int SelectionLength { get; set; }

    /// <summary>
    /// <para>
    /// 是否选中文本。
    /// </para>
    /// <para>
    /// Whether the text is selected.
    /// </para>
    /// </summary>
    public bool SelectionStarted { get; set; }

    /// <summary>
    /// <para>
    /// 是否正在滚动。（注意：仅指 Drag 引发的滚动）
    /// </para>
    /// <para>
    /// Whether the text is scrolling.(NOTICE: Only means drag scroll)
    /// </para>
    /// </summary>
    public bool ScrollStarted { get; set; }

    /// <summary>
    /// <para>
    /// 当前获得焦点的文本框。
    /// </para>
    /// <para>
    /// The focused text box.
    /// </para>
    /// </summary>
    public static TextBoxWidget FocusedTextBox { get; set; }

    /// <summary>
    /// <para>
    /// 最后一次获取焦点的时间（<see cref="Time.RealTime"/>），
    /// 用于绘制光标（请见<see cref="Draw"/>方法）。
    /// </para>
    /// <para>
    /// Last focus time(<see cref="Time.RealTime"/>),
    /// used to draw caret(see <see cref="Draw"/> method).
    /// </para>
    /// </summary>
    public double FocusStartTime { get; set; }

    /// <summary>
    /// <para>
    /// 当前文本框是否获得焦点。
    /// </para>
    /// <para>
    /// Whether this text box has focus.
    /// </para>
    /// </summary>
    public bool HasFocus
    {
        get => FocusedTextBox == this;
        set
        {
            if (value)
            {
                FocusedTextBox = this;
                SetCursorPosition(this);
                return;
            }

            FocusedTextBox = null;
        }
    }

    /// <summary>
    /// <para>
    /// 供 <see cref="TextBoxWidget"/> 内部使用。
    /// </para>
    /// <para>
    /// For internal use by <see cref="TextBoxWidget"/>.
    /// </para>
    /// </summary>
    /// <param name="str"></param>
    /// <param name="splitPosition"></param>
    /// <returns></returns>
    public static string[] SplitStringAt(string str, int splitPosition)
    {
        string left = str[..splitPosition];
        string right = str[splitPosition..];
        return [left, right];
    }

    /// <summary>
    /// <para>
    /// 插入字符。
    /// </para>
    /// <para>
    /// Enter character.
    /// </para>
    /// </summary>
    /// <param name="value">
    ///     <para>
    ///     插入的字符。
    ///     </para>
    ///     <para>
    ///     Character to enter.
    ///     </para>
    /// </param>
    /// <param name="position">
    ///     <para>
    ///     插入的位置，默认为 -1 （-1 表示插入到光标位置）。
    ///     </para>
    ///     <para>
    ///     Position to enter, default is -1 (-1 means insert at caret position)
    ///     </para>
    /// </param>
    /// <param name="moveCaret">
    ///     <para>
    ///     是否移动光标。
    ///     </para>
    ///     <para>
    ///     Whether to move caret.
    ///     </para>
    /// </param>
    public void EnterCharacter(char value, int position = -1, bool moveCaret = true)
    {
        if (SelectionLength != 0)
        {
            DeleteSelection(false);
        }

        FocusStartTime = Time.RealTime;
        if (OverwriteMode)
        {
            OverwriteCharacter();
            return;
        }

        InsertCharacter();
        return;

        void OverwriteCharacter()
        {
            if (position is -1)
            {
                position = Caret;
            }

            string str;
            if (value is '\t' && IndentAsSpace)
            {
                var distanceToNextIndent = IndentWidth - position % IndentWidth;
                str = new string(' ', distanceToNextIndent);
            }
            else
            {
                str = value.ToString();
            }

            if (str.Length + Text.Length > MaximumLength)
            {
                str = str[..(MaximumLength - Text.Length)];
            }

            Text = Text.Remove(position, str.Length);
            Text = Text.Insert(position, str);

            SetCursorPosition(this);
            TextChanged?.Invoke(this);
        }

        void InsertCharacter()
        {
            if (Text.Length >= MaximumLength)
            {
                return;
            }

            if (position is -1)
            {
                position = Caret;
            }

            string str;
            if (value is '\t' && IndentAsSpace)
            {
                var distanceToNextIndent = IndentWidth - position % IndentWidth;
                str = new string(' ', distanceToNextIndent);
            }
            else
            {
                str = value.ToString();
            }

            if (str.Length + Text.Length > MaximumLength)
            {
                str = str[..(MaximumLength - Text.Length)];
            }

            Text = Text.Insert(position, str);
            if (moveCaret)
            {
                Caret += str.Length;
            }

            SetCursorPosition(this);
            TextChanged?.Invoke(this);
        }
    }


    /// <summary>
    /// <para>
    /// 删除选中的字符。
    /// </para>
    /// <para>
    /// Delete selected text.
    /// </para>
    /// <param name="invokeTextChanged">
    ///     <para>
    ///     如果为 true，调用 <see cref="TextChanged"/> 事件。
    ///     </para>
    ///     <para>
    ///     If true, invoke <see cref="TextChanged"/> event.
    ///     </para>
    /// </param>
    /// </summary>
    public void DeleteSelection(bool invokeTextChanged = true)
    {
        int caret = Caret;
        int selectionLength = SelectionLength;
        if (SelectionLength < 0)
        {
            caret += selectionLength;
            selectionLength = -selectionLength;
        }

        Text = Text.Remove(caret, selectionLength);
        SelectionLength = 0;
        Caret = Math.Clamp(caret, 0, Text.Length);
        if (invokeTextChanged)
        {
            TextChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// <para>
    /// 在光标后插入字符串。
    /// </para>
    /// </summary>
    public void EnterText(string value)
    {
        EnterText(value, Caret);
    }

    /// <summary>
    /// <para>
    /// 在指定位置插入字符串。
    /// </para>
    /// <para>
    /// Insert string at specified position.
    /// </para>
    /// </summary>
    public void EnterText(string value, int index)
    {
        foreach (var character in value)
        {
            EnterCharacter(character, index++);
        }
    }

    /// <summary>
    /// <para>
    /// 字符分类表， 用于 <see cref="Delete"/> 方法 和 <see cref="BackSpace"/>，
    /// 每个元素都存储着某一类字符的所有字符。
    /// </para>
    /// <para>
    /// Characters table for <see cref="Delete"/> and <see cref="BackSpace"/>,
    /// Every element stores all character of a character kind.
    /// </para>
    /// </summary>
    public static string[] CharacterKindsMap =
    [
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
        "0123456789"
    ];

    /// <summary>
    /// <para>
    /// 向左删除字符。
    /// </para>
    /// <para>
    /// Delete the character on the left of the caret.
    /// </para>
    /// </summary>
    /// <param name="character">
    ///     <para>
    ///     删除的字符，可为 null （为 null 则匹配所有字符）。
    ///     </para>
    ///     <para>
    ///     Character to delete, can be null (if null then match all characters).
    ///     </para>
    /// </param>
    /// <param name="count">
    ///     <para>
    ///     删除字符的数量 为 -1 则表示删除所有匹配字符
    ///     </para>
    ///     <para>
    ///     Number of characters to delete, if -1 then delete all matching characters.
    ///     </para>
    /// </param>
    /// <param name="moveCaret">
    ///     <para>
    ///     是否移动光标。
    ///     </para>
    ///     <para>
    ///     Whether to move the caret.
    ///     </para>
    /// </param>
    public void BackSpace(char? character = null, int count = 1, bool moveCaret = true)
    {
        if (SelectionLength != 0)
        {
            DeleteSelection();
            return;
        }

        var map = character.HasValue
                      ? CharacterKindsMap.FirstOrDefault(x => x.Contains(character.Value), character.ToString())
                      : null;

        int i;
        for (i = Caret; i > 0 && count != 0; i--)
        {
            if (map != null && !map.Contains(Text[i - 1]))
            {
                break;
            }

            Text = Text.Remove(i - 1, 1);
            count--;
        }

        if (moveCaret)
        {
            Caret = i;
        }

        FocusStartTime = Time.RealTime;
        SetCursorPosition(this);
        TextChanged?.Invoke(this);
    }

    /// <summary>
    /// <para>
    ///     向右删除字符。
    /// </para>
    /// <para>
    ///     Delete character on the right of the caret.
    /// </para>
    /// </summary>
    /// <param name="character">
    ///     <para>
    ///     删除的字符，可为 null （为 null 则匹配所有字符）。
    ///     </para>
    ///     <para>
    ///     Character to delete, can be null (if null then match all characters).
    ///     </para>
    /// </param>
    /// <param name="count">
    ///     <para>
    ///     删除字符的数量 为 -1 则表示删除所有匹配字符.
    ///     </para>
    ///     <para>
    ///     Number of characters to delete, if -1 then delete all matching characters.
    ///     </para>
    /// </param>
    public void Delete(char? character = null, int count = 1)
    {
        if (SelectionLength != 0)
        {
            DeleteSelection();
            return;
        }

        var map = character.HasValue
                      ? CharacterKindsMap.FirstOrDefault(x => x.Contains(character.Value), character.ToString())
                      : null;

        for (; count != 0 && Caret < Text.Length; count--)
        {
            if (map != null && !map.Contains(Text[Caret]))
            {
                break;
            }

            Text = Text.Remove(Caret, 1);
        }

        FocusStartTime = Time.RealTime;
        SetCursorPosition(this);
        TextChanged?.Invoke(this);
    }

    static TextBoxWidget()
    {
#if WINDOWS
        InputMethod.TextCompositionCallback += (text, pos) =>
        {
            if (FocusedTextBox is null)
            {
                return;
            }

            if (FocusedTextBox.SelectionLength != 0)
            {
                FocusedTextBox.DeleteSelection();
            }

            FocusedTextBox.CompositionText = text;
            FocusedTextBox.CompositionTextCaret = pos;
            FocusedTextBox.FocusStartTime = Time.RealTime;
        };

        InputMethod.TextInputCallback += character =>
        {
            if (FocusedTextBox is null)
            {
                return;
            }

            FocusedTextBox.CompositionText = null;
            switch (character)
            {
                case '\b':
                {
                    FocusedTextBox.BackSpace();
                    break;
                }
                case (char)127:
                {
                    if (FocusedTextBox.Caret is 0)
                    {
                        break;
                    }

                    FocusedTextBox.BackSpace(character: FocusedTextBox.Text[FocusedTextBox.Caret - 1], count: -1);
                    break;
                }
                case (char)3:
                {
                    // Ctrl + C
                    if (FocusedTextBox == null)
                    {
                        break;
                    }

                    if (Keyboard.IsKeyDown(Key.Control) && Keyboard.IsKeyDownOnce(Key.C))
                    {
                        ClipboardManager.ClipboardString = FocusedTextBox.SelectionString;
                    }

                    break;
                }
                case (char)1:
                {
                    // Ctrl + V
                    if (FocusedTextBox == null)
                    {
                        break;
                    }

                    FocusedTextBox.Caret = 0;
                    FocusedTextBox.SelectionLength = FocusedTextBox.Text.Length;
                    break;
                }
                case (char)22:
                {
                    // Ctrl + V
                    if (FocusedTextBox == null)
                    {
                        break;
                    }

                    var text = ClipboardManager.ClipboardString;
                    if (text != null)
                    {
                        FocusedTextBox.EnterText(text);
                    }
                    break;
                }
                case (char)24:
                {
                    // Ctrl + X
                    if (FocusedTextBox == null)
                    {
                        break;
                    }

                    ClipboardManager.ClipboardString = FocusedTextBox.SelectionString;
                    FocusedTextBox.DeleteSelection();
                    break;
                }
                case (char)27:
                {
                    // Escape
                    break;
                    // TextBoxWidget 的 Esc 处理不依赖此输入，所以直接跳过。
                }
                default:
                {
                    FocusedTextBox.EnterCharacter(character);
                    break;
                }
            }
        };
/*#elif ANDROID
        Window.Activity.OnDispatchKeyEvent += keyEvent =>
        {
            if (FocusedTextBox == null)
            {
                return false;
            }

            if (keyEvent.Characters == "\t")
            {
                
                FocusedTextBox.Enter?.Invoke(FocusedTextBox);
                return true;
            }
            FocusedTextBox.EnterText(keyEvent.Characters ?? "");
            return keyEvent.Characters != null && keyEvent.Characters.Length > 0;
        };*/
#endif
    }

    /// <summary>
    /// <para>
    /// Drag 操作开始时 <see cref="Time.RealTime"/> 的值。
    /// </para>
    /// <para>
    /// Value of <see cref="Time.RealTime"/> when drag started.
    /// </para>
    /// </summary>
    public double DragStartTime { get; set; } = -1;

    /// <summary>
    /// <para>
    /// 上一次 <see cref="Update"/> 方法被执行时 <see cref="WidgetInput.Drag"/> 的值。
    /// </para>
    /// </summary>
    public Vector2? LastDragPosition { get; set; }

    /// <summary>
    /// <para>
    /// 拖拽开始时是否位于文本框内。
    /// </para>
    /// <para>
    /// Whether the drag started inside the text box.
    /// </para>
    /// </summary>
    public bool DragStartedInsideTextBox { get; set; }
    
    public override void UpdateCeases()
    {
        if (HasFocus)
        {
            CloseInputMethod();
        }
    }
        
    public override void Update()
    {
        if (Input.Click.HasValue)
        {
            // 处理点击，使文本框在被点击时获取焦点。
            if (HitTestGlobal(Input.Click.Value.Start) == this && HitTestGlobal(Input.Click.Value.End) == this)
            {
                FocusStartTime = Time.RealTime;
#if WINDOWS
                ShowInputMethod();
#elif ANDROID
                Keyboard.ShowKeyboard(
                    Title ?? "",
                    Description ?? "",
                    Text,
                    passwordMode: PasswordMode,
                    text =>
                    {
                        Text = text;
                        HasFocus = false;
                    },
                    () => HasFocus = false);
#endif
                HasFocus = true;
                Caret = CalculateClickedCharacterIndex(Font, PasswordMode ? new string('*', Text.Length) : Text,
                    InteractionToWidget(Input.Click.Value.Start + new Vector2(Scroll * GlobalTransform.M11, 0)),
                    FontScale, FontSpacing.X);
                OnFocus?.Invoke(this);
            }
            else if (FocusedTextBox == this)
            {
                // 如果点击不在文本框内，则失去焦点。
                CloseInputMethod();
                HasFocus = false;
                FocusLost?.Invoke(this);
            }

            SelectionLength = 0;
        }

#if !ANDROID
		if(Input.Scroll.HasValue &&
					Input.MousePosition.HasValue &&
					HitTestGlobal(Input.MousePosition.Value) == this)
		{
			var scroll = Input.Scroll.Value.X * Input.Scroll.Value.Z / 92;
			Scroll += scroll;
			Scroll = Math.Clamp(Scroll,-Font.MeasureText(FullText,new Vector2(FontScale),FontSpacing).X,
				Font.MeasureText(FullText,new Vector2(FontScale),FontSpacing).X);
		}
        if (Input.Drag.HasValue)
        {
            if (DragStartTime < 0)
            {
                // 拖拽刚开始时：
                DragStartTime = Time.RealTime;
	            SelectionLength = 0;
                if (HitTestGlobal(Input.Drag.Value) == this)
                {
                    HasFocus = true;
                    SelectionStarted = true;
                    ShowInputMethod();
                    Caret = CalculateClickedCharacterIndex(Font, PasswordMode ? new string('*', Text.Length) : Text,
                        InteractionToWidget(Input.Drag.Value + new Vector2(Scroll * GlobalTransform.M11, 0)),
                        FontScale, FontSpacing.X);
                    DragStartedInsideTextBox = true;
                }
                else
                {
                    SelectionStarted = false;
                    DragStartedInsideTextBox = false;
                    HasFocus = false;
                }
            }
            else if (Time.RealTime - DragStartTime > 0 && DragStartedInsideTextBox)
            {
                // 拖拽正在进行时：
                var caret2 = CalculateClickedCharacterIndex(Font, PasswordMode ? new string('*', Text.Length) : Text,
                    InteractionToWidget(Input.Drag.Value + new Vector2(Scroll * GlobalTransform.M11, 0)),
                    FontScale, FontSpacing.X);
                if (SelectionStarted)
                {
                    SelectionLength = caret2 - Caret;
                }

                if (Math.Abs(caret2 - Caret) > 1)
                {
                    ScrollStarted = true;
                }
            }

            LastDragPosition = Input.Drag.Value;
        }
        else if (DragStartTime >= 0 && !Input.Drag.HasValue)
        {
            DragStartTime = -1;
            SelectionStarted = false;
            ScrollStarted = false;
        }

        // 处理光标移动。
        if (HasFocus && string.IsNullOrEmpty(CompositionText))
        {
            if (Keyboard.IsKeyDownRepeat(Key.LeftArrow))
            {
                Caret = Math.Max(0, Caret - 1);
                SelectionLength = 0;
                SelectionStarted = false;
                FocusStartTime = Time.RealTime;
            }

            if (Keyboard.IsKeyDownRepeat(Key.RightArrow))
            {
                Caret = Math.Min(Text.Length, Caret + 1);
                SelectionLength = 0;
                SelectionStarted = false;
                FocusStartTime = Time.RealTime;
            }

            if (Keyboard.IsKeyDownOnce(Key.Home) || Keyboard.IsKeyDownOnce(Key.UpArrow))
            {
                Caret = 0;
                SelectionLength = 0;
                SelectionStarted = false;
                FocusStartTime = Time.RealTime;
            }

            if (Keyboard.IsKeyDownOnce(Key.End) || Keyboard.IsKeyDownOnce(Key.DownArrow))
            {
                Caret = Text.Length;
                SelectionLength = 0;
                SelectionStarted = false;
                FocusStartTime = Time.RealTime;
            }
        }

        if (HasFocus && Keyboard.IsKeyDownOnce(Key.Escape))
        {
            Escape?.Invoke(this);
        }

//#if !ANDROID
        // 处理 Delete 键。
        if (HasFocus && Caret != Text.Length && Keyboard.IsKeyDownRepeat(Key.Delete))
        {
            if (Keyboard.IsKeyDown(Key.Control))
            {
                Delete(count: -1, character: Text[Caret]);
            }
            else
            {
                Delete();
            }
        }

        if (HasFocus && Keyboard.IsKeyDownOnce(Key.Enter))
        {
            Enter?.Invoke(this);
            HasFocus = false;
            CloseInputMethod();
        }
//#endif

        // 处理键盘输入。
        // 如果输入法已开启，就跳过。
        if (!InputMethodEnabled && HasFocus)
        {
            // 处理 BackSpace 键。

            if(Caret != 0 && (Keyboard.IsKeyDownRepeat(Key.Delete)||Keyboard.IsKeyDownRepeat(Key.BackSpace)))
            {
                if (Keyboard.IsKeyDown(Key.Control))
                {
                    BackSpace(count: -1, character: Text[Caret - 1]);
                }
                else
                {
                    BackSpace();
                }

                FocusStartTime = Time.RealTime;
            }

            // 如果 Tab 键切换文本框的功能未启用，则将 Tab 键视为制表符输入。
            if (!SwitchTextBoxWhenTabbed && Keyboard.IsKeyDownRepeat(Key.Tab))
            {
                EnterCharacter('\t');
            }

            // 处理文本输入。
            var lastChar = Keyboard.LastChar;
            if (lastChar != null && lastChar != '\n')
            {
                EnterCharacter(lastChar.Value);
            }
        }

        // 处理 Tab 键切换文本框。
        if (HasFocus && SwitchTextBoxWhenTabbed && Keyboard.IsKeyDownRepeat(Key.Tab))
        {
            if (RootWidget is not ContainerWidget rootWidget) return;
            var textBoxes = FindTextBoxWidgets(rootWidget);
            int thisIndex = textBoxes.IndexOf(this);
            FocusedTextBox = textBoxes[(thisIndex + 1) % textBoxes.Count];
        }

        if (HasFocus && SelectionLength != 0 && !InputMethodEnabled)
        {
            if (Keyboard.IsKeyDown(Key.Control) && Keyboard.IsKeyDownOnce(Key.C))
            {
                ClipboardManager.ClipboardString = SelectionString;
            }

            if (Keyboard.IsKeyDown(Key.Control) && Keyboard.IsKeyDownOnce(Key.X))
            {
                ClipboardManager.ClipboardString = SelectionString;
                DeleteSelection();
            }

            if (Keyboard.IsKeyDown(Key.Control) && Keyboard.IsKeyDownOnce(Key.V))
            {
                var text = ClipboardManager.ClipboardString;
                if (text != null)
                {
                    EnterText(ClipboardManager.ClipboardString);
                }
            }
        }

        return;
#endif

        static List<TextBoxWidget> FindTextBoxWidgets(ContainerWidget widget)
        {
            List<TextBoxWidget> textBoxes = new(capacity: 16);
            foreach (var child in widget.Children)
            {
                if (child is TextBoxWidget textBoxWidget)
                {
                    textBoxes.Add(textBoxWidget);
                }

                if (child is not ContainerWidget containerWidget)
                {
                    continue;
                }

                var result = FindTextBoxWidgets(containerWidget);
                if (result.Count is not 0)
                {
                    textBoxes.AddRange(result);
                }
            }

            return textBoxes;
        }

        static int CalculateClickedCharacterIndex(BitmapFont font, string text, Vector2 clickPosition, float fontScale, float fontSpacing)
        {
	        return font.FitText(clickPosition.X, text, fontScale, fontSpacing);
        }

        Vector2 InteractionToWidget(Vector2 position)
        {
            return ScreenToWidget(position);
        }
    }

    /// <summary>
    /// <para>
    /// 显示 Windows 系统输入法
    /// </para>
    /// </summary>
    public static void ShowInputMethod()
    {
#if WINDOWS
        InputMethodEnabled = true;
#endif
    }

    public static void CloseInputMethod()
    {
#if WINDOWS
        InputMethodEnabled = false;
#endif
    }

    #endregion

    // 选项

    #region Options

    /// <summary>
    /// <para>
    /// 是否显示候选窗。
    /// </para>
    /// <para>
    /// Whether to show the candidates window.
    /// </para>
    /// </summary>
    public static bool ShowCandidatesWindow { get; set; } = true;

    /// <summary>
    /// <para>
    /// 是否把输入的制表符替换为空格
    /// （不会影响已经输入过的制表符）。
    /// </para>
    /// <para>
    /// 注意：此属性和 <see cref="SwitchTextBoxWhenTabbed"/> 冲突，二者不能同时为 true，否则只有一个生效。
    /// </para>
    /// <para>
    /// Whether replace indent with spaces when typing
    /// (doesn't change the already inputted).
    /// </para>
    /// <para>
    /// NOTICE: This property conflicts with <see cref="SwitchTextBoxWhenTabbed"/>,
    /// they cannot be set to true at the same time, otherwise only one will take effect.
    /// </para>
    /// </summary>
    public bool IndentAsSpace { get; set; } = true;

    /// <summary>
    /// <para>
    /// 当 <see cref="IndentAsSpace"/> 为 true 时，输入的制表符会被替换为空格，空格数量由 <see cref="IndentWidth"/> 决定。
    /// </para>
    /// <para>
    /// 在绘制时，制表符会被视为这个数量的空格。
    /// </para>
    /// <para>
    /// When <see cref="IndentAsSpace"/> is true, inputted indents will be replaced with this number of spaces.
    /// </para>
    /// <para>
    /// When drawing, indents will be considered as this number of spaces.
    /// </para>
    /// </summary>
    public int IndentWidth { get; set; } = 4;

    /// <summary>
    /// <para>
    /// <see cref="MaximumLength"/> 的后台字段，
    /// 不推荐直接使用此值，请使用 <see cref="MaximumLength"/>。
    /// </para>
    /// <para>
    /// The backend field of <see cref="MaximumLength"/>,
    /// This is not safe to use this field directly, use <see cref="MaximumLength"/> instead
    /// </para>
    /// </summary>
    public int m_maximumLength = 512;

    /// <summary>
    /// <para>
    /// 文本长度限制，设置此属性时会截断超过长度的文本，不可小于 0，若需要设置为无限，请使用 <see cref="int.MaxValue"/>。
    /// </para>
    /// <para>
    /// Maximum length of text, the text will be cut when it is longer than this value, the value cannot be less than 0,
    /// to set it to infinite, please use <see cref="int.MaxValue"/>.
    /// </para>
    /// </summary>
    public int MaximumLength
    {
        get => m_maximumLength;
        set
        {
            if (value < 0)
            {
                throw new InvalidOperationException($"{nameof(MaximumLength)} 必须大于或等于 0.");
            }

            if (m_maximumLength > value)
            {
                if (Text.Length > value)
                {
                    Text = Text[..value];
                }

                Caret = Math.Clamp(Caret, 0, value);
            }

            m_maximumLength = value;
        }
    }

    /// <summary>
    /// <para>
    /// 密码模式，开启后所有文本都会被显示为 *（组合窗除外）。
    /// </para>
    /// <para>
    /// Password mode, all text but composition text ill be drawn as "*".
    /// </para>
    /// </summary>
    public bool PasswordMode { get; set; }

    /// <summary>
    /// <para>
    /// 为 true 时，文本框中输入的字符将覆盖已有文本。
    /// </para>
    /// <para>
    /// When true, characters inputted will overwrite existing text.
    /// </para>
    /// </summary>
    public bool OverwriteMode { get; set; }

    /// <summary>
    /// <para>
    /// 当用户按下 Tab 键时，是否切换到下一个文本框
    /// </para>
    /// <para>
    /// 注意：此属性和 <see cref="SwitchTextBoxWhenTabbed"/> 冲突，二者不能同时为 true，否则只有一个生效。
    /// </para>
    /// <para>
    /// If true, when the user presses the tab key, the focus will be switched to the next text box.
    /// </para>
    /// <para>
    /// NOTICE: This property conflicts with <see cref="SwitchTextBoxWhenTabbed"/>,
    /// they cannot be set to true at the same time, otherwise only one will take effect.
    /// </para>
    /// </summary>
    public bool SwitchTextBoxWhenTabbed { get; set; } = true;

    #endregion

    // 外观

    #region Appearance

    public Color Color { get; set; } = Color.White;

    public float Scroll { get; set; }

    /// <summary>
    /// <para>
    /// 文本框轮廓颜色。
    /// </para>
    /// <para>
    /// Text box outline color.
    /// </para>
    /// </summary>
    public Color OutlineColor { get; set; } = Color.White;

    /// <summary>
    /// <para>
    /// 候选窗选中项颜色。
    /// </para>
    /// <para>
    /// Candidate window selected item color.
    /// </para>
    /// </summary>
    public Color CandidateSelectionColor { get; set; } = Color.Red;

    /// <summary>
    /// <para>
    /// 候选窗文本颜色。
    /// </para>
    /// <para>
    /// Candidate window text color.
    /// </para>
    /// </summary>
    public Color CandidateTextColor { get; set; } = Color.White;
    
    /// <summary>
    /// <para>
    /// 如果为 true 则每帧都会由 <see cref="MeasureOverride"/> 自动确定 <see cref="Size"/> 的值。
    /// </para>
    /// <para>
    /// If true, the size will be set by <see cref="MeasureOverride"/> every frame.
    /// </para>
    /// </summary>
    public bool AutoSize { get; set; }= true;

    /// <summary>
    /// <para>
    /// <see cref="Size"/> 的后台字段。
    /// </para>
    /// <para>
    /// The backend field of <see cref="Size"/>.
    /// </para>
    /// </summary>
    public Vector2 m_sizeValue;
    
    /// <summary>
    /// <para>
    /// 输入框大小（不一定是输入框的真实大小，真实大小请见 <see cref="Widget.ActualSize"/>）。
    /// </para>
    /// <para>
    /// Text box size (may not be the actual size of the text box, the actual size is <see cref="Widget.ActualSize"/>)
    /// </para>
    /// </summary>
    public Vector2 Size
    {
        get => m_sizeValue;
        set
        {
            m_sizeValue = value;
            AutoSize = false;
        }
    }
    
    /// <summary>
    /// <para>
    /// 字体间距。
    /// </para>
    /// <para>
    /// Font spacing.
    /// </para>
    /// </summary>
    public Vector2 FontSpacing { get; set; }

    /// <summary>
    /// <para>
    /// 字体缩放。
    /// </para>
    /// <para>
    /// Font scale.
    /// </para>
    /// </summary>
    public float FontScale { get; set; } = 1;

    /// <summary>
    /// <para>
    /// 如果为 true，则字体使用线性过滤。
    /// </para>
    /// <para>
    /// If true, font will use linear texture filtering.
    /// </para>
    /// </summary>
    public bool TextureLinearFilter { get; set; } = true;
    
    /// <summary>
    /// <para>
    /// 候选窗位置偏移量，
    /// 原点为光标延长线与文本框下边缘的交点。
    /// </para>
    /// <para>
    /// Position offset of the candidate list,
    /// zero means the intersection point of the bottom outline and the extended line of caret.
    /// </para>
    /// </summary>
    public Vector2 CandidateListOffset { get; set; } = new(0, 16);

    /// <summary>
    /// <para>
    /// 候选词之间的间距。
    /// </para>
    /// <para>
    /// Candidates spacing.
    /// </para>
    /// </summary>
    public float CandidatesSpacing { get; set; } = 4;

    /// <summary>
    /// <para>
    /// 输入框字体。
    /// </para>
    /// <para>
    /// Text box font.
    /// </para>
    /// </summary>
    public BitmapFont Font { get; set; } = ContentManager.Get<BitmapFont>("Fonts/Pericles");

    /// <summary>
    /// <para>
    /// 候选窗显示的长度。
    /// </para>
    /// <para>
    /// The length of the candidates window.
    /// </para>
    /// </summary>
    public float CandidateWindowLength { get; set; } = 80;

    /// <summary>
    /// <para>
    /// 候选窗背景颜色。
    /// </para>
    /// <para>
    /// Candidate window background color.
    /// </para>
    /// </summary>
    public Color CandidateWindowColor { get; set; } = Color.DarkGray;

    #endregion

    // 输入法 Api

    #region Input Method Api

    private static void SetCursorPosition(TextBoxWidget widget)
    {
#if WINDOWS
        var windowPosition = widget.WidgetToScreen(new Vector2(widget.FullTextCaretPosition, 0));
        windowPosition.X -= widget.Scroll * widget.GlobalTransform.M11;
        InputMethod.SetTextInputRect((int)windowPosition.X, (int)(windowPosition.Y + widget.Font.LineHeight * widget.GlobalTransform.M11), 0, 0);
#endif
    }

    /// <summary>
    /// <para>
    /// 是否启用输入法。
    /// </para>
    /// <para>
    /// Whether to enable input method.
    /// </para>
    /// </summary>
    public static bool InputMethodEnabled
    {
#if WINDOWS
        get => InputMethod.Enabled;
        set => InputMethod.Enabled = value;
#else
        get => false;
        set { }
#endif
    }

    /// <summary>
    /// <para>
    /// 所有候选词。
    /// 请结合 <see cref="CandidatesSelection"/> 和 <see cref="CandidatesPageSize"/> 以获取当前候选词页的候选词。
    /// </para>
    /// <para>
    /// All candidates.
    /// Please get current candidates page's candidates with <see cref="CandidatesSelection"/> and <see cref="CandidatesPageSize"/>.
    /// </para>
    /// </summary>
    public static string[] CandidatesList =>
#if WINDOWS
        InputMethod.CandidateList.Select(x => x.ToString()).ToArray();
#else
        Array.Empty<string>();
#endif

    /// <summary>
    /// <para>
    /// 候选窗中当前选中的词。
    /// </para>
    /// <para>
    /// Current selected candidate.
    /// </para>
    /// </summary>
    public static int CandidatesSelection =>
#if WINDOWS
        InputMethod.CandidateSelection;
#else
        -1;
#endif

    /// <summary>
    /// <para>
    /// 候选窗中当前候选词页大小。
    /// </para>
    /// <para>
    /// Size of current selected candidates page.
    /// </para>
    /// </summary>
    public static int CandidatesPageSize =>
#if WINDOWS
        InputMethod.CandidatePageSize;
#else
        0;
#endif

    #endregion

    // 事件

    #region Events

    /// <summary>
    /// <para>
    /// 输入框失去焦点时触发。
    /// </para>
    /// <para>
    /// Events will be call when text box loses focus.
    /// </para>
    /// </summary>
    public event Action<TextBoxWidget> FocusLost;

    /// <summary>
    /// <para>
    /// 输入框获得焦点时触发。
    /// </para>
    /// <para>
    /// Events will be call when text box gets focus.
    /// </para>
    /// </summary>
    public event Action<TextBoxWidget> OnFocus;

    /// <summary>
    /// <para>
    /// 按下回车时触发。
    /// </para>
    /// <para>
    /// Events will be call when pressing enter.
    /// </para>
    /// </summary>
    public event Action<TextBoxWidget> Enter;

    /// <summary>
    /// <para>
    /// 按下 Escape 时触发。
    /// </para>
    /// <para>
    /// Events will be call when pressing escape.
    /// </para>
    /// </summary>
    public event Action<TextBoxWidget> Escape;

    /// <summary>
    /// <para>
    /// 文本改变时触发。
    /// </para>
    /// <para>
    /// Events will be call when text changed.
    /// </para>
    /// </summary>
    public event Action<TextBoxWidget> TextChanged;

    #endregion

    // 绘制

    #region Render

    public override void Overdraw(DrawContext dc)
    {
        if (CandidatesPageSize is 0 || !ShowCandidatesWindow || !HasFocus)
        {
            return;
        }

        var backgroundFlatBatch = dc.PrimitivesRenderer2D.FlatBatch(layer: 0);
        var outlineFlatBatch = dc.PrimitivesRenderer2D.FlatBatch(layer: 1);
        var foregroundFlatBatch = dc.PrimitivesRenderer2D.FlatBatch(layer: 2);

        var fontBatch = dc.PrimitivesRenderer2D.FontBatch(Font, layer: 3,
            samplerState: TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp);

        var candidateWindowCorner1 = new Vector2(FullTextCaretPosition, ActualSize.Y);
        candidateWindowCorner1 += CandidateListOffset;

        // 绘制背景。
        backgroundFlatBatch.QueueQuad(Vector2.Zero,
            new Vector2(CandidateWindowLength,
                CandidatesPageSize * Font.GlyphHeight + CandidatesPageSize * CandidatesSpacing), 0,
            CandidateWindowColor);
        outlineFlatBatch.QueueRectangle(Vector2.Zero,
            new Vector2(CandidateWindowLength,
                CandidatesPageSize * Font.GlyphHeight + CandidatesPageSize * CandidatesSpacing),
            0, OutlineColor);

        // 绘制候选词文字。
        for (var i = CandidatesSelection / CandidatesPageSize;
             i < CandidatesSelection / CandidatesPageSize + CandidatesPageSize;
             i++)
        {
            // 获取候选词文字，并在前面加上序号。
            string candidate = i + 1 + " " + CandidatesList[i];

            // 遍历计算当前文本长度
            float width = 0;

            int characterIndex = 0;
            while (characterIndex < candidate.Length)
            {
                BitmapFont.Glyph glyph;
                try
                {
                    glyph = Font.GetGlyph(candidate[characterIndex]);
                }
                catch
                {
                    glyph = Font.FallbackGlyph;
                }

                width += glyph.Width;
                if (width >= CandidateWindowLength - 8 - 2 * Font.GetGlyph('.').Width)
                {
                    break;
                }

                characterIndex++;
            }

            // 如果文本长度超出窗口长度，则截断并替换位 “.."。
            if (width >= CandidateWindowLength - 8 - 2 * Font.GetGlyph('.').Width)
            {
                candidate = candidate[..characterIndex];
                candidate += "..";
            }

            // 绘制文本。
            fontBatch.QueueText(candidate,
                new Vector2(2, i * Font.GlyphHeight + i * CandidatesSpacing), 0,
                CandidatesSelection == i ? CandidateSelectionColor : CandidateTextColor,
                TextAnchor.Left | TextAnchor.Top,
                new Vector2(FontScale), FontSpacing);
        }

        foregroundFlatBatch.TransformTriangles(Matrix.CreateTranslation(new Vector3(candidateWindowCorner1, 0)));
        foregroundFlatBatch.TransformTriangles(GlobalTransform);
        backgroundFlatBatch.TransformTriangles(Matrix.CreateTranslation(new Vector3(candidateWindowCorner1, 0)));
        backgroundFlatBatch.TransformTriangles(GlobalTransform);
        outlineFlatBatch.TransformLines(Matrix.CreateTranslation(new Vector3(candidateWindowCorner1, 0)));
        outlineFlatBatch.TransformLines(GlobalTransform);
        fontBatch.TransformTriangles(Matrix.CreateTranslation(new Vector3(candidateWindowCorner1, 0)));
        fontBatch.TransformTriangles(GlobalTransform);

        ClampToBounds = true;
    }

    /// <summary>
    /// <para>
    /// 光标相对于 Widget 的显示位置。
    /// </para>
    /// <para>
    /// Position of text caret relative to widget.
    /// </para>
    /// </summary>
    public float TextCaretPosition
    {
        get
        {
            var caretPosition =
                Font.MeasureText(Text, 0, Caret, new Vector2(FontScale),
                    FontSpacing) *
                Vector2.UnitX;
            return caretPosition.X;
        }
    }

    /// <summary>
    /// <para>
    /// 文本光标（包括 <see cref="CompositionText"/>）相对于 Widget 的显示位置。
    /// </para>
    /// <para>
    /// Position of text caret (including <see cref="CompositionText"/>) relative to widget.
    /// </para>
    /// </summary>
    public float FullTextCaretPosition
    {
        get
        {
            var caretPosition =
                Font.MeasureText(FullText, 0, Caret + CompositionTextCaret, new Vector2(FontScale),
                    FontSpacing) *
                Vector2.UnitX;
            return caretPosition.X;
        }
    }

    public override void Draw(DrawContext dc)
    {
        try
        {
            var textToDraw = Text.Replace("\t", new string(' ', IndentWidth));

            if (PasswordMode)
            {
                textToDraw = new string('*', textToDraw.Length);
            }

            var flatBatch = dc.PrimitivesRenderer2D.FlatBatch(blendState: BlendState.NonPremultiplied);
            var outlineFlatBatch = dc.PrimitivesRenderer2D.FlatBatch(layer: 1);

            var fontBatch = dc.PrimitivesRenderer2D.FontBatch(Font);
            var underlineFlatBatch = dc.PrimitivesRenderer2D.FlatBatch(layer: 1);
            if (SelectionLength == 0 && FocusedTextBox == this &&
                ((Time.RealTime - FocusStartTime - 0.4) % 1.0 <= 0.3f ||
                 (Time.RealTime - FocusStartTime - 0.4) % 1.0 >= 0.8f))
            {
                DrawCaret(flatBatch, 1, Font.GlyphHeight * FontScale * Font.Scale, (FullTextCaretPosition, ActualSize.Y / 2),
                    Scroll);
            }

            if (SelectionLength != 0 && CompositionText == null)
            {
                var selectionEndPosition = Font.MeasureText(FullText, 0, Caret + SelectionLength,
                    new Vector2(FontScale),
                    FontSpacing);
                var topOfCaret = (ActualSize.Y - Font.GlyphHeight * FontScale * Font.Scale) / 2;
                flatBatch.QueueQuad((0 - Scroll + TextCaretPosition, topOfCaret),
                    (0 - Scroll, topOfCaret) + selectionEndPosition, 0, (64, 64, 255, 128));
            }

            Vector2 currentDrawPosition = new Vector2(0, ActualSize.Y / 2);

            List<TextDrawItem> drawItems = new(capacity: 3);

            var split = SplitStringAt(textToDraw, Caret);
            drawItems.Add(new NormalDrawItem(textToDraw, 0, split[0].Length, fontBatch, FontScale, FontSpacing, Color));
            drawItems.Add(
                new CompositionTextDrawItem(CompositionText ?? "", fontBatch, underlineFlatBatch, FontScale,
                    FontSpacing, Color));
            if (split.Length > 1)
            {
                drawItems.Add(new NormalDrawItem(textToDraw, split[0].Length, split[1].Length, fontBatch, FontScale, FontSpacing, Color));
            }

            foreach (var drawItem in drawItems)
            {
                drawItem.Draw(ref currentDrawPosition, Scroll);
            }

            fontBatch.TransformTriangles(GlobalTransform);
            flatBatch.TransformTriangles(GlobalTransform);
            flatBatch.TransformLines(GlobalTransform);
            outlineFlatBatch.TransformLines(GlobalTransform);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return;
        }

        return;

        //static void DrawBackground(FlatBatch2D flatBatch, FlatBatch2D outlineFlatBatch, Vector2 size,
        //                           Color outlineColor, Color color)
        //{
        //    outlineFlatBatch.QueueRectangle(Vector2.Zero, size, 0, outlineColor);
        //    flatBatch.QueueQuad(Vector2.Zero, size, 0, color);
        //}

        static void DrawCaret(FlatBatch2D flatBatch, float width,
                              float height, Vector2 position, float scroll)
        {
            flatBatch.QueueQuad(
                position + (-scroll, -height / 2),
                position + (-scroll + width, height / 2), 0, Color.White);
        }
    }

    /// <inheritdoc/>
    public override void MeasureOverride(Vector2 parentAvailableSize)
    {
        DesiredSize = Size;
        IsDrawRequired = true;
        IsOverdrawRequired = true;
        ClampToBounds = true;

        if (!AutoSize)
        {
            return;
        }
        
        if (Text.Length == 0)
        { 
            DesiredSize = Font.MeasureText(" ", new Vector2(FontScale), FontSpacing);
        }
        else
        {
            DesiredSize = Font.MeasureText(Text, new Vector2(FontScale), FontSpacing);
        }

        base.MeasureOverride(parentAvailableSize);
    }

    public abstract class TextDrawItem
    {
        public abstract void Draw(ref Vector2 position, float scroll);
    }

    public class NormalDrawItem(
	    string fullText,
	    int start,
	    int length,
	    FontBatch2D fontBatch,
	    float fontScale,
	    Vector2 fontSpacing,
	    Color color)
        : TextDrawItem
    {
        public override void Draw(ref Vector2 position, float scroll)
        {
	        if(length == 0)
	        {
		        return;
	        }
	        
            var font = fontBatch.Font;

            var size = font.MeasureText(fullText, start, length, new Vector2(fontScale),
                Vector2.Zero);
            fontBatch.QueueText(fullText.Substring(start, length), (position.X - scroll, position.Y), 0, color, TextAnchor.VerticalCenter,
                new Vector2(fontScale),
                fontSpacing);
            position.X += size.X;
        }
    }

    public class CompositionTextDrawItem(
        string compositionText,
        FontBatch2D fontBatch,
        FlatBatch2D underlineFlatBatch,
        float fontScale,
        Vector2 fontSpacing,
        Color color)
        : TextDrawItem
    {
        public override void Draw(ref Vector2 position, float scroll)
        {
            var font = fontBatch.Font;
            var size = font.MeasureText(compositionText, 0, compositionText.Length,
                new Vector2(fontScale),
                Vector2.Zero);
            
            fontBatch.QueueText(compositionText, (position.X- scroll, position.Y), 0, color, TextAnchor.VerticalCenter,
                new Vector2(fontScale), fontSpacing);
            underlineFlatBatch.QueueLine((position.X - scroll, position.Y) + size / 2 * Vector2.UnitY,
                (position.X - scroll, position.Y) + size / 2 * Vector2.UnitY + new Vector2(size.X, 0), 0, Color.White);

            position.X += size.X;
        }
    }

    #endregion
}