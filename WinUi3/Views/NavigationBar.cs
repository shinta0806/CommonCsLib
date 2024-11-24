// ============================================================================
// 
// トグルボタンをラジオボタンとして使うコンテナ
// 
// ============================================================================

// ----------------------------------------------------------------------------
// CommunityToolkit.WinUI.Controls.Primitives パッケージ前提
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2024/01/21 (Sun) | 作成開始。
//  1.00  | 2024/01/21 (Sun) | ファーストバージョン。
// (1.01) | 2024/11/24 (Sun) |   前提パッケージを変更。
// ============================================================================

using CommunityToolkit.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Shinta.WinUi3.Views;

internal sealed class NavigationBar : WrapPanel, INavigationBar
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public NavigationBar()
	{
		// デフォルトプロパティー（XAML で上書き可能）
		SetDefaultProperties();

		// イベントハンドラー
		Loaded += NavigationBarLoaded;
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 押下したトグルボタンのインデックス
	/// </summary>
	public static readonly DependencyProperty SelectedIndexProperty
			= DependencyProperty.Register(nameof(SelectedIndex), typeof(Int32), typeof(NavigationBar),
			new PropertyMetadata(0, new PropertyChangedCallback(OnSelectedIndexChanged)));
	public Int32 SelectedIndex
	{
		get => (Int32)GetValue(SelectedIndexProperty);
		set => SetValue(SelectedIndexProperty, value);
	}

	/// <summary>
	/// 縦に並べるボタンの数
	/// </summary>
	public static readonly DependencyProperty RowsProperty
			= DependencyProperty.Register(nameof(Rows), typeof(Int32), typeof(NavigationBar),
			new PropertyMetadata(0, new PropertyChangedCallback(OnRowsChanged)));
	public Int32 Rows
	{
		get => (Int32)GetValue(RowsProperty);
		set => SetValue(RowsProperty, value);
	}

	/// <summary>
	/// 幅を揃える
	/// </summary>
	public static readonly DependencyProperty UniformWidthProperty
			= DependencyProperty.Register(nameof(UniformWidth), typeof(Boolean), typeof(NavigationBar),
			new PropertyMetadata(false, new PropertyChangedCallback(OnUniformWidthChanged)));
	public Boolean UniformWidth
	{
		get => (Boolean)GetValue(UniformWidthProperty);
		set => SetValue(UniformWidthProperty, value);
	}

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// 子トグルボタン群の配置を調整
	/// </summary>
	private void LayoutToggleButtons()
	{
		List<ToggleButton> toggleButtons = ((INavigationBar)this).ToggleButtons();

		// 折り返し
		if (0 < Rows && Rows < toggleButtons.Count)
		{
			ToggleButton toggleButton = toggleButtons[Rows - 1];
			Height = toggleButton.ActualOffset.Y + toggleButton.ActualHeight;
		}

		// 横幅を揃える
		if (UniformWidth)
		{
			Double maxWidth = toggleButtons.Max(x => x.ActualWidth);
			if (maxWidth > 0)
			{
				foreach (ToggleButton toggleButton in toggleButtons)
				{
					toggleButton.Width = maxWidth;
				}
			}
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void NavigationBarLoaded(Object sender, RoutedEventArgs args)
	{
		try
		{
			LayoutToggleButtons();
			((INavigationBar)this).SetToggleButtons();
			((INavigationBar)this).SelectedIndexToToggleButtons();
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("NavigationBar ロード時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="dependencyObject"></param>
	/// <param name="args"></param>
	private static void OnRowsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="dependencyObject"></param>
	/// <param name="args"></param>
	private static void OnSelectedIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		try
		{
			if (dependencyObject is not INavigationBar navigationBar)
			{
				return;
			}

			navigationBar.SelectedIndexToToggleButtons();
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("NavigationBar SelectedIndex 変更時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="dependencyObject"></param>
	/// <param name="args"></param>
	private static void OnUniformWidthChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
	}

	/// <summary>
	/// デフォルトプロパティー
	/// </summary>
	private void SetDefaultProperties()
	{
		Orientation = Orientation.Vertical;
	}
}
