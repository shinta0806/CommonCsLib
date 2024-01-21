// ============================================================================
// 
// NavigationBar の基本機能を備えるインターフェース
// 
// ============================================================================

// ----------------------------------------------------------------------------
// NavigationBar をカスタマイズする際に本クラスが便利
// ----------------------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Shinta.WinUi3.Views;

internal interface INavigationBar
{
	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 押下したトグルボタンのインデックス
	/// </summary>
	public Int32 SelectedIndex
	{
		get;
		set;
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// SelectedIndex に応じてトグルボタンを押下
	/// </summary>
	public void SelectedIndexToToggleButtons()
	{
		List<ToggleButton> toggleButtons = ToggleButtons();
		if (SelectedIndex < 0 || SelectedIndex >= toggleButtons.Count)
		{
			return;
		}
		if (toggleButtons[SelectedIndex].IsChecked == true)
		{
			return;
		}

		// 押下状態設定
		toggleButtons[SelectedIndex].IsChecked = true;
	}

	/// <summary>
	/// 子トグルボタン群の設定
	/// </summary>
	public void SetToggleButtons()
	{
		List<ToggleButton> toggleButtons = ToggleButtons();

		foreach (ToggleButton toggleButton in toggleButtons)
		{
			// イベントハンドラー設定
			toggleButton.PointerEntered -= ToggleButtonPointerEntered;
			toggleButton.PointerEntered += ToggleButtonPointerEntered;
			toggleButton.PointerExited -= ToggleButtonPointerExited;
			toggleButton.PointerExited += ToggleButtonPointerExited;
			toggleButton.PointerCanceled -= ToggleButtonPointerCanceled;
			toggleButton.PointerCanceled += ToggleButtonPointerCanceled;
			toggleButton.Checked -= ToggleButtonChecked;
			toggleButton.Checked += ToggleButtonChecked;
			toggleButton.Unchecked -= ToggleButtonUnchecked;
			toggleButton.Unchecked += ToggleButtonUnchecked;

			// ツールチップ設定
			SetToggleButtonToolTip(toggleButton);
		}
	}

	/// <summary>
	/// 子トグルボタン列挙
	/// </summary>
	/// <returns></returns>
	public List<ToggleButton> ToggleButtons()
	{
		List<ToggleButton> toggleButtons = new();

		Panel panel = (Panel)this;
		foreach (UIElement element in panel.Children)
		{
			if (element is ToggleButton toggleButton)
			{
				toggleButtons.Add(toggleButton);
			}
		}
		return toggleButtons;
	}

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// ツールチップ表示・消去
	/// </summary>
	/// <param name="dependencyObject">ツールチップが設定されているオブジェクト</param>
	private static void OpenCloseToolTip(DependencyObject dependencyObject, Boolean open)
	{
		ToolTip? toolTip = ToolTipService.GetToolTip(dependencyObject) as ToolTip;
		if (toolTip == null)
		{
			return;
		}

		toolTip.IsEnabled = open;
		toolTip.IsOpen = open;
	}

	/// <summary>
	/// 子トグルボタンのツールチップ設定
	/// </summary>
	/// <param name="toggleButton"></param>
	private static void SetToggleButtonToolTip(ToggleButton toggleButton)
	{
		// ツールチップ取得
		Object? obj = ToolTipService.GetToolTip(toggleButton);
		ToolTip? toolTip = obj as ToolTip;
		if (obj is String str)
		{
			// XAML で文字列が指定されていた場合
			toolTip = new()
			{
				Content = str
			};
		}
		if (toolTip == null)
		{
			return;
		}

		// ツールチップ設定
		toolTip.Placement = PlacementMode.Right;

		// 自動でツールチップが開く場合と、IsOpen で制御する場合で、(0, 0, 0, 0) の時の位置が異なる（いずれもボタンサイズ、マウスポインタ位置によらない）
		// 自動 → ツールチップ枠の左下がボタンの (20, 15) あたりに来る
		// IsOpen → ツールチップ枠の左下がボタンの (0, 15) あたりに来る
		toolTip.PlacementRect = new(toggleButton.ActualWidth, toggleButton.ActualHeight - 15, 0, 0);
		toolTip.IsEnabled = false;
		ToolTipService.SetToolTip(toggleButton, toolTip);
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void ToggleButtonChecked(Object sender, RoutedEventArgs args)
	{
		try
		{
			List<ToggleButton> toggleButtons = ToggleButtons();
			Int32 index = toggleButtons.IndexOf((ToggleButton)sender);

			// 他のトグルボタンは押下解除
			UncheckExcept(toggleButtons, index);

			// 選択変更
			SelectedIndex = index;
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("NavigationBar トグルボタンチェック時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void ToggleButtonPointerCanceled(Object sender, PointerRoutedEventArgs args)
	{
		try
		{
			OpenCloseToolTip((ToggleButton)sender, false);
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("NavigationBar ポインターキャンセル時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void ToggleButtonPointerEntered(Object sender, PointerRoutedEventArgs args)
	{
		try
		{
			ToggleButton toggleButton = (ToggleButton)sender;

			// ロード時に非表示の場合（？）ツールチップの位置がきちんと設定されない為、表示直前に設定する
			SetToggleButtonToolTip(toggleButton);

			// 押下されていない場合は直ちにツールチップを表示（押下されている場合はツールチップが無効化されているので表示されない）
			OpenCloseToolTip(toggleButton, true);
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("NavigationBar トグルボタン入時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void ToggleButtonPointerExited(Object sender, PointerRoutedEventArgs args)
	{
		try
		{
			OpenCloseToolTip((ToggleButton)sender, false);
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("NavigationBar トグルボタン出時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void ToggleButtonUnchecked(Object sender, RoutedEventArgs args)
	{
		try
		{
			List<ToggleButton> toggleButtons = ToggleButtons();
			ToggleButton? checkedButton = toggleButtons.FirstOrDefault(x => x.IsChecked == true);
			if (checkedButton == null)
			{
				// 押下されているボタンがない場合は、今のボタンを再度押下
				((ToggleButton)sender).IsChecked = true;
			}
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("NavigationBar トグルボタンチェック解除時エラー", ex);
		}
	}

	/// <summary>
	/// 指定されたトグルボタン以外を押下解除
	/// </summary>
	/// <param name="exceptIndex"></param>
	private static void UncheckExcept(List<ToggleButton> toggleButtons, Int32 exceptIndex)
	{
		for (Int32 i = 0; i < toggleButtons.Count; i++)
		{
			if (i != exceptIndex)
			{
				toggleButtons[i].IsChecked = false;
			}
		}
	}
}
