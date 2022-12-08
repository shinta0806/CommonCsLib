﻿// ============================================================================
// 
// WinUI 3 環境で使用する共通関数群
// Copyright (C) 2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Serilog.Sinks.File
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
//  
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/11/19 (Sat) | 作成開始。
//  1.00  | 2022/11/19 (Sat) | ファーストバージョン。
// ============================================================================

using Microsoft.UI.Xaml;

using PInvoke;

using Serilog;
using Serilog.Events;

using Windows.Foundation;
using Windows.UI.Popups;

using WinRT.Interop;

using WinUIEx;

namespace Shinta.WinUi3;

internal class WinUi3Common
{
	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// タイトルバーのコンテキストヘルプボタンを有効にする
	/// </summary>
	/// <param name="window"></param>
	/// <param name="subclassProc"></param>
	/// <returns>有効に出来た、または、既に有効な場合は true</returns>
	public static Boolean EnableContextHelp(Window window, WindowsApi.SubclassProc subclassProc)
	{
		IntPtr handle = WindowNative.GetWindowHandle(window);
		User32.SetWindowLongFlags exStyle = (User32.SetWindowLongFlags)User32.GetWindowLong(handle, User32.WindowLongIndexFlags.GWL_EXSTYLE);
		if ((exStyle & User32.SetWindowLongFlags.WS_EX_CONTEXTHELP) != 0)
		{
			return true;
		}
		User32.SetWindowLong(handle, User32.WindowLongIndexFlags.GWL_EXSTYLE, exStyle | User32.SetWindowLongFlags.WS_EX_CONTEXTHELP);
		return WindowsApi.SetWindowSubclass(handle, subclassProc, IntPtr.Zero, IntPtr.Zero);
	}

	/// <summary>
	/// ログの記録と表示
	/// </summary>
	/// <param name="logEventLevel"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	public static IAsyncOperation<IUICommand> ShowLogMessageDialogAsync(WindowEx window, LogEventLevel logEventLevel, String message)
	{
		Log.Write(logEventLevel, message);
		return window.CreateMessageDialog(message, logEventLevel.ToString().ToLocalized()).ShowAsync();
	}
}
