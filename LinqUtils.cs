// ============================================================================
// 
// Linq ユーティリティクラス
// Copyright (C) 2015-2017 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2015/xx/xx (xxx) | 作成開始。
//  1.00  | 2015/xx/xx (xxx) | オリジナルバージョン。
// (1.01) | 2017/12/02 (Sat) | CreateIndex() の引数を変更。
//  1.10  | 2017/12/02 (Sat) | SqliteMaster クラスを作成。
//  1.20  | 2017/12/02 (Sat) | Tables() を作成。
//  1.30  | 2017/12/02 (Sat) | DropTable() を作成。
//  1.40  | 2017/12/02 (Sat) | DropAllTables() を作成。
//  1.50  | 2017/12/02 (Sat) | Vacuum() を作成。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data.Linq.Mapping;
using System.Text;
using System.Data.SQLite;
using System.Data.Linq;

namespace Shinta
{
	// ====================================================================
	// Linq ユーティリティクラス
	// ====================================================================

	public class LinqUtils
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースの型
		// --------------------------------------------------------------------
		public const String DB_TYPE_INTEGER = "INT";
		public const String DB_TYPE_DOUBLE = "REAL";
		public const String DB_TYPE_STRING = "NVARCHAR";

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// データベースファイル内に、oTable で示される型に対応したテーブルを作成する
		// ＜引数＞ oCmd: 対象となるデータベースファイルに接続された状態の DbCommand
		//          oTable: テーブルの型を定義したクラスのインスタンス
		//          oUniques: ユニーク制約を付けるフィールド名（複数キーで 1 つのユニークにする場合はカンマ区切りで 1 つの String とする）
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void CreateTable(DbCommand oCmd, Type oTypeOfTable, List<String> oUniques = null, String oAutoIncrement = null)
		{
			StringBuilder aCmdText = new StringBuilder();
			aCmdText.Append("CREATE TABLE IF NOT EXISTS " + TableName(oTypeOfTable) + "(");
			IEnumerable<ColumnAttribute> aAttrs = oTypeOfTable.GetProperties().Select(x => Attribute.GetCustomAttribute(x, typeof(ColumnAttribute)) as ColumnAttribute);
			if (aAttrs.Count() == 0)
			{
				throw new Exception("テーブルにフィールドが存在しません。");
			}

			// フィールド
			foreach (ColumnAttribute aFieldAttr in aAttrs)
			{
				// Name 属性の無いプロパティーは非保存とみなす
				if (aFieldAttr == null)
				{
					continue;
				}

				// フィールド名
				aCmdText.Append(aFieldAttr.Name);

				// 型
				if (aFieldAttr.DbType == "INT")
				{
					// INT は INTEGER にする（SQLite の AUTOINCREMENT でのエラー回避）
					aCmdText.Append(" INTEGER");
				}
				else
				{
					aCmdText.Append(" " + aFieldAttr.DbType);
				}

				// NOT NULL
				if (!aFieldAttr.CanBeNull)
				{
					aCmdText.Append(" NOT NULL");
				}

				// 主キー
				if (aFieldAttr.IsPrimaryKey)
				{
					aCmdText.Append(" PRIMARY KEY");
				}

				// オートインクリメント
				if (!String.IsNullOrEmpty(oAutoIncrement) && aFieldAttr.Name == oAutoIncrement)
				{
					aCmdText.Append(" AUTOINCREMENT");
				}

				aCmdText.Append(",");
			}

			// ユニーク制約
			if (oUniques != null)
			{
				foreach (String aUnique in oUniques)
				{
					aCmdText.Append("UNIQUE(");
					String[] aUniqueSplit = aUnique.Split(',');
					foreach (String aElement in aUniqueSplit)
					{
						aCmdText.Append(aElement + ",");
					}
					aCmdText.Remove(aCmdText.Length - 1, 1);
					aCmdText.Append("),");
				}
			}

			aCmdText.Remove(aCmdText.Length - 1, 1);
			aCmdText.Append(");");

			// テーブル作成
			oCmd.CommandText = aCmdText.ToString();
			oCmd.ExecuteNonQuery();

		}

		// --------------------------------------------------------------------
		// データベースファイル内のテーブルにインデックスを作成
		// --------------------------------------------------------------------
		public static void CreateIndex(DbCommand oCmd, String oTableName, List<String> oIndices)
		{
			if (oIndices == null)
			{
				return;
			}
			foreach (String aIndex in oIndices)
			{
				oCmd.CommandText = "CREATE INDEX IF NOT EXISTS index_" + aIndex + " ON " + oTableName + "(" + aIndex + ");";
				oCmd.ExecuteNonQuery();
			}
		}

#if USE_OBSOLETE_CREATE_INDEX
		// --------------------------------------------------------------------
		// データベースファイル内のテーブルにインデックスを作成
		// --------------------------------------------------------------------
		public static void CreateIndex(DbCommand oCmd, Type oTypeOfTable, List<String> oIndices)
		{
			CreateIndex(oCmd, TableName(oTypeOfTable), oIndices);
		}
#endif

		// --------------------------------------------------------------------
		// データベース内のすべてのテーブルを削除（ドロップ）
		// ＜引数＞ oConnection: データベース接続
		// --------------------------------------------------------------------
		public static void DropAllTables(SQLiteConnection oConnection)
		{
			List<String> aTables = Tables(oConnection);

			using (SQLiteCommand aCmd = new SQLiteCommand(oConnection))
			{
				foreach (String aTable in aTables)
				{
					DropTable(aCmd, aTable);
				}
				Vacuum(aCmd);
			}
		}

		// --------------------------------------------------------------------
		// データベース内のテーブルを削除（ドロップ）
		// ＜引数＞ oCmd: 対象となるデータベースファイルに接続された状態の DbCommand
		//          oTableName: 削除したいテーブル名
		// ＜例外＞ Exception（データベース内にテーブルが存在しない場合など）
		// --------------------------------------------------------------------
		public static void DropTable(DbCommand oCmd, String oTableName)
		{
			oCmd.CommandText = "DROP TABLE " + oTableName;
			oCmd.ExecuteNonQuery();
		}

		// --------------------------------------------------------------------
		// テーブル名
		// ＜引数＞ oTypeOfTable: typeof(テーブル定義クラス名) or aInstance.GetType()
		// ＜例外＞ Exception（Name 属性の無いクラスを指定した場合など）
		// --------------------------------------------------------------------
		public static String TableName(Type oTypeOfTable)
		{
			return (Attribute.GetCustomAttribute(oTypeOfTable, typeof(TableAttribute)) as TableAttribute).Name;
		}

		// --------------------------------------------------------------------
		// データベース内のテーブル群を返す（システムテーブルを除く）
		// ＜引数＞ oConnection: データベース接続
		// --------------------------------------------------------------------
		public static List<String> Tables(SQLiteConnection oConnection)
		{
			List<String> aTables = new List<String>();

			using (DataContext aContext = new DataContext(oConnection))
			{
				Table<SqliteMaster> aSqliteMaster = aContext.GetTable<SqliteMaster>();
				IQueryable<SqliteMaster> aQueryResult =
						from x in aSqliteMaster
						where x.Type == "table"
						select x;
				foreach (SqliteMaster aRecord in aQueryResult)
				{
					if (aRecord.Name.IndexOf("sqlite_") < 0)
					{
						aTables.Add(aRecord.Name);
					}
				}
			}

			return aTables;
		}

		// --------------------------------------------------------------------
		// データベースの最適化
		// ＜引数＞ oCmd: 対象となるデータベースファイルに接続された状態の DbCommand
		// --------------------------------------------------------------------
		public static void Vacuum(DbCommand oCmd)
		{
			oCmd.CommandText = "VACUUM";
			oCmd.ExecuteNonQuery();
		}

	}
	// public class LinqUtils ___END___

	// ====================================================================
	// SQLite3 マスターテーブル
	// ====================================================================

	[Table(Name = "sqlite_master")]
	public class SqliteMaster
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		public const String FIELD_NAME_SQLITE_MASTER_TYPE = "type";
		public const String FIELD_NAME_SQLITE_MASTER_NAME = "name";
		public const String FIELD_NAME_SQLITE_MASTER_TBL_NAME = "tbl_name";
		public const String FIELD_NAME_SQLITE_MASTER_ROOTPAGE = "rootpage";
		public const String FIELD_NAME_SQLITE_MASTER_SQL = "sql";

		// ====================================================================
		// フィールド
		// ====================================================================

		// タイプ
		[Column(Name = FIELD_NAME_SQLITE_MASTER_TYPE, DbType = LinqUtils.DB_TYPE_STRING)]
		public String Type { get; set; }

		// 名前
		[Column(Name = FIELD_NAME_SQLITE_MASTER_NAME, DbType = LinqUtils.DB_TYPE_STRING)]
		public String Name { get; set; }

		// テーブル名
		[Column(Name = FIELD_NAME_SQLITE_MASTER_TBL_NAME, DbType = LinqUtils.DB_TYPE_STRING)]
		public String TblName { get; set; }

		// ルートページ
		[Column(Name = FIELD_NAME_SQLITE_MASTER_ROOTPAGE, DbType = LinqUtils.DB_TYPE_INTEGER)]
		public Int32 Rootpage { get; set; }

		// SQL
		[Column(Name = FIELD_NAME_SQLITE_MASTER_SQL, DbType = LinqUtils.DB_TYPE_STRING)]
		public String Sql { get; set; }
	}
	// public class SqliteMaster ___END___

}
// namespace Shinta ___END___

