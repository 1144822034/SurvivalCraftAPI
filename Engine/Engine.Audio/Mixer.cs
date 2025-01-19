using System.Reflection;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace Engine.Audio
{
	public static class Mixer
	{
		private static float m_masterVolume = 1f;

		public static readonly List<Sound> m_soundsToStop = [];

		public static HashSet<Sound> m_soundsToStopPoll = [];

		public static AudioContext m_audioContext;

		public static bool m_isInitialized;

		public static float MasterVolume
		{
			get
			{
				return m_masterVolume;
			}
			set
			{
				value = MathUtils.Saturate(value);
				if (value != m_masterVolume)
				{
					m_masterVolume = value;
					InternalSetMasterVolume(value);
				}
			}
		}
		internal static void Initialize()
		{
#if !ANDROID
			//直接加载
			string fullPath = Path.GetDirectoryName(RunPath.GetExecutablePath() == ""? RunPath.GetEntryPath(): RunPath.GetExecutablePath());//路径备选方案
			Environment.SetEnvironmentVariable("PATH", fullPath + ";" + RunPath.GetEnvironmentPath(), EnvironmentVariableTarget.Process);
#endif
			m_audioContext = new AudioContext();
			if (!CheckALErrorFull())
			{
				m_isInitialized = true;
			}
		}

		
		internal static void Dispose()
		{
			m_isInitialized = false;
			m_audioContext?.Dispose();
		}

		internal static void BeforeFrame()
		{
			foreach (Sound item in m_soundsToStopPoll)
			{
				if (item.m_source != 0 && item.State == SoundState.Playing && AL.GetSourceState(item.m_source) == ALSourceState.Stopped)
				{
					m_soundsToStop.Add(item);
				}
			}
			foreach (Sound item2 in m_soundsToStop)
			{
				item2.Stop();
			}
			m_soundsToStop.Clear();
		}

		internal static void AfterFrame()
		{
		}

		internal static void InternalSetMasterVolume(float volume)
		{
			if (m_isInitialized)
			{
				AL.Listener(ALListenerf.Gain, volume);
			}
		}
		/*
		internal static void CheckALError()
		{
			ALError error = AL.GetError();
			//if (error != 0)
			//{
			//	throw new InvalidOperationException(AL.GetErrorString(error));
			//}
		}*/
		
		public static ALError CheckALError()
        {
            ALError error = AL.GetError();
			if (error != ALError.NoError)
			{
				Log.Error("OPENAL出错! " + error.ToString());
			}
			return error;
        }
        /// <summary>
        /// 完整检查 OpenAL 是否可用和有无问题
        /// </summary>
        /// <returns>如果出错返回真</returns>
		public static bool CheckALErrorFull()//注意返回值为是否出错
		{
			try
			{
				ALError error = AL.GetError();
				if (error != ALError.NoError)
				{
					Log.Error("OPENAL出错! " + error.ToString());
					//throw new InvalidOperationException(AL.GetErrorString(error));
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception e)
			{
				Log.Error("OPENAL无法调用 " + e.ToString());
				return true;
			}
		}
	}
}
