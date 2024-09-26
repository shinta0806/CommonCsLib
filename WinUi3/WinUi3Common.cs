// ============================================================================
// 
// WinUI 3 環境で使用する共通関数群
// Copyright (C) 2022-2024 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   CsWin32
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
// (1.12) | 2023/08/19 (Sat) |   CsWin32 導入。
//  1.20  | 2023/08/19 (Sat) | IsMsix() を作成。
//  1.30  | 2023/08/19 (Sat) | SettingsFolder() を作成。
// (1.31) | 2024/04/08 (Mon) |   IsMsix() を移管。
// (1.32) | 2024/04/08 (Mon) |   SettingsFolder() を移管。
//  1.40  | 2024/09/25 (Wed) | PickSingleOpenFolderAsync() を作成。
// ============================================================================

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

using WinRT.Interop;

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
	//public static readonly Color TITLE_BAR_COLOR = Color.FromArgb(255, 238, 244, 249);

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// FileOpenPicker に拡張子を追加
	/// </summary>
	public static void AddFileOpenPickerExtensions(FileOpenPicker fileOpenPicker, IEnumerable<String> exts)
	{
		foreach (String ext in exts)
		{
			fileOpenPicker.FileTypeFilter.Add(ext);
		}
	}

	/// <summary>
	/// FileSavePicker に拡張子を追加
	/// </summary>
	/// <param name="fileSavePicker"></param>
	/// <param name="exts"></param>
	/// <param name="names"></param>
	public static void AddFileSavePickerExtensions(FileSavePicker fileSavePicker, IEnumerable<String> exts, IEnumerable<String> names)
	{
		IEnumerator<String> namesEnumerator = names.GetEnumerator();
		namesEnumerator.MoveNext();
		foreach (String ext in exts)
		{
			fileSavePicker.FileTypeChoices.Add(namesEnumerator.Current, [ext]);
			namesEnumerator.MoveNext();
		}
	}

	/// <summary>
	/// ウィンドウが存在するディスプレイの表示スケール
	/// </summary>
	/// <param name="window"></param>
	/// <returns></returns>
	public static Double DisplayScale(Window window)
	{
		HWND hWnd = (HWND)WindowNative.GetWindowHandle(window);
		UInt32 dpi = PInvoke.GetDpiForWindow(hWnd);
		return dpi / Common.DEFAULT_DPI;
	}

	/// <summary>
	/// タイトルバーのコンテキストヘルプボタンを有効にする
	/// </summary>
	/// <param name="window"></param>
	/// <param name="subclassProc">関数を直接渡すのではなく、new したものを格納した変数を渡す必要がある。また、その変数はずっと保持しておく必要がある</param>
	/// <returns>有効に出来た、または、既に有効な場合は true</returns>
	public static Boolean EnableContextHelp(Window window, SUBCLASSPROC subclassProc)
	{
		HWND hWnd = (HWND)WindowNative.GetWindowHandle(window);
		Int32 exStyle = PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
		if (((WINDOW_EX_STYLE)exStyle).HasFlag(WINDOW_EX_STYLE.WS_EX_CONTEXTHELP))
		{
			return true;
		}
		_ = PInvoke.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle | (Int32)WINDOW_EX_STYLE.WS_EX_CONTEXTHELP);
		return PInvoke.SetWindowSubclass(hWnd, subclassProc, UIntPtr.Zero, UIntPtr.Zero);
	}

	/// <summary>
	/// FileOpenPicker でファイルを 1 つ取得
	/// </summary>
	/// <returns></returns>
	public static async Task<StorageFile?> PickSingleOpenFileAsync(WindowEx window, String[] exts)
	{
		FileOpenPicker fileOpenPicker = window.CreateOpenFilePicker();
		AddFileOpenPickerExtensions(fileOpenPicker, exts);
		fileOpenPicker.FileTypeFilter.Add("*");

		return await fileOpenPicker.PickSingleFileAsync();
	}

	/// <summary>
	/// FolderPicker でフォルダーを取得
	/// </summary>
	/// <param name="window"></param>
	/// <returns></returns>
	public static async Task<StorageFolder?> PickSingleOpenFolderAsync(WindowEx window)
	{
		FolderPicker folderPicker = new FolderPicker();
		folderPicker.FileTypeFilter.Add("*");
		InitializeWithWindow.Initialize(folderPicker, window.GetWindowHandle());

		return await folderPicker.PickSingleFolderAsync();
	}

	/// <summary>
	/// FileSavePicker でファイルを 1 つ取得
	/// </summary>
	/// <param name="window"></param>
	/// <param name="exts"></param>
	/// <returns></returns>
	public static async Task<StorageFile?> PickSingleSaveFileAsync(WindowEx window, String[] exts, String[] names, String suggestedFileName)
	{
		FileSavePicker fileSavePicker = window.CreateSaveFilePicker();
		AddFileSavePickerExtensions(fileSavePicker, exts, names);
		fileSavePicker.SuggestedFileName = suggestedFileName;

		return await fileSavePicker.PickSaveFileAsync();
	}

#if false
	/// <summary>
	/// RECT が点を内包するか
	/// </summary>
	/// <param name="rect"></param>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public static Boolean RectContains(RECT rect, Int32 x, Int32 y)
	{
		// Windows.Foundation.Rect.Contains() の実装は右端を「以下」で判定しているので、それに倣う
		return rect.X <= x && x <= rect.X + rect.Width && rect.Y <= y && y <= rect.Y + rect.Height;
	}
#endif

	/// <summary>
	/// ログの記録と表示（ContentDialog 版）
	/// </summary>
	/// <param name="window"></param>
	/// <param name="logEventLevel"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	public static async Task<ContentDialogResult> ShowLogContentDialogAsync(WindowEx window, LogEventLevel logEventLevel, String message)
	{
		Log.Write(logEventLevel, message);
		ContentDialog contentDialog = new()
		{
			XamlRoot = window.Content.XamlRoot,
			Title = logEventLevel.ToString().ToLocalized(),
			Content = message,
			CloseButtonText = Common.LK_GENERAL_LABEL_OK.ToLocalized(),
		};
		return await contentDialog.ShowAsync();
	}

	/// <summary>
	/// ログの記録と表示（MessageDialog 版）
	/// </summary>
	/// <param name="logEventLevel"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	public static async Task<IUICommand> ShowLogMessageDialogAsync(WindowEx window, LogEventLevel logEventLevel, String message)
	{
		Log.Write(logEventLevel, message);
		return await window.CreateMessageDialog(message, logEventLevel.ToString().ToLocalized()).ShowAsync();
	}
}
