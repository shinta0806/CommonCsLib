// ============================================================================
// 
// ちょちょいと自動更新を起動する
// Copyright (C) 2014-2021 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/12/06 (Sat) | 作成開始。
//  1.00  | 2014/12/07 (Sun) | オリジナルバージョン。
// (1.01) | 2014/12/22 (Mon) |   String 型のメンバー変数は Empty で初期化するようにした。
// (1.02) | 2015/05/23 (Sat) |   エラーメッセージをユーザーに表示できるようにした。
// (1.03) | 2016/09/24 (Sat) |   LogWriter を使用するように変更。
// (1.04) | 2017/11/18 (Sat) |   StatusT を廃止。
// (1.05) | 2019/01/20 (Sun) |   WPF アプリケーションでも使用可能にした。
// (1.06) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.07) | 2020/03/29 (Sun) |   null 許容参照型の対応を強化。
// (1.08) | 2020/03/29 (Sun) |   発行した場合を考慮し Relaunch を活用。
// (1.09) | 2020/05/05 (Tue) |   null 許容参照型を無効化できるようにした。
//  1.10  | 2020/05/05 (Tue) | マイナーバージョンアップの積み重ね。
//                               プロセスを破棄していなかったのを修正。
// (1.11) | 2020/05/05 (Tue) |   SelfLaunch プロパティーをサポート。
// (1.12) | 2020/11/15 (Sun) |   NotifyHWnd が空の場合に検出できない不具合を修正。
// (1.13) | 2020/11/15 (Sun) |   .NET 5 の単一ファイルに対応。
// (1.14) | 2021/05/28 (Fri) |   null 許容参照型を必須にした。
// (1.15) | 2021/10/30 (Sat) |   UpdaterExePath プロパティーをサポート。
// (1.16) | 2021/10/30 (Sat) |   Verbose 時にも NotifyHWnd を渡すようにした。
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;

namespace Shinta
{
	public class UpdaterLauncher
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public UpdaterLauncher()
		{
		}

		// ====================================================================
		// public プロパティ
		// ====================================================================

		// --------------------------------------------------------------------
		// 共通
		// --------------------------------------------------------------------

		// アプリの ID
		public String ID { get; set; } = String.Empty;

		// アプリの名前（表示用）
		public String Name { get; set; } = String.Empty;

		// 最新情報・更新が無くてもユーザーに通知
		public Boolean ForceShow { get; set; }

		// チェック開始までの待ち時間 [s]
		public Int32 Wait { get; set; }

		// アプリのメインウィンドウのハンドル
		public IntPtr NotifyHWnd { get; set; }

		// ちょちょいと自動更新自身による起動
		public Boolean SelfLaunch { get; set; }

		// ちょちょいと自動更新の実行ファイルパス
		public String UpdaterExePath { get; set; } = String.Empty;

		// --------------------------------------------------------------------
		// 共通（オンリー系）
		// --------------------------------------------------------------------

		// バージョン情報ダイアログを表示するのみ
		public Boolean Verbose { get; set; }

		// 古い Updater.exe を削除するのみ（更新後の作業用）（未実装）
		public Boolean DeleteOld { get; set; }

		// --------------------------------------------------------------------
		// 最新情報確認用
		// --------------------------------------------------------------------

		// 最新情報を保持している RSS の URL
		public String LatestRss { get; set; } = String.Empty;

		// --------------------------------------------------------------------
		// 更新用
		// --------------------------------------------------------------------

		// 更新情報を保持している RSS の URL
		public String UpdateRss { get; set; } = String.Empty;

		// 更新対象アプリのバージョン
		public String CurrentVer { get; set; } = String.Empty;

		// 更新後に起動するアプリのパス（Launch() では自動設定）
		public String Relaunch { get; set; } = String.Empty;

		// 更新情報をクリア
		public Boolean ClearUpdateCache { get; set; }

		// 強制的にインストール
		public Boolean ForceInstall { get; set; }

		// Updater.exe を起動した依頼元アプリ（Launch() では自動設定）
		public Int32 PID { get; set; }

		// --------------------------------------------------------------------
		// ログ
		// --------------------------------------------------------------------

		// ログ
		public LogWriter? LogWriter { get; set; }

		// ====================================================================
		// public 定数
		// ====================================================================

		// --------------------------------------------------------------------
		// コマンドライン引数
		// --------------------------------------------------------------------
		public const String PARAM_STR_CLEAR_UPDATE_CACHE = "/ClearUpdateCache";
		public const String PARAM_STR_CURRENT_VER = "/CurrentVer";
		public const String PARAM_STR_DELETE_OLD = "/DeleteOld";
		public const String PARAM_STR_FORCE_INSTALL = "/ForceInstall";
		public const String PARAM_STR_FORCE_SHOW = "/ForceShow";
		public const String PARAM_STR_ID = "/ID";
		public const String PARAM_STR_LATEST_RSS = "/LatestRSS";
		public const String PARAM_STR_NAME = "/Name";
		public const String PARAM_STR_NOTIFY_HWND = "/NotifyHWnd";
		public const String PARAM_STR_PID = "/PID";
		public const String PARAM_STR_RELAUNCH = "/Relaunch";
		public const String PARAM_STR_SELF_LAUNCH = "/SelfLaunch";
		public const String PARAM_STR_UPDATE_RSS = "/UpdateRSS";
		public const String PARAM_STR_VERBOSE = "/Verbose";
		public const String PARAM_STR_WAIT = "/Wait";

		// --------------------------------------------------------------------
		// 外部通信用メッセージ定数
		// --------------------------------------------------------------------

		// ウィンドウを表示した
		public const Int32 WM_UPDATER_UI_DISPLAYED = 0x8000 /*WM_APP*/ + 0x1000;

		// 起動完了した（ウィンドウは非表示）
		public const Int32 WM_UPDATER_LAUNCHED = WM_UPDATER_UI_DISPLAYED + 1;

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 最新情報確認をするか
		// --------------------------------------------------------------------
		public Boolean IsLatestMode()
		{
			return !String.IsNullOrEmpty(LatestRss);
		}

		// --------------------------------------------------------------------
		// 必須プロパティが設定されているか確認
		// 詳細は、ちょちょいと自動更新の仕様を参照
		// --------------------------------------------------------------------
		public Boolean IsRequiredValid()
		{
			// オンリー系が指定されている場合は OK
			if (DeleteOld || Verbose)
			{
				return true;
			}

			// 最新情報確認か自動更新のどちらかは必要なので、どちらも無ければアウト
			if (!IsLatestMode() && !IsUpdateMode())
			{
				return false;
			}

			// 共通オプションの必須項目が無い場合はアウト
			if (String.IsNullOrEmpty(ID))
			{
				return false;
			}

			// 自動更新の場合は、その他の必須オプションが無いとアウト
			if (IsUpdateMode() && String.IsNullOrEmpty(CurrentVer))
			{
				return false;
			}

			return true;
		}

		// --------------------------------------------------------------------
		// 自動更新をするか
		// --------------------------------------------------------------------
		public Boolean IsUpdateMode()
		{
			return !String.IsNullOrEmpty(UpdateRss);
		}

		// --------------------------------------------------------------------
		// ちょちょいと自動更新を起動
		// --------------------------------------------------------------------
		public Boolean Launch(Boolean showError = false)
		{
			String param = String.Empty;

			if (!IsRequiredValid())
			{
				return false;
			}

			LogMessage(TraceEventType.Information, "ちょちょいと自動更新を起動しています...");

			// 引数の設定
			if (Verbose)
			{
				// オンリー系
				param += PARAM_STR_VERBOSE + " ";
				AddNotifyHWndIfNeeded(ref param);
			}
			else if (DeleteOld)
			{
				// オンリー系
				param += PARAM_STR_DELETE_OLD + " ";
			}
			else
			{
				// 共通
				param += PARAM_STR_ID + " \"" + ID + "\" ";
				if (!String.IsNullOrEmpty(Name))
				{
					param += PARAM_STR_NAME + " \"" + Name + "\" ";
				}
				if (ForceShow)
				{
					param += PARAM_STR_FORCE_SHOW + " ";
				}
				if (Wait > 0)
				{
					param += PARAM_STR_WAIT + " " + Wait.ToString() + " ";
				}
				AddNotifyHWndIfNeeded(ref param);
				if (SelfLaunch)
				{
					param += PARAM_STR_SELF_LAUNCH + " ";
				}

				// 最新情報確認
				if (!String.IsNullOrEmpty(LatestRss))
				{
					param += PARAM_STR_LATEST_RSS + " \"" + LatestRss + "\" ";
				}

				// 更新
				if (!String.IsNullOrEmpty(UpdateRss))
				{
					param += PARAM_STR_UPDATE_RSS + " \"" + UpdateRss + "\" ";
					param += PARAM_STR_CURRENT_VER + " \"" + CurrentVer + "\" ";
					param += PARAM_STR_PID + " ";
					if (PID == 0)
					{
						param += Environment.ProcessId.ToString();

					}
					else
					{
						param += PID.ToString();
					}
					param += " " + PARAM_STR_RELAUNCH + " \"" + ExePath() + "\" ";
					if (ClearUpdateCache)
					{
						param += PARAM_STR_CLEAR_UPDATE_CACHE + " ";
					}
					if (ForceInstall)
					{
						param += PARAM_STR_FORCE_INSTALL + " ";
					}
				}
			}
			LogMessage(TraceEventType.Verbose, "UpdaterLauncher.Launch() aParam: " + param);

			// 起動
			ProcessStartInfo psInfo = new();
			try
			{
				if (String.IsNullOrEmpty(UpdaterExePath))
				{
					psInfo.FileName = Path.GetDirectoryName(ExePath()) + "\\" + FILE_NAME_CUPDATER;
				}
				else
				{
					psInfo.FileName = UpdaterExePath;
				}
				psInfo.Arguments = param;

				Process? process;
				process = Process.Start(psInfo);
				process?.Dispose();
				LogMessage(TraceEventType.Information, "ちょちょいと自動更新を起動しました。");
			}
			catch (Exception excep)
			{
				if (LogWriter != null)
				{
					String errMsg = "ちょちょいと自動更新を起動できませんでした：\n" + excep.Message + "\n" + psInfo.FileName;
					if (showError)
					{
						LogWriter.ShowLogMessage(TraceEventType.Error, errMsg);
					}
					else
					{
						LogWriter.LogMessage(TraceEventType.Error, errMsg);
					}
				}

				return false;
			}

			return true;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		private const String FILE_NAME_CUPDATER = "Updater.exe";

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// param に PARAM_STR_NOTIFY_HWND を付加
		// --------------------------------------------------------------------
		private void AddNotifyHWndIfNeeded(ref String param)
		{
			if (NotifyHWnd != IntPtr.Zero)
			{
				param += PARAM_STR_NOTIFY_HWND + " " + NotifyHWnd.ToString() + " ";
			}
		}

		// --------------------------------------------------------------------
		// Relaunch または EXE パスを返す
		// --------------------------------------------------------------------
		private String ExePath()
		{
			String exePath = Relaunch;
			if (String.IsNullOrEmpty(exePath))
			{
				exePath = Path.ChangeExtension(Environment.GetCommandLineArgs()[0], Common.FILE_EXT_EXE);
			}
			return exePath;
		}

		// --------------------------------------------------------------------
		// ログ書き込み
		// --------------------------------------------------------------------
		private void LogMessage(TraceEventType eventType, String message)
		{
			LogWriter?.LogMessage(eventType, message);
		}
	}
}
