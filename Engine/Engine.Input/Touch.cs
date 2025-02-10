#if ANDROID

using System.Collections.Concurrent;
using Android.Views;

#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Input
{
	public static class Touch
	{
        public struct TouchInfo
        {
            public int PointerId;
            public Vector2 Position;
            public int ActionMasked;//1: down, 2: move, 3: up
            public TouchInfo (int pointerId, Vector2 position, int actionMasked)
            {
                PointerId = pointerId;
                Position = position;
                ActionMasked = actionMasked;
            }
        }
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
        public static ConcurrentQueue<TouchInfo> m_cachedTouchEvents = [];
		internal static void HandleTouchEvent(MotionEvent e)
        {
            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Pointer1Down:
                    m_cachedTouchEvents.Enqueue(new TouchInfo(e.GetPointerId(e.ActionIndex), new Vector2(e.GetX(e.ActionIndex), e.GetY(e.ActionIndex)), 1));
                    break;
                case MotionEventActions.Move:
                    for (int i = 0; i < e.PointerCount; i++)
                    {
                        m_cachedTouchEvents.Enqueue(new TouchInfo(e.GetPointerId(i), new Vector2(e.GetX(i), e.GetY(i)), 2));
                    }
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Pointer1Up:
                case MotionEventActions.Cancel:
                case MotionEventActions.Outside:
                    m_cachedTouchEvents.Enqueue(new TouchInfo(e.GetPointerId(e.ActionIndex), new Vector2(e.GetX(e.ActionIndex), e.GetY(e.ActionIndex)), 3));
                    break;
                default:
                    return;
            }
		}

#endif

		public static void Clear()
		{
			m_touchLocations.Clear();
		}

		internal static void BeforeFrame()
		{
#if ANDROID
            while (!m_cachedTouchEvents.IsEmpty)
            {
                if (m_cachedTouchEvents.TryDequeue(out TouchInfo touchInfo))
                {
                    switch (touchInfo.ActionMasked)
                    {
                        case 1: ProcessTouchPressed(touchInfo.PointerId, touchInfo.Position); break;
                        case 2: ProcessTouchMoved(touchInfo.PointerId, touchInfo.Position); break;
                        case 3: ProcessTouchReleased(touchInfo.PointerId, touchInfo.Position); break;
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