// ============================================================================
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

using Serilog;
using Serilog.Events;

using Windows.Foundation;
using Windows.UI.Popups;

namespace Shinta.WinUi3;

internal class WinUi3Common
{
	// ====================================================================
	// public 関数
	// ====================================================================

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
