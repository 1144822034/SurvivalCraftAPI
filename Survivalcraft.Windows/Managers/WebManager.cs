#if ANDROID
using Android.OS;
using Android.Net;
#elif WINDOWS
using System.Runtime.InteropServices;
#elif LINUX
using System.Net.NetworkInformation;
#endif
using Engine;
using System.Net;
using System.Net.Http;
using System.Text;
using OperationCanceledException = System.OperationCanceledException;
using Uri = System.Uri;

namespace Game
{
	public static class WebManager
	{
		public class ProgressHttpContent : HttpContent
		{
			public Stream m_sourceStream;

			public CancellableProgress m_progress;

			public ProgressHttpContent(Stream sourceStream, CancellableProgress progress)
			{
				m_sourceStream = sourceStream;
				m_progress = progress ?? new CancellableProgress();
			}

			protected override bool TryComputeLength(out long length)
			{
				length = m_sourceStream.Length;
				return true;
			}

			protected override async Task SerializeToStreamAsync(Stream targetStream, TransportContext context)
			{
				byte[] buffer = new byte[1024];
				long written = 0L;
				while (true)
				{
					m_progress.Total = m_sourceStream.Length;
					m_progress.Completed = written;
					if (m_progress.CancellationToken.IsCancellationRequested)
					{
						break;
					}
					int read = m_sourceStream.Read(buffer, 0, buffer.Length);
					if (read > 0)
					{
						await targetStream.WriteAsync(buffer, 0, read, m_progress.CancellationToken);
						written += read;
					}
					if (read <= 0)
					{
						return;
					}
				}
				throw new OperationCanceledException("Operation cancelled.");
			}
		}
#if WINDOWS
		[DllImport("wininet.dll")]
		internal static extern bool InternetGetConnectedState(out int Description, int ReservedValue);
#elif ANDROID
#pragma warning disable CA1416
#pragma warning disable CA1422
		internal static ConnectivityManager m_connectivityManager { get; } = GetConnectivityManager();
#endif
		public static bool IsInternetConnectionAvailable()
		{
			try
			{
#if ANDROID
				switch(Build.VERSION.SdkInt)
				{
					case >= (BuildVersionCodes)29:
						return m_connectivityManager?.GetNetworkCapabilities(m_connectivityManager.ActiveNetwork)?.HasCapability(NetCapability.Validated) ?? false;
					case >= (BuildVersionCodes)21:
						return m_connectivityManager?.ActiveNetworkInfo?.IsConnected ?? false;
					default:
						return true;
				}
#elif WINDOWS
				return InternetGetConnectedState(out int _, 0);
#elif LINUX
				return NetworkInterface.GetIsNetworkAvailable();
#else
				return true;
#endif
			}
			catch (Exception e)
			{
				Log.Warning(ExceptionManager.MakeFullErrorMessage("Could not check internet connection availability.", e));
			}
			return true;
		}

#if ANDROID
		private static ConnectivityManager GetConnectivityManager() => Build.VERSION.SdkInt >= (BuildVersionCodes)21 ? (ConnectivityManager)Window.Activity.GetSystemService("connectivity") : null;
#pragma warning restore CA1416
#pragma warning restore CA1422
#endif

		public static void Get(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			MemoryStream targetStream;
			Exception e = null;
			Task.Run(async delegate
			{
				Uri requestUri = (parameters != null && parameters.Count > 0) ? new Uri($"{address}?{UrlParametersToString(parameters)}") : new Uri(address);
				try
				{
					progress = progress ?? new CancellableProgress();
					if (!IsInternetConnectionAvailable())
					{
						throw new InvalidOperationException("Internet connection is unavailable.");
					}
					using (HttpClient client = new())
					{
						client.DefaultRequestHeaders.Referrer = new Uri(address);
						if (headers != null)
						{
							foreach (KeyValuePair<string, string> header in headers)
							{
								client.DefaultRequestHeaders.Add(header.Key, header.Value);
							}
						}
						HttpResponseMessage responseMessage = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, progress.CancellationToken);
						await VerifyResponse(responseMessage);
						long? contentLength = responseMessage.Content.Headers.ContentLength;
#if !ANDROID
						progress.Total = contentLength ?? 0;
#else
						progress.Total = contentLength.GetValueOrDefault();
#endif
						using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync();
						targetStream = new MemoryStream();
						try
						{
							long written = 0L;
							byte[] buffer = new byte[1024];
							int num;
							do
							{
								num = await responseStream.ReadAsync(buffer,progress.CancellationToken);
								if(num > 0)
								{
									targetStream.Write(buffer,0,num);
									written += num;
									progress.Completed = written;
								}
							}
							while(num > 0);
							if(success != null)
							{
								Dispatcher.Dispatch(delegate
								{
									// ReSharper disable AccessToDisposedClosure
									success(targetStream?.ToArray());
									// ReSharper restore AccessToDisposedClosure
								});
							}
						}
						finally
						{
							((IDisposable)targetStream)?.Dispose();
						}
					}
				}
				catch (Exception ex)
				{
					Log.Warning(e.Message + ":\nThe connection is unavailable. Url: " + requestUri);
					if(failure != null)
					{
						Dispatcher.Dispatch(delegate
						{
							failure(ex);
						});
					}
				}
			});
		}

		public static void Put(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			PutOrPost(isPost: false, address, parameters, headers, data, progress, success, failure);
		}

		public static void Post(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			PutOrPost(isPost: true, address, parameters, headers, data, progress, success, failure);
		}

		public static string UrlParametersToString(Dictionary<string, string> values)
		{
			StringBuilder stringBuilder = new();
			string value = string.Empty;
			foreach (KeyValuePair<string, string> value2 in values)
			{
				stringBuilder.Append(value);
				value = "&";
				stringBuilder.Append(Uri.EscapeDataString(value2.Key));
				stringBuilder.Append('=');
				if (!string.IsNullOrEmpty(value2.Value))
				{
					stringBuilder.Append(Uri.EscapeDataString(value2.Value));
				}
			}
			return stringBuilder.ToString();
		}

		public static byte[] UrlParametersToBytes(Dictionary<string, string> values)
		{
			return Encoding.UTF8.GetBytes(UrlParametersToString(values));
		}

		public static MemoryStream UrlParametersToStream(Dictionary<string, string> values)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(UrlParametersToString(values)));
		}

		public static Dictionary<string, string> UrlParametersFromString(string s)
		{
			Dictionary<string,string> dictionary = new();
			string[] array = s.Split('&', StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = Uri.UnescapeDataString(array[i]).Split('=');
				if (array2.Length == 2)
				{
					dictionary[array2[0]] = array2[1];
				}
			}
			return dictionary;
		}

		public static Dictionary<string, string> UrlParametersFromBytes(byte[] bytes)
		{
			return UrlParametersFromString(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
		}

		public static void PutOrPost(bool isPost, string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			Task.Run(async delegate
			{
				Uri requestUri = (parameters != null && parameters.Count > 0) ? new Uri($"{address}?{UrlParametersToString(parameters)}") : new Uri(address);
				try
				{
					if (!IsInternetConnectionAvailable())
					{
						throw new InvalidOperationException("Internet connection is unavailable.");
					}
					using HttpClient client = new();
					Dictionary<string,string> dictionary = new();
					if(headers != null)
					{
						foreach(KeyValuePair<string,string> header in headers)
						{
							if(!client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key,header.Value))
							{
								dictionary.Add(header.Key,header.Value);
							}
						}
					}
#if !ANDROID
					ProgressHttpContent httpContent = new(data, progress);
#else
					HttpContent httpContent = (progress != null) ? new ProgressHttpContent(data,progress) : new StreamContent(data);
#endif
					foreach(KeyValuePair<string,string> item in dictionary)
					{
						httpContent.Headers.Add(item.Key,item.Value);
					}
#if !ANDROID
						HttpResponseMessage responseMessage = isPost ? (await client.PostAsync(requestUri, httpContent, progress.CancellationToken)) : (await client.PutAsync(requestUri, httpContent, progress.CancellationToken));
#else
					HttpResponseMessage responseMessage = isPost ? ((progress == null) ? (await client.PostAsync(requestUri,httpContent)) : (await client.PostAsync(requestUri,httpContent,progress.CancellationToken))) : ((progress == null) ? (await client.PutAsync(requestUri,httpContent)) : (await client.PutAsync(requestUri,httpContent,progress.CancellationToken)));
#endif
					await VerifyResponse(responseMessage);
					byte[] responseData = await responseMessage.Content.ReadAsByteArrayAsync();
					if(success != null)
					{
						Dispatcher.Dispatch(delegate
					{
						success(responseData);
					});
					}
				}
				catch (Exception e)
				{
					Log.Warning(e.Message + ":\nThe connection is unavailable. Url: " + requestUri);
					if (failure != null)
					{
						Dispatcher.Dispatch(delegate
						{
							failure(e);
						});
					}
				}
			});
		}

		public static async Task VerifyResponse(HttpResponseMessage message)
		{
			if (!message.IsSuccessStatusCode)
			{
				string responseText = string.Empty;
				try
				{
					responseText = await message.Content.ReadAsStringAsync();
				}
				catch
				{
					// ignored
				}
				throw new InvalidOperationException($"{message.StatusCode} ({(int)message.StatusCode})\n{responseText}");
			}
		}
	}
}