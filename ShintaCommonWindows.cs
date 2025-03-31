// ============================================================================
// 
// よく使う一般的な定数や関数（Windows に依存するもの）
// Copyright (C) 2021-2024 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Microsoft.Windows.CsWin32
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2021/03/28 (Sun) | 作成開始。
//  1.00  | 2021/03/28 (Sun) | ShintaCommon より移管。
// (1.01) | 2022/02/02 (Wed) |   GetMonitorRects() を作成。
// (1.02) | 2022/02/06 (Sun) |   GetMonitorRects() を廃止。
// (1.03) | 2023/11/19 (Sun) |   Microsoft.Windows.CsWin32 パッケージを導入。
//  1.10  | 2024/04/08 (Mon) | IsMsix() を作成。
//  1.20  | 2024/04/08 (Mon) | SettingsFolder() を作成。
// (1.21) | 2024/04/08 (Mon) |   AdjustWindowRect() を廃止。
// ============================================================================

using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Shinta;

public class CommonWindows
{
	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// ミューテックスを取得できない場合は、同名のプロセスのウィンドウをアクティベートする
	/// </summary>
	/// <param name="mutexName"></param>
	/// <returns>既存プロセスが存在しミューテックスが取得できなかった場合：null
	/// 既存プロセスが存在せずミューテックスが取得できた場合：取得したミューテックス（使い終わった後で呼び出し元にて解放する必要がある） </returns>
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

	/// <summary>
	/// 外部プロセスのウィンドウをアクティベートする
	/// </summary>
	/// <param name="hWnd"></param>
	public static void ActivateExternalWindow(HWND hWnd)
	{
		if (hWnd.IsNull)
		{
			return;
		}

		// ウィンドウが最小化されていれば元に戻す
		if (PInvoke.IsIconic(hWnd))
		{
			PInvoke.ShowWindowAsync(hWnd, SHOW_WINDOW_CMD.SW_RESTORE);
		}

		// アクティベート
		PInvoke.SetForegroundWindow(hWnd);
	}

	/// <summary>
	/// 指定プロセスと同名プロセスのウィンドウをアクティベートする
	/// </summary>
	/// <param name="specifyProcess"></param>
	public static void ActivateSameNameProcessWindow(Process? specifyProcess = null)
	{
		List<Process> sameNameProcesses = Common.SameNameProcesses(specifyProcess);
		if (sameNameProcesses.Any())
		{
			ActivateExternalWindow((HWND)sameNameProcesses[0].MainWindowHandle);
		}
	}

	/// <summary>
	/// ZoneID を削除
	/// </summary>
	/// <param name="path"></param>
	/// <returns>削除できたら true</returns>
	public static Boolean DeleteZoneID(String path)
	{
		return PInvoke.DeleteFile(path + STREAM_NAME_ZONE_ID);
	}

	/// <summary>
	/// ZoneID を削除（フォルダ配下のすべてのファイル）
	/// </summary>
	/// <param name="folder"></param>
	/// <param name="option"></param>
	/// <returns>ファイル列挙で何らかのエラーが発生したら Error、削除できなくても Ok は返る</returns>
	public static Boolean DeleteZoneID(String folder, SearchOption option)
	{
		try
		{
			String[] files = Directory.GetFiles(folder, "*", option);
			foreach (String file in files)
			{
				DeleteZoneID(file);
			}
		}
		catch
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// MSIX パッケージで実行されているかどうか
	/// </summary>
	/// <returns></returns>
	public static Boolean IsMsix()
	{
		UInt32 length = 0;
		WIN32_ERROR error = PInvoke.GetCurrentPackageFullName(ref length, null);
		return !error.HasFlag(WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE);
	}

	/// <summary>
	/// 設定保存用フォルダー（末尾 '\\'）
	/// </summary>
	/// <returns></returns>
	public static String SettingsFolder()
	{
		if (IsMsix())
		{
			// MSIX パッケージの場合は Local\Packages\... 配下
			return Path.GetDirectoryName(ApplicationData.Current.LocalFolder.Path) + "\\" + Common.FOLDER_NAME_SETTINGS;
		}
		else
		{
			// 非 MSIX パッケージの場合は Roaming\SHINTA\... 配下
			return Common.UserAppDataFolderPath();
		}
	}

	// ====================================================================
	// private 定数
	// ====================================================================

	private const String STREAM_NAME_ZONE_ID = ":Zone.Identifier";
}

