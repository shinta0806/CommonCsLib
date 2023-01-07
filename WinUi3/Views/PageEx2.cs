// ============================================================================
// 
// ページの拡張
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/12/08 (Thu) | 作成開始。
//  1.00  | 2022/12/08 (Thu) | ファーストバージョン。
// (1.01) | 2023/01/04 (Wed) |   PageEx2Loaded() で表示スケールを考慮するようにした。
// ============================================================================

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Serilog;

using Windows.Graphics;

namespace Shinta.WinUi3.Views;

public class PageEx2 : Page
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public PageEx2(WindowEx2 window)
	{
		// 初期化
		_window = window;

		// イベントハンドラー
		Loaded += PageEx2Loaded;
	}

	// ====================================================================
	// protected 変数
	// ====================================================================

	/// <summary>
	/// ウィンドウ
	/// </summary>
	protected readonly WindowEx2 _window;

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// イベントハンドラー：ページがロードされた
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void PageEx2Loaded(Object _1, RoutedEventArgs _2)
	{
		try
		{
			// WindowEx2.SizeToContent の処理
			// ToDo: Window.SizeToContent が実装されれば不要となるコード
			if (_window.SizeToContent != SizeToContent.Manual)
			{
				// Windows App SDK 1.2 現在、コンテンツのサイズは表示スケールに自動追随するが、ResizeClient() は表示スケールを考慮してくれない
				Double scale = WinUi3Common.DisplayScale(_window);

				Int32 width = _window.AppWindow.ClientSize.Width;
				if (_window.SizeToContent == SizeToContent.Width || _window.SizeToContent == SizeToContent.WidthAndHeight)
				{
					width = (Int32)Content.ActualSize.X;
					if (Content is FrameworkElement frameworkElement)
					{
						width += (Int32)(frameworkElement.Margin.Left + frameworkElement.Margin.Right);
					}
					width = (Int32)(width * scale);
				}
				Int32 height = _window.AppWindow.ClientSize.Height;
				if (_window.SizeToContent == SizeToContent.Height || _window.SizeToContent == SizeToContent.WidthAndHeight)
				{
					height = (Int32)Content.ActualSize.Y;
					if (Content is FrameworkElement frameworkElement)
					{
						height += (Int32)(frameworkElement.Margin.Top + frameworkElement.Margin.Bottom);
					}
					height = (Int32)(height * scale);
				}
				_window.AppWindow.ResizeClient(new SizeInt32(width, height));
			}
		}
		catch (Exception ex)
		{
			// ユーザー起因では発生しないイベントなのでログのみ
			Log.Error("ページロード時エラー：" + GetType().Name + "\n" + ex.Message);
			Log.Information("スタックトレース：\n" + ex.StackTrace);
		}
	}
}
