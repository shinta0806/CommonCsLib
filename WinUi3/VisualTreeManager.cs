// ============================================================================
// 
// VisualTree を活用した操作をするクラス
// Copyright (C) 2023 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ・スレッドセーフではない。
//     複数スレッドで使用したい場合はスレッドごとにインスタンスを作成する必要がある。
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   PInvoke.Kernel32, PInvoke.User32
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2023/04/17 (Mon) | 作成開始。
//  1.00  | 2023/04/17 (Mon) | ファーストバージョン。
// ============================================================================

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PInvoke;

using System.Diagnostics;

using Windows.Foundation;

namespace Shinta.WinUi3;

internal class VisualTreeManager
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public VisualTreeManager()
	{
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// スレッドに関連付けられているすべての子以外のウィンドウを取得
	/// 通常はアプリのウィンドウを取得することになる
	/// IME 等のウィンドウも含まれることになる
	/// </summary>
	/// <returns></returns>
	public List<Window> GetWindows()
	{
		_windows = new();
		User32.EnumThreadWindows(Kernel32.GetCurrentThreadId(), EnumThreadCallback, IntPtr.Zero);
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	// ウィンドウ格納用
	private List<Window>? _windows;

	// ====================================================================
	// private 関数
	// ====================================================================

	private Boolean EnumThreadCallback(nint hWnd, nint lParam)
	{
		WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
		AppWindow? appWindow = AppWindow.GetFromWindowId(windowId);
		if (appWindow != null)
		{
			appWindow.
		}
		

		Log.Debug("CustomEnumThreadWndProc() " + hWnd.ToString("x"));
		Log.Debug("CustomEnumThreadWndProc() " + PInvoke.User32.GetWindowText(hWnd));
		return true;
	}


}
