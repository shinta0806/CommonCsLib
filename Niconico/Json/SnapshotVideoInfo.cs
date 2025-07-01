// ============================================================================
// 
// スナップショット検索 API v2 で受信する結果 JSON の 1 動画分の情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
// https://site.nicovideo.jp/search-api-docs/snapshot
// ContentId が String なので TreeVideoInfo と共通化できない
// ----------------------------------------------------------------------------

namespace Shinta.Niconico.Json;

internal class SnapshotVideoInfo
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public SnapshotVideoInfo()
	{
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// sm 番号
	/// </summary>
	public String? ContentId
	{
		get;
		set;
	}

	/// <summary>
	/// 動画タイトル
	/// </summary>
	public String? Title
	{
		get;
		set;
	}

	/// <summary>
	/// 投稿者による説明文（サイト上で「動画情報」と表記されるもの）
	/// </summary>
	public String? Description
	{
		get;
		set;
	}

	/// <summary>
	/// 投稿者のユーザー ID
	/// </summary>
	public Int64 UserId
	{
		get;
		set;
	}

	/// <summary>
	/// 再生時間 [s]
	/// </summary>
	public Int32 LengthSeconds
	{
		get;
		set;
	}

	/// <summary>
	/// 動画サムネイル URL
	/// </summary>
	public String? ThumbnailUrl
	{
		get;
		set;
	}

	/// <summary>
	/// 投稿日時
	/// "2024-03-29 21:40:42"（かな？　未確認）
	/// </summary>
	public String? StartTime
	{
		get;
		set;
	}
}
