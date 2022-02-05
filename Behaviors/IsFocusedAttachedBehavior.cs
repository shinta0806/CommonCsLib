// ============================================================================
// 
// IsFocused 添付ビヘイビア
// Copyright (C) 2019-2022 by SHINTA
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
// (1.02) | 2022/02/05 (Sat) |   null 許容参照型の対応強化。
// ============================================================================

using System;
using System.Windows;

namespace Shinta.Behaviors
{
	public class IsFocusedAttachedBehavior
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		#region IsFocused 添付プロパティー
		public static readonly DependencyProperty IsFocusedProperty =
				DependencyProperty.RegisterAttached("IsFocused", typeof(Boolean), typeof(IsFocusedAttachedBehavior),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceIsFocusedChanged));

		public static Boolean GetIsFocused(DependencyObject obj)
		{
			return (Boolean)obj.GetValue(IsFocusedProperty);
		}

		public static void SetIsFocused(DependencyObject obj, Boolean value)
		{
			obj.SetValue(IsFocusedProperty, value);
		}
		#endregion

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ViewModel 側で IsFocused が変更された
		// --------------------------------------------------------------------
		private static void SourceIsFocusedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if ((Boolean)args.NewValue)
			{
				if (obj is UIElement element)
				{
					element.Focus();
				}

				// 再度フォーカスを当てる際にイベント駆動するように false にしておく
				SetIsFocused(obj, false);
			}
		}
	}
}

