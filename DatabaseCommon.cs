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
//  1.20  | 2024/09/28 (Sat) | Vacuum() を作成した。
// (1.21) | 2024/10/05 (Sat) |   SetJournalModeToDelete() を改善。
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
		try
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
		}
		catch (Exception)
		{
			// PropertyRecord が旧式（備考無し）の場合は例外になる
			Log.Information("データベースプロパティーのフォーマットが異なります。");
			return false;
		}

		return true;
	}

	/// <summary>
	/// ジャーナルモードを DELETE に設定
	/// </summary>
	/// <param name="dbContext">本関数呼び出し後は使い回さないことを推奨</param>
	public static void SetJournalModeToDelete(DbContext dbContext)
	{
		// この SqliteConnection は Dispose() してはいけない
		// https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.getdbconnection
		if (dbContext.Database.GetDbConnection() is not SqliteConnection sqliteConnection)
		{
			throw new Exception("データベースの接続を取得できませんでした。");
		}

		sqliteConnection.Open();
		using SqliteCommand command = sqliteConnection.CreateCommand();
		command.CommandText = @"PRAGMA journal_mode = 'delete'";
		command.ExecuteNonQuery();

		// 接続を閉じるかどうかは明確な指針がない模様
		// 閉じないと dbContext 破棄後でも他の dbContext でデータベースにアクセスできない事象が発生したので閉じておく
		sqliteConnection.Close();
		Log.Information("ジャーナルモードを DELETE に設定しました。");
	}

	/// <summary>
	/// データベースのプロパティーを指定バージョンのものにする
	/// </summary>
	/// <param name="dbContext"></param>
	/// <param name="propertyRecords"></param>
	public static void SetVersionProperty(DbContext dbContext, DbSet<PropertyRecord> propertyRecords, String appVer)
	{
		try
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
		catch (Exception ex)
		{
			// PropertyRecord が旧式（備考無し）の場合は例外になる
			SerilogUtils.LogException("データベースプロパティーのフォーマットが異なるため設定できません。", ex);
		}
	}

	/// <summary>
	/// データベースファイルサイズ削減
	/// </summary>
	/// <param name="dbContext"></param>
	public static Boolean Vacuum(DbContext dbContext)
	{
		try
		{
			// この SqliteConnection は Dispose() してはいけない
			if (dbContext.Database.GetDbConnection() is not SqliteConnection sqliteConnection)
			{
				throw new Exception("データベースの接続を取得できませんでした。");
			}

			sqliteConnection.Open();
			using SqliteCommand command = sqliteConnection.CreateCommand();
			command.CommandText = @"VACUUM";
			command.ExecuteNonQuery();
			sqliteConnection.Close();
			Log.Information("データベースを VACUUM しました。");
			return true;
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("VACUUM 時エラー", ex);
			return false;
		}
	}
}