// ============================================================================
// 
// IsFocused 添付ビヘイビア
// Copyright (C) 2019 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// IsFocused を true にすることにより、コントロールにフォーカスが当たる
// ToDo: ViewModel 側に false が伝播されないので、ViewModel 側は RaisePropertyChangedIfSet() ではなく
// RaisePropertyChanged() で強制発効しないと再度フォーカスを当てられない
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/06/24 (Mon) | オリジナルバージョン。
// (1.01) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// ============================================================================

using System;
using System.Windows;

#nullable enable

namespace Shinta.Behaviors
{
	public class IsFocusedAttachedBehavior
	{
		// ====================================================================
		// public メンバー変数
		// ====================================================================

		// IsFocused 添付プロパティー
		public static readonly DependencyProperty IsFocusedProperty =
				DependencyProperty.RegisterAttached("IsFocused", typeof(Boolean), typeof(IsFocusedAttachedBehavior),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceIsFocusedChanged));

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// IsFocused 添付プロパティー GET
		// --------------------------------------------------------------------
		public static Boolean GetIsFocused(DependencyObject oObject)
		{
			return (Boolean)oObject.GetValue(IsFocusedProperty);
		}

		// --------------------------------------------------------------------
		// IsFocused 添付プロパティー SET
		// --------------------------------------------------------------------
		public static void SetIsFocused(DependencyObject oObject, Boolean oValue)
		{
			oObject.SetValue(IsFocusedProperty, oValue);
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ViewModel 側で IsFocused が変更された
		// --------------------------------------------------------------------
		private static void SourceIsFocusedChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			if ((Boolean)oArgs.NewValue)
			{
				UIElement? aElement = oObject as UIElement;
				aElement?.Focus();

				// 再度フォーカスを当てる際にイベント駆動するように false にしておく
				SetIsFocused(oObject, false);
			}
		}
	}
	// public class IsFocusedAttachedBehavior ___END___
}
// namespace Shinta.Behaviors ___END___
