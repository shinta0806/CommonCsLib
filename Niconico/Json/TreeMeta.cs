// ============================================================================
// 
// ニコニコ動画 API で受信するコンテンツツリー JSON の通信情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

namespace Shinta.Niconico.Json;

internal class TreeMeta
{
	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 通信応答
	/// </summary>
	public Int32 Status
	{
		get;
		set;
	}
}
