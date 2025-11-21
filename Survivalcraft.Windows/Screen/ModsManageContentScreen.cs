using System.Text;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Game;
using NuGet.Versioning;

public class ModsManageContentScreen : Screen {
    public static string fName = "ModsManageContentScreen";

    public ListPanelWidget m_modsContentList;
    public ButtonWidget m_viewDetailButton;
    public ButtonWidget m_triggerEnableButton;
    public ButtonWidget m_openHomepageButton;

    public bool m_needRestart;

    public static bool IsOldApiVersionMod(ModEntity modEntity, out string detail) {
        ModInfo modInfo = modEntity.modInfo;
        if (modInfo == null) {
            detail = string.Format(LanguageControl.Get(fName, "68"), LanguageControl.Unknown);
            return true;
        }
        if (modInfo.ApiVersion.StartsWith("1.4")
            || modInfo.ApiVersion.StartsWith("1.5")
            || modInfo.ApiVersion.StartsWith("1.6")
            || modInfo.ApiVersion.StartsWith("1.7")) {
            detail = string.Format(LanguageControl.Get(fName, "68"), modInfo.ApiVersion);
            return true;
        }
        if (modInfo.ApiVersionRange != null) {
            if (!modInfo.ApiVersionRange.Satisfies(ModsManager.APINuGetVersion)) {
                detail = string.Format(LanguageControl.Get(fName, "76"), modInfo.ApiVersion);
                return true;
            }
            if (!modInfo.ApiVersionRange.HasUpperBound
                && modInfo.ApiVersionRange.MinVersion != null
                && modInfo.ApiVersionRange.MinVersion.Major == 1
                && modInfo.ApiVersionRange.MinVersion.Minor <= 7) {
                detail = string.Format(LanguageControl.Get(fName, "68"), modInfo.ApiVersion);
                return true;
            }
        }
        detail = modInfo.Description;
        return false;
    }

    public ModsManageContentScreen() {
        XElement node = ContentManager.Get<XElement>("Screens/ModsManageContentScreen");
        LoadContents(this, node);
        m_modsContentList = Children.Find<ListPanelWidget>("ModsContentList");
        Children.Find<LabelWidget>("TopBar.Label").Text = LanguageControl.Get(fName, "1");
        m_viewDetailButton = Children.Find<ButtonWidget>("ViewDetailButton");
        m_triggerEnableButton = Children.Find<BevelledButtonWidget>("TriggerEnableButton");
        m_openHomepageButton = Children.Find<BevelledButtonWidget>("OpenHomepageButton");
        m_viewDetailButton.IsEnabled = false;
        m_viewDetailButton.Text = LanguageControl.Get(fName, "89");
        m_triggerEnableButton.IsEnabled = false;
        m_triggerEnableButton.Text = LanguageControl.Get(fName, "18");
        m_openHomepageButton.IsEnabled = false;
        m_openHomepageButton.Text = LanguageControl.Get(fName, "77");
        m_modsContentList.ItemWidgetFactory = item => {
            if (item is not ModEntity entity) {
                return null;
            }
            GetTriggerAndTitle(entity, out string title, out Color titleColor);
            ModsManageContentItemWidget result = new() { Title = title, IsDisabled = entity.IsDisabled, TitleColor = titleColor };
            if (IsOldApiVersionMod(entity, out string detail)) {
                result.TitleColor = Color.Red;
                if (entity.modInfo == null) {
                    result.IsInformationVisible = false;
                }
            }
            else {
                result.Information =
                    $"{LanguageControl.Get(fName, "79")}{entity.modInfo!.Version}  {LanguageControl.Get(fName, "80")}{entity.modInfo.ApiVersion}  {LanguageControl.Get(fName, "81")}{entity.modInfo.Author}  {LanguageControl.Get(fName, "82")}{DataSizeFormatter.Format(entity.Size, 2)}";
            }
            if (entity.Icon != null) {
                result.Icon = entity.Icon;
            }
            result.Detail = detail;
            return result;
        };
        m_modsContentList.ItemClicked += item => {
            if (item is not ModEntity entity) {
                return;
            }
            m_viewDetailButton.IsEnabled = true;
            m_triggerEnableButton.IsEnabled = entity.IsDisabled
                ? entity.DisableReason == ModDisableReason.Manually
                : entity.modInfo?.PackageName is not "survivalcraft" and not "fastdebug";
            m_triggerEnableButton.Text = LanguageControl.Get(fName, GetTrigger(entity) ? "19" : "18");
            m_openHomepageButton.IsEnabled = true;
            if (ReferenceEquals(entity, m_modsContentList.SelectedItem)) {
                ViewDetail(entity);
            }
        };
    }

    public override void Enter(object[] parameters) {
        m_modsContentList.AddItems(ModsManager.ModListAll);
    }

    public override void Leave() {
        m_modsContentList.ClearItems();
        m_viewDetailButton.IsEnabled = false;
        m_triggerEnableButton.IsEnabled = false;
        m_openHomepageButton.IsEnabled = false;
    }

    public override void Update() {
        if (m_modsContentList.SelectedItem is ModEntity entity) {
            if (m_viewDetailButton.IsClicked) {
                ViewDetail(entity);
            }
            if (m_triggerEnableButton.IsClicked) {
                if (entity.IsDisabled) {
                    if (entity.DisableReason == ModDisableReason.Manually) {
                        if (ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions)) {
                            if (!versions.Remove(entity.modInfo.Version)) {
                                versions.Add(entity.modInfo.Version);
                            }
                        }
                        else {
                            ModsManager.DisabledMods.Add(entity.modInfo.PackageName, [entity.modInfo.Version]);
                        }
                        m_needRestart = true;
                    }
                }
                else if (entity.modInfo!.PackageName is not "survivalcraft" and not "fastdebug") {
                    if (ModsManager.DisabledMods.TryGetValue(entity.modInfo.PackageName, out HashSet<string> versions)) {
                        if (!versions.Remove(entity.modInfo.Version)) {
                            versions.Add(entity.modInfo.Version);
                        }
                    }
                    else {
                        ModsManager.DisabledMods.Add(entity.modInfo.PackageName, [entity.modInfo.Version]);
                    }
                    m_needRestart = true;
                }
                if (m_modsContentList.m_widgetsByIndex[m_modsContentList.SelectedIndex!.Value] is ModsManageContentItemWidget itemWidget) {
                    m_triggerEnableButton.Text = LanguageControl.Get(
                        fName,
                        GetTriggerAndTitle(entity, out string title, out Color titleColor) ? "19" : "18"
                    );
                    itemWidget.Title = title;
                    itemWidget.TitleColor = titleColor;
                }
            }
            if (m_openHomepageButton.IsClicked) {
                if (string.IsNullOrEmpty(entity.modInfo?.Link)) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(LanguageControl.Error, LanguageControl.Get(fName, 78), LanguageControl.Ok, null, null)
                    );
                }
                else {
                    WebBrowserManager.LaunchBrowser(entity.modInfo.Link);
                }
            }
        }
        if (Input.Back
            || Input.Cancel
            || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
            if (m_needRestart) {
                DialogsManager.ShowDialog(
                    null,
                    new MessageDialog(
                        LanguageControl.Get(fName, 4),
                        LanguageControl.Get(fName, 38),
                        LanguageControl.Get(fName, 39),
                        LanguageControl.Get(fName, 31),
                        delegate(MessageDialogButton result) {
                            if (result == MessageDialogButton.Button1) {
                                SettingsManager.SaveSettings();
                                Environment.Exit(0);
                            }
                            if (result == MessageDialogButton.Button2) {
                                ScreensManager.SwitchScreen("Content");
                            }
                        }
                    )
                );
            }
            else {
                ScreensManager.SwitchScreen("Content");
            }
        }
    }

    /// <returns>true: mod能被启用，false：mod能被禁用</returns>
    public bool GetTriggerAndTitle(ModEntity entity, out string title, out Color titleColor) {
        title = entity.modInfo?.Name ?? Storage.GetFileName(entity.ModFilePath);
        titleColor = Color.White;
        if (entity.IsDisabled) {
            if (entity.DisableReason == ModDisableReason.Manually) {
                if (ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions1)
                    && versions1.Contains(entity.modInfo.Version)) {
                    title = $"[{LanguageControl.Get(fName, "22")}] {title}";
                    titleColor = Color.Red;
                    return true;
                }
                else {
                    title = $"[{LanguageControl.Get(fName, "23")} {LanguageControl.Get(fName, "90")}] {title}";
                    titleColor = Color.Yellow;
                    return false;
                }
            }
            title = $"[{LanguageControl.Get(fName, "22")}] {title}";
            titleColor = Color.Red;
            return true;
        }
        if (entity.modInfo != null
            && ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions2)
            && versions2.Contains(entity.modInfo.Version)) {
            title = $"[{LanguageControl.Get(fName, "22")} {LanguageControl.Get(fName, "90")}] {title}";
            titleColor = Color.Yellow;
            return true;
        }
        return false;
    }

    /// <returns>true: mod能被启用，false：mod能被禁用</returns>
    public bool GetTrigger(ModEntity entity) {
        if (entity.IsDisabled) {
            if (entity.DisableReason == ModDisableReason.Manually) {
                return ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions1)
                    && versions1.Contains(entity.modInfo.Version);
            }
            return true;
        }
        return entity.modInfo != null
            && ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions2)
            && versions2.Contains(entity.modInfo.Version);
    }

    public void ViewDetail(ModEntity entity) {
        StringBuilder sb = new();
        if (entity.IsDisabled) {
            sb.AppendLine($"{LanguageControl.Get(fName, "88")}{LanguageControl.Get("ModDisableReason", entity.DisableReason.ToString())}");
        }
        ModInfo modInfo = entity.modInfo;
        if (modInfo != null) {
            sb.AppendLine($"{LanguageControl.Get(fName, "79")}{(string.IsNullOrEmpty(modInfo.Version) ? LanguageControl.None : modInfo.Version)}");
            sb.AppendLine($"{LanguageControl.Get(fName, "80")}{(string.IsNullOrEmpty(modInfo.ApiVersion) ? LanguageControl.Unknown : modInfo.ApiVersion)}");
            sb.AppendLine($"{LanguageControl.Get(fName, "81")}{(string.IsNullOrEmpty(modInfo.Author) ? LanguageControl.Unknown : modInfo.Author)}");
            sb.AppendLine($"{LanguageControl.Get(fName, "82")}{entity.Size}");
            sb.AppendLine($"{LanguageControl.Get(fName, "83")}{(string.IsNullOrEmpty(modInfo.Description) ? LanguageControl.None : modInfo.Description)}");
            sb.AppendLine($"{LanguageControl.Get(fName, "84")}{(string.IsNullOrEmpty(modInfo.Link) ? LanguageControl.None : modInfo.Link)}");
            if (!string.IsNullOrEmpty(entity.ModFilePath)) {
                sb.AppendLine($"{LanguageControl.Get(fName, "85")}{Storage.GetFileName(entity.ModFilePath)}");
            }
            sb.AppendLine($"{LanguageControl.Get(fName, "86")}{modInfo.PackageName}");
            if (modInfo.DependencyRanges.Count > 0) {
                sb.AppendLine(LanguageControl.Get(fName, "87"));
                foreach (KeyValuePair<string, VersionRange> dependency in modInfo.DependencyRanges) {
                    sb.AppendLine($"  {dependency.Key} {dependency.Value}");
                }
            }
        }
        DialogsManager.ShowDialog(
            null,
            new MessageDialog(
                $"{(entity.IsDisabled ? $"[{LanguageControl.Get(fName, "22")}] " : "")}{entity.modInfo?.Name ?? Storage.GetFileName(entity.ModFilePath)}",
                sb.ToString(),
                LanguageControl.Ok,
                null,
                null
            )
        );
    }
}