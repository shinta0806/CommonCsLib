// ============================================================================
// 
// データベース関連の関数
// Copyright (C) 2024 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2024/09/26 (Thu) | ファーストバージョン。
//  1.10  | 2024/09/27 (Fri) | IsVersionPropertySame() を作成した。
// ============================================================================

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Shinta;

internal class DatabaseCommon
{
	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// データベースのプロパティーが指定バージョンと同一か
	/// </summary>
	/// <param name="propertyRecords"></param>
	/// <returns></returns>
	public static Boolean IsVersionPropertySame(DbSet<PropertyRecord> propertyRecords, String appVer)
	{
		PropertyRecord? propertyRecord = propertyRecords.FirstOrDefault();
		if (propertyRecord == null)
		{
			// プロパティーが存在していない場合は NG
			Log.Information("データベースプロパティーが存在していません。");
			return false;
		}
		if (propertyRecord.AppVer != appVer)
		{
			// バージョンが異なる場合は NG
			Log.Information("データベースプロパティーのバージョンが異なります。");
			return false;
		}

		return true;
	}

	/// <summary>
	/// ジャーナルモードを DELETE に設定
	/// </summary>
	/// <param name="videoInfoContext"></param>
	public static void SetJournalModeToDelete(DbContext dbContext)
	{
		// この SqliteConnection は Dispose() してはいけない
		if (dbContext.Database.GetDbConnection() is not SqliteConnection sqliteConnection)
		{
			throw new Exception("データベースの接続を取得できませんでした。");
		}

		sqliteConnection.Open();
		using SqliteCommand command = sqliteConnection.CreateCommand();
		command.CommandText = @"PRAGMA journal_mode = 'delete'";
		command.ExecuteNonQuery();
		Log.Information("ジャーナルモードを DELETE に設定しました。");
	}

	/// <summary>
	/// データベースのプロパティーを指定バージョンのものにする
	/// </summary>
	/// <param name="dbContext"></param>
	/// <param name="propertyRecords"></param>
	public static void SetVersionProperty(DbContext dbContext, DbSet<PropertyRecord> propertyRecords, String appVer)
	{
		// いったんプロパティー削除
		propertyRecords.RemoveRange(propertyRecords);
		dbContext.SaveChanges();

		// プロパティー追加
		PropertyRecord propertyRecord = new()
		{
			AppVer = appVer,
		};
		propertyRecords.Add(propertyRecord);
		dbContext.SaveChanges();
	}
}