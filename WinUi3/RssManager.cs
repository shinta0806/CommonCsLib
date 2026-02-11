// ============================================================================
// 
// RSS を解析・管理するクラス
// Copyright (C) 2022-2025 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Serilog.Sinks.File
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// 初回読み込み時（既読情報が無い場合）は、読み込んだ RSS アイテムを更新情報として扱わない
// RSS 2.0 のみ対応
// SSL 対応済
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/11/19 (Sat) | WPF 版を元に作成開始。
//  1.00  | 2022/11/19 (Sat) | ファーストバージョン。
// (1.01) | 2023/08/16 (Wed) |   既読アイテム保持の最大数のデフォルト値を 20 に変更。
//  1.10  | 2023/08/19 (Sat) | 保存を JSON 形式に変更。
//  1.20  | 2025/03/19 (Wed) | AOT 対応。
//  1.30  | 2025/07/30 (Wed) | Load2() を作成。
// ============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Shinta.WinUi3;

// ========================================================================
// 
// RSS を解析・管理するクラス
// 
// ========================================================================

internal class RssManager
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	/// <param name="settingsPath">絶対パスでも相対パスでも可</param>
	public RssManager(String settingsPath)
	{
		_settingsPath = settingsPath;
		String? folder = Path.GetDirectoryName(settingsPath);
		if (!String.IsNullOrEmpty(folder))
		{
			_settingsPath2 = folder + "\\";
		}
		_settingsPath2 += Path.GetFileNameWithoutExtension(settingsPath) + "2" + Path.GetExtension(settingsPath);
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 既読の RSS を取得した日付
	/// </summary>
	public DateTime PastDownloadDate
	{
		get;
		set;
	}

	/// <summary>
	/// 過去に読み込んだ既読アイテム（新しい項目が先頭、guid のみ）
	/// </summary>
	public List<String> PastRssGuids
	{
		get;
		set;
	} = new();

	/// <summary>
	/// 最新の RSS をダウンロードする間隔（日数）
	/// </summary>
	public Int32 CheckLatestInterval
	{
		get;
		set;
	} = CHECK_LATEST_INTERVAL_DEFAULT;

	/// <summary>
	/// 既読アイテム保持の最大数
	/// </summary>
	public Int32 PastRssGuidsCapacity
	{
		get;
		set;
	} = PAST_RSS_GUIDS_CAPACITY_DEFAULT;

	/// <summary>
	/// ユーザーエージェント
	/// </summary>
	public String UserAgent
	{
		get => _downloader.UserAgent;
		set => _downloader.UserAgent = value;
	}

	/// <summary>
	/// 終了要求制御
	/// </summary>
	public CancellationToken CancellationToken
	{
		get => _downloader.CancellationToken;
		set => _downloader.CancellationToken = value;
	}

	// ====================================================================
	// public 定数
	// ====================================================================

	/// <summary>
	/// XML ノード名
	/// </summary>
	public const String NODE_NAME_CHANNEL = "channel";
	public const String NODE_NAME_CLOUD = "cloud";
	public const String NODE_NAME_GUID = "guid";
	public const String NODE_NAME_ITEM = "item";
	public const String NODE_NAME_LINK = "link";
	public const String NODE_NAME_TITLE = "title";

	/// <summary>
	/// XML 属性名
	/// </summary>
	public const String ATTRIBUTE_NAME_APP_VER_MAX = "appvermax";
	public const String ATTRIBUTE_NAME_APP_VER_MIN = "appvermin";
	public const String ATTRIBUTE_NAME_MD5 = "md5";
	public const String ATTRIBUTE_NAME_URL = "url";

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// 読み込んだ RSS の全項目（RSS で最初に記述されている項目が先頭）
	/// ただし、guid が無いものは除く
	/// </summary>
	/// <returns></returns>
	public List<RssItem> GetAllItems()
	{
		List<RssItem> allItems = [.. _latestRssItems];
		return allItems;
	}

	/// <summary>
	/// 最新 RSS で追加された項目を取得（RSS で最初に記述されている項目が先頭）
	/// </summary>
	/// <param name="forceFirst">過去にチェックを行っていない場合も追加項目を取得</param>
	/// <returns></returns>
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

	/// <summary>
	/// 最新 RSS のダウンロードおよび更新通知が必要か
	/// </summary>
	/// <returns></returns>
	public Boolean IsDownloadNeeded()
	{
		TimeSpan diffDate = new(CheckLatestInterval, 0, 0, 0);
		return (PastDownloadDate == DateTime.MinValue)
				|| (DateTime.Now.Date - PastDownloadDate.Date >= diffDate);
	}

	/// <summary>
	/// 過去の RSS 管理情報を読み込む
	/// </summary>
	public void Load()
	{
#if !USE_AOT
		// AOT 非対応版
		RssSettings rssSettings = _jsonManager.Load<RssSettings>(_settingsPath, false, _jsonSerializerOptions);
#else
		// AOT 対応版
		RssSettings rssSettings = _jsonManager.LoadAot(_settingsPath, false, RmJsonSerializerContext.Default.RssSettings);
#endif
		PastDownloadDate = rssSettings.PastDownloadDate;
		PastRssGuids = rssSettings.PastRssGuids;
	}

	/// <summary>
	/// ローカルに保存済の RSS の内容を読み込む（ファイルが無い場合などは例外）
	/// </summary>
	/// <returns></returns>
	public List<RssItem> LoadRssItems()
	{
#if !USE_AOT
		// AOT 非対応版では保存していない
		throw new NotImplementedException("AOT 非対応版では Load2() は実装されていません。");
#else
		// AOT 対応版
		List<RssItem> rssItems = _jsonManager.LoadAot(_settingsPath2, false, RmJsonSerializerContext.Default.ListRssItem);
		return rssItems;
#endif
	}

	/// <summary>
	/// 最新 RSS のダウンロード
	/// </summary>
	/// <param name="source"></param>
	/// <param name="appVer"></param>
	/// <returns></returns>
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
		catch (Exception ex)
		{
			errorMessage = ex.Message;
#if DEBUGz
			var i = ex.InnerException;
#endif
		}
		finally
		{
			stream?.Close();
		}
		return (result, errorMessage);
	}

	/// <summary>
	/// RSS 管理情報を保存
	/// </summary>
	public void Save()
	{
		RssSettings rssSettings = new()
		{
			PastDownloadDate = PastDownloadDate,
			PastRssGuids = PastRssGuids
		};
#if !USE_AOT
		// AOT 非対応版
		_jsonManager.Save(rssSettings, _settingsPath, false, _jsonSerializerOptions);
#else
		// AOT 対応版
		_jsonManager.SaveAot(rssSettings, _settingsPath, false, RmJsonSerializerContext.Default.RssSettings);
		_jsonManager.SaveAot(_latestRssItems, _settingsPath2, false, RmJsonSerializerContext.Default.ListRssItem);
#endif
	}

	/// <summary>
	/// 最新 RSS で追加された項目を既読にする
	/// </summary>
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

	/// <summary>
	/// n 日ごとに RSS を確認するのデフォルト値
	/// </summary>
	private const Int32 CHECK_LATEST_INTERVAL_DEFAULT = 3;

	/// <summary>
	/// 既読アイテム保持の最大数のデフォルト値
	/// </summary>
	private const Int32 PAST_RSS_GUIDS_CAPACITY_DEFAULT = 20;

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// RSS ダウンローダー
	/// </summary>
	private readonly Downloader _downloader = new();

	/// <summary>
	/// 最新の RSS に含まれるアイテム（新しい項目が先頭）
	/// </summary>
	private readonly List<RssItem> _latestRssItems = new();

	/// <summary>
	/// 最新の RSS を取得した時点の日付（ゆくゆくは PastDownloadDate をこの値で上書きすることになる）
	/// </summary>
	private DateTime _latestDownloadDate;

	/// <summary>
	/// 保存パス（絶対パスでも相対パスでも可）：RssSettings（ほぼ Guid のみ）を保存するためのパス
	/// </summary>
	private readonly String _settingsPath;

	/// <summary>
	/// 保存パス（絶対パスでも相対パスでも可）：内容も保存するためのパス（歴史的経緯により _settingsPath と併存）
	/// </summary>
	private readonly String _settingsPath2;

	/// <summary>
	/// 設定保存管理
	/// </summary>
	private readonly JsonManager _jsonManager = new();

	/// <summary>
	/// 設定保存時のオプション
	/// </summary>
	private readonly JsonSerializerOptions _jsonSerializerOptions = new();

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// 認識すべき RSS 項目か
	/// </summary>
	/// <param name="appVer"></param>
	/// <param name="rssItem"></param>
	/// <returns></returns>
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

	/// <summary>
	/// cloud タグの処理（RSS サイト負荷監視用カウンターを回す（中身は読み捨てる））
	/// </summary>
	/// <param name="cloud"></param>
	/// <returns></returns>
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

	/// <summary>
	/// RSS を読み込む中心部分（解析）
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="appVer"></param>
	/// <returns></returns>
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
		catch (Exception ex)
		{
			errorMessage = ex.Message;
		}
		return (result, errorMessage);
	}

	/// <summary>
	/// 最大値を超えた既読アイテムを捨てる
	/// </summary>
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
// RssSettings を AOT 下で JSON 化するためのクラス
// 
// ========================================================================

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<RssItem>))]
[JsonSerializable(typeof(RssSettings))]
internal partial class RmJsonSerializerContext : JsonSerializerContext
{
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

	/// <summary>
	/// 要素情報（[要素名/属性名] = 値）
	/// </summary>
	public Dictionary<String, String> Elements
	{
		get;
		set;
	} = new();

	/// <summary>
	/// 記事のタイトル
	/// </summary>
	[JsonIgnore]
	public String Title
	{
		get
		{
			if (Elements.TryGetValue(RssManager.NODE_NAME_TITLE, out String? title))
			{
				return title;
			}
			return String.Empty;
		}
	}

	/// <summary>
	/// 記事のタイトル（Guid が日付であることを前提とした日付を付加）
	/// </summary>
	[JsonIgnore]
	public String TitleAndDate
	{
		get
		{
			String date = String.Empty;
			String guid = Guid;
			if (!String.IsNullOrEmpty(guid))
			{
				date = "（" + guid[0..10] + "）";
			}
			return Title + date;
		}
	}

	/// <summary>
	/// 記事へのリンク
	/// </summary>
	[JsonIgnore]
	public String Link
	{
		get
		{
			if (Elements.TryGetValue(RssManager.NODE_NAME_LINK, out String? link))
			{
				return link;
			}
			return String.Empty;
		}
	}

	/// <summary>
	/// 記事の Guid（実際には日付＋αのことが多い）
	/// </summary>
	[JsonIgnore]
	public String Guid
	{
		get
		{
			if (Elements.TryGetValue(RssManager.NODE_NAME_GUID, out String? guid))
			{
				return guid;
			}
			return String.Empty;
		}
	}

	// ====================================================================
	// public 定数
	// ====================================================================

	/// <summary>
	/// RssItem の要素名と属性の区切り
	/// </summary>
	public const String RSS_ITEM_NAME_DELIMITER = "/";
}

// ========================================================================
// 
// RSS 管理情報を保存するためのクラス
// 
// ========================================================================

public class RssSettings
{
	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 既読の RSS を取得した日付
	/// </summary>
	public DateTime PastDownloadDate
	{
		get;
		set;
	}

	/// <summary>
	/// 過去に読み込んだ既読アイテム（新しい項目が先頭、guid のみ）
	/// </summary>
	public List<String> PastRssGuids
	{
		get;
		set;
	} = new();
}

