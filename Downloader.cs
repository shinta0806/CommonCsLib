// ============================================================================
// 
// ファイルをダウンロードするクラス（ログイン用に POST 機能あり）
// Copyright (C) 2014-2021 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// SSL 対応済
// 1 つのインスタンスならクッキー情報を保持する
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/12/28 (Sun) | 作成開始。
//  1.00  | 2014/12/29 (Mon) | オリジナルバージョン。
// (1.01) | 2015/01/03 (Sat) |   Download() の引数に out を付けた。
//  1.10  | 2015/01/10 (Sat) | ITerminatableThread インターフェースを利用するようにした。
//  1.20  | 2015/01/10 (Sat) | DownloadBufferSize プロパティを付けた。
//  1.30  | 2015/10/04 (Sun) | Download() を HttpClient で行うように変更した。
//  1.40  | 2015/10/04 (Sun) | Post() を作成した。
//  1.50  | 2015/11/07 (Sat) | ITerminatableThread の代わりに CancellationToken を使うようにした。
// (1.51) | 2018/05/21 (Mon) |   Post() をファイル送信にも対応させた。
//  1.60  | 2019/06/24 (Mon) | IDisposable を実装した。
// (1.61) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.62) | 2020/05/05 (Tue) |   null 許容参照型を無効化できるようにした。
// (1.63) | 2021/04/28 (Wed) |   WebRequestHandler 廃止。
// (1.64) | 2021/05/03 (Mon) |   null 許容参照型が常に有効化されるようにした。
// (1.65) | 2021/05/04 (Tue) |   リソースリークを修正。
// (1.66) | 2021/08/29 (Sun) |   標準のユーザーエージェントを更新。
//  2.00  | 2021/08/30 (Mon) | 再構築。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shinta
{
	//delegate Task<HttpResponseMessage>? CoreHttpDg(String url, Object? option);

	public class Downloader
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public Downloader(HttpClient? httpClient = null)
		{
			// http クライアントの設定
			if (httpClient == null)
			{
				// 外部から http クライアントが注入されない場合は、自前の http クライアントを使う
				if (_defaultHttpClient == null)
				{
					// 自前の http クライアントが未作成の場合は作成する
					_defaultHttpClient = new HttpClient();
				}
				httpClient = _defaultHttpClient;
			}
			_httpClient = httpClient;

			// インスタンスが変わっても変わらないヘッダーの設定
			_httpClient.DefaultRequestHeaders.Clear();
			_httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "ja,en-us;q=0.7,en;q=0.3");
			_httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache,no-store");

			// その他
			UserAgent = DefaultUserAgent();
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ダウンロードバッファサイズ［バイト］（この単位でスレッド中止の制御を行うことに注意）
		//public Int32 DownloadBufferSize { get; set; }

		// ダウンロード時のユーザーエージェント
		public String UserAgent { get; set; }

		// 終了要求制御
		public CancellationToken CancellationToken { get; set; }

		// クッキー等を保持
		//public SocketsHttpHandler? ClientHandler { get; private set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

#pragma warning disable CA1822
		// --------------------------------------------------------------------
		// 標準のユーザーエージェント
		// --------------------------------------------------------------------
		public String DefaultUserAgent()
		{
			// Firefox 30.0 の UA：Mozilla/5.0 (Windows NT 6.1; WOW64; rv:30.0) Gecko/20100101 Firefox/30.0
			// Firefox 91.0 の UA：Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0
			String ua = "Mozilla/5.0 (Windows NT";
			SystemEnvironment se = new();
			if (se.GetOSVersion(out Double osVersion))
			{
				ua += " " + osVersion.ToString();
			}
			ua += "; ";

			// OS が 64 ビットの場合は情報を付加（32 ビットの場合は何も付かない）
			if (Environment.Is64BitOperatingSystem)
			{
				if (Environment.Is64BitProcess)
				{
					// ネイティブ 64 ビットアプリ
					ua += "Win64; x64; ";
				}
				else
				{
					// OS は 64 ビットだが、アプリ（自分自身）は 32 ビット
					ua += "WOW64; ";
				}
			}

			ua += "rv:91.0) Gecko/20100101 Firefox/91.0";

			return ua;
		}
#pragma warning restore CA1822

		// --------------------------------------------------------------------
		// ダウンロード（ファイルとして保存）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public async Task<HttpResponseMessage> DownloadAsFileAsync(String url, String path)
		{
			using FileStream fileStream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
			HttpResponseMessage response = await DownloadAsStreamAsync(url, fileStream);
			return response;
		}

		// --------------------------------------------------------------------
		// ダウンロード（toStream 内にコピー：toStream.Position は末尾になる）
		// ＜返値＞ 応答（破棄不要の模様）
		// ＜例外＞ Exception（ドメインが間違っている等、サーバーに接続できない場合）
		// --------------------------------------------------------------------
		public async Task<HttpResponseMessage> DownloadAsStreamAsync(String url, Stream toStream)
		{
			// リクエスト
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			AddHeaders(request, url);

			// ダウンロード
			return await DownloadAsStreamCoreAsync(request, toStream);
		}

		// --------------------------------------------------------------------
		// ダウンロード（文字列として取得）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public async Task<(HttpResponseMessage, String)> DownloadAsStringAsync(String url, Encoding encoding)
		{
			// ダウンロード
			using MemoryStream memStream = new();
			HttpResponseMessage response = await DownloadAsStreamAsync(url, memStream);

			// 変換して返す
			return (response, encoding.GetString(memStream.ToArray()));
		}

		// --------------------------------------------------------------------
		// 送信して結果をダウンロード（Stream 内にコピー）
		// ＜引数＞ post: Name=Value, files: Name=Path
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public async Task<HttpResponseMessage> PostAndDownloadAsStreamAsync(String url, Stream toStream, Dictionary<String, String?> post, Dictionary<String, String>? files = null)
		{
			if (files == null || files.Count == 0)
			{
				// リクエストは post パラメーターのみ
				HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, url);
				postRequest.Content = new FormUrlEncodedContent(post.Select(x => new KeyValuePair<String?, String?>(x.Key, x.Value)));
				AddHeaders(postRequest, url);

				// ダウンロード
				return await DownloadAsStreamCoreAsync(postRequest, toStream);
			}

			using MultipartFormDataContent multipart = new();

			// post パラメーター文字列
			foreach (KeyValuePair<String, String?> kvp in post)
			{
				StringContent stringContent = new(kvp.Value ?? String.Empty);
				stringContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
				{
					Name = kvp.Key,
				};
				multipart.Add(stringContent);
			}

			// ファイル
			foreach (KeyValuePair<String, String> kvp in files)
			{
				StreamContent fileContent = new(File.OpenRead(kvp.Value));
				fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue(/*"attachment"*/"form-data")
				{
					Name = kvp.Key,
					FileName = Path.GetFileName(kvp.Value),
				};
				multipart.Add(fileContent);
			}

			// リクエストは post パラメーターとファイル
			HttpRequestMessage bothRequest = new HttpRequestMessage(HttpMethod.Post, url);
			bothRequest.Content = multipart;
			AddHeaders(bothRequest, url);

			// ダウンロード
			return await DownloadAsStreamCoreAsync(bothRequest, toStream);
		}

		// --------------------------------------------------------------------
		// 送信して結果をダウンロード（文字列として取得）
		// ＜引数＞ post: Name=Value, files: Name=Path
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public async Task<(HttpResponseMessage, String)> PostAndDownloadAsStringAsync(String url, Encoding encoding, Dictionary<String, String?> post, Dictionary<String, String>? files = null)
		{
			// ダウンロード
			using MemoryStream memStream = new();
			HttpResponseMessage response = await PostAndDownloadAsStreamAsync(url, memStream, post, files);

			// 変換して返す
			return (response, encoding.GetString(memStream.ToArray()));
		}

#if false
		// --------------------------------------------------------------------
		// Post
		// ＜引数＞ oPost: Name=Value, oFiles: Name=Path
		// ＜例外＞
		// --------------------------------------------------------------------
		public void Post(String url, Dictionary<String, String?> post, Dictionary<String, String>? files = null)
		{
			if (files == null || files.Count == 0)
			{
				// oPost のみを送信
				HttpMethod(url, CoreHttpPost, new FormUrlEncodedContent(post.Select(x => new KeyValuePair<String?, String?>(x.Key, x.Value))));
				return;
			}

			using MultipartFormDataContent multipart = new();

			// 文字列
			foreach (KeyValuePair<String, String?> kvp in post)
			{
				StringContent stringContent = new(kvp.Value ?? String.Empty);
				stringContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
				{
					Name = kvp.Key,
				};
				multipart.Add(stringContent);
			}

			// ファイル
			foreach (KeyValuePair<String, String> kvp in files)
			{
				StreamContent fileContent = new(File.OpenRead(kvp.Value));
				fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue(/*"attachment"*/"form-data")
				{
					Name = kvp.Key,
					FileName = Path.GetFileName(kvp.Value),
				};
				multipart.Add(fileContent);
			}

			// POST で送信
			HttpMethod(url, CoreHttpPostWithFile, multipart);
		}
#endif

		// ====================================================================
		// private 定数
		// ====================================================================

		// ダウンロードバッファサイズのデフォルト値［バイト］
		//private const Int32 DOWNLOAD_BUFFER_SIZE_DEFAULT = 100 * 1024;

		// GET リクエストのリトライ時の間隔 [ms]
		//private const Int32 GET_RETRY_INTERVAL = 5000;

		// GET リクエストのリトライ回数
		//private const Int32 GET_RETRY_MAX = 3;

		// スレッドスリープの単位時間 [ms]
		//private const Int32 THREAD_SLEEP_INTERVAL = 100;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 使用する http クライアント
		private HttpClient _httpClient;

		// 外部から指定されなかった場合に使用する http クライアント
		// 複数インスタンスで使い回す
		private static HttpClient? _defaultHttpClient;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リクエストにヘッダーを付加
		// --------------------------------------------------------------------
		private void AddHeaders(HttpRequestMessage request, String url)
		{
			request.Headers.Add("User-Agent", UserAgent);
			request.Headers.Add("Referer", url);
		}

#if false
		// --------------------------------------------------------------------
		// httpMethod() で使うデリゲート関数：GET 用
		// --------------------------------------------------------------------
		private Task<HttpResponseMessage>? CoreHttpGet(String url, Object? option)
		{
			return _httpClient?.GetAsync(url);
		}

		// --------------------------------------------------------------------
		// httpMethod() で使うデリゲート関数：POST 用
		// --------------------------------------------------------------------
		private Task<HttpResponseMessage>? CoreHttpPost(String url, Object? option)
		{
			if (option is FormUrlEncodedContent content)
			{
				return _httpClient?.PostAsync(url, content);
			}

			return null;
		}

		// --------------------------------------------------------------------
		// httpMethod() で使うデリゲート関数：POST（ファイル送信）用
		// --------------------------------------------------------------------
		private Task<HttpResponseMessage>? CoreHttpPostWithFile(String url, Object? option)
		{
			if (option is MultipartFormDataContent content)
			{
				return _httpClient?.PostAsync(url, content);
			}

			return null;
		}
#endif

		// --------------------------------------------------------------------
		// ダウンロード（toStream 内にコピー：toStream.Position は末尾になる）
		// ＜返値＞ 応答（破棄不要の模様）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async Task<HttpResponseMessage> DownloadAsStreamCoreAsync(HttpRequestMessage request, Stream toStream)
		{
#if DEBUGz
			String headers = String.Empty;
			foreach (KeyValuePair<String, IEnumerable<String>> header in _httpClient.DefaultRequestHeaders)
			{
				headers += header.Key + " => " + String.Join(", ", header.Value) + "\n";
			}
			Debug.WriteLine("Headers\n" + headers);
#endif
			HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
			using Stream responseStream = await response.Content.ReadAsStreamAsync();
			responseStream.CopyTo(toStream);
			return response;
		}

#if false
		// --------------------------------------------------------------------
		// http リクエストを処理する汎用関数
		// ＜例外＞
		// --------------------------------------------------------------------
		private Task<HttpResponseMessage> HttpMethod(String url, CoreHttpDg coreDg, Object? coreDgOption)
		{
			String? err = null;
			Task<HttpResponseMessage>? res = null;

			// クライアントの設定
			SetClient(url);

			// ダウンロード
			for (Int32 i = 0; i < GET_RETRY_MAX; i++)
			{
				try
				{
					// http コアリクエスト（デリゲート）
					res = coreDg(url, coreDgOption);
					if (res != null)
					{
						res.Wait();
						ThrowIfCancellationRequested();

						// 成功したら関数から返る
						if (res.Result.IsSuccessStatusCode)
						{
							return res;
						}
					}
				}
				catch (OperationCanceledException)
				{
					// ユーザーからの中止指示の場合は、直ちに再スロー
					throw;
				}
				catch (Exception excep)
				{
					// エラーメッセージを一旦設定するが、後のコードでリトライして、結果的に成功することはありえる
					err = excep.Message;
				}

				// 失敗した場合
				if (res != null && res.Result.StatusCode == HttpStatusCode.RequestTimeout)
				{
					// タイムアウトなら、一定時間後にリトライする
					Wait(GET_RETRY_INTERVAL);
				}
				else
				{
					// 直ちに失敗が確定
					i = GET_RETRY_MAX;
				}
			}

			// リトライしても成功しなかったので、エラー確定
			throw new Exception(err);
		}
#endif

#if false
		// --------------------------------------------------------------------
		// http クライアントの設定
		// --------------------------------------------------------------------
		private void SetClient(String url)
		{
			// ハンドラが作成されていない場合は作成する
			if (ClientHandler == null)
			{
				SocketsHttpHandler socketsHttpHandler = new();
				socketsHttpHandler.UseCookies = true;
				ClientHandler = socketsHttpHandler;
			}

			// クライアントが作成されていない場合は作成する
			if (_httpClient == null)
			{
				_httpClient = new HttpClient(ClientHandler);
			}

			// リクエストヘッダーを設定
			_httpClient.DefaultRequestHeaders.Clear();
			_httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
			_httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "ja,en-us;q=0.7,en;q=0.3");
			_httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache,no-store");
			_httpClient.DefaultRequestHeaders.Add("Referer", url);
		}
#endif

#if false
		// --------------------------------------------------------------------
		// 中止指示があったら例外を発生させる
		// ＜例外＞
		// --------------------------------------------------------------------
		private void ThrowIfCancellationRequested()
		{
			CancellationToken.ThrowIfCancellationRequested();
		}

		// --------------------------------------------------------------------
		// 指定時間 [ms] スレッドを中断
		// ＜例外＞
		// --------------------------------------------------------------------
		private void Wait(Int32 waitMS)
		{
			Int32 restTime = 0;

			while (restTime < waitMS)
			{
				Thread.Sleep(THREAD_SLEEP_INTERVAL);
				restTime += THREAD_SLEEP_INTERVAL;
				ThrowIfCancellationRequested();
			}
		}
#endif
	}
}
