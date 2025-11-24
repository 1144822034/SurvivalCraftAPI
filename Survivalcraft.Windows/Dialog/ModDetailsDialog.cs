using System.Xml.Linq;
using Engine;
using NuGet.Versioning;

namespace Game {
    public class ModDetailsDialog : Dialog {
        public ModsManageContentScreen m_screen;
        public ModEntity m_entity;

        public LabelWidget m_titleLabel;
        public StackPanelWidget m_contentPanel;
        public ButtonWidget m_triggerEnableButton;
        public ButtonWidget m_deleteButton;
        public ButtonWidget m_closeButton;

        public const string fName = "ModDetailsDialog";

        public ModDetailsDialog(ModsManageContentScreen screen, ModEntity entity) {
            m_screen = screen;
            m_entity = entity;
            XElement node = ContentManager.Get<XElement>("Dialogs/ModDetailsDialog");
            LoadContents(this, node);
            m_titleLabel = Children.Find<LabelWidget>("ModDetailsDialog.Title");
            m_contentPanel = Children.Find<StackPanelWidget>("ModDetailsDialog.Content");
            m_triggerEnableButton = Children.Find<ButtonWidget>("ModDetailsDialog.TriggerEnable");
            m_deleteButton = Children.Find<ButtonWidget>("ModDetailsDialog.Delete");
            m_closeButton = Children.Find<ButtonWidget>("ModDetailsDialog.Close");
            Vector2 margin = new Vector2(0f, 4f);
            if (entity.IsDisabled) {
                m_contentPanel.Children.Add(
                    new LabelWidget {
                        Text = $"{LanguageControl.Get(fName, "1")}{LanguageControl.Get("ModDisableReason", entity.DisableReason.ToString())}",
                        Margin = margin
                    }
                );
            }
            ModInfo modInfo = entity.modInfo;
            if (modInfo == null) {
                Size = new Vector2(600f, 250f);
            }
            else {
                m_contentPanel.Children.Add(
                    new LabelWidget {
                        Text = $"{LanguageControl.Get(fName, "2")}{(string.IsNullOrEmpty(modInfo.Version) ? LanguageControl.None : modInfo.Version)}",
                        Margin = margin
                    }
                );
                m_contentPanel.Children.Add(
                    new LabelWidget {
                        Text =
                            $"{LanguageControl.Get(fName, "3")}{(string.IsNullOrEmpty(modInfo.ApiVersion) ? LanguageControl.Unknown : modInfo.ApiVersion)}",
                        Margin = margin
                    }
                );
                m_contentPanel.Children.Add(
                    new LabelWidget {
                        Text =
                            $"{LanguageControl.Get(fName, "4")}{(string.IsNullOrEmpty(modInfo.Author) ? LanguageControl.Unknown : modInfo.Author)}",
                        WordWrap = true,
                        Margin = margin
                    }
                );
                m_contentPanel.Children.Add(
                    new LabelWidget { Text = $"{LanguageControl.Get(fName, "5")}{DataSizeFormatter.Format(entity.Size)}", Margin = margin }
                );
                m_contentPanel.Children.Add(
                    new LabelWidget {
                        Text =
                            $"{LanguageControl.Get(fName, "6")}{(string.IsNullOrEmpty(modInfo.Description) ? LanguageControl.None : modInfo.Description)}",
                        WordWrap = true,
                        Margin = margin
                    }
                );
                if (string.IsNullOrEmpty(modInfo.Link)) {
                    m_contentPanel.Children.Add(
                        new LabelWidget { Text = $"{LanguageControl.Get(fName, "7")}{LanguageControl.None}", Margin = margin }
                    );
                }
                else {
                    m_contentPanel.Children.Add(
                        new StackPanelWidget {
                            Direction = LayoutDirection.Horizontal,
                            Children = {
                                new LabelWidget { Text = LanguageControl.Get(fName, "7") },
                                new LinkWidget { Text = modInfo.Link, Url = modInfo.Link, Color = new Color(64, 192, 64) }
                            },
                            Margin = margin
                        }
                    );
                }
                if (!string.IsNullOrEmpty(entity.ModFilePath)) {
                    m_contentPanel.Children.Add(
                        new LabelWidget {
                            Text = $"{LanguageControl.Get(fName, "8")}{Storage.GetFileName(entity.ModFilePath)}", WordWrap = true, Margin = margin
                        }
                    );
                }
                m_contentPanel.Children.Add(new LabelWidget { Text = $"{LanguageControl.Get(fName, "9")}{modInfo.PackageName}", WordWrap = true });
                if (modInfo.DependencyRanges.Count > 0) {
                    m_contentPanel.Children.Add(new CanvasWidget { Size = new Vector2(0f, 8f) });
                    m_contentPanel.Children.Add(new LabelWidget { Text = LanguageControl.Get(fName, "10") });
                    foreach (KeyValuePair<string, VersionRange> dependency in modInfo.DependencyRanges) {
                        m_contentPanel.Children.Add(new LabelWidget { Text = $"    - {dependency.Key} {dependency.Value}" });
                    }
                }
            }
            m_triggerEnableButton.IsEnabled = entity.IsDisabled
                ? entity.DisableReason == ModDisableReason.Manually
                : entity.modInfo?.PackageName is not "survivalcraft" and not "fastdebug";
            m_deleteButton.IsEnabled = entity.modInfo?.PackageName is not "survivalcraft" and not "fastdebug"
                && !string.IsNullOrEmpty(entity.ModFilePath);
            UpdateTitleAndTriggerEnableButton();
        }

        public override void Update() {
            if (m_triggerEnableButton.IsClicked) {
                m_screen.TriggerEnable(m_entity);
                UpdateTitleAndTriggerEnableButton();
            }
            if (m_deleteButton.IsClicked) {
                DialogsManager.ShowDialog(
                    null,
                    new MessageDialog(
                        LanguageControl.Get(fName, "11"),
                        string.Format(LanguageControl.Get(fName, "12"), m_entity.ModFilePath),
                        LanguageControl.Yes,
                        LanguageControl.No,
                        button => {
                            if (button == MessageDialogButton.Button1) {
                                try {
                                    Storage.DeleteFile(m_entity.ModFilePath);
                                    m_screen.m_needRestart = true;
                                    ModsManager.ModListAll.Remove(m_entity);
                                    ModsManager.ModList.Remove(m_entity);
                                    if (m_entity.modInfo != null
                                        && ModsManager.DisabledMods.TryGetValue(m_entity.modInfo.PackageName, out HashSet<string> versions)) {
                                        versions.Remove(m_entity.modInfo.Version);
                                        if (versions.Count == 0) {
                                            ModsManager.DisabledMods.Remove(m_entity.modInfo.PackageName);
                                        }
                                    }
                                    m_screen.m_modsContentList.RemoveItem(m_entity);
                                    DialogsManager.HideDialog(this);
                                    DialogsManager.ShowDialog(
                                        null,
                                        new MessageDialog(
                                            LanguageControl.Success,
                                            string.Format(LanguageControl.Get(fName, "14"), m_entity.ModFilePath),
                                            LanguageControl.Ok,
                                            null,
                                            null
                                        )
                                    );
                                }
                                catch (Exception e) {
                                    string str = string.Format(LanguageControl.Get(fName, "13"), m_entity.ModFilePath, e.Message);
                                    Log.Error(str);
                                    DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, str, LanguageControl.Ok, null, null));
                                }
                            }
                        }
                    )
                );
            }
            if (Input.Back
                || Input.Cancel
                || m_closeButton.IsClicked) {
                DialogsManager.HideDialog(this);
            }
        }

        public void UpdateTitleAndTriggerEnableButton() {
            bool canBeEnabled = ModsManageContentScreen.GetTriggerAndTitle(m_entity, out string title, out Color titleColor);
            m_titleLabel.Text = title;
            m_titleLabel.Color = titleColor;
            m_triggerEnableButton.Text = LanguageControl.Get("ModsManageContentScreen", canBeEnabled ? "19" : "18");
        }
    }
}