using System;
using Engine.Media;
using OpenTK.Audio.OpenAL;

namespace Engine.Audio
{
	public class Sound : BaseSound
	{
        public SoundBuffer m_soundBuffer;

		public SoundBuffer SoundBuffer => m_soundBuffer;

		public override void Dispose()
		{
			base.Dispose();
			if (m_soundBuffer != null)
			{
                m_soundBuffer.UseCount-=1;
                m_soundBuffer = null;
			}
		}

		internal void Initialize(SoundBuffer soundBuffer)
		{
			ArgumentNullException.ThrowIfNull(soundBuffer);
			m_soundBuffer = soundBuffer;
            m_soundBuffer.UseCount+=1;
        }

		public Sound(SoundBuffer soundBuffer, float volume = 1f, float pitch = 1f, float pan = 0f, bool isLooped = false, bool disposeOnStop = false)
		{
			ArgumentNullException.ThrowIfNull(soundBuffer);
            if (Mixer.m_isInitialized)
            {
                AL.Source(m_source, ALSourcei.Buffer, soundBuffer.m_buffer);
                Mixer.CheckALError();
            }
            Initialize(soundBuffer);
			base.ChannelsCount = soundBuffer.ChannelsCount;
			base.SamplingFrequency = soundBuffer.SamplingFrequency;
			base.Volume = volume;
			base.Pitch = pitch;
			base.Pan = pan;
			base.IsLooped = isLooped;
			base.DisposeOnStop = disposeOnStop;
			Mixer.m_soundsToStopPoll.Add(this);
		}

		public Sound(StreamingSource streamingSource, SoundBuffer soundBuffer, float volume = 1f, float pitch = 1f, float pan = 0f, bool isLooped = false, bool disposeOnStop = false)
		{
			ArgumentNullException.ThrowIfNull(soundBuffer);
			AL.Source(m_source, ALSourcei.Buffer, soundBuffer.m_buffer);
			Mixer.CheckALError();
			Initialize(soundBuffer);
			base.ChannelsCount = soundBuffer.ChannelsCount;
			base.SamplingFrequency = soundBuffer.SamplingFrequency;
			base.Volume = volume;
			base.Pitch = pitch;
			base.Pan = pan;
			base.IsLooped = isLooped;
			base.DisposeOnStop = disposeOnStop;
			Mixer.m_soundsToStopPoll.Add(this);
		}
        /// <summary>
        /// 在指定位置播放音频
        /// </summary>
        /// <param name="direction">相对于玩家的相对位置</param>
		internal override void InternalPlay(OpenTK.Vector3 direction)
		{
            if (m_source != 0)
            {
                AL.Source(m_source, ALSource3f.Position, ref direction);
                AL.Source(m_source, ALSourceb.Looping, m_isLooped);
                AL.SourcePlay(m_source);
            }
            Mixer.CheckALError();
		}

		internal override void InternalPause()
		{
            if (m_source != 0)
            {
                AL.SourcePause(m_source);
                Mixer.CheckALError();
            }
        }

		internal override void InternalStop()
		{
            if (m_source != 0)
            {
                AL.SourceRewind(m_source);
                Mixer.CheckALError();
            }
        }

		internal override void InternalDispose()
		{
			base.InternalDispose();
			Mixer.m_soundsToStopPoll.Remove(this);
		}
    }
}
