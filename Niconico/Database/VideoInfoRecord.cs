// ============================================================================
// 
// 子動画 1 動画分のレコード情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 容量節約のため、TreeVideoInfo のうちの一部のみをレコード化
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

using Shinta.Niconico.Json;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shinta.Niconico.Database;

[Table(TABLE_NAME_VIDEO_INFO)]
[Index(nameof(UserId))]
[Index(nameof(UpdateTime))]
internal class VideoInfoRecord
{
	// ====================================================================
	// public 定数
	// ====================================================================

	// --------------------------------------------------------------------
	// コア
	// --------------------------------------------------------------------

	public const String NAME_CORE = "video_info";

	// --------------------------------------------------------------------
	// テーブル名
	// --------------------------------------------------------------------

	public const String TABLE_NAME_VIDEO_INFO = "t_" + NAME_CORE;

	// --------------------------------------------------------------------
	// フィールド名
	// --------------------------------------------------------------------

	public const String FIELD_NAME_AUTO_KEY = NAME_CORE + "_auto_key";
	public const String FIELD_NAME_CONTENT_ID = NAME_CORE + "_content_id";
	public const String FIELD_NAME_GLOBAL_ID = NAME_CORE + "_global_id";
	public const String FIELD_NAME_CONTENT_KIND = NAME_CORE + "_content_kind";
	public const String FIELD_NAME_CREATED = NAME_CORE + "_created";
	public const String FIELD_NAME_TITLE = NAME_CORE + "_title";
	public const String FIELD_NAME_WATCH_URL = NAME_CORE + "_watch_url";
	public const String FIELD_NAME_THUMBNAIL_URL = NAME_CORE + "_thumbnail_url";
	public const String FIELD_NAME_DESCRIPTION = NAME_CORE + "_description";
	public const String FIELD_NAME_USER_ID = NAME_CORE + "_user_id";

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 主キー（ContentId は重複がありえるため）
	/// </summary>
	[Key]
	[Column(FIELD_NAME_AUTO_KEY)]
	public Int64 AutoKey
	{
		get;
		set;
	}

	/// <summary>
	/// sm 番号の数値部分
	/// 
	/// </summary>
	[Column(FIELD_NAME_CONTENT_ID)]
	public Int64 ContentId
	{
		get;
		set;
	}

	/// <summary>
	/// sm 番号
	/// </summary>
	[Column(FIELD_NAME_GLOBAL_ID)]
	public String GlobalId
	{
		get;
		set;
	} = String.Empty;

	/// <summary>
	/// "video"
	/// </summary>
	[Column(FIELD_NAME_CONTENT_KIND)]
	public String ContentKind
	{
		get;
		set;
	} = String.Empty;

	/// <summary>
	/// 投稿日時ではなく、コンテンツツリー登録日時と思われる
	/// "2024-03-29 21:40:42"
	/// </summary>
	[Column(FIELD_NAME_CREATED)]
	public String Created
	{
		get;
		set;
	} = String.Empty;

	/// <summary>
	/// 動画タイトル
	/// </summary>
	[Column(FIELD_NAME_TITLE)]
	public String Title
	{
		get;
		set;
	} = String.Empty;

	/// <summary>
	/// 動画視聴用 URL
	/// </summary>
	[Column(FIELD_NAME_WATCH_URL)]
	public String WatchUrl
	{
		get;
		set;
	} = String.Empty;

	/// <summary>
	/// 動画サムネイル URL
	/// </summary>
	[Column(FIELD_NAME_THUMBNAIL_URL)]
	public String ThumbnailUrl
	{
		get;
		set;
	} = String.Empty;

	/// <summary>
	/// 投稿者による説明文（サイト上で「動画情報」と表記されるもの）
	/// </summary>
	[Column(FIELD_NAME_DESCRIPTION)]
	public String? Description
	{
		get;
		set;
	}

	/// <summary>
	/// 投稿者のユーザー ID
	/// </summary>
	[Column(FIELD_NAME_USER_ID)]
	public Int64 UserId
	{
		get;
		set;
	}

	/// <summary>
	/// データベースレコード更新日時 UTC（修正ユリウス日）
	/// </summary>
	public Double UpdateTime
	{
		get;
		set;
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// 必須事項（全部）が埋まっているか
	/// </summary>
	/// <returns></returns>
	public Boolean CheckRequired()
	{
		try
		{
			if (ContentId == 0)
			{
				throw new Exception("ContentId");
			}
			if (String.IsNullOrEmpty(GlobalId))
			{
				throw new Exception("GlobalId");
			}
			if (String.IsNullOrEmpty(ContentKind))
			{
				throw new Exception("ContentKind");
			}
			if (String.IsNullOrEmpty(Created))
			{
				throw new Exception("Created");
			}
			if (String.IsNullOrEmpty(Title))
			{
				throw new Exception("Title");
			}
			if (String.IsNullOrEmpty(WatchUrl))
			{
				throw new Exception("WatchUrl");
			}
			if (String.IsNullOrEmpty(ThumbnailUrl))
			{
				throw new Exception("ThumbnailUrl");
			}
			if (UserId == 0)
			{
				throw new Exception("UserId");
			}
			if (UpdateTime == 0.0)
			{
				throw new Exception("UpdateTime");
			}
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("動画情報の " + ex.Message + " が不足しています：" + GlobalId);
			return false;
		}
	}

	/// <summary>
	/// TreeVideoInfo から情報をインポート
	/// </summary>
	/// <param name="treeVideoInfo"></param>
	/// <returns></returns>
	public Boolean ImportFrom(TreeVideoInfo treeVideoInfo, Boolean importDescription)
	{
		ContentId = treeVideoInfo.ContentId;
		GlobalId = treeVideoInfo.GlobalId ?? String.Empty;
		ContentKind = treeVideoInfo.ContentKind ?? String.Empty;
		Created = treeVideoInfo.Created ?? String.Empty;
		Title = treeVideoInfo.Title ?? String.Empty;
		WatchUrl = treeVideoInfo.WatchUrl ?? String.Empty;
		ThumbnailUrl = treeVideoInfo.ThumbnailUrl ?? String.Empty;
		if (importDescription)
		{
			Description = treeVideoInfo.Description;
		}
		UserId = treeVideoInfo.UserId;
		UpdateTime = JulianDay.DateTimeToModifiedJulianDate(DateTime.UtcNow);
		return CheckRequired();
	}
}
