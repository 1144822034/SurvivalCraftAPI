using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TemplatesDatabase;

namespace Game
{
	public class BestiaryScreen : Screen
	{
		public ListPanelWidget m_creaturesList;

		public Screen m_previousScreen;

		public virtual void OnCreaturesListItemClicked(object item)
		{
			ScreensManager.SwitchScreen("BestiaryDescription",item,m_creaturesList.Items.Cast<BestiaryCreatureInfo>().ToList());
		}
		public BestiaryScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/BestiaryScreen");
			LoadContents(this, node);
			m_creaturesList = Children.Find<ListPanelWidget>("CreaturesList");
			m_creaturesList.ItemWidgetFactory = delegate (object item)
			{
				var bestiaryCreatureInfo2 = (BestiaryCreatureInfo)item;
				XElement node2 = ContentManager.Get<XElement>("Widgets/BestiaryItem");
				var obj = (ContainerWidget)LoadWidget(this, node2, null);
				ModelWidget modelWidget = obj.Children.Find<ModelWidget>("BestiaryItem.Model");
				SetupBestiaryModelWidget(bestiaryCreatureInfo2, modelWidget, (m_creaturesList.Items.IndexOf(item) % 2 == 0) ? new Vector3(-1f, 0f, -1f) : new Vector3(1f, 0f, -1f), autoRotate: false, autoAspect: false);
				obj.Children.Find<LabelWidget>("BestiaryItem.Text").Text = bestiaryCreatureInfo2.DisplayName;
				obj.Children.Find<LabelWidget>("BestiaryItem.Details").Text = bestiaryCreatureInfo2.Description;
				ModsManager.HookAction("LoadCreatureInfoInBestiaryScreen", loader =>
				{
					loader.LoadCreatureInfoInBestiaryScreen(this, obj, bestiaryCreatureInfo2, bestiaryCreatureInfo2.EntityValuesDictionary);
					return false;
				});
				return obj;
			};
			m_creaturesList.ItemClicked += OnCreaturesListItemClicked;
			var list = new List<BestiaryCreatureInfo>();
			foreach (ValuesDictionary entitiesValuesDictionary in DatabaseManager.EntitiesValuesDictionaries)
			{
				ValuesDictionary valuesDictionary = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentCreature));
				if (valuesDictionary != null)
				{
					string value = valuesDictionary.GetValue<string>("DisplayName");
					if (value.StartsWith("[") && value.EndsWith("]"))
					{
						string[] lp = value.Substring(1, value.Length - 2).Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
						value = LanguageControl.GetDatabase("DisplayName", lp[1]);
					}
					if (!string.IsNullOrEmpty(value))
					{
						int order = -1;
						ValuesDictionary value2 = entitiesValuesDictionary.GetValue<ValuesDictionary>("CreatureEggData", null);
						ValuesDictionary value3 = entitiesValuesDictionary.GetValue<ValuesDictionary>("Player", null);
						if (value2 != null || value3 != null)
						{
							if (value2 != null)
							{
								int value4 = value2.GetValue<int>("EggTypeIndex");
								if (value4 < 0)
								{
									continue;
								}
								order = value4;
							}
							ValuesDictionary valuesDictionary2 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentCreatureModel));
							ValuesDictionary valuesDictionary3 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentBody));
							ValuesDictionary valuesDictionary4 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentHealth));
							ValuesDictionary valuesDictionary5 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentMiner));
							ValuesDictionary valuesDictionary6 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentLocomotion));
							ValuesDictionary valuesDictionary7 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentHerdBehavior));
							ValuesDictionary valuesDictionary8 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentMount));
							ValuesDictionary valuesDictionary9 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentLoot));
							string dy = valuesDictionary.GetValue<string>("Description");
							if (dy.StartsWith("[") && dy.EndsWith("]"))
							{
								string[] lp = dy.Substring(1, dy.Length - 2).Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
								dy = LanguageControl.GetDatabase("Description", lp[1]);
							}
							var bestiaryCreatureInfo = new BestiaryCreatureInfo
							{
								EntityValuesDictionary = entitiesValuesDictionary,
								Order = order,
								DisplayName = value,
								Description = dy,
								ModelName = valuesDictionary2.GetValue<string>("ModelName"),
								TextureOverride = valuesDictionary2.GetValue<string>("TextureOverride"),
								Mass = valuesDictionary3.GetValue<float>("Mass"),
								AttackResilience = valuesDictionary4.GetValue<float>("AttackResilience"),
								AttackPower = valuesDictionary5?.GetValue<float>("AttackPower") ?? 0f,
								MovementSpeed = MathUtils.Max(valuesDictionary6.GetValue<float>("WalkSpeed"), valuesDictionary6.GetValue<float>("FlySpeed"), valuesDictionary6.GetValue<float>("SwimSpeed")),
								JumpHeight = MathUtils.Sqr(valuesDictionary6.GetValue<float>("JumpSpeed")) / 20f,
								IsHerding = valuesDictionary7 != null,
								CanBeRidden = valuesDictionary8 != null,
								HasSpawnerEgg = value2?.GetValue<bool>("ShowEgg") ?? false,
								Loot = (valuesDictionary9 != null) ? ComponentLoot.ParseLootList(valuesDictionary9.GetValue<ValuesDictionary>("Loot")) : []
							};
							if (value3 != null && entitiesValuesDictionary.DatabaseObject.Name.ToLower().Contains("female"))
							{
								bestiaryCreatureInfo.AttackPower *= 0.8f;
								bestiaryCreatureInfo.AttackResilience *= 0.8f;
								bestiaryCreatureInfo.MovementSpeed *= 1.03f;
								bestiaryCreatureInfo.JumpHeight *= MathUtils.Sqr(1.03f);
							}
							list.Add(bestiaryCreatureInfo);
						}
					}
				}
			}
			foreach (BestiaryCreatureInfo item in list.OrderBy((BestiaryCreatureInfo ci) => ci.Order))
			{
				m_creaturesList.AddItem(item);
			}
		}

		public override void Enter(object[] parameters)
		{
			if (ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("BestiaryDescription"))
			{
				m_previousScreen = ScreensManager.PreviousScreen;
			}
			m_creaturesList.SelectedItem = null;
		}

		public override void Update()
		{
			if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(m_previousScreen);
			}
		}

		public static void SetupBestiaryModelWidget(BestiaryCreatureInfo info, ModelWidget modelWidget, Vector3 offset, bool autoRotate, bool autoAspect)
		{
			modelWidget.Model = ContentManager.Get<Model>(info.ModelName);
			modelWidget.TextureOverride = ContentManager.Get<Texture2D>(info.TextureOverride);
			var absoluteTransforms = new Matrix[modelWidget.Model.Bones.Count];
			modelWidget.Model.CopyAbsoluteBoneTransformsTo(absoluteTransforms);
			BoundingBox boundingBox = modelWidget.Model.CalculateAbsoluteBoundingBox(absoluteTransforms);
			float x = MathUtils.Max(boundingBox.Size().X, 1.4f * boundingBox.Size().Y, boundingBox.Size().Z);
			modelWidget.ViewPosition = new Vector3(boundingBox.Center().X, 1.5f, boundingBox.Center().Z) + (2.6f * MathF.Pow(x, 0.75f) * offset);
			modelWidget.ViewTarget = boundingBox.Center();
			modelWidget.ViewFov = 0.3f;
			modelWidget.AutoRotationVector = autoRotate ? new Vector3(0f, Math.Clamp(1.7f / boundingBox.Size().Length(), 0.25f, 1.4f), 0f) : Vector3.Zero;
			if (autoAspect)
			{
				float num = Math.Clamp(boundingBox.Size().XZ.Length() / boundingBox.Size().Y, 1f, 1.5f);
				modelWidget.Size = new Vector2(modelWidget.Size.Y * num, modelWidget.Size.Y);
			}
		}
	}
}
