using Engine;
using Engine.Graphics;
using Engine.Media;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Game
{
	public class FontTextWidget : Widget
	{
		public string m_text;

		public TextOrientation m_textOrientation;

		public BitmapFont m_font;

		public Vector2 m_fontSpacing;

		public float m_fontScale;

		public int m_maxLines = int.MaxValue;

		public bool m_wordWrap;

		public bool m_ellipsis;

		public List<string> m_lines = [];

		public Vector2? m_linesSize;

		public float? m_linesAvailableWidth;

		public float? m_linesAvailableHeight;

		public Vector2 Size
		{
			get;
			set;
		}


		public virtual string Text
		{
			get
			{
				return m_text;
			}
			set
			{
				if (m_text != value && value != null)
				{
					m_text = value;
					m_linesSize = null;
				}
			}
		}

		public TextAnchor TextAnchor
		{
			get;
			set;
		}

		public TextOrientation TextOrientation
		{
			get
			{
				return m_textOrientation;
			}
			set
			{
				if (value != m_textOrientation)
				{
					m_textOrientation = value;
					m_linesSize = null;
				}
			}
		}

		public BitmapFont Font
		{
			get
			{
				return m_font ?? (LabelWidget.BitmapFont ?? BitmapFont.DebugFont);
			}
			set
			{
				if (value != m_font)
				{
					m_font = value;
					m_linesSize = null;
				}
			}
		}

		public float FontScale
		{
			get
			{
				return m_fontScale;
			}
			set
			{
				if (value != m_fontScale)
				{
					m_fontScale = value;
					m_linesSize = null;
				}
			}
		}

		public Vector2 FontSpacing
		{
			get
			{
				return m_fontSpacing;
			}
			set
			{
				if (value != m_fontSpacing)
				{
					m_fontSpacing = value;
					m_linesSize = null;
				}
			}
		}

		public bool WordWrap
		{
			get
			{
				return m_wordWrap;
			}
			set
			{
				if (value != m_wordWrap)
				{
					m_wordWrap = value;
					m_linesSize = null;
				}
			}
		}

		public bool Ellipsis
		{
			get
			{
				return m_ellipsis;
			}
			set
			{
				if (value != m_ellipsis)
				{
					m_ellipsis = value;
					m_linesSize = null;
				}
			}
		}

		public int MaxLines
		{
			get
			{
				return m_maxLines;
			}
			set
			{
				if (value != m_maxLines)
				{
					m_maxLines = value;
					m_linesSize = null;
				}
			}
		}

		public Color Color
		{
			get;
			set;
		}

		public bool DropShadow
		{
			get;
			set;
		}

		public bool TextureLinearFilter
		{
			get;
			set;
		}
		public bool m_usedebug;

		public FontTextWidget()
		{
			IsHitTestVisible = false;
			Text = string.Empty;
			FontScale = 1f;
			Color = Color.White;
			TextureLinearFilter = true;
			Size = new Vector2(-1f);
		}


		public override void Draw(DrawContext dc)
		{
			if (!string.IsNullOrEmpty(Text) && Color.A != 0)
			{
				SamplerState samplerState = TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp;
				FontBatch2D fontBatch2D = dc.PrimitivesRenderer2D.FontBatch(Font, 1, DepthStencilState.None, null, null, samplerState);
				int count = fontBatch2D.TriangleVertices.Count;
				float num = 0f;
				if ((TextAnchor & TextAnchor.VerticalCenter) != 0)
				{
					float num2 = (Font.GlyphHeight * FontScale * Font.Scale) + ((m_lines.Count - 1) * (((Font.GlyphHeight + Font.Spacing.Y) * FontScale * Font.Scale) + FontSpacing.Y));
					num = (ActualSize.Y - num2) / 2f;
				}
				else if ((TextAnchor & TextAnchor.Bottom) != 0)
				{
					float num3 = (Font.GlyphHeight * FontScale * Font.Scale) + ((m_lines.Count - 1) * (((Font.GlyphHeight + Font.Spacing.Y) * FontScale * Font.Scale) + FontSpacing.Y));
					num = ActualSize.Y - num3;
				}
				TextAnchor anchor = TextAnchor & ~(TextAnchor.VerticalCenter | TextAnchor.Bottom);
				Color color = Color * GlobalColorTransform;
				float num4 = CalculateLineHeight();
				foreach (string line in m_lines)
				{
					float x = 0f;
					if ((TextAnchor & TextAnchor.HorizontalCenter) != 0)
					{
						x = ActualSize.X / 2f;
					}
					else if ((TextAnchor & TextAnchor.Right) != 0)
					{
						x = ActualSize.X;
					}
					bool flag = true;
					Vector2 vector = Vector2.Zero;
					float angle = 0f;
					if (TextOrientation == TextOrientation.Horizontal)
					{
						vector = new Vector2(x, num);
						angle = 0f;
						_ = Display.ScissorRectangle;
						flag = true;
					}
					else if (TextOrientation == TextOrientation.VerticalLeft)
					{
						vector = new Vector2(x, ActualSize.Y + num);
						angle = MathUtils.DegToRad(-90f);
						flag = true;
					}
					if (flag)
					{
						if (DropShadow)
						{
							fontBatch2D.QueueText(line, vector + (1f * new Vector2(FontScale)), 0f, new Color((byte)0, (byte)0, (byte)0, color.A), anchor, new Vector2(FontScale), FontSpacing, angle);
						}
						fontBatch2D.QueueText(line, vector, 0f, color, anchor, new Vector2(FontScale), FontSpacing, angle);
					}
					num += num4;
				}
				fontBatch2D.TransformTriangles(GlobalTransform, count);
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			IsDrawRequired = !string.IsNullOrEmpty(Text) && Color.A != 0;
			if (TextOrientation == TextOrientation.Horizontal)
			{
				UpdateLines(parentAvailableSize.X, parentAvailableSize.Y);
				DesiredSize = new Vector2((Size.X < 0f) ? m_linesSize.Value.X : Size.X, (Size.Y < 0f) ? m_linesSize.Value.Y : Size.Y);
			}
			else if (TextOrientation == TextOrientation.VerticalLeft)
			{
				UpdateLines(parentAvailableSize.Y, parentAvailableSize.X);
				DesiredSize = new Vector2((Size.X < 0f) ? m_linesSize.Value.Y : Size.X, (Size.Y < 0f) ? m_linesSize.Value.X : Size.Y);
			}
		}

		public float CalculateLineHeight()
		{
			return (Font.GlyphHeight + Font.Spacing.Y + FontSpacing.Y) * FontScale * Font.Scale;
		}

		public void UpdateLines(float availableWidth, float availableHeight)
		{
			if (m_linesAvailableHeight.HasValue && m_linesAvailableHeight == availableHeight && m_linesAvailableWidth.HasValue && m_linesSize.HasValue)
			{
				float num = MathUtils.Min(m_linesSize.Value.X, m_linesAvailableWidth.Value) - 0.1f;
				float num2 = MathUtils.Max(m_linesSize.Value.X, m_linesAvailableWidth.Value) + 0.1f;
				if (availableWidth >= num && availableWidth <= num2)
				{
					return;
				}
			}
			availableWidth += 0.1f;
			m_lines.Clear();
			string[] array = (Text ?? string.Empty).Split(new string[] { "\n" }, StringSplitOptions.None);
			string text = "...";
			float x = Font.MeasureText(text, new Vector2(FontScale), FontSpacing).X;
			if (WordWrap)
			{
				int num3 = (int)MathUtils.Min(MathF.Floor(availableHeight / CalculateLineHeight()), MaxLines);
				for (int i = 0; i < array.Length; i++)
				{
					string text2 = array[i].TrimEnd();
					if (text2.Length == 0)
					{
						m_lines.Add(string.Empty);
						continue;
					}
					while (text2.Length > 0)
					{
						bool flag;
						int num4;
						if (Ellipsis && m_lines.Count + 1 >= num3)
						{
							num4 = Font.FitText(MathUtils.Max(availableWidth - x, 0f), text2, 0, text2.Length, FontScale, FontSpacing.X);
							flag = true;
						}
						else
						{
							num4 = Font.FitText(availableWidth, text2, 0, text2.Length, FontScale, FontSpacing.X);
							num4 = MathUtils.Max(num4, 1);
							flag = false;
							if (num4 < text2.Length)
							{
								int num5 = num4;
								int num6 = num5 - 2;
								while (num6 >= 0 && !char.IsWhiteSpace(text2[num6]) && !char.IsPunctuation(text2[num6]))
								{
									num6--;
								}
								if (num6 < 0)
								{
									num6 = num5 - 1;
								}
								num4 = num6 + 1;
							}
						}
						string text3;
						if (num4 == text2.Length)
						{
							text3 = text2;
							text2 = string.Empty;
						}
						else
						{
							text3 = text2.Substring(0, num4).TrimEnd();
							if (flag)
							{
								text3 += text;
							}
							text2 = text2.Substring(num4, text2.Length - num4).TrimStart();
						}
						m_lines.Add(text3);
						if (!flag)
						{
							continue;
						}

						if (m_lines.Count > MaxLines)
						{
							m_lines = m_lines.Take(MaxLines).ToList();
						}
					}
				}
			}
			else if (Ellipsis)
			{
				for (int j = 0; j < array.Length; j++)
				{
					string text4 = array[j].TrimEnd();
					int num7 = Font.FitText(MathUtils.Max(availableWidth - x, 0f), text4, 0, text4.Length, FontScale, FontSpacing.X);
					if (num7 < text4.Length)
					{
						m_lines.Add(text4.Substring(0, num7).TrimEnd() + text);
					}
					else
					{
						m_lines.Add(text4);
					}
				}
			}
			else
			{
				m_lines.AddRange(array);
			}
			if (m_lines.Count > MaxLines)
			{
				m_lines = m_lines.Take(MaxLines).ToList();
			}
			Vector2 zero = Vector2.Zero;
			for (int k = 0; k < m_lines.Count; k++)
			{
				Vector2 vector = Font.MeasureText(m_lines[k], new Vector2(FontScale), FontSpacing);
				zero.X = MathUtils.Max(zero.X, vector.X);
				if (k < m_lines.Count - 1)
				{
					zero.Y += (Font.GlyphHeight + Font.Spacing.Y + FontSpacing.Y) * FontScale * Font.Scale;
				}
				else
				{
					zero.Y += Font.GlyphHeight * FontScale * Font.Scale;
				}
			}
			m_linesSize = zero;
			m_linesAvailableWidth = availableWidth;
			m_linesAvailableHeight = availableHeight;
		}
	}
}
