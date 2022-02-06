// ============================================================================
// 
// よく使う一般的な定数や関数（Windows に依存するもの）
// Copyright (C) 2021-2022 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2021/03/28 (Sun) | 作成開始。
//  1.00  | 2021/03/28 (Sun) | ShintaCommon より移管。
// (1.01) | 2022/02/02 (Wed) |   GetMonitorRects() を作成。
// (1.02) | 2022/02/06 (Sun) |   GetMonitorRects() を廃止。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace Shinta
{
	// ====================================================================
	// 共用ユーティリティークラス
	// ====================================================================

	public class CommonWindows
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ミューテックスを取得できない場合は、同名のプロセスのウィンドウをアクティベートする
		// ＜返値＞ 既存プロセスが存在しミューテックスが取得できなかった場合：null
		//          既存プロセスが存在せずミューテックスが取得できた場合：取得したミューテックス（使い終わった後で呼び出し元にて解放する必要がある）
		// --------------------------------------------------------------------
		public static Mutex? ActivateAnotherProcessWindowIfNeeded(String mutexName)
		{
			// ミューテックスを取得する
			Mutex ownedMutex = new(false, mutexName);
			try
			{
				if (ownedMutex.WaitOne(0))
				{
					// ミューテックスを取得できた
					return ownedMutex;
				}
			}
			catch (AbandonedMutexException)
			{
				// ミューテックスが放棄されていた場合にこの例外となるが、取得自体はできている
				return ownedMutex;
			}

			// ミューテックスが取得できなかったため、同名プロセスを探し、そのウィンドウをアクティベートする
			ActivateSameNameProcessWindow();
			return null;
		}

		// --------------------------------------------------------------------
		// 外部プロセスのウィンドウをアクティベートする
		// --------------------------------------------------------------------
		public static void ActivateExternalWindow(IntPtr hWnd)
		{
			if (hWnd == IntPtr.Zero)
			{
				return;
			}

			// ウィンドウが最小化されていれば元に戻す
			if (WindowsApi.IsIconic(hWnd))
			{
				WindowsApi.ShowWindowAsync(hWnd, (Int32)ShowWindowCommands.SW_RESTORE);
			}

			// アクティベート
			WindowsApi.SetForegroundWindow(hWnd);
		}

		// --------------------------------------------------------------------
		// 指定プロセスと同名プロセスのウィンドウをアクティベートする
		// --------------------------------------------------------------------
		public static void ActivateSameNameProcessWindow(Process? specifyProcess = null)
		{
			List<Process> sameNameProcesses = Common.SameNameProcesses(specifyProcess);
			if (sameNameProcesses.Count > 0)
			{
				ActivateExternalWindow(sameNameProcesses[0].MainWindowHandle);
			}
		}

		// --------------------------------------------------------------------
		// ウィンドウがスクリーンから完全にはみ出している場合はスクリーン内に移動する
		// --------------------------------------------------------------------
		public static Rect AdjustWindowRect(Rect windowRect)
		{
			Rect screenRect = new(0, 0, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

			// ウィンドウとスクリーンがぴったりの場合は移動不要
			if (screenRect == windowRect)
			{
				return windowRect;
			}

			// ウィンドウとスクリーンが一部重なっている場合は移動不要
			// ※ウィンドウがスクリーンより完全に大きい場合を除く
			Rect intersect = Rect.Intersect(screenRect, windowRect);
			if (!intersect.IsEmpty && intersect != screenRect)
			{
				return windowRect;
			}

			// 移動の必要がある
			return new Rect(0, 0, windowRect.Width, windowRect.Height);
		}

		// --------------------------------------------------------------------
		// ZoneID を削除
		// ＜返値＞削除できたら true
		// --------------------------------------------------------------------
		public static Boolean DeleteZoneID(String oPath)
		{
			return WindowsApi.DeleteFile(oPath + STREAM_NAME_ZONE_ID);
		}

		// --------------------------------------------------------------------
		// ZoneID を削除（フォルダ配下のすべてのファイル）
		// ＜返値＞ファイル列挙で何らかのエラーが発生したら Error、削除できなくても Ok は返る
		// --------------------------------------------------------------------
		public static Boolean DeleteZoneID(String oFolder, SearchOption oOption)
		{
			try
			{
				String[] aFiles = Directory.GetFiles(oFolder, "*", oOption);
				foreach (String aFile in aFiles)
				{
					DeleteZoneID(aFile);
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		private const String STREAM_NAME_ZONE_ID = ":Zone.Identifier";
	}
}

