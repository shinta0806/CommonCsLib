// ============================================================================
// 
// ニコニコ動画 API で受信するコンテンツツリー JSON の子動画 1 動画分の情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 各種 ID 数値は多めにみて Int64 にしておく
// ----------------------------------------------------------------------------

namespace Shinta.Niconico.Json;

internal class TreeVideoInfo
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public TreeVideoInfo()
	{
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// "external"
	/// </summary>
	public String? Kind
	{
		get;
		set;
	}

	/// <summary>
	/// 恐らくニコニコ動画内部用の ID
	/// </summary>
	public Int64 Id
	{
		get;
		set;
	}

	/// <summary>
	/// sm 番号
	/// </summary>
	public String? GlobalId
	{
		get;
		set;
	}

	/// <summary>
	/// sm 番号の数値部分
	/// </summary>
	public Int64 ContentId
	{
		get;
		set;
	}

	/// <summary>
	/// "video"
	/// </summary>
	public String? ContentKind
	{
		get;
		set;
	}

	/// <summary>
	/// "visible"
	/// </summary>
	public String? VisibleStatus
	{
		get;
		set;
	}

	/// <summary>
	/// 投稿日時ではなく、コンテンツツリー登録日時と思われる
	/// "2024-03-29 21:40:42"
	/// </summary>
	public String? Created
	{
		get;
		set;
	}

	/// <summary>
	/// コンテンツツリー修正日時？
	/// </summary>
	public String? Updated
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
	/// ニコニコ動画のロゴ画像
	/// </summary>
	public String? LogoUrl
	{
		get;
		set;
	}

	/// <summary>
	/// 動画視聴用 URL
	/// </summary>
	public String? WatchUrl
	{
		get;
		set;
	}

	/// <summary>
	/// コンテンツツリー URL
	/// </summary>
	public String? TreeUrl
	{
		get;
		set;
	}

	/// <summary>
	/// コンテンツツリー編集用 URL
	/// </summary>
	public String? TreeEditUrl
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
	/// 親作品の数
	/// </summary>
	public Int32 ParentsCount
	{
		get;
		set;
	}

	/// <summary>
	/// 子作品の数
	/// </summary>
	public Int32 ChildrenCount
	{
		get;
		set;
	}

	/// <summary>
	/// 何が編集可能なのか？
	/// </summary>
	public Boolean IsEditable
	{
		get;
		set;
	}
}
