// ============================================================================
// 
// DataGrid を拡張するビヘイビア
// Copyright (C) 2024-2025 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 前提パッケージ：
// ・CommunityToolkit.WinUI.Behaviors
// ・CommunityToolkit.WinUI.UI.Controls.DataGrid
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2024/07/05 (Fri) | ファーストバージョン。
//  1.10  | 2024/07/05 (Fri) | AutoScroll プロパティーを付けた。
// (1.11) | 2025/07/17 (Thu) |   前提パッケージを変更。
// ============================================================================

using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.UI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Shinta.WinUi3.Behaviors;

public class DataGridExtensionBehavior : BehaviorBase<DataGrid>
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public DataGridExtensionBehavior()
	{
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 選択した行が見える位置にスクロールするか
	/// </summary>
	public static readonly DependencyProperty AutoScrollProperty
			= DependencyProperty.Register(nameof(AutoScroll), typeof(Boolean), typeof(DataGridExtensionBehavior),
			new PropertyMetadata(true, new PropertyChangedCallback(OnAutoScrollChanged)));
	public Boolean AutoScroll
	{
		get => (Boolean)GetValue(AutoScrollProperty);
		set => SetValue(AutoScrollProperty, value);
	}

	// ====================================================================
	// protected 関数
	// ====================================================================

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	protected override void OnAssociatedObjectLoaded()
	{
		base.OnAssociatedObjectLoaded();

		try
		{
			AssociatedObject.SelectionChanged += AssociatedObjectSelectionChanged;
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("DataGridExtensionBehavior.OnAssociatedObjectLoaded() エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	protected override void OnAssociatedObjectUnloaded()
	{
		base.OnAssociatedObjectUnloaded();

		try
		{
			// AssociatedObject == null の場合がある（例：DataGrid を含むタブを切り替えた場合など）
			if (AssociatedObject != null)
			{
				AssociatedObject.SelectionChanged -= AssociatedObjectSelectionChanged;
			}
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("DataGridExtensionBehavior.OnAssociatedObjectUnloaded() エラー", ex);
		}
	}

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void AssociatedObjectSelectionChanged(Object sender, SelectionChangedEventArgs args)
	{
		if (AutoScroll && AssociatedObject != null)
		{
			AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem, null);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="dependencyObject"></param>
	/// <param name="args"></param>
	private static void OnAutoScrollChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
	}
}
