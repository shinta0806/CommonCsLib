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
//  1.10  | 2023/03/25 (Sat) | IsCustomTitleBarEnabled を作成。
// ============================================================================

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// ELEMENT_NAME_CUSTOM_TITLE_BAR をカスタムタイトルバーとして使用する
	/// </summary>
	public readonly DependencyProperty IsCustomTitleBarEnabledProperty
			= DependencyProperty.Register(nameof(IsCustomTitleBarEnabled), typeof(Boolean), typeof(PageEx2),
			new PropertyMetadata(false, new PropertyChangedCallback(OnIsCustomTitleBarEnabledChanged)));
	public Boolean IsCustomTitleBarEnabled
	{
		get => (Boolean)GetValue(IsCustomTitleBarEnabledProperty);
		set => SetValue(IsCustomTitleBarEnabledProperty, value);
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
	/// 実際の背景ブラシ
	/// </summary>
	/// <param name="dependencyObject"></param>
	/// <returns></returns>
	private Brush? ActualBackground(DependencyObject dependencyObject)
	{
		if (dependencyObject is Control control && control.Background != null)
		{
			// Control として背景がある
			return control.Background;
		}

		if (dependencyObject is Panel panel && panel.Background != null)
		{
			// Panel として背景がある
			return panel.Background;
		}

		DependencyObject? parent = (dependencyObject as FrameworkElement)?.Parent;
		if (parent != null)
		{
			// 親の背景を返す
			return ActualBackground(parent);
		}

		// 親がいない場合は null
		return null;
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="dependencyObject"></param>
	/// <param name="args"></param>
	private static void OnIsCustomTitleBarEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyObject is not PageEx2 pageEx2)
		{
			return;
		}
		if (!AppWindowTitleBar.IsCustomizationSupported())
		{
			return;
		}

		pageEx2._window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = pageEx2.IsCustomTitleBarEnabled;

		// ToDo: Windows App SDK 1.2 現在、効果が無い模様
		pageEx2._window.AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
	}

	/// <summary>
	/// イベントハンドラー：ページがロードされた
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void PageEx2Loaded(Object _1, RoutedEventArgs _2)
	{
		try
		{
			WindowSizeToContent();
			SetCustomTitleBar();
		}
		catch (Exception ex)
		{
			// ユーザー起因では発生しないイベントなのでログのみ
			Log.Error("ページロード時エラー：" + GetType().Name + "\n" + ex.Message);
			Log.Information("スタックトレース：\n" + ex.StackTrace);
		}
	}

	/// <summary>
	/// ELEMENT_NAME_CUSTOM_TITLE_BAR をカスタムタイトルバーとして設定
	/// </summary>
	private void SetCustomTitleBar()
	{
		if (!IsCustomTitleBarEnabled || !AppWindowTitleBar.IsCustomizationSupported())
		{
			return;
		}
		FrameworkElement? customTitleBar = FindName(WinUi3Common.ELEMENT_CUSTOM_TITLE_BAR) as FrameworkElement;
		if (customTitleBar == null)
		{
			return;
		}

		// ドラッグできるようにする
		RectInt32[] rects = { new RectInt32((Int32)customTitleBar.ActualOffset.X, 0, Int32.MaxValue, (Int32)customTitleBar.ActualHeight) };
		_window.AppWindow.TitleBar.SetDragRectangles(rects);

		// ボタンの色を設定（デフォルト以外にしたい場合はアプリコードで設定が必要）
		_window.AppWindow.TitleBar.ButtonBackgroundColor = WinUi3Common.TITLE_BAR_COLOR;
		_window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = (Application.Current.Resources["SolidBackgroundFillColorBaseBrush"] as SolidColorBrush)?.Color;
	}

	/// <summary>
	/// WindowEx2.SizeToContent の処理
	/// </summary>
	private void WindowSizeToContent()
	{
		if (_window.SizeToContent == SizeToContent.Manual)
		{
			return;
		}

		// ToDo: Window.SizeToContent が実装されれば不要となるコード
		// Windows App SDK 1.2 現在、コンテンツのサイズは表示スケールに自動追随するが、ResizeClient() は表示スケールを考慮してくれない
		// ページコンテナが StackPanel の場合、HorizontalAlignment="Left" VerticalAlignment="Top" を指定する必要がある。
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
