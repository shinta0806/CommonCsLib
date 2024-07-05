// ============================================================================
// 
// DataGrid を拡張するビヘイビア
// Copyright (C) 2024 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2024/07/05 (Fri) | ファーストバージョン。
// ============================================================================

using CommunityToolkit.WinUI.UI.Behaviors;
using CommunityToolkit.WinUI.UI.Controls;

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
		AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem, null);
	}
}
