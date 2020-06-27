﻿// ============================================================================
// 
// TextBox がフォーカスを得たときに全選択するためのビヘイビア
// Copyright (C) 2020 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2020/06/01 (Mon) | オリジナルバージョン。
// ============================================================================

using Microsoft.Xaml.Behaviors;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#nullable enable

namespace Shinta.Behaviors
{
	public class TextBoxAutoSelectBehavior : Behavior<TextBox>
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ====================================================================
		// public メンバー変数
		// ====================================================================

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

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
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// View 側でフォーカスが取得された
		// --------------------------------------------------------------------
		private void ControlGotFocus(Object sender, RoutedEventArgs e)
		{
			if (sender is TextBox textBox)
			{
				textBox.SelectAll();
			}
		}



	}
	// public class TextBoxAutoSelectBehavior ___END___

}
// namespace Shinta.Behaviors ___END___
