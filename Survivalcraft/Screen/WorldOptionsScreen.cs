using Engine;
using Engine.Graphics;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Game
{
	public class WorldOptionsScreen : Screen
	{
		public static string fName = "WorldOptionsScreen";
		public Widget m_newWorldOnlyPanel;

		public ButtonWidget m_terrainGenerationButton;

		public Widget m_continentTerrainPanel;

		private Widget m_yearDaysPanel;

		public Widget m_islandTerrainPanel;

		public SliderWidget m_islandSizeEW;

		public SliderWidget m_islandSizeNS;

		public Widget m_flatTerrainPanel;

		public SliderWidget m_flatTerrainLevelSlider;

		public SliderWidget m_flatTerrainShoreRoughnessSlider;

		public BlockIconWidget m_flatTerrainBlock;

		public LabelWidget m_flatTerrainBlockLabel;

		public ButtonWidget m_flatTerrainBlockButton;

		public CheckboxWidget m_flatTerrainMagmaOceanCheckbox;

		public SliderWidget m_seaLevelOffsetSlider;

		public SliderWidget m_temperatureOffsetSlider;

		public SliderWidget m_humidityOffsetSlider;

		public SliderWidget m_biomeSizeSlider;

		public RectangleWidget m_blocksTextureIcon;

		public LabelWidget m_blocksTextureLabel;

		public LabelWidget m_blocksTextureDetails;

		public ButtonWidget m_blocksTextureButton;

		public ButtonWidget m_paletteButton;

		public ButtonWidget m_supernaturalCreaturesButton;

		public ButtonWidget m_friendlyFireButton;

		private Widget m_seasonsPanel;

		private CheckboxWidget m_areSeasonsChangingCheckBox;

		private SliderWidget m_yearDaysSlider;

		private SliderWidget m_timeOfYearSlider;

		public Widget m_creativeModePanel;

		public ButtonWidget m_environmentBehaviorButton;

		public ButtonWidget m_timeOfDayButton;

		public ButtonWidget m_weatherEffectsButton;

		public ButtonWidget m_adventureRespawnButton;

		public ButtonWidget m_adventureSurvivalMechanicsButton;

		public LabelWidget m_descriptionLabel;

		public WorldSettings m_worldSettings;

		public bool m_isExistingWorld;

		public BlocksTexturesCache m_blockTexturesCache = new();

		public static float[] m_islandSizes = new float[20]
		{
			30f,
			40f,
			50f,
			60f,
			80f,
			100f,
			120f,
			150f,
			200f,
			250f,
			300f,
			400f,
			500f,
			600f,
			800f,
			1000f,
			1200f,
			1500f,
			2000f,
			2500f
		};

		public static float[] m_biomeSizes = new float[9]
		{
			0.25f,
			0.33f,
			0.5f,
			0.75f,
			1f,
			1.5f,
			2f,
			3f,
			4f
		};

		private static float[] m_yearDays = new float[9] { 8f, 12f, 16f, 20f, 24f, 32f, 48f, 64f, 96f };

		public WorldOptionsScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/WorldOptionsScreen");
			LoadContents(this, node);
			m_creativeModePanel = Children.Find<Widget>("CreativeModePanel");
			m_seasonsPanel = Children.Find<Widget>("SeasonsPanel");
			m_newWorldOnlyPanel = Children.Find<Widget>("NewWorldOnlyPanel");
			m_continentTerrainPanel = Children.Find<Widget>("ContinentTerrainPanel");
			m_yearDaysPanel = Children.Find<Widget>("YearDaysPanel");
			m_islandTerrainPanel = Children.Find<Widget>("IslandTerrainPanel");
			m_islandSizeNS = Children.Find<SliderWidget>("IslandSizeNS");
			m_islandSizeEW = Children.Find<SliderWidget>("IslandSizeEW");
			m_flatTerrainPanel = Children.Find<Widget>("FlatTerrainPanel");
			m_blocksTextureIcon = Children.Find<RectangleWidget>("BlocksTextureIcon");
			m_blocksTextureLabel = Children.Find<LabelWidget>("BlocksTextureLabel");
			m_blocksTextureDetails = Children.Find<LabelWidget>("BlocksTextureDetails");
			m_blocksTextureButton = Children.Find<ButtonWidget>("BlocksTextureButton");
			m_seaLevelOffsetSlider = Children.Find<SliderWidget>("SeaLevelOffset");
			m_temperatureOffsetSlider = Children.Find<SliderWidget>("TemperatureOffset");
			m_humidityOffsetSlider = Children.Find<SliderWidget>("HumidityOffset");
			m_biomeSizeSlider = Children.Find<SliderWidget>("BiomeSize");
			m_paletteButton = Children.Find<ButtonWidget>("Palette");
			m_supernaturalCreaturesButton = Children.Find<ButtonWidget>("SupernaturalCreatures");
			m_friendlyFireButton = Children.Find<ButtonWidget>("FriendlyFire");
			m_environmentBehaviorButton = Children.Find<ButtonWidget>("EnvironmentBehavior");
			m_timeOfDayButton = Children.Find<ButtonWidget>("TimeOfDay");
			m_areSeasonsChangingCheckBox = Children.Find<CheckboxWidget>("AreSeasonsChanging");
			m_yearDaysSlider = Children.Find<SliderWidget>("YearDays");
			m_timeOfYearSlider = Children.Find<SliderWidget>("TimeOfYear");
			m_weatherEffectsButton = Children.Find<ButtonWidget>("WeatherEffects");
			m_adventureRespawnButton = Children.Find<ButtonWidget>("AdventureRespawn");
			m_adventureSurvivalMechanicsButton = Children.Find<ButtonWidget>("AdventureSurvivalMechanics");
			m_terrainGenerationButton = Children.Find<ButtonWidget>("TerrainGeneration");
			m_flatTerrainLevelSlider = Children.Find<SliderWidget>("FlatTerrainLevel");
			m_flatTerrainShoreRoughnessSlider = Children.Find<SliderWidget>("FlatTerrainShoreRoughness");
			m_flatTerrainBlock = Children.Find<BlockIconWidget>("FlatTerrainBlock");
			m_flatTerrainBlockLabel = Children.Find<LabelWidget>("FlatTerrainBlockLabel");
			m_flatTerrainBlockButton = Children.Find<ButtonWidget>("FlatTerrainBlockButton");
			m_flatTerrainMagmaOceanCheckbox = Children.Find<CheckboxWidget>("MagmaOcean");
			m_descriptionLabel = Children.Find<LabelWidget>("Description");
			m_islandSizeEW.MinValue = 0f;
			m_islandSizeEW.MaxValue = m_islandSizes.Length - 1;
			m_islandSizeEW.Granularity = 1f;
			m_islandSizeNS.MinValue = 0f;
			m_islandSizeNS.MaxValue = m_islandSizes.Length - 1;
			m_islandSizeNS.Granularity = 1f;
			m_biomeSizeSlider.MinValue = 0f;
			m_biomeSizeSlider.MaxValue = m_biomeSizes.Length - 1;
			m_biomeSizeSlider.Granularity = 1f;
			m_yearDaysSlider.MinValue = 0f;
			m_yearDaysSlider.MaxValue = m_yearDays.Length - 1;
			m_yearDaysSlider.Granularity = 1f;
		}

		public static string FormatOffset(float value)
		{
			if (value != 0f)
			{
				return ((value >= 0f) ? "+" : "") + value.ToString();
			}
			return LanguageControl.Get(fName, 6);
		}

		public override void Enter(object[] parameters)
		{
			m_worldSettings = (WorldSettings)parameters[0];
			m_isExistingWorld = (bool)parameters[1];
			m_descriptionLabel.Text = StringsManager.GetString("EnvironmentBehaviorMode." + m_worldSettings.EnvironmentBehaviorMode.ToString() + ".Description");
		}

		public override void Leave()
		{
			m_blockTexturesCache.Clear();
		}

		public override void Update()
		{
			if (m_terrainGenerationButton.IsClicked && !m_isExistingWorld)
			{
				IList<int> enumValues = EnumUtils.GetEnumValues(typeof(TerrainGenerationMode));
				DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get(fName, 1), enumValues, 56f, (object e) => StringsManager.GetString("TerrainGenerationMode." + ((TerrainGenerationMode)e).ToString() + ".Name"), delegate (object e)
				{
					if (m_worldSettings.GameMode != 0 && ((TerrainGenerationMode)e == TerrainGenerationMode.FlatContinent || (TerrainGenerationMode)e == TerrainGenerationMode.FlatIsland))
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), LanguageControl.Get(fName, 5), LanguageControl.Ok, null, null));
					}
					else
					{
						m_worldSettings.TerrainGenerationMode = (TerrainGenerationMode)e;
						m_descriptionLabel.Text = StringsManager.GetString("TerrainGenerationMode." + m_worldSettings.TerrainGenerationMode.ToString() + ".Description");
					}
				}));
			}
			if (m_islandSizeEW.IsSliding && !m_isExistingWorld)
			{
				m_worldSettings.IslandSize.X = m_islandSizes[Math.Clamp((int)m_islandSizeEW.Value, 0, m_islandSizes.Length - 1)];
			}
			if (m_islandSizeNS.IsSliding && !m_isExistingWorld)
			{
				m_worldSettings.IslandSize.Y = m_islandSizes[Math.Clamp((int)m_islandSizeNS.Value, 0, m_islandSizes.Length - 1)];
			}
			if (m_flatTerrainLevelSlider.IsSliding && !m_isExistingWorld)
			{
				m_worldSettings.TerrainLevel = Math.Clamp((int)m_flatTerrainLevelSlider.Value / (int)m_flatTerrainLevelSlider.Granularity * (int)m_flatTerrainLevelSlider.Granularity, 2, 252);
				m_descriptionLabel.Text = StringsManager.GetString("FlatTerrainLevel.Description");
			}
			if (m_flatTerrainShoreRoughnessSlider.IsSliding && !m_isExistingWorld)
			{
				m_worldSettings.ShoreRoughness = m_flatTerrainShoreRoughnessSlider.Value;
				m_descriptionLabel.Text = StringsManager.GetString("FlatTerrainShoreRoughness.Description");
			}
			if (m_flatTerrainBlockButton.IsClicked && !m_isExistingWorld)
			{
				int[] items = new int[19]
				{
					8,
					2,
					7,
					3,
					67,
					66,
					4,
					5,
					26,
					73,
					21,
					46,
					47,
					15,
					62,
					68,
					126,
					71,
					1
				};
				DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get(fName, 2), items, 72f, delegate (object index)
				{
					XElement node2 = ContentManager.Get<XElement>("Widgets/SelectBlockItem");
					var obj2 = (ContainerWidget)LoadWidget(null, node2, null);
					obj2.Children.Find<BlockIconWidget>("SelectBlockItem.Block").Contents = (int)index;
					obj2.Children.Find<LabelWidget>("SelectBlockItem.Text").Text = BlocksManager.Blocks[(int)index].GetDisplayName(null, Terrain.MakeBlockValue((int)index));
					return obj2;
				}, delegate (object index)
				{
					m_worldSettings.TerrainBlockIndex = (int)index;
				}));
			}
			if (m_flatTerrainMagmaOceanCheckbox.IsClicked)
			{
				m_worldSettings.TerrainOceanBlockIndex = (m_worldSettings.TerrainOceanBlockIndex == 18) ? 92 : 18;
				m_descriptionLabel.Text = StringsManager.GetString("FlatTerrainMagmaOcean.Description");
			}
			if (m_seaLevelOffsetSlider.IsSliding && !m_isExistingWorld)
			{
				m_worldSettings.SeaLevelOffset = (int)m_seaLevelOffsetSlider.Value;
				m_descriptionLabel.Text = StringsManager.GetString("SeaLevelOffset.Description");
			}
			if (m_temperatureOffsetSlider.IsSliding && !m_isExistingWorld)
			{
				m_worldSettings.TemperatureOffset = m_temperatureOffsetSlider.Value;
				m_descriptionLabel.Text = StringsManager.GetString("TemperatureOffset.Description");
			}
			if (m_humidityOffsetSlider.IsSliding && !m_isExistingWorld)
			{
				m_worldSettings.HumidityOffset = m_humidityOffsetSlider.Value;
				m_descriptionLabel.Text = StringsManager.GetString("HumidityOffset.Description");
			}
			if (m_biomeSizeSlider.IsSliding && !m_isExistingWorld)
			{
				m_worldSettings.BiomeSize = m_biomeSizes[Math.Clamp((int)m_biomeSizeSlider.Value, 0, m_biomeSizes.Length - 1)];
				m_descriptionLabel.Text = StringsManager.GetString("BiomeSize.Description");
			}
			if (m_blocksTextureButton.IsClicked)
			{
				BlocksTexturesManager.UpdateBlocksTexturesList();
				var dialog = new ListSelectionDialog(LanguageControl.Get(fName, 3), BlocksTexturesManager.BlockTexturesNames, 64f, delegate (object item)
				{
					XElement node = ContentManager.Get<XElement>("Widgets/BlocksTextureItem");
					var obj = (ContainerWidget)LoadWidget(this, node, null);
					Texture2D texture2 = m_blockTexturesCache.GetTexture((string)item);
					obj.Children.Find<LabelWidget>("BlocksTextureItem.Text").Text = BlocksTexturesManager.GetDisplayName((string)item);
					obj.Children.Find<LabelWidget>("BlocksTextureItem.Details").Text = $"{texture2.Width}x{texture2.Height}";
					obj.Children.Find<RectangleWidget>("BlocksTextureItem.Icon").Subtexture = new Subtexture(texture2, Vector2.Zero, Vector2.One);
					return obj;
				}, delegate (object item)
				{
					m_worldSettings.BlocksTextureName = (string)item;
				});
				DialogsManager.ShowDialog(null, dialog);
				m_descriptionLabel.Text = StringsManager.GetString("BlocksTexture.Description");
			}
			if (m_paletteButton.IsClicked)
			{
				DialogsManager.ShowDialog(null, new EditPaletteDialog(m_worldSettings.Palette));
			}
			if (m_supernaturalCreaturesButton.IsClicked)
			{
				m_worldSettings.AreSupernaturalCreaturesEnabled = !m_worldSettings.AreSupernaturalCreaturesEnabled;
				m_descriptionLabel.Text = StringsManager.GetString("SupernaturalCreatures." + m_worldSettings.AreSupernaturalCreaturesEnabled.ToString());
			}
			if (m_friendlyFireButton.IsClicked)
			{
				m_worldSettings.IsFriendlyFireEnabled = !m_worldSettings.IsFriendlyFireEnabled;
				m_descriptionLabel.Text = StringsManager.GetString("FriendlyFire." + m_worldSettings.IsFriendlyFireEnabled.ToString());
			}
			if (m_environmentBehaviorButton.IsClicked)
			{
				IList<int> enumValues2 = EnumUtils.GetEnumValues(typeof(EnvironmentBehaviorMode));
				m_worldSettings.EnvironmentBehaviorMode = (EnvironmentBehaviorMode)((enumValues2.IndexOf((int)m_worldSettings.EnvironmentBehaviorMode) + 1) % enumValues2.Count);
				m_descriptionLabel.Text = StringsManager.GetString("EnvironmentBehaviorMode." + m_worldSettings.EnvironmentBehaviorMode.ToString() + ".Description");
			}
			if (m_timeOfDayButton.IsClicked)
			{
				DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get(fName, "7"), EnumUtils.GetEnumValues(typeof(TimeOfDayMode)), 56f, (object e) => LanguageControl.Get("TimeOfDayMode", ((TimeOfDayMode)e).ToString()), delegate(object e)
				{
					m_worldSettings.TimeOfDayMode = (TimeOfDayMode)e;
					m_descriptionLabel.Text = StringsManager.GetString(string.Concat("TimeOfDayMode.", (TimeOfDayMode)e, ".Description"));
				}));
			}
			if (m_areSeasonsChangingCheckBox.IsClicked)
			{
				m_worldSettings.AreSeasonsChanging = !m_worldSettings.AreSeasonsChanging;
				m_descriptionLabel.Text = StringsManager.GetString($"AreSeasonsChanging.{m_worldSettings.AreSeasonsChanging}");
			}
			if (m_yearDaysSlider.IsSliding)
			{
				m_worldSettings.YearDays = m_yearDays[Math.Clamp((int)m_yearDaysSlider.Value, 0, m_yearDays.Length - 1)];
				m_descriptionLabel.Text = StringsManager.GetString("YearDays.Description");
			}
			if (m_timeOfYearSlider.IsSliding)
			{
				m_worldSettings.TimeOfYear = Math.Clamp(m_timeOfYearSlider.Value, 0f, 0.999f);
				m_descriptionLabel.Text = StringsManager.GetString("TimeOfYear.Description");
			}
			if (m_weatherEffectsButton.IsClicked)
			{
				m_worldSettings.AreWeatherEffectsEnabled = !m_worldSettings.AreWeatherEffectsEnabled;
				m_descriptionLabel.Text = StringsManager.GetString("WeatherMode." + m_worldSettings.AreWeatherEffectsEnabled.ToString());
			}
			if (m_adventureRespawnButton.IsClicked)
			{
				m_worldSettings.IsAdventureRespawnAllowed = !m_worldSettings.IsAdventureRespawnAllowed;
				m_descriptionLabel.Text = StringsManager.GetString("AdventureRespawnMode." + m_worldSettings.IsAdventureRespawnAllowed.ToString());
			}
			if (m_adventureSurvivalMechanicsButton.IsClicked)
			{
				m_worldSettings.AreAdventureSurvivalMechanicsEnabled = !m_worldSettings.AreAdventureSurvivalMechanicsEnabled;
				m_descriptionLabel.Text = StringsManager.GetString("AdventureSurvivalMechanics." + m_worldSettings.AreAdventureSurvivalMechanicsEnabled.ToString());
			}
			m_creativeModePanel.IsVisible = m_worldSettings.GameMode == GameMode.Creative;
			m_newWorldOnlyPanel.IsVisible = !m_isExistingWorld;
			m_seasonsPanel.IsVisible = m_worldSettings.GameMode == GameMode.Creative || !m_isExistingWorld;
			m_continentTerrainPanel.IsVisible = m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Continent || m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatContinent;
			m_islandTerrainPanel.IsVisible = m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Island || m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatIsland;
			m_flatTerrainPanel.IsVisible = m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatContinent || m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatIsland;
			m_yearDaysPanel.IsVisible = m_worldSettings.AreSeasonsChanging;
			m_terrainGenerationButton.Text = StringsManager.GetString("TerrainGenerationMode." + m_worldSettings.TerrainGenerationMode.ToString() + ".Name");
			m_islandSizeEW.Value = FindNearestIndex(m_islandSizes, m_worldSettings.IslandSize.X);
			m_islandSizeEW.Text = m_worldSettings.IslandSize.X.ToString();
			m_islandSizeNS.Value = FindNearestIndex(m_islandSizes, m_worldSettings.IslandSize.Y);
			m_islandSizeNS.Text = m_worldSettings.IslandSize.Y.ToString();
			m_flatTerrainLevelSlider.Value = m_worldSettings.TerrainLevel;
			m_flatTerrainLevelSlider.Text = m_worldSettings.TerrainLevel.ToString();
			m_flatTerrainShoreRoughnessSlider.Value = m_worldSettings.ShoreRoughness;
			m_flatTerrainShoreRoughnessSlider.Text = $"{m_worldSettings.ShoreRoughness * 100f:0}%";
			m_flatTerrainBlock.Contents = m_worldSettings.TerrainBlockIndex;
			m_flatTerrainMagmaOceanCheckbox.IsChecked = m_worldSettings.TerrainOceanBlockIndex == 92;
			string text = (BlocksManager.Blocks[m_worldSettings.TerrainBlockIndex] != null) ? BlocksManager.Blocks[m_worldSettings.TerrainBlockIndex].GetDisplayName(null, Terrain.MakeBlockValue(m_worldSettings.TerrainBlockIndex)) : string.Empty;
			m_flatTerrainBlockLabel.Text = (text.Length > 10) ? (text.Substring(0, 10) + "...") : text;
			Texture2D texture = m_blockTexturesCache.GetTexture(m_worldSettings.BlocksTextureName);
			m_blocksTextureIcon.Subtexture = new Subtexture(texture, Vector2.Zero, Vector2.One);
			m_blocksTextureLabel.Text = BlocksTexturesManager.GetDisplayName(m_worldSettings.BlocksTextureName);
			m_blocksTextureDetails.Text = $"{texture.Width}x{texture.Height}";
			m_seaLevelOffsetSlider.Value = m_worldSettings.SeaLevelOffset;
			m_seaLevelOffsetSlider.Text = FormatOffset(m_worldSettings.SeaLevelOffset);
			m_temperatureOffsetSlider.Value = m_worldSettings.TemperatureOffset;
			m_temperatureOffsetSlider.Text = FormatOffset(m_worldSettings.TemperatureOffset);
			m_humidityOffsetSlider.Value = m_worldSettings.HumidityOffset;
			m_humidityOffsetSlider.Text = FormatOffset(m_worldSettings.HumidityOffset);
			m_biomeSizeSlider.Value = FindNearestIndex(m_biomeSizes, m_worldSettings.BiomeSize);
			m_biomeSizeSlider.Text = m_worldSettings.BiomeSize.ToString() + "x";
			m_environmentBehaviorButton.Text = LanguageControl.Get("EnvironmentBehaviorMode", m_worldSettings.EnvironmentBehaviorMode.ToString());
			m_timeOfDayButton.Text = LanguageControl.Get("TimeOfDayMode", m_worldSettings.TimeOfDayMode.ToString());
			m_areSeasonsChangingCheckBox.IsChecked = m_worldSettings.AreSeasonsChanging;
			m_yearDaysSlider.Value = FindNearestIndex(m_yearDays, m_worldSettings.YearDays);
			m_yearDaysSlider.Text = $"{m_worldSettings.YearDays}{LanguageControl.Get(fName, "8")}";
			m_timeOfYearSlider.Value = m_worldSettings.TimeOfYear;
			m_timeOfYearSlider.Text = $"{SubsystemSeasons.GetTimeOfYearName(m_worldSettings.TimeOfYear)}";
			m_timeOfYearSlider.TextColor = SubsystemSeasons.GetTimeOfYearColor(m_worldSettings.TimeOfYear);
			m_weatherEffectsButton.Text = m_worldSettings.AreWeatherEffectsEnabled ? LanguageControl.Enable : LanguageControl.Disable;
			m_adventureRespawnButton.Text = m_worldSettings.IsAdventureRespawnAllowed ? LanguageControl.Allowed : LanguageControl.NAllowed;
			m_adventureSurvivalMechanicsButton.Text = m_worldSettings.AreAdventureSurvivalMechanicsEnabled ? LanguageControl.Enable : LanguageControl.Disable;
			m_supernaturalCreaturesButton.Text = m_worldSettings.AreSupernaturalCreaturesEnabled ? LanguageControl.Enable : LanguageControl.Disable;
			m_friendlyFireButton.Text = m_worldSettings.IsFriendlyFireEnabled ? LanguageControl.Allowed : LanguageControl.NAllowed;
			if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}

		public static int FindNearestIndex(IList<float> list, float v)
		{
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				if (MathF.Abs(list[i] - v) < MathF.Abs(list[num] - v))
				{
					num = i;
				}
			}
			return num;
		}
	}
}
