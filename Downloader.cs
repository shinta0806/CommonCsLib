// ============================================================================
// 
// ファイルをダウンロードするクラス（ログイン用に POST 機能あり）
// Copyright (C) 2014-2018 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// SSL 対応済
// 1 つのインスタンスならクッキー情報を保持する
// ToDo: Disposable にして HttpClient を破棄する
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// HttpClient は破棄される時に HttpClientHandler を道連れにするものと推測される
// （1 回 1 回 HttpClient を破棄するコードを書いたら、2 度目に HttpClientHandler にアクセスできなかった）
// HttpClientHandler はクッキー情報の保持に必要なので、必然的に、HttpClient も
// ずっと保持しておく必要が生じる
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
// (1.51) | 2018/05/21 (Mon) | Post() をファイル送信にも対応させた。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shinta
{
	delegate Task<HttpResponseMessage> CoreHttpDg(String oUrl, Object oOption);

	public class Downloader
	{
		// ====================================================================
		// public プロパティ
		// ====================================================================

		// ダウンロードバッファサイズ［バイト］（この単位でスレッド中止の制御を行うことに注意）
		public Int32 DownloadBufferSize { get; set; }

		// ダウンロード時の User Agent
		public String UserAgent { get; set; }

		// 終了要求制御
		public CancellationToken CancellationToken { get; set; }

#if USE_ITERMINATE_THREAD
		// 終了要求制御用：obsolete：代わりに CancellationToken を使う
		public ITerminatableThread OwnerThread { get; set; }
#endif

		// クッキー等を保持
		public HttpClientHandler ClientHandler { get; private set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public Downloader()
		{
			DownloadBufferSize = DOWNLOAD_BUFFER_SIZE_DEFAULT;
			UserAgent = DefaultUserAgent();
		}

		// --------------------------------------------------------------------
		// 標準のユーザーエージェント
		// --------------------------------------------------------------------
		public String DefaultUserAgent()
		{
			// Firefox 30.0 の UA：Mozilla/5.0 (Windows NT 6.1; WOW64; rv:30.0) Gecko/20100101 Firefox/30.0
			String aUA = "Mozilla/5.0 (Windows NT";
			SystemEnvironment aSE = new SystemEnvironment();
			Double aOSVersion;
			if (aSE.GetOSVersion(out aOSVersion))
			{
				aUA += " " + aOSVersion.ToString();
			}
			aUA += "; ";

			// OS が 64 ビットの場合は情報を付加（32 ビットの場合は何も付かない）
			if (Environment.Is64BitOperatingSystem)
			{
				if (Environment.Is64BitProcess)
				{
					// ネイティブ 64 ビットアプリ
					aUA += "Win64; ";
				}
				else
				{
					// OS は 64 ビットだが、アプリ（自分自身）は 32 ビット
					aUA += "WOW64; ";
				}
			}

			aUA += "rv:30.0) Gecko/20100101";

			return aUA;
		}

		// --------------------------------------------------------------------
		// ダウンロード（文字列として取得）
		// ＜例外＞
		// --------------------------------------------------------------------
		public String Download(String oUrl, Encoding oEncoding)
		{
			String aContents;

			using (MemoryStream aMemStream = new MemoryStream())
			{
				Download(oUrl, aMemStream);
				aContents = oEncoding.GetString(aMemStream.ToArray());
			}
			return aContents;
		}

		// --------------------------------------------------------------------
		// ダウンロード（Stream 内にコピー）
		// ＜例外＞
		// --------------------------------------------------------------------
		public void Download(String oUrl, Stream oMemStream)
		{
			HttpResponseMessage aHrm = HttpMethod(oUrl, CoreHttpGet, null).Result;
			Stream aStream = aHrm.Content.ReadAsStreamAsync().Result;
			try
			{
				aStream.CopyTo(oMemStream);
			}
			finally
			{
				aStream.Dispose();
			}
		}

		// --------------------------------------------------------------------
		// ダウンロード（ファイルに保存）
		// ＜例外＞
		// --------------------------------------------------------------------
		public void Download(String oUrl, String oPath)
		{
			using (FileStream aFS = new FileStream(oPath, FileMode.Create))
			{
				HttpResponseMessage aHrm = HttpMethod(oUrl, CoreHttpGet, null).Result;
				Byte[] aBytes = aHrm.Content.ReadAsByteArrayAsync().Result;
				aFS.Write(aBytes, 0, aBytes.Length);
			}
		}

		// --------------------------------------------------------------------
		// Post
		// ＜引数＞ oPost: Name=Value, oFiles: Name=Path
		// ＜例外＞
		// --------------------------------------------------------------------
		public void Post(String oUrl, Dictionary<String, String> oPost, Dictionary<String, String> oFiles = null)
		{
			if (oFiles == null || oFiles.Count == 0)
			{
				// oPost のみを送信
				HttpMethod(oUrl, CoreHttpPost, new FormUrlEncodedContent(oPost));
				return;
			}

			using (MultipartFormDataContent aMultipart = new MultipartFormDataContent())
			{
				// 文字列
				foreach (KeyValuePair<String, String> aKVP in oPost)
				{
					StringContent aStringContent = new StringContent(aKVP.Value);
					aStringContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
					{
						Name = aKVP.Key,
					};
					aMultipart.Add(aStringContent);
				}

				// ファイル
				foreach (KeyValuePair<String, String> aKVP in oFiles)
				{
					StreamContent aFileContent = new StreamContent(File.OpenRead(aKVP.Value));
					aFileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue(/*"attachment"*/"form-data")
					{
						Name = aKVP.Key,
						FileName = Path.GetFileName(aKVP.Value),
					};
					aMultipart.Add(aFileContent);
				}

				// POST で送信
				HttpMethod(oUrl, CoreHttpPostWithFile, aMultipart);
			}
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ダウンロードバッファサイズのデフォルト値［バイト］
		private const Int32 DOWNLOAD_BUFFER_SIZE_DEFAULT = 100 * 1024;

		// GET リクエストのリトライ時の間隔 [ms]
		private const Int32 GET_RETRY_INTERVAL = 5000;

		// GET リクエストのリトライ回数
		private const Int32 GET_RETRY_MAX = 3;

		// スレッドスリープの単位時間 [ms]
		private const Int32 THREAD_SLEEP_INTERVAL = 100;

		// スレッド中止時のエラーメッセージ
		private const String ERROR_MESSAGE_THREAD_TERMINATED = "スレッドが中止されました。";

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// http クライアント
		private HttpClient mClient = null;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// httpMethod() で使うデリゲート関数：GET 用
		// --------------------------------------------------------------------
		private Task<HttpResponseMessage> CoreHttpGet(String oUrl, Object oOption)
		{
			return mClient.GetAsync(oUrl);
		}

		// --------------------------------------------------------------------
		// httpMethod() で使うデリゲート関数：POST 用
		// --------------------------------------------------------------------
		private Task<HttpResponseMessage> CoreHttpPost(String oUrl, Object oOption)
		{
			return mClient.PostAsync(oUrl, (FormUrlEncodedContent)oOption);
		}

		// --------------------------------------------------------------------
		// httpMethod() で使うデリゲート関数：POST（ファイル送信）用
		// --------------------------------------------------------------------
		private Task<HttpResponseMessage> CoreHttpPostWithFile(String oUrl, Object oOption)
		{
			return mClient.PostAsync(oUrl, (MultipartFormDataContent)oOption);
		}

		// --------------------------------------------------------------------
		// http リクエストを処理する汎用関数
		// ＜例外＞
		// --------------------------------------------------------------------
		private Task<HttpResponseMessage> HttpMethod(String oUrl, CoreHttpDg oCoreDg, Object oCoreDgOption)
		{
			String aErr = null;
			Task<HttpResponseMessage> aRes = null;

			// クライアントの設定
			SetClient(oUrl);

			// ダウンロード
			for (Int32 i = 0; i < GET_RETRY_MAX; i++)
			{
				try
				{
					// http コアリクエスト（デリゲート）
					aRes = oCoreDg(oUrl, oCoreDgOption);
					aRes.Wait();
					ThrowIfCancellationRequested();

					// 成功したら関数から返る
					if (aRes.Result.IsSuccessStatusCode)
					{
						return aRes;
					}
				}
				catch (OperationCanceledException oExcep)
				{
					// ユーザーからの中止指示の場合は、直ちに再スロー
					throw oExcep;
				}
				catch (Exception oExcep)
				{
					// エラーメッセージを一旦設定するが、後のコードでリトライして、結果的に成功することはありえる
					aErr = oExcep.Message;
				}

				// 失敗した場合
				if (aRes != null && aRes.Result.StatusCode == HttpStatusCode.RequestTimeout)
				{
					// タイムアウトなら、一定時間後にリトライする
#if DEBUGz
					MessageBox.Show("[DEBUG] Downloader::HttpMethod()\nデリゲート：" + oCoreDg.ToString() + "\nHTTP ステータスエラー：リトライします");
#endif
					Wait(GET_RETRY_INTERVAL);
				}
				else
				{
					// 直ちに失敗が確定
					i = GET_RETRY_MAX;
				}
			}

			// リトライしても成功しなかったので、エラー確定
			throw new Exception(aErr);
		}

		// --------------------------------------------------------------------
		// http クライアントの設定
		// --------------------------------------------------------------------
		private void SetClient(String oUrl)
		{
			// ハンドラが作成されていない場合は作成する
			if (ClientHandler == null)
			{
				WebRequestHandler aWebRequestHandler = new WebRequestHandler();
				aWebRequestHandler.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
				aWebRequestHandler.UseCookies = true;
				ClientHandler = aWebRequestHandler;
			}

			// クライアントが作成されていない場合は作成する
			if (mClient == null)
			{
				mClient = new HttpClient(ClientHandler);
			}

			// リクエストヘッダーを設定
			mClient.DefaultRequestHeaders.Clear();
			mClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
			mClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
			mClient.DefaultRequestHeaders.Add("Accept-Language", "ja,en-us;q=0.7,en;q=0.3");
			mClient.DefaultRequestHeaders.Add("Referer", oUrl);
		}

		// --------------------------------------------------------------------
		// 中止指示があったら例外を発生させる
		// ＜例外＞
		// --------------------------------------------------------------------
		private void ThrowIfCancellationRequested()
		{
			if (CancellationToken != null)
			{
				CancellationToken.ThrowIfCancellationRequested();
			}
#if USE_ITERMINATE_THREAD
			if (OwnerThread != null && OwnerThread.TerminateRequested)
			{
				throw new Exception(ERROR_MESSAGE_THREAD_TERMINATED);
			}
#endif
		}

		// --------------------------------------------------------------------
		// 指定時間 [ms] スレッドを中断
		// ＜例外＞
		// --------------------------------------------------------------------
		private void Wait(Int32 oWaitMS)
		{
			Int32 aRestTime = 0;

			while (aRestTime < oWaitMS)
			{
				Thread.Sleep(THREAD_SLEEP_INTERVAL);
				aRestTime += THREAD_SLEEP_INTERVAL;
				ThrowIfCancellationRequested();
			}
		}

	}
}

