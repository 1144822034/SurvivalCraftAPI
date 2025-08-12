using Engine;
using Engine.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Game
{
	public class EditMemoryBankDialogAPI : Dialog
	{
		public MemoryBankData memory;
		public DynamicArray<byte> Data = [];
		public StackPanelWidget MainView;
		public Action onCancel;
		public int clickpos = 0;
		public bool isSetPos = false;//是否为设定位置模式
		public int setPosN = 0;//第几位数
		public int lastvalue = 0;
		public bool isclick = true;
		public List<ClickTextWidget> list = [];

		public byte LastOutput
		{
			get;
			set;
		}

		public EditMemoryBankDialogAPI(MemoryBankData memoryBankData, Action onCancel)
		{
			//Keyboard.CharacterEntered += ChangeNumber;
			memory = memoryBankData;
			Data.Clear();
			Data.AddRange(memory.Data);
			var canvasWidget = new CanvasWidget() { Size = new Vector2(600f, 500f), HorizontalAlignment = WidgetAlignment.Center, VerticalAlignment = WidgetAlignment.Center };
			var rectangleWidget = new BevelledRectangleWidget() { Style = ContentManager.Get<XElement>("Styles/DialogArea")};
			var stackPanel = new StackPanelWidget() { Direction = LayoutDirection.Vertical };
			var labelWidget = new LabelWidget() { Text = LanguageControl.GetContentWidgets(GetType().Name, 0), HorizontalAlignment = WidgetAlignment.Center, Margin = new Vector2(0, 10) };
			var stackPanelWidget = new StackPanelWidget() { Direction = LayoutDirection.Horizontal, HorizontalAlignment = WidgetAlignment.Near, VerticalAlignment = WidgetAlignment.Near, Margin = new Vector2(10f, 10f) };
			Children.Add(canvasWidget);
			canvasWidget.Children.Add(rectangleWidget);
			canvasWidget.Children.Add(stackPanel);
			stackPanel.Children.Add(labelWidget);
			stackPanel.Children.Add(stackPanelWidget);
			stackPanelWidget.Children.Add(initData());
			stackPanelWidget.Children.Add(InitButton());
			MainView = stackPanel;
			this.onCancel = onCancel;
			lastvalue = memory.Read(0);
		}
		public byte Read(int address)
		{
			return address >= 0 && address < Data.Count ? Data.Array[address] : (byte)0;
		}

		public void Write(int address, byte data)
		{
			if (address >= 0 && address < Data.Count)
			{
				Data.Array[address] = data;
			}
			else if (address >= 0 && address < 256 && data != 0)
			{
				Data.Count = Math.Max(Data.Count, address + 1);
				Data.Array[address] = data;
			}
		}
		public void LoadString(string data)
		{
			string[] array = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
			if (array.Length >= 1)
			{
				string text = array[0];
				text = text.TrimEnd('0');
				Data.Clear();
				for (int i = 0; i < Math.Min(text.Length, 256); i++)
				{
					int num = MemoryBankData.m_hexChars.IndexOf(char.ToUpperInvariant(text[i]));
					if (num < 0)
					{
						num = 0;
					}
					Data.Add((byte)num);
				}
			}
			if (array.Length >= 2)
			{
				string text2 = array[1];
				int num2 = MemoryBankData.m_hexChars.IndexOf(char.ToUpperInvariant(text2[0]));
				if (num2 < 0)
				{
					num2 = 0;
				}
				LastOutput = (byte)num2;
			}
		}

		public string SaveString()
		{
			return SaveString(saveLastOutput: true);
		}

		public string SaveString(bool saveLastOutput)
		{
			var stringBuilder = new StringBuilder();
			int num = Data.Count;
			for (int j = 0; j < num; j++)
			{
				int index = Math.Clamp((int)Data.Array[j], 0, 15);
				stringBuilder.Append(MemoryBankData.m_hexChars[index]);
			}
			if (saveLastOutput)
			{
				stringBuilder.Append(';');
				stringBuilder.Append(MemoryBankData.m_hexChars[Math.Clamp((int)LastOutput, 0, 15)]);
			}
			return stringBuilder.ToString();
		}

		public Widget initData()
		{
			var stack = new StackPanelWidget() { Direction = LayoutDirection.Vertical, VerticalAlignment = WidgetAlignment.Center, HorizontalAlignment = WidgetAlignment.Far, Margin = new Vector2(10, 0) };
			for (int i = 0; i < 17; i++)
			{
				var line = new StackPanelWidget() { Direction = LayoutDirection.Horizontal };
				for (int j = 0; j < 17; j++)
				{
					int addr = ((i - 1) * 16) + (j - 1);
					if (j > 0 && i > 0)
					{
						var clickTextWidget = new ClickTextWidget(new Vector2(22), string.Format("{0}", MemoryBankData.m_hexChars[Read(addr)]), delegate ()
						{
							AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
							clickpos = addr;
							isclick = true;
						});
						list.Add(clickTextWidget);
						line.Children.Add(clickTextWidget);
					}
					else
					{
						int p = 0;
						if (i == 0 && j > 0) p = j - 1;
						else if (j == 0 && i > 0) p = i - 1;
						else
						{
							var click = new ClickTextWidget(new Vector2(22), "", null);
							line.Children.Add(click);
							continue;
						};
						var clickTextWidget = new ClickTextWidget(new Vector2(22), MemoryBankData.m_hexChars[p].ToString(), delegate ()
						{

						});
						clickTextWidget.labelWidget.Color = Color.DarkGray;
						line.Children.Add(clickTextWidget);
					}
				}
				stack.Children.Add(line);
			}
			return stack;
		}

		public Widget makeFuncButton(string txt, Action func)
		{
			var clickText = new ClickTextWidget(new Vector2(40),txt,func,true)
			{
				BorderColor = Color.White,
				Margin = new Vector2(2)
			};
			clickText.labelWidget.FontScale = txt.Length > 1 ? 0.7f : 1f;
			clickText.labelWidget.Color = Color.White;
			return clickText;
		}
		private void ChangeNumber(char pp)
		{
				AudioManager.PlaySound("Audio/UI/ButtonClick",1f,0f,0f);
				Write(clickpos,(byte)pp);//写入数据
				lastvalue = pp;
				clickpos += 1;//自动加1
				if(clickpos > 255)
				{
					clickpos = 0;
				}
				isclick = true;
		}
		private StackPanelWidget InitButton()
		{
			var stack = new StackPanelWidget() { Direction = LayoutDirection.Vertical, VerticalAlignment = WidgetAlignment.Center, HorizontalAlignment = WidgetAlignment.Far, Margin = new Vector2(10, 10) };
			for (int i = 0; i < 6; i++)
			{
				var stackPanelWidget = new StackPanelWidget() { Direction = LayoutDirection.Horizontal };
				for (int j = 0; j < 3; j++)
				{
					int cc = (i * 3) + j;
					if (cc < 15)
					{
						int pp = cc + 1;
						stackPanelWidget.Children.Add(makeFuncButton(string.Format("{0}", MemoryBankData.m_hexChars[pp]), delegate ()
						{
							AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
							if (!isSetPos)
							{
								Write(clickpos, (byte)pp);//写入数据
								lastvalue = pp;
								clickpos += 1;//自动加1
								if (clickpos > 255)
								{
									clickpos = 0;
								}
								isclick = true;
							}
							else
							{ //处于设定位置模式
								if (setPosN == 0) clickpos = 16 * pp;
								else if (setPosN == 1) clickpos += pp;
								setPosN += 1;
								if (setPosN == 2)
								{
									if (clickpos > 0xff) clickpos = 0;
									setPosN = 0;
									isclick = true;
									isSetPos = false;
								}
							}
						}));
					}
					else if (cc == 15)
					{
						stackPanelWidget.Children.Add(makeFuncButton(string.Format("{0}", MemoryBankData.m_hexChars[0]), delegate ()
						{
							AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
							if (!isSetPos)
							{
								Write(clickpos, 0);//写入数据
								lastvalue = 0;
								clickpos += 1;//自动加1
								if (clickpos >= 255)
								{
									clickpos = 0;
								}
								isclick = true;
							}
							else
							{ //处于设定位置模式
								if (setPosN == 0) clickpos = 0;
								else if (setPosN == 1) clickpos += 0;
								setPosN += 1;
								if (setPosN == 2)
								{
									if (clickpos > 0xff) clickpos = 0;
									setPosN = 0;
									isclick = true;
									isSetPos = false;
								}
							}
						}));
						continue;
					}
					else if (cc == 16)
					{
						stackPanelWidget.Children.Add(makeFuncButton(LanguageControl.GetContentWidgets(GetType().Name, 1), delegate ()
						{
							AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
							for (int ai = 0; ai < Data.Count; ai++)
							{
								Write(ai, 0);
							}
							isclick = true;

						}));
						continue;
					}
					else if (cc == 17)
					{
						stackPanelWidget.Children.Add(makeFuncButton(LanguageControl.GetContentWidgets(GetType().Name, 2), delegate ()
						{
							AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
							var tmp = new DynamicArray<byte>();
							tmp.AddRange(Data);
							tmp.Count = 256;
							for (int c = 0; c < 16; c++)
							{
								for (int d = 0; d < 16; d++)
								{
									Write(c + (d * 16), tmp[(c * 16) + d]);
								}
							}
							clickpos = 0;
							isclick = true;
						}));
						continue;
					}
				}
				stack.Children.Add(stackPanelWidget);
			}
			var labelWidget = new LabelWidget() { FontScale = 0.8f, Text = LanguageControl.GetContentWidgets(GetType().Name, 3), HorizontalAlignment = WidgetAlignment.Center, Margin = new Vector2(0f, 10f), Color = Color.DarkGray };
			stack.Children.Add(labelWidget);
			stack.Children.Add(makeTextBox(delegate (TextBoxWidget textBoxWidget)
			{
				LoadString(textBoxWidget.Text);
				isclick = true;
			}, memory.SaveString(false)));
			stack.Children.Add(MakeButton(LanguageControl.GetContentWidgets(GetType().Name, 4), delegate ()
			{
				for (int i = 0; i < Data.Count; i++)
				{
					memory.Write(i, Data[i]);
				}
				onCancel?.Invoke();
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				DialogsManager.HideDialog(this);
			}));
			stack.Children.Add(MakeButton(LanguageControl.GetContentWidgets(GetType().Name, 5), delegate ()
			{
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				DialogsManager.HideDialog(this);
				isclick = true;
			}));
			return stack;
		}

		public Widget makeTextBox(Action<TextBoxWidget> ac, string text = "")
		{
			var canvasWidget = new CanvasWidget() { HorizontalAlignment = WidgetAlignment.Center };
			var rectangleWidget = new RectangleWidget() { FillColor = Color.Black, OutlineColor = Color.White, Size = new Vector2(120, 30) };
			var stack = new StackPanelWidget() { Direction = LayoutDirection.Vertical };
			var textBox = new TextBoxWidget() { VerticalAlignment = WidgetAlignment.Center, Color = new Color(255, 255, 255), Margin = new Vector2(4f, 0f), Size = new Vector2(120, 30), MaximumLength = 256 };
			textBox.FontScale = 0.7f;
			textBox.Text = text;
			textBox.TextChanged += ac;
			stack.Children.Add(textBox);
			canvasWidget.Children.Add(rectangleWidget);
			canvasWidget.Children.Add(stack);
			return canvasWidget;
		}

		private static Widget MakeButton(string txt, Action tas)
		{
			var clickTextWidget = new ClickTextWidget(new Vector2(120,30),txt,tas)
			{
				BorderColor = Color.White,
				Margin = new Vector2(0,3)
			};
			clickTextWidget.labelWidget.FontScale = 0.7f;
			clickTextWidget.labelWidget.Color = Color.Green;
			return clickTextWidget;
		}

		public override void Update()
		{
			if (Input.Back || Input.Cancel)
			{
				DialogsManager.HideDialog(this);
			}
			if (isSetPos)
			{
				list[clickpos].BorderColor = Color.Red;//设定选择颜色
				return;
			}
			if (!isclick) return;
			for (int i = 0; i < list.Count; i++)
			{
				if (i == clickpos)
				{
					list[i].BorderColor = Color.Yellow;//设定选择颜色
				}
				else
				{
					list[i].BorderColor = Color.Transparent;//设定选择颜色
				}
				list[i].labelWidget.Text = string.Format("{0}", MemoryBankData.m_hexChars[Read(i)]);

			}
			isclick = false;
		}
	}
}