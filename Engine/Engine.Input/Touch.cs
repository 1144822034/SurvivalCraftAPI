#if ANDROID

using Android.Views;

#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Input
{
	public static class Touch
	{
		private static List<TouchLocation> m_touchLocations = [];

		public static ReadOnlyList<TouchLocation> TouchLocations => new(m_touchLocations);

		public static event Action<TouchLocation> TouchPressed;

		public static event Action<TouchLocation> TouchReleased;

		public static event Action<TouchLocation> TouchMoved;

		internal static void Initialize()
		{
		}

		internal static void Dispose()
		{
		}

#if ANDROID

		internal static void HandleTouchEvent(MotionEvent e)
		{
			if (e.ActionMasked == MotionEventActions.Down || e.ActionMasked == MotionEventActions.Pointer1Down)
			{
				int pointerId = e.GetPointerId(e.ActionIndex);
				float x = e.GetX(e.ActionIndex);
				float y = e.GetY(e.ActionIndex);
				ProcessTouchPressed(pointerId, new Vector2(x, y));
			}
			else if (e.ActionMasked == MotionEventActions.Move)
			{
				for (int i = 0; i < e.PointerCount; i++)
				{
					int pointerId2 = e.GetPointerId(i);
					float x2 = e.GetX(i);
					float y2 = e.GetY(i);
					ProcessTouchMoved(pointerId2, new Vector2(x2, y2));
				}
			}
			else if (e.ActionMasked == MotionEventActions.Up || e.ActionMasked == MotionEventActions.Pointer1Up || e.ActionMasked == MotionEventActions.Cancel || e.ActionMasked == MotionEventActions.Outside)
			{
				int pointerId3 = e.GetPointerId(e.ActionIndex);
				float x3 = e.GetX(e.ActionIndex);
				float y3 = e.GetY(e.ActionIndex);
				ProcessTouchReleased(pointerId3, new Vector2(x3, y3));
			}
		}

#endif

		public static void Clear()
		{
			m_touchLocations.Clear();
		}

		internal static void BeforeFrame()
		{
		}

		internal static void AfterFrame()
		{
			int num = 0;
			while (num < m_touchLocations.Count)
			{
				if (m_touchLocations[num].State == TouchLocationState.Released)
				{
					m_touchLocations.RemoveAt(num);
					continue;
				}
				TouchLocation value;
				if (m_touchLocations[num].ReleaseQueued)
				{
					List<TouchLocation> touchLocations = m_touchLocations;
					int index = num;
					value = new TouchLocation
					{
						Id = m_touchLocations[num].Id,
						Position = m_touchLocations[num].Position,
						State = TouchLocationState.Released
					};
					touchLocations[index] = value;
				}
				else if (m_touchLocations[num].State == TouchLocationState.Pressed)
				{
					List<TouchLocation> touchLocations2 = m_touchLocations;
					int index2 = num;
					value = new TouchLocation
					{
						Id = m_touchLocations[num].Id,
						Position = m_touchLocations[num].Position,
						State = TouchLocationState.Moved
					};
					touchLocations2[index2] = value;
				}
				num++;
			}
		}

		private static int FindTouchLocationIndex(int id)
		{
			for (int i = 0; i < m_touchLocations.Count; i++)
			{
				if (m_touchLocations[i].Id == id)
				{
					return i;
				}
			}
			return -1;
		}

        public static void ProcessTouchPressed(int id, Vector2 position)
		{
			ProcessTouchMoved(id, position);
		}

        public static void ProcessTouchMoved(int id, Vector2 position)
		{
			if (!Window.IsActive || Keyboard.IsKeyboardVisible)
			{
				return;
			}
			int num = FindTouchLocationIndex(id);
			TouchLocation touchLocation;
			if (num >= 0)
			{
				if (m_touchLocations[num].State == TouchLocationState.Moved)
				{
					List<TouchLocation> touchLocations = m_touchLocations;
					touchLocation = new TouchLocation
					{
						Id = id,
						Position = position,
						State = TouchLocationState.Moved
					};
					touchLocations[num] = touchLocation;
				}
				Touch.TouchMoved?.Invoke(m_touchLocations[num]);
			}
			else
			{
				List<TouchLocation> touchLocations2 = m_touchLocations;
				touchLocation = new TouchLocation
				{
					Id = id,
					Position = position,
					State = TouchLocationState.Pressed
				};
				touchLocations2.Add(touchLocation);
				Touch.TouchPressed?.Invoke(m_touchLocations[^1]);
			}
		}

        public static void ProcessTouchReleased(int id, Vector2 position)
		{
			if (!Window.IsActive || Keyboard.IsKeyboardVisible)
			{
				return;
			}
			int num = FindTouchLocationIndex(id);
			if (num >= 0)
			{
				TouchLocation value;
				if (m_touchLocations[num].State == TouchLocationState.Pressed)
				{
					List<TouchLocation> touchLocations = m_touchLocations;
					value = new TouchLocation
					{
						Id = id,
						Position = position,
						State = TouchLocationState.Pressed,
						ReleaseQueued = true
					};
					touchLocations[num] = value;
				}
				else
				{
					List<TouchLocation> touchLocations2 = m_touchLocations;
					value = new TouchLocation
					{
						Id = id,
						Position = position,
						State = TouchLocationState.Released
					};
					touchLocations2[num] = value;
				}
				Touch.TouchReleased?.Invoke(m_touchLocations[num]);
			}
		}
	}
}