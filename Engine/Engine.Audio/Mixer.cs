using System.Reflection;
using Silk.NET.OpenAL;

namespace Engine.Audio
{
	public static class Mixer
    {
        public static AL AL;
		private static float m_masterVolume = 1f;

		public static readonly List<Sound> m_soundsToStop = [];

		public static HashSet<Sound> m_soundsToStopPoll = [];

		public static ALContext m_audioContext;

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
		internal static unsafe void Initialize()
		{
#if !ANDROID
			//直接加载
			string fullPath = Path.GetDirectoryName(RunPath.GetExecutablePath() == ""? RunPath.GetEntryPath(): RunPath.GetExecutablePath());//路径备选方案
			Environment.SetEnvironmentVariable("PATH", fullPath + ";" + RunPath.GetEnvironmentPath(), EnvironmentVariableTarget.Process);
#endif
			m_audioContext = ALContext.GetApi();
            AL = AL.GetApi();
            Device* device = m_audioContext.OpenDevice("");
            if (device == null)
            {
                Log.Error("Could not create audio device");
                return;
            }
            var c = m_audioContext.CreateContext(device, null);
            m_audioContext.MakeContextCurrent(c);
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
				if (item.m_source != 0 && item.State == SoundState.Playing)
                {
                    AL.GetSourceProperty((uint)item.m_source, GetSourceInteger.SourceState, out int sourceState);
                    if (sourceState == (int)SourceState.Stopped)
                    {
                        m_soundsToStop.Add(item);
                    }
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
                AL.SetListenerProperty(ListenerFloat.Gain, volume);
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
		
		public static AudioError CheckALError()
        {
            AudioError error = AL.GetError();
			if (error != AudioError.NoError)
			{
				Log.Error("OPENAL ERROR: " + error.ToString());
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
				AudioError error = AL.GetError();
				if (error != AudioError.NoError)
				{
					Log.Error("OPENAL ERROR: " + error.ToString());
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
				Log.Error("Unable to load OPENAL: " + e.ToString());
				return true;
			}
		}
	}
}
