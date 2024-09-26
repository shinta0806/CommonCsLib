// ============================================================================
// 
// データベースプロパティーのレコード情報
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2024/09/25 (Wed) | ファーストバージョン。
//  1.10  | 2024/09/26 (Thu) | Comment プロパティーを作成。
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shinta;

[Table(TABLE_NAME_PROPERTY)]
internal class PropertyRecord
{
	// ====================================================================
	// public 定数
	// ====================================================================

	// --------------------------------------------------------------------
	// コア
	// --------------------------------------------------------------------

	public const String NAME_CORE = "property";

	// --------------------------------------------------------------------
	// テーブル名
	// --------------------------------------------------------------------

	public const String TABLE_NAME_PROPERTY = "t_" + NAME_CORE;

	// --------------------------------------------------------------------
	// フィールド名
	// --------------------------------------------------------------------

	public const String FIELD_NAME_AUTO_KEY = NAME_CORE + "_auto_key";
	public const String FIELD_NAME_APP_VER = NAME_CORE + "_app_ver";
	public const String FIELD_NAME_COMMENT = NAME_CORE + "_comment";

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 主キー
	/// </summary>
	[Key]
	[Column(FIELD_NAME_AUTO_KEY)]
	public Int32 AutoKey
	{
		get;
		set;
	}

	/// <summary>
	/// データベース更新時のアプリケーションのバージョン
	/// </summary>
	[Column(FIELD_NAME_APP_VER)]
	public String AppVer
	{
		get;
		set;
	} = String.Empty;

	/// <summary>
	/// 備考
	/// </summary>
	[Column(FIELD_NAME_COMMENT)]
	public String? Comment
	{
		get;
		set;
	}
}
