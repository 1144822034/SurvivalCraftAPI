using System.Xml.Linq;
using Engine;
using Game;
using GameEntitySystem;

public class GameWidget : CanvasWidget
{
    readonly List<Camera> m_cameras = new List<Camera>();
	readonly Dictionary<Camera,Func<GameWidget,bool>> m_isCameraEnable = new Dictionary<Camera, Func<GameWidget, bool>>();

    public Camera m_activeCamera;

	public ViewWidget ViewWidget { get; set; }

	public ContainerWidget GuiWidget { get; set; }

	public int GameWidgetIndex { get; set; }

	public SubsystemGameWidgets SubsystemGameWidgets { get; set; }

	public PlayerData PlayerData { get; set; }

	public ReadOnlyList<Camera> Cameras => new ReadOnlyList<Camera>(m_cameras);

	public Camera ActiveCamera
	{
		get => m_activeCamera;
		set
		{
			if (value == null || value.GameWidget != this)
			{
				throw new InvalidOperationException("Invalid camera.");
			}
			if (!IsCameraAllowed(value))
			{
				value = FindCamera<FppCamera>();
			}
			if (value != m_activeCamera)
			{
				Camera activeCamera = m_activeCamera;
				m_activeCamera = value;
				m_activeCamera.Activate(activeCamera);
			}
		}
	}

	public ComponentCreature Target { get; set; }

	public GameWidget(PlayerData playerData, int gameViewIndex)
	{
		PlayerData = playerData;
		GameWidgetIndex = gameViewIndex;
		SubsystemGameWidgets = playerData.SubsystemGameWidgets;
		LoadContents(this, ContentManager.Get<XElement>("Widgets/GameWidget"));
		ViewWidget = Children.Find<ViewWidget>("View");
		GuiWidget = Children.Find<ContainerWidget>("Gui");
		AddCamera(new FppCamera(this));
		AddCamera(new DeathCamera(this));
		AddCamera(new IntroCamera(this));
		AddCamera(new TppCamera(this));
		AddCamera(new OrbitCamera(this));
		AddCamera(new FixedCamera(this));
		AddCamera(new LoadingCamera(this));
		ModsManager.HookAction("ManageCameras",modLoader =>
		{
			modLoader.ManageCameras(this);
			return false;
		});
		for(int i = SettingsManager.CameraManageSettings.Count - 1; i >= 0; i--)
		{//若保存的设置中存在无法读取的类(可能是卸载模组导致)，将其移除
			string key = SettingsManager.CameraManageSettings.Keys.ToList()[i];
			if(Type.GetType(key,throwOnError: false) == null)
				SettingsManager.CameraManageSettings.Remove(key);
		}
		foreach(var item in SettingsManager.CameraManageSettings)
		{//若保存的设置中存在某个摄像机而GameWidget中没有，创建一个新的并添加到GameWidget
			Type type = Type.GetType(item.Key);
			if(FindCamera(type,throwOnError: false) == null)
			{
				try
				{
					AddCamera((Camera)Activator.CreateInstance(type, this));
				}
				catch(Exception e)
				{
					Log.Warning($"Cannot create camera in GameWidget from saved settings. Reason:{e}");
				}
			}
		}
		m_activeCamera = FindCamera<LoadingCamera>();
	}

	public T FindCamera<T>(bool throwOnError = true) where T : Camera
	{
		T val = (T)m_cameras.FirstOrDefault((Camera c) => c is T);
		if (val != null || !throwOnError)
		{
			return val;
		}
		throw new InvalidOperationException($"Camera with type \"{typeof(T).Name}\" not found.");
	}

	public Camera FindCamera(Type type, bool throwOnError = true)
	{
		var val = m_cameras.FirstOrDefault((Camera c) => c.GetType() == type);
		if (val != null || !throwOnError)
		{
			return val;
		}
		throw new InvalidOperationException($"Camera with type \"{type.Name}\" not found.");
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="type"></param>
	/// <param name="isEnable">用于判定当前摄像机是否可用，比如在非创造模式中调试视角不可用</param>
	/// <param name="throwOnError"></param>
	/// <returns></returns>
	public Camera FindCamera(Type type, out bool isEnable, bool throwOnError = true)
	{
		isEnable = true;
		var result = FindCamera(type,throwOnError);
		if(m_isCameraEnable.TryGetValue(result,out Func<GameWidget,bool> func))
			isEnable = func?.Invoke(this) ?? true;
		return result;
	}
	/// <summary>
	/// 此方法建议在ModLoader.ManageCameras接口中使用，避免重复添加。若无需结合条件判断摄像机是否可用(比如调试视角仅创造模式可用)，则isEnable可传null
	/// </summary>
	/// <param name="camera"></param>
	/// <param name="isEnable">在Func中进行判断，若输出false，则表示该摄像机目前不可用</param>
	public void AddCamera(Camera camera, Func<GameWidget,bool> isEnable = null)
	{
		if(camera == null)
			return;
		m_cameras.Add(camera);
		if(isEnable != null)
			m_isCameraEnable.Add(camera,isEnable);
	}
	public bool IsEntityTarget(Entity entity)
	{
		if (Target != null)
		{
			return Target.Entity == entity;
		}
		return false;
	}

	public bool IsEntityFirstPersonTarget(Entity entity)
	{
		if (IsEntityTarget(entity))
		{
			return ActiveCamera is FppCamera;
		}
		return false;
	}

	public override void Update()
	{
		WidgetInputDevice widgetInputDevice = DetermineInputDevices();
		if (WidgetsHierarchyInput == null || WidgetsHierarchyInput.Devices != widgetInputDevice)
		{
			WidgetsHierarchyInput = new WidgetInput(widgetInputDevice);
		}
		if ((widgetInputDevice & WidgetInputDevice.MultiMice) != 0 && (widgetInputDevice & WidgetInputDevice.Mouse) == 0)
		{
			WidgetsHierarchyInput.UseSoftMouseCursor = true;
		}
		else
		{
			WidgetsHierarchyInput.UseSoftMouseCursor = false;
		}
		if (GuiWidget.ParentWidget == null)
		{
			UpdateWidgetsHierarchy(GuiWidget);
		}
	}

    public WidgetInputDevice DetermineInputDevices()
	{
		bool flag = false;
		foreach (PlayerData playersDatum in PlayerData.SubsystemPlayers.PlayersData)
		{
			if ((playersDatum.InputDevice & WidgetInputDevice.MultiMice) != 0)
			{
				flag = true;
			}
		}
		WidgetInputDevice widgetInputDevice = WidgetInputDevice.None;
		foreach (WidgetInputDevice allInputDevice in PlayerScreen.AllInputDevices)
		{
			if (!flag || allInputDevice != (WidgetInputDevice.Keyboard | WidgetInputDevice.Mouse))
			{
				widgetInputDevice |= allInputDevice;
			}
		}
		if (PlayerData.SubsystemPlayers.PlayersData.Count > 0 && PlayerData == PlayerData.SubsystemPlayers.PlayersData[0])
		{
			WidgetInputDevice widgetInputDevice2 = WidgetInputDevice.None;
			foreach (PlayerData playersDatum2 in PlayerData.SubsystemPlayers.PlayersData)
			{
				if (playersDatum2 != PlayerData)
				{
					widgetInputDevice2 |= playersDatum2.InputDevice;
				}
			}
			return (widgetInputDevice & ~widgetInputDevice2) | WidgetInputDevice.Touch | PlayerData.InputDevice;
		}
		WidgetInputDevice widgetInputDevice3 = WidgetInputDevice.None;
		foreach (PlayerData playersDatum3 in PlayerData.SubsystemPlayers.PlayersData)
		{
			if (playersDatum3 == PlayerData)
			{
				break;
			}
			widgetInputDevice3 |= playersDatum3.InputDevice;
		}
		return (PlayerData.InputDevice & ~widgetInputDevice3) | WidgetInputDevice.Touch;
	}

	public bool IsCameraAllowed(Camera camera)
	{
		if (camera is LoadingCamera)
		{
			return false;
		}
		return true;
	}
}
