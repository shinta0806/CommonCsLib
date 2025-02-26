// ============================================================================
// 
// ファイルをダウンロードするクラス（ログイン用に POST 機能あり）
// Copyright (C) 2014-2025 by SHINTA
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
// (2.01) | 2021/10/28 (Thu) |   軽微なリファクタリング。
// (2.02) | 2022/01/09 (Sun) |   DefaultUserAgent() を改善。
// (2.03) | 2022/01/10 (Mon) |   軽微なリファクタリング。
// (2.04) | 2023/08/22 (Tue) |   軽微なリファクタリング。
// (2.05) | 2025/02/20 (Thu) |   進捗報告できるようにした。
// (2.06) | 2025/02/21 (Fri) |   レジュームできるようにした。
// (2.07) | 2025/02/26 (Wed) |   DownloadAsStreamAsync() のスキップ処理を改善。
// ============================================================================

using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Shinta;

public class Downloader
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	/// <param name="httpClient">使い回す HttpClient インスタンス</param>
	public Downloader(HttpClient? httpClient = null)
	{
		// http クライアントの設定
		if (httpClient == null)
		{
			// 外部から http クライアントが注入されない場合は、自前の http クライアントを使う
			// 自前の http クライアントが未作成の場合は作成する
			_defaultHttpClient ??= new HttpClient();
			_httpClient = _defaultHttpClient;
		}
		else
		{
			_httpClient = httpClient;
		}

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

	/// <summary>
	/// ダウンロード時のユーザーエージェント
	/// </summary>
	public String UserAgent { get; set; }

	/// <summary>
	/// 終了要求制御
	/// </summary>
	public CancellationToken CancellationToken { get; set; }

	// ====================================================================
	// public 関数
	// ====================================================================

#pragma warning disable CA1822
	/// <summary>
	/// 標準のユーザーエージェント
	/// </summary>
	/// <returns></returns>
	public String DefaultUserAgent()
	{
		// Firefox 30.0 の UA：Mozilla/5.0 (Windows NT 6.1; WOW64; rv:30.0) Gecko/20100101 Firefox/30.0
		// Firefox 91.0 の UA：Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0
		// Firefox 124.0.2 の UA：Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0
		// Firefox 135.0.1 の UA：Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:135.0) Gecko/20100101 Firefox/135.0
		// Chrome 133.0.6943.98 の UA：Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36
		Version osVersion = Environment.OSVersion.Version;
		String ua = "Mozilla/5.0 (Windows NT " + osVersion.Major.ToString() + "." + osVersion.Minor.ToString() + "; ";

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

		ua += "rv:135.0) Gecko/20100101 Firefox/135.0";

		return ua;
	}
#pragma warning restore CA1822

	/// <summary>
	/// ダウンロード（ファイルとして保存）
	/// </summary>
	/// <param name="url"></param>
	/// <param name="path"></param>
	/// <returns></returns>
	public async Task<HttpResponseMessage> DownloadAsFileAsync(String url, String path, IProgress<Double>? progress = null, Boolean resume = false)
	{
		FileMode fileMode = resume ? FileMode.Append : FileMode.Create;
		using FileStream fileStream = new(path, fileMode, FileAccess.Write, FileShare.None);
		HttpResponseMessage response = await DownloadAsStreamAsync(url, fileStream, progress, resume);

		// 失敗の場合はファイルを削除
		if (!response.IsSuccessStatusCode && fileStream.Length == 0)
		{
			fileStream.Close();
			try
			{
				File.Delete(path);
			}
			catch (Exception)
			{
			}
		}

		return response;
	}

	/// <summary>
	/// ダウンロード（toStream 内にコピー：toStream.Position は末尾になる）
	/// </summary>
	/// <param name="url"></param>
	/// <param name="toStream">resume の場合は予め resume 位置（通常は末尾）にシークしておく必要がある</param>
	/// <param name="progress">進捗は 0～1 で報告される</param>
	/// <returns>応答（破棄不要の模様）</returns>
	/// <exception cref="Exception">ドメインが間違っている等、サーバーに接続できない場合</exception>
	public async Task<HttpResponseMessage> DownloadAsStreamAsync(String url, Stream toStream, IProgress<Double>? progress = null, Boolean resume = false)
	{
		// 総サイズ
		Int64 totalSize = await TotalSizeAsync(url);
		if (resume && totalSize >= 0 && toStream.Position >= totalSize)
		{
			// レジューム位置がファイルサイズ以上の場合は何もしない
			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		// リクエスト
		HttpRequestMessage request = new(HttpMethod.Get, url);

		// ダウンロード
		return await DownloadAsStreamCoreAsync(url, request, toStream, totalSize, progress, resume);
	}

	/// <summary>
	/// ダウンロード（文字列として取得）
	/// </summary>
	/// <param name="url"></param>
	/// <param name="encoding"></param>
	/// <returns></returns>
	public async Task<(HttpResponseMessage, String)> DownloadAsStringAsync(String url, Encoding encoding, IProgress<Double>? progress = null)
	{
		// ダウンロード
		using MemoryStream memStream = new();
		HttpResponseMessage response = await DownloadAsStreamAsync(url, memStream, progress);

		// 変換して返す
		return (response, encoding.GetString(memStream.ToArray()));
	}

	/// <summary>
	/// 送信して結果をダウンロード（Stream 内にコピー）
	/// </summary>
	/// <param name="url"></param>
	/// <param name="toStream"></param>
	/// <param name="post">Name=Value</param>
	/// <param name="files">Name=Path</param>
	/// <returns></returns>
	public async Task<HttpResponseMessage> PostAndDownloadAsStreamAsync(String url, Stream toStream, Dictionary<String, String?> post, Dictionary<String, String>? files = null)
	{
		if (files == null || files.Count == 0)
		{
			// リクエストは post パラメーターのみ
			HttpRequestMessage postRequest = new(HttpMethod.Post, url)
			{
				Content = new FormUrlEncodedContent(post.Select(x => new KeyValuePair<String?, String?>(x.Key, x.Value)))
			};

			// ダウンロード
			return await DownloadAsStreamCoreAsync(url, postRequest, toStream);
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
		HttpRequestMessage bothRequest = new(HttpMethod.Post, url)
		{
			Content = multipart
		};

		// ダウンロード
		return await DownloadAsStreamCoreAsync(url, bothRequest, toStream);
	}

	/// <summary>
	/// 送信して結果をダウンロード（文字列として取得）
	/// </summary>
	/// <param name="url"></param>
	/// <param name="encoding"></param>
	/// <param name="post">Name=Value</param>
	/// <param name="files">Name=Path</param>
	/// <returns></returns>
	public async Task<(HttpResponseMessage, String)> PostAndDownloadAsStringAsync(String url, Encoding encoding, Dictionary<String, String?> post, Dictionary<String, String>? files = null)
	{
		// ダウンロード
		using MemoryStream memStream = new();
		HttpResponseMessage response = await PostAndDownloadAsStreamAsync(url, memStream, post, files);

		// 変換して返す
		return (response, encoding.GetString(memStream.ToArray()));
	}

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// ダウンロードバッファーサイズ
	/// </summary>
	private const Int32 BUFFER_SIZE = 8 * 1024;

	/// <summary>
	/// 進捗報告間隔の最小値
	/// </summary>
	private const Int32 PROGRESS_INTERVAL_MIN = 1;

	/// <summary>
	/// 進捗報告間隔の最大値
	/// </summary>
	private const Int32 PROGRESS_INTERVAL_MAX = 100;

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// 使用する http クライアント
	/// </summary>
	private readonly HttpClient _httpClient;

	/// <summary>
	/// 外部から指定されなかった場合に使用する http クライアント
	/// 複数インスタンスで使い回す
	/// </summary>
	private static HttpClient? _defaultHttpClient;

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// リクエストにヘッダーを付加
	/// </summary>
	/// <param name="request"></param>
	/// <param name="url"></param>
	private void AddHeaders(HttpRequestMessage request, String url)
	{
		request.Headers.Add("User-Agent", UserAgent);
		request.Headers.Add("Referer", url);
	}

	/// <summary>
	/// ダウンロード（toStream 内にコピー：toStream.Position は末尾になる）
	/// </summary>
	/// <param name="request"></param>
	/// <param name="toStream"></param>
	/// <param name="totalSize">ダウンロード URL のファイル総サイズ</param>
	/// <param name="progress"></param>
	/// <returns>応答（破棄不要の模様）</returns>
	private async Task<HttpResponseMessage> DownloadAsStreamCoreAsync(String url, HttpRequestMessage request, Stream toStream, Int64 totalSize = -1,
		IProgress<Double>? progress = null, Boolean resume = false)
	{
		return await Task.Run(() =>
		{
			// ヘッダーの調整
			AddHeaders(request, url);
			if (resume)
			{
				if (toStream.Position > 0)
				{
					Log.Debug("DownloadAsStreamCoreAsync() レジューム位置: " + toStream.Position);
					request.Headers.Range = new(toStream.Position, null);
				}
			}

			// ヘッダーの確認
			HttpResponseMessage response = _httpClient.Send(request, HttpCompletionOption.ResponseHeadersRead);
			if (!response.IsSuccessStatusCode)
			{
				return response;
			}

			// ダウンロード
			using Stream contentStream = response.Content.ReadAsStream();
			Byte[] buffer = new Byte[BUFFER_SIZE];
			Int32 bytesRead;
			Int32 progressCount = 0;

			// 進捗報告は基本 1% ごとだが、totalSize が大きい場合はもっと細かくする
			Int32 progressInterval = Math.Clamp((Int32)(totalSize / (BUFFER_SIZE * 100)), PROGRESS_INTERVAL_MIN, PROGRESS_INTERVAL_MAX);
			Log.Debug("DownloadAsStreamCoreAsync() totalSize: " + totalSize.ToString("#,0") + ", progressInterval: " + progressInterval.ToString("#,0"));

			while ((bytesRead = contentStream.Read(buffer, 0, buffer.Length)) > 0)
			{
				// 応答内容を書き込む（bytesRead はサーバーのその時の状態によって変動する）
				toStream.Write(buffer, 0, bytesRead);

				// 進捗報告
				if (totalSize > 0 && progress != null)
				{
					progressCount++;
					if (progressCount % progressInterval == 0)
					{
						progress.Report((Double)toStream.Position / totalSize);
					}
				}

				CancellationToken.ThrowIfCancellationRequested();
			}
			progress?.Report(1.0d);
			return response;
		});
	}

	/// <summary>
	/// ダウンロードするファイルの総サイズ
	/// </summary>
	/// <param name="url"></param>
	/// <returns>取得できない場合は -1</returns>
	private async Task<Int64> TotalSizeAsync(String url)
	{
		using HttpRequestMessage request = new(HttpMethod.Head, url);
		AddHeaders(request, url);
		using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		if (!response.IsSuccessStatusCode || !response.Content.Headers.ContentLength.HasValue)
		{
			return -1;
		}
		return response.Content.Headers.ContentLength.Value;
	}
}
