using Engine;
using System.Net;
using System.Text;
using System.Net.Http;
using System.IO;
#if ANDROID
using Android.Net;
using System.Text.Json;
#else
using System.Runtime.InteropServices;
#endif
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
#if !ANDROID
		[DllImport("wininet.dll")]
		public extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
#endif
		public static bool IsInternetConnectionAvailable()
		{
			try
			{
#if ANDROID
				return ((ConnectivityManager)Window.Activity.GetSystemService("connectivity")).ActiveNetworkInfo?.IsConnected ?? false;
#else
				return InternetGetConnectedState(out int Desc, 0);
#endif
			}
			catch (Exception e)
			{
				Log.Warning(ExceptionManager.MakeFullErrorMessage("Could not check internet connection availability.", e));
			}
			return true;
		}

		public static void Get(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			MemoryStream targetStream = default;
			Exception e = default;
			Task.Run(async delegate
			{
				try
				{
					progress = progress ?? new CancellableProgress();
					if (!IsInternetConnectionAvailable())
					{
						throw new InvalidOperationException("Internet connection is unavailable.");
					}
					using (HttpClient client = new())
					{
						Uri requestUri = (parameters != null && parameters.Count > 0) ? new System.Uri($"{address}?{UrlParametersToString(parameters)}") : new Uri(address);
						client.DefaultRequestHeaders.Referrer = new Uri(address);
						if (headers != null)
						{
							foreach (KeyValuePair<string, string> header in headers)
							{
								client.DefaultRequestHeaders.Add(header.Key, header.Value);
							}
						}
						ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
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
									success(targetStream.ToArray());
								});
							}
						}
						finally
						{
							if(targetStream != null)
							{
								((IDisposable)targetStream).Dispose();
							}
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error(ExceptionManager.MakeFullErrorMessage(e));
					if (failure != null)
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
			var stringBuilder = new StringBuilder();
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
			var dictionary = new Dictionary<string, string>();
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
			byte[] responseData = default;
			Task.Run(async delegate
			{
				try
				{
					if (!IsInternetConnectionAvailable())
					{
						throw new InvalidOperationException("Internet connection is unavailable.");
					}
					using var client = new HttpClient();
					var dictionary = new Dictionary<string,string>();
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
					Uri requestUri = (parameters != null && parameters.Count > 0) ? new Uri($"{address}?{UrlParametersToString(parameters)}") : new Uri(address);
#if !ANDROID
						var httpContent = new ProgressHttpContent(data, progress);
#else
					HttpContent httpContent = (progress != null) ? ((HttpContent)new ProgressHttpContent(data,progress)) : ((HttpContent)new StreamContent(data));
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
					responseData = await responseMessage.Content.ReadAsByteArrayAsync();
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
					Log.Error(ExceptionManager.MakeFullErrorMessage(e));
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
				}
				throw new InvalidOperationException($"{message.StatusCode} ({(int)message.StatusCode})\n{responseText}");
			}
		}
	}
}