// ============================================================================
// 
// RSS を解析・管理するクラス
// Copyright (C) 2014-2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 初回読み込み時（既読情報が無い場合）は、読み込んだ RSS アイテムを更新情報として扱わない
// RSS 2.0 のみ対応
// SSL 対応済
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/12/28 (Sun) | 作成開始。
//  1.00  | 2015/01/02 (Fri) | オリジナルバージョン。
//  1.10  | 2015/01/10 (Sat) | ITerminatableThread インターフェースを利用するようにした。
// (1.11) | 2020/05/05 (Tue) |   StatusT を廃止。
//  2.00  | 2021/08/29 (Sun) | SerializableSettings 派生に変更。
// (2.01) | 2021/08/30 (Mon) |   再構築後の Downloader に対応。
// (2.02) | 2021/09/01 (Wed) |   保存部分のみを SerializableSettings 派生とした。
// (2.03) | 2021/09/04 (Sat) |   SerializableSettings の更新に対応。
// (2.04) | 2021/10/28 (Thu) |   軽微なリファクタリング。
//  2.10  | 2022/02/26 (Sat) | アプリケーションバージョンによる選別機能を付けた。
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Shinta
{
	// ========================================================================
	// 
	// RSS を解析・管理するクラス
	// 
	// ========================================================================

	public class RssManager
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター（引数あり）
		// --------------------------------------------------------------------
		public RssManager(LogWriter? logWriter, String settingsPath)
		{
			_logWriter = logWriter;
			_settingsPath = settingsPath;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 既読の RSS を取得した日付
		public DateTime PastDownloadDate { get; set; }

		// 過去に読み込んだ既読アイテム（新しい項目が先頭、guid のみ）
		public List<String> PastRssGuids { get; set; } = new();

		// 最新の RSS をダウンロードする間隔（日数）
		public Int32 CheckLatestInterval { get; set; } = CHECK_LATEST_INTERVAL_DEFAULT;

		// 既読アイテム保持の最大数
		public Int32 PastRssGuidsCapacity { get; set; } = PAST_RSS_GUIDS_CAPACITY_DEFAULT;

		// ユーザーエージェント
		public String UserAgent
		{
			get => _downloader.UserAgent;
			set => _downloader.UserAgent = value;
		}

		// 終了要求制御
		public CancellationToken CancellationToken
		{
			get => _downloader.CancellationToken;
			set => _downloader.CancellationToken = value;
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		// XML ノード名
		public const String NODE_NAME_CHANNEL = "channel";
		public const String NODE_NAME_CLOUD = "cloud";
		public const String NODE_NAME_GUID = "guid";
		public const String NODE_NAME_ITEM = "item";
		public const String NODE_NAME_LINK = "link";
		public const String NODE_NAME_TITLE = "title";

		// XML 属性名
		public const String ATTRIBUTE_NAME_APP_VER_MAX = "appvermax";
		public const String ATTRIBUTE_NAME_APP_VER_MIN = "appvermin";
		public const String ATTRIBUTE_NAME_MD5 = "md5";
		public const String ATTRIBUTE_NAME_URL = "url";

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込んだ RSS の全項目（RSS で最初に記述されている項目が先頭）
		// ただし、guid が無いものは除く
		// --------------------------------------------------------------------
		public List<RssItem> GetAllItems()
		{
			List<RssItem> allItems = new(_latestRssItems);
			return allItems;
		}

		// --------------------------------------------------------------------
		// 最新 RSS で追加された項目を取得（RSS で最初に記述されている項目が先頭）
		// ＜引数＞ forceFirst: 過去にチェックを行っていない場合も追加項目を取得
		// --------------------------------------------------------------------
		public List<RssItem> GetNewItems(Boolean forceFirst = false)
		{
			List<RssItem> newItems = new();

			// 既読情報が無い（過去にチェックを行っていない）場合は、更新情報として扱わない
			if (PastDownloadDate == DateTime.MinValue && !forceFirst)
			{
				return newItems;
			}

			foreach (RssItem rssItem in _latestRssItems)
			{
				if (!rssItem.Elements.TryGetValue(NODE_NAME_GUID, out String? guid) || String.IsNullOrEmpty(guid))
				{
					continue;
				}

				// 既読の guid と一致しなければ新しい
				if (!PastRssGuids.Contains(guid))
				{
					newItems.Add(rssItem);
				}
			}

			return newItems;
		}

		// --------------------------------------------------------------------
		// 最新 RSS のダウンロードおよび更新通知が必要か
		// --------------------------------------------------------------------
		public Boolean IsDownloadNeeded()
		{
			TimeSpan diffDate = new(CheckLatestInterval, 0, 0, 0);
			return (PastDownloadDate == DateTime.MinValue)
					|| (DateTime.Now.Date - PastDownloadDate.Date >= diffDate);
		}

		// --------------------------------------------------------------------
		// 過去の RSS 管理情報を読み込む
		// --------------------------------------------------------------------
		public void Load()
		{
			RssSettings rssSettings = new(_logWriter, _settingsPath);
			rssSettings.Load();

			PastDownloadDate = rssSettings.PastDownloadDate;
			PastRssGuids = rssSettings.PastRssGuids;
		}

		// --------------------------------------------------------------------
		// 最新 RSS のダウンロード
		// --------------------------------------------------------------------
		public async Task<(Boolean result, String? errorMessage)> ReadLatestRssAsync(String source, String appVer)
		{
			Boolean result = false;
			Stream? stream = null;
			String? errorMessage = null;

			try
			{
				if (String.Compare(source, 0, "http://", 0, 7) == 0
						|| String.Compare(source, 0, "https://", 0, 8) == 0
						|| String.Compare(source, 0, "ftp://", 0, 6) == 0
						|| String.Compare(source, 0, "file://", 0, 7) == 0)
				{
					// URL 形式のソースからダウンロードしたストリームを作成
					stream = new MemoryStream();
					HttpResponseMessage response = await _downloader.DownloadAsStreamAsync(source, stream);
					if (!response.IsSuccessStatusCode)
					{
						throw new Exception(response.StatusCode.ToString());
					}
					stream.Position = 0;
				}
				else
				{
					// パスで指定されたファイルでストリームを作成
					stream = new FileStream(source, FileMode.Open);
				}

				(result, errorMessage) = await ReadLatestRssCoreAsync(stream, appVer);
				if (!result)
				{
					throw new Exception(errorMessage);
				}
			}
			catch (Exception excep)
			{
				errorMessage = excep.Message;
#if DEBUG
				var i = excep.InnerException;
#endif
			}
			finally
			{
				stream?.Close();
			}
			return (result, errorMessage);
		}

		// --------------------------------------------------------------------
		// RSS 管理情報を保存
		// --------------------------------------------------------------------
		public void Save()
		{
			RssSettings rssSettings = new(_logWriter, _settingsPath);
			rssSettings.PastDownloadDate = PastDownloadDate;
			rssSettings.PastRssGuids = PastRssGuids;
			rssSettings.Save();
		}

		// --------------------------------------------------------------------
		// 最新 RSS で追加された項目を既読にする
		// --------------------------------------------------------------------
		public void UpdatePastRss()
		{
			List<RssItem> newItems = GetNewItems(true);
			List<String> addGuids = new();
			foreach (RssItem newItem in newItems)
			{
				if (!newItem.Elements.TryGetValue(NODE_NAME_GUID, out String? guid) || String.IsNullOrEmpty(guid))
				{
					continue;
				}

				addGuids.Add(guid);
			}
			PastRssGuids.InsertRange(0, addGuids);
			Truncate();

			// 取得日を更新
			PastDownloadDate = _latestDownloadDate;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// n 日ごとに RSS を確認するのデフォルト値
		private const Int32 CHECK_LATEST_INTERVAL_DEFAULT = 3;

		// 既読アイテム保持の最大数のデフォルト値
		private const Int32 PAST_RSS_GUIDS_CAPACITY_DEFAULT = 10;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// RSS ダウンローダー
		private readonly Downloader _downloader = new();

		// 最新の RSS に含まれるアイテム（新しい項目が先頭）
		private readonly List<RssItem> _latestRssItems = new();

		// 最新の RSS を取得した時点の日付（ゆくゆくは PastDownloadDate をこの値で上書きすることになる）
		private DateTime _latestDownloadDate;

		// 保存パス
		private readonly String _settingsPath;

		// ログ
		private readonly LogWriter? _logWriter;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 認識すべき RSS 項目か
		// --------------------------------------------------------------------
		private static Boolean IsValidRssItem(String appVer, RssItem rssItem)
		{
			// アイテム内に guid が無ければ無効
			if (!rssItem.Elements.TryGetValue(NODE_NAME_GUID, out String? guid) || String.IsNullOrEmpty(guid))
			{
				return false;
			}

			// appVer が appvermin 未満なら無効
			if (rssItem.Elements.TryGetValue(NODE_NAME_GUID + RssItem.RSS_ITEM_NAME_DELIMITER + ATTRIBUTE_NAME_APP_VER_MIN, out String? appVerMin)
					&& Common.CompareVersionString(appVer, appVerMin) < 0)
			{
				return false;
			}

			// appVer が appvermax 超過なら無効
			if (rssItem.Elements.TryGetValue(NODE_NAME_GUID + RssItem.RSS_ITEM_NAME_DELIMITER + ATTRIBUTE_NAME_APP_VER_MAX, out String? appVerMax)
					&& Common.CompareVersionString(appVer, appVerMax) > 0)
			{
				return false;
			}

			return true;
		}

		// --------------------------------------------------------------------
		// cloud タグの処理（RSS サイト負荷監視用カウンターを回す（中身は読み捨てる））
		// --------------------------------------------------------------------
		private async Task<Boolean> LoadCloudAsync(XmlNode cloud)
		{
			Boolean result = false;

			try
			{
				XmlAttributeCollection? attrs = cloud.Attributes;
				if (attrs == null)
				{
					return false;
				}
				XmlAttribute? url = attrs[ATTRIBUTE_NAME_URL];
				if (url == null)
				{
					return false;
				}
				using MemoryStream memoryStream = new();
				await _downloader.DownloadAsStreamAsync(url.Value, memoryStream);
				result = true;
			}
			catch (Exception)
			{
			}

			return result;
		}

		// --------------------------------------------------------------------
		// RSS を読み込む中心部分（解析）
		// --------------------------------------------------------------------
		private async Task<(Boolean result, String? errorMessage)> ReadLatestRssCoreAsync(Stream stream, String appVer)
		{
			Boolean result = false;
			String? errorMessage = null;

			try
			{
#if DEBUGz
				await Task.Delay(3000);
#endif
				_latestRssItems.Clear();

				// パーサーに読み込ませる
				XmlDocument xml = new();
				xml.Load(stream);

				// ルートとチャンネル
				if (xml.DocumentElement == null)
				{
					throw new Exception("ルート要素がありません。");
				}
				XmlNode? channel = xml.DocumentElement.FirstChild;
				if (channel == null || channel.Name != NODE_NAME_CHANNEL)
				{
					throw new Exception(NODE_NAME_CHANNEL + " 要素がありません。");
				}

				// アイテムを取り出す
				foreach (XmlNode item in channel.ChildNodes)
				{
					if (item.Name == NODE_NAME_CLOUD)
					{
						// RSS 負荷監視（エラー発生でも続行）
						await LoadCloudAsync(item);
						continue;
					}
					else if (item.Name != NODE_NAME_ITEM)
					{
						continue;
					}

					// アイテムタグの処理
					RssItem rssItem = new();
					foreach (XmlNode leaf in item.ChildNodes)
					{
						// 要素名と値
						rssItem.Elements[leaf.Name] = leaf.InnerText;
#if DEBUGz
						if (leaf.Name == NODE_NAME_GUID)
						{
						}
#endif

						if (leaf.Attributes == null)
						{
							continue;
						}
						foreach (XmlNode attr in leaf.Attributes)
						{
							// 属性と値
							rssItem.Elements[leaf.Name + RssItem.RSS_ITEM_NAME_DELIMITER + attr.Name] = attr.Value ?? String.Empty;
						}
					}
#if DEBUGz
					String DB = String.Empty;
					foreach (KeyValuePair<String, String> DB1 in aRSSItem.Elements)
					{
						DB += DB1.Key + "=" + DB1.Value + "\n";
					}
					MessageBox.Show(DB);
#endif

					if (!IsValidRssItem(appVer, rssItem))
					{
						continue;
					}

					_latestRssItems.Add(rssItem);
				}


				// 日付更新
				_latestDownloadDate = DateTime.Now.Date;
				result = true;
			}
			catch (Exception excep)
			{
				errorMessage = excep.Message;
			}
			return (result, errorMessage);
		}

		// --------------------------------------------------------------------
		// 最大値を超えた既読アイテムを捨てる
		// --------------------------------------------------------------------
		private void Truncate()
		{
			if (PastRssGuids.Count > PastRssGuidsCapacity)
			{
				PastRssGuids.RemoveRange(PastRssGuidsCapacity, PastRssGuids.Count - PastRssGuidsCapacity);
			}
		}
	}

	// ========================================================================
	// 
	// 1 つの RSS に詰め込まれている情報群を保持しておくためのクラス
	// 
	// ========================================================================

	public class RssItem
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 要素情報（[要素名/属性名] = 値）
		public Dictionary<String, String> Elements { get; set; } = new();

		// ====================================================================
		// public 定数
		// ====================================================================

		// RssItem の要素名と属性の区切り
		public const String RSS_ITEM_NAME_DELIMITER = "/";
	}

	// ========================================================================
	// 
	// RSS 管理情報を保存するためのクラス
	// 
	// ========================================================================

	public class RssSettings : SerializableSettings
	{
		// --------------------------------------------------------------------
		// コンストラクター（引数あり）
		// --------------------------------------------------------------------
		public RssSettings(LogWriter? logWriter, String settingsPath)
				: base(logWriter)
		{
			_settingsPath = settingsPath;
		}

		// --------------------------------------------------------------------
		// コンストラクター（引数なし：シリアライズに必要）
		// --------------------------------------------------------------------
		public RssSettings()
				: base(null)
		{
			_settingsPath = String.Empty;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 既読の RSS を取得した日付
		public DateTime PastDownloadDate { get; set; }

		// 過去に読み込んだ既読アイテム（新しい項目が先頭、guid のみ）
		public List<String> PastRssGuids { get; set; } = new();

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		public override String SettingsPath()
		{
			return _settingsPath;
		}

		// ====================================================================
		// private 変数
		// ====================================================================

		// 保存パス
		private readonly String _settingsPath;
	}
}

