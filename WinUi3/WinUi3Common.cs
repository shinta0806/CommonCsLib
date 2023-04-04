﻿// ============================================================================
// 
// WinUI 3 環境で使用する共通関数群
// Copyright (C) 2022-2023 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   PInvoke.User32
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
//  1.10  | 2022/12/08 (Thu) | EnableContextHelp() を作成。
// (1.11) | 2023/03/25 (Sat) |   カスタムタイトルバー定数を定義。
// ============================================================================

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NicoKaraMaker3.Views.MainWindows;
using PInvoke;

using Serilog;
using Serilog.Events;

using Windows.Foundation;
using Windows.UI;
using Windows.UI.Popups;

using WinRT.Interop;

using WinUIEx;

namespace Shinta.WinUi3;

// ====================================================================
// public 列挙子
// ====================================================================

/// <summary>
/// 内容に合わせてサイズ調整
/// </summary>
public enum SizeToContent
{
	Manual,
	Width,
	Height,
	WidthAndHeight,
}

internal class WinUi3Common
{
	// ====================================================================
	// public 定数
	// ====================================================================

	// --------------------------------------------------------------------
	// UI 要素名
	// --------------------------------------------------------------------

	// カスタムタイトルバー
	public const String ELEMENT_CUSTOM_TITLE_BAR = "CustomTitleBar";

	// 描画完了チェック用の UI 要素
	public const String ELEMENT_CHECK_RENDER = "ElementCheckRender";

	// --------------------------------------------------------------------
	// 色
	// --------------------------------------------------------------------

	// タイトルバーの色
	// ThemeResource では定義されてない模様（WindowCaptionBackground は白のようだ）
	// https://github.com/microsoft/microsoft-ui-xaml/blob/main/dev/CommonStyles/Common_themeresources_any.xaml
	public static readonly Color TITLE_BAR_COLOR = Color.FromArgb(255, 238, 244, 249);

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// ウィンドウが存在するディスプレイの表示スケール
	/// </summary>
	/// <param name="window"></param>
	/// <returns></returns>
	public static Double DisplayScale(Window window)
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(window);
		Int32 dpi = User32.GetDpiForWindow(hWnd);
		return dpi / Common.DEFAULT_DPI;
	}

	/// <summary>
	/// タイトルバーのコンテキストヘルプボタンを有効にする
	/// </summary>
	/// <param name="window"></param>
	/// <param name="subclassProc">関数を直接渡すのではなく、new したものを格納した変数を渡す必要がある。また、その変数はずっと保持しておく必要がある</param>
	/// <returns>有効に出来た、または、既に有効な場合は true</returns>
	public static Boolean EnableContextHelp(Window window, WindowsApi.SubclassProc subclassProc)
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(window);
		User32.SetWindowLongFlags exStyle = (User32.SetWindowLongFlags)User32.GetWindowLong(hWnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
		if ((exStyle & User32.SetWindowLongFlags.WS_EX_CONTEXTHELP) != 0)
		{
			return true;
		}
		User32.SetWindowLong(hWnd, User32.WindowLongIndexFlags.GWL_EXSTYLE, exStyle | User32.SetWindowLongFlags.WS_EX_CONTEXTHELP);
		return WindowsApi.SetWindowSubclass(hWnd, subclassProc, IntPtr.Zero, IntPtr.Zero);
	}

	/// <summary>
	/// ログの記録と表示（ContentDialog 版）
	/// </summary>
	/// <param name="window"></param>
	/// <param name="logEventLevel"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	public static IAsyncOperation<ContentDialogResult> ShowLogContentDialogAsync(WindowEx window, LogEventLevel logEventLevel, String message)
	{
		Log.Write(logEventLevel, message);
		ContentDialog contentDialog = new()
		{
			XamlRoot = window.Content.XamlRoot,
			Title = logEventLevel.ToString().ToLocalized(),
			Content = message,
			CloseButtonText = "OK".ToLocalized()
		};
		return contentDialog.ShowAsync();
	}

	/// <summary>
	/// ログの記録と表示（MessageDialog 版）
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
