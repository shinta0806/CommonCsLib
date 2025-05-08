// ============================================================================
// 
// 子作品データベースコンテキスト
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Shinta.Niconico.Database;

internal class VideoInfoContext : DbContext
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	/// <param name="dbPath"></param>
	public VideoInfoContext(String dbPath)
	{
		_dbPath = dbPath;
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 子作品群
	/// </summary>
	public DbSet<VideoInfoRecord> VideoInfoRecords
	{
		get;
		set;
	}

	/// <summary>
	/// プロパティー
	/// </summary>
	public DbSet<PropertyRecord> PropertyRecords
	{
		get;
		set;
	}

	// ====================================================================
	// protected 関数
	// ====================================================================

	/// <summary>
	/// 接続設定
	/// </summary>
	/// <param name="optionsBuilder"></param>
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		SqliteConnectionStringBuilder stringBuilder = new()
		{
			DataSource = _dbPath,
		};
		using SqliteConnection sqliteConnection = new(stringBuilder.ToString());
		optionsBuilder.UseSqlite(sqliteConnection);
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// データベースファイルのパス
	/// </summary>
	private readonly String _dbPath;
}
