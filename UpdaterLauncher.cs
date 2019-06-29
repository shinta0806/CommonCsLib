﻿// ============================================================================
// 
// ちょちょいと自動更新を起動する
// Copyright (C) 2014-2019 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/12/06 (Sat) | 作成開始。
//  1.00  | 2014/12/07 (Sun) | オリジナルバージョン。
// (1.01) | 2014/12/22 (Mon) | String 型のメンバー変数は Empty で初期化するようにした。
// (1.02) | 2015/05/23 (Sat) | エラーメッセージをユーザーに表示できるようにした。
// (1.03) | 2016/09/24 (Sat) | LogWriter を使用するように変更。
// (1.04) | 2017/11/18 (Sat) | StatusT を廃止。
// (1.05) | 2019/01/20 (Sun) | WPF アプリケーションでも使用可能にした。
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Shinta
{
	public class UpdaterLauncher
	{
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
		// public プロパティ
		// ====================================================================

		// --------------------------------------------------------------------
		// 共通
		// --------------------------------------------------------------------

		// アプリの ID
		public String ID { get; set; }

		// アプリの名前（表示用）
		public String Name { get; set; }

		// 最新情報・更新が無くてもユーザーに通知
		public Boolean ForceShow { get; set; }

		// チェック開始までの待ち時間 [s]
		public Int32 Wait { get; set; }

		// アプリのメインウィンドウのハンドル
		public IntPtr NotifyHWnd { get; set; }

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
		public String LatestRss { get; set; }

		// --------------------------------------------------------------------
		// 更新用
		// --------------------------------------------------------------------

		// 更新情報を保持している RSS の URL
		public String UpdateRss { get; set; }

		// 更新対象アプリのバージョン
		public String CurrentVer { get; set; }

		// 更新後に起動するアプリのパス（Launch() では自動設定）
		public String Relaunch { get; set; }

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
		public LogWriter LogWriter { get; set; }        

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public UpdaterLauncher()
		{
			// 共通
			ID = String.Empty;
			Name = String.Empty;

			// 最新情報確認用
			LatestRss = String.Empty;

			// 更新用
			UpdateRss = String.Empty;
			CurrentVer = String.Empty;
			Relaunch = String.Empty;
		}

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
		public Boolean Launch(Boolean oShowError = false)
		{
			String aParam = String.Empty;

			if (!IsRequiredValid())
			{
				return false;
			}

			LogMessage(TraceEventType.Information, "ちょちょいと自動更新を起動しています...");

			// 引数の設定
			if (Verbose)
			{
				// オンリー系
				aParam += PARAM_STR_VERBOSE + " ";
			}
			else if (DeleteOld)
			{
				// オンリー系
				aParam += PARAM_STR_DELETE_OLD + " ";
			}
			else
			{
				// 共通
				aParam += PARAM_STR_ID + " \"" + ID + "\" ";
				if (!String.IsNullOrEmpty(Name))
				{
					aParam += PARAM_STR_NAME + " \"" + Name + "\" ";
				}
				if (ForceShow)
				{
					aParam += PARAM_STR_FORCE_SHOW + " ";
				}
				if (Wait > 0)
				{
					aParam += PARAM_STR_WAIT + " " + Wait.ToString() + " ";
				}
				if (NotifyHWnd != null)
				{
					aParam += PARAM_STR_NOTIFY_HWND + " " + NotifyHWnd.ToString() + " ";
				}
				// 最新情報確認
				if (!String.IsNullOrEmpty(LatestRss))
				{
					aParam += PARAM_STR_LATEST_RSS + " \"" + LatestRss + "\" ";
				}
				// 更新
				if (!String.IsNullOrEmpty(UpdateRss))
				{
					aParam += PARAM_STR_UPDATE_RSS + " \"" + UpdateRss + "\" ";
					aParam += PARAM_STR_CURRENT_VER + " \"" + CurrentVer + "\" ";
					aParam += PARAM_STR_PID + " ";
					if (PID == 0)
					{
						aParam += Process.GetCurrentProcess().Id.ToString();

					}
					else
					{
						aParam += PID.ToString();
					}
					aParam += " " + PARAM_STR_RELAUNCH + " \"";
					if (String.IsNullOrEmpty(Relaunch))
					{
						aParam += Assembly.GetEntryAssembly().Location;

					}
					else
					{
						aParam += Relaunch;
					}
					aParam += "\" ";
					if (ClearUpdateCache)
					{
						aParam += PARAM_STR_CLEAR_UPDATE_CACHE + " ";
					}
					if (ForceInstall)
					{
						aParam += PARAM_STR_FORCE_INSTALL + " ";
					}
				}

			}
			LogMessage(TraceEventType.Verbose, "UpdaterLauncher.Launch() aParam: " + aParam);

			// 起動
			try
			{
				ProcessStartInfo aPSInfo = new ProcessStartInfo();
				aPSInfo.FileName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\" + FILE_NAME_CUPDATER;
				aPSInfo.Arguments = aParam;
				Process.Start(aPSInfo);
				LogMessage(TraceEventType.Information, "ちょちょいと自動更新を起動しました。");
			}
			catch (Exception oExcep)
			{
				if (LogWriter != null)
				{
					String aErrMsg = "ちょちょいと自動更新を起動できませんでした：\n" + oExcep.Message;
					if (oShowError)
					{
						LogWriter.ShowLogMessage(TraceEventType.Error, aErrMsg);
					}
					else
					{
						LogWriter.LogMessage(TraceEventType.Error, aErrMsg);
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
		// ログ書き込み
		// --------------------------------------------------------------------
		private void LogMessage(TraceEventType oEventType, String oMessage)
		{
			if (LogWriter != null)
			{
				LogWriter.LogMessage(oEventType, oMessage);
			}
		}

	}
	// public class UpdaterLauncher ___END___

}
// namespace Shinta ___END___
