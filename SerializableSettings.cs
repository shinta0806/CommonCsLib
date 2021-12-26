// ============================================================================
// 
// 所定のフォルダーに保存する設定の基底クラス
// Copyright (C) 2021 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2021/08/28 (Sat) | 作成開始。
//  1.00  | 2021/08/28 (Sat) | オリジナルバージョン。
// (1.01) | 2021/09/04 (Sat) |   デフォルトの保存パスを指定できるようにした。
//  1.10  | 2021/12/19 (Sun) | SetLogWriter() 作成。
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;

namespace Shinta
{
	public abstract class SerializableSettings
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		protected SerializableSettings(LogWriter? logWriter, String? defaultSettingsPath)
		{
			_logWriter = logWriter;
			_defaultSettingsPath = defaultSettingsPath;
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み
		// --------------------------------------------------------------------
		public void Load()
		{
			try
			{
				AdjustBeforeLoad();
			}
			catch (Exception excep)
			{
				_logWriter?.LogMessage(TraceEventType.Error, GetType().Name + "読み込み前設定調整時エラー：\n" + excep.Message);
				_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				if (!File.Exists(SettingsPath()))
				{
					throw new Exception("設定が保存されていません：" + SettingsPath());
				}

				// 直接 this に代入できないので全プロパティーをコピーする
				SerializableSettings loaded = Common.Deserialize(SettingsPath(), this);
				Common.ShallowCopyProperties(loaded, this);
			}
			catch (Exception excep)
			{
				_logWriter?.LogMessage(TraceEventType.Error, GetType().Name + " 設定読み込み時エラー：\n" + excep.Message);
				_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				AdjustAfterLoad();
			}
			catch (Exception excep)
			{
				_logWriter?.LogMessage(TraceEventType.Error, GetType().Name + "読み込み後設定調整時エラー：\n" + excep.Message);
				_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// ログ設定（主にコンストラクターでは設定できない状況で使用する）
		// --------------------------------------------------------------------
		public void SetLogWriter(LogWriter? logWriter)
		{
			_logWriter = logWriter;
		}

		// --------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------
		public void Save()
		{
			try
			{
				AdjustBeforeSave();
			}
			catch (Exception excep)
			{
				_logWriter?.LogMessage(TraceEventType.Error, GetType().Name + "保存前設定調整時エラー：\n" + excep.Message);
				_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				Common.Serialize(SettingsPath(), this);
			}
			catch (Exception excep)
			{
				_logWriter?.LogMessage(TraceEventType.Error, GetType().Name + "設定保存時エラー：\n" + excep.Message);
				_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			try
			{
				AdjustAfterSave();
			}
			catch (Exception excep)
			{
				_logWriter?.LogMessage(TraceEventType.Error, GetType().Name + "保存後設定調整時エラー：\n" + excep.Message);
				_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// ログ
		protected LogWriter? _logWriter;

		// デフォルトの保存パス（デフォルトの実装では保存パスとしてこれを使う：シリアライズされないことに注意）
		protected String? _defaultSettingsPath;

		// ====================================================================
		// protected メンバー関数
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

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		protected virtual String SettingsPath()
		{
			if (String.IsNullOrEmpty(_defaultSettingsPath))
			{
				throw new Exception("保存パスが指定されていません。");
			}
			return _defaultSettingsPath;
		}
	}
}
