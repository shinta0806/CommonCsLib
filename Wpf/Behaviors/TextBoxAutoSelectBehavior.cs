// ============================================================================
// 
// TextBox がフォーカスを得たときに全選択するためのビヘイビア
// Copyright (C) 2020-2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2020/06/01 (Mon) | オリジナルバージョン。
// (1.01) | 2022/05/22 (Sun) |   マウスクリックでも全選択するようにした。
// ============================================================================

using Microsoft.Xaml.Behaviors;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shinta.Wpf.Behaviors
{
	public class TextBoxAutoSelectBehavior : Behavior<TextBox>
	{
		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// アタッチ時の準備作業
		// --------------------------------------------------------------------
		protected override void OnAttached()
		{
			base.OnAttached();

			AssociatedObject.GotFocus += ControlGotFocus;
			AssociatedObject.PreviewMouseLeftButtonDown += ControlLeftButtonDown;
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// View 側でフォーカスが取得された
		// --------------------------------------------------------------------
		private static void ControlGotFocus(Object sender, RoutedEventArgs e)
		{
			if (sender is TextBox textBox)
			{
				textBox.SelectAll();
			}
		}

		// --------------------------------------------------------------------
		// View 側でフォーカスが取得された
		// --------------------------------------------------------------------
		private static void ControlLeftButtonDown(Object sender, MouseButtonEventArgs e)
		{
			if (sender is TextBox textBox)
			{
				if (textBox.IsFocused)
				{
					return;
				}
				textBox.Focus();
				e.Handled = true;
			}
		}
	}
}
