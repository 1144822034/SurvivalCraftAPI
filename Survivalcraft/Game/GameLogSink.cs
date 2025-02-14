using Engine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Game
{
	public class GameLogSink : ILogSink
	{
		public static Stream m_stream;

		public static StreamWriter m_writer;

		public const string fName = "GameLogSink";

		public GameLogSink()
		{
			try
			{
				if (m_stream != null)
				{
					throw new InvalidOperationException("GameLogSink already created.");
				}
				Storage.CreateDirectory(ModsManager.LogPath);
				string path = Storage.CombinePaths(ModsManager.LogPath, "Game.log");
				FileInfo fileInfo = Storage.GetFileInfo(path);
				if(!fileInfo.Exists)
				{
					m_stream = fileInfo.Create();
				}
				else
				{
					if(fileInfo.Length > 2097152)//2MiB
					{
						CultureInfo cultureInfo = Program.SystemLanguage ==null ? CultureInfo.CurrentCulture : new CultureInfo(Program.SystemLanguage);
						string destination = Storage.ProcessPath(Storage.CombinePaths(ModsManager.LogPath, Storage.SanitizeFileName($"Game {DateTime.Now.ToString(cultureInfo)}.log")), true, false);
						fileInfo.MoveTo(destination, true);
						m_stream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
					}
					else
					{
						m_stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
					}
				}
				m_stream.Position = m_stream.Length;
				m_writer = new StreamWriter(m_stream);
			}
			catch (Exception ex)
			{
#if WINDOWS
				AllocConsole();
				Window.Closed += () => FreeConsole();
				Console.Title = "Logs of Survivalcraft API";
				Engine.Log.RemoveAllLogSinks();
				Engine.Log.AddLogSink(new ConsoleLogSink());
				Engine.Log.Information("Error creating GameLogSink, and a console window for viewing logs is created. Reason: {0}", ex.Message);
#else
				Engine.Log.Error("Error creating GameLogSink. Reason: {0}", ex.Message);
#endif
			}
		}

		public static string GetRecentLog(int bytesCount)
		{
			if (m_stream == null)
			{
				return LanguageControl.Get(fName, "1");
			}
			lock (m_stream)
			{
				try
				{
					m_stream.Position = Math.Max(m_stream.Position - bytesCount, 0L);
					return new StreamReader(m_stream).ReadToEnd();
				}
				finally
				{
					m_stream.Position = m_stream.Length;
				}
			}
		}

		public static List<string> GetRecentLogLines(int bytesCount)
		{
			if (m_stream == null)
			{
				return [LanguageControl.Get(fName, "1")];
			}
			lock (m_stream)
			{
				try
				{
					m_stream.Position = Math.Max(m_stream.Position - bytesCount, 0L);
					var streamReader = new StreamReader(m_stream);
					var list = new List<string>();
					while (true)
					{
						string text = streamReader.ReadLine();
						if (text == null)
						{
							break;
						}
						list.Add(text);
					}
					return list;
				}
				finally
				{
					m_stream.Position = m_stream.Length;
				}
			}
		}

		public void Log(LogType type, string message)
		{
			if (m_stream != null)
			{
				lock (m_stream)
				{
					string value;
					switch (type)
					{
						case LogType.Debug:
							value = "DEBUG: ";
							break;
						case LogType.Verbose:
							value = "INFO: ";
							break;
						case LogType.Information:
							value = "INFO: ";
							break;
						case LogType.Warning:
							value = "WARNING: ";
							break;
						case LogType.Error:
							value = "ERROR: ";
							break;
						default:
							value = string.Empty;
							break;
					}
					m_writer.Write(DateTime.Now.ToString("HH:mm:ss.fff"));
					m_writer.Write(" ");
					m_writer.Write(value);
					m_writer.WriteLine(message);
					m_writer.Flush();
				}
			}
		}

#if WINDOWS
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool FreeConsole();
#endif
	}
}
