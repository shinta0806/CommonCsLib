// ============================================================================
// 
// 所定のフォルダーに保存する設定の基底クラス
// Copyright (C) 2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// シリアライズされるため派生クラスも含め public class である必要がある
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Serilog.Sinks.File
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/11/19 (Sat) | WPF 版を元に作成開始。
//  1.00  | 2022/11/19 (Sat) | ファーストバージョン。
// ============================================================================

using Serilog;

namespace Shinta.WinUi3
{
	public abstract class SerializableSettings
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		/// <summary>
		/// メインコンストラクター
		/// </summary>
		protected SerializableSettings()
		{
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み
		// --------------------------------------------------------------------
		public Boolean Load(String? overridePath = null)
		{
			Boolean allSuccess = true;

			try
			{
				AdjustBeforeLoad();
			}
			catch (Exception ex)
			{
				Log.Error(GetType().Name + "読み込み前設定調整時エラー：\n" + ex.Message);
				Log.Information("スタックトレース：\n" + ex.StackTrace);
				allSuccess = false;
			}
			String path = overridePath ?? SettingsPath();
			try
			{
				if (!File.Exists(path))
				{
					throw new Exception("設定が保存されていません：" + path);
				}

				// 直接 this に代入できないので全プロパティーをコピーする
				SerializableSettings loaded = Common.Deserialize(path, this);
				Common.ShallowCopyProperties(loaded, this);
			}
			catch (Exception ex)
			{
				Log.Error(GetType().Name + " 設定読み込み時エラー：\n" + ex.Message + "\n" + path);
				Log.Information("スタックトレース：\n" + ex.StackTrace);
				allSuccess = false;
			}
			try
			{
				AdjustAfterLoad();
			}
			catch (Exception ex)
			{
				Log.Error(GetType().Name + "読み込み後設定調整時エラー：\n" + ex.Message);
				Log.Information("スタックトレース：\n" + ex.StackTrace);
				allSuccess = false;
			}

			return allSuccess;
		}

		// --------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------
		public Boolean Save()
		{
			Boolean allSuccess = true;

			try
			{
				AdjustBeforeSave();
			}
			catch (Exception ex)
			{
				Log.Error(GetType().Name + "保存前設定調整時エラー：\n" + ex.Message);
				Log.Information("スタックトレース：\n" + ex.StackTrace);
				allSuccess = false;
			}
			try
			{
				Common.Serialize(SettingsPath(), this);
			}
			catch (Exception ex)
			{
				Log.Error(GetType().Name + "設定保存時エラー：\n" + ex.Message);
				Log.Information("スタックトレース：\n" + ex.StackTrace);
				allSuccess = false;
			}
			try
			{
				AdjustAfterSave();
			}
			catch (Exception ex)
			{
				Log.Error(GetType().Name + "保存後設定調整時エラー：\n" + ex.Message);
				Log.Information("スタックトレース：\n" + ex.StackTrace);
				allSuccess = false;
			}

			return allSuccess;
		}

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		public abstract String SettingsPath();

		// ====================================================================
		// protected 関数
		// ====================================================================

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み後の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustAfterLoad()
		{
		}

		// --------------------------------------------------------------------
		// 保存後の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustAfterSave()
		{
		}

		// --------------------------------------------------------------------
		// 読み込み前の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustBeforeLoad()
		{
		}

		// --------------------------------------------------------------------
		// 保存前の調整
		// --------------------------------------------------------------------
		protected virtual void AdjustBeforeSave()
		{
		}
	}
}
