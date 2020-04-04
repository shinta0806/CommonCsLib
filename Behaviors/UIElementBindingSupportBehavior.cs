// ============================================================================
// 
// UIElement のバインド可能なプロパティーを増やすためのビヘイビア
// Copyright (C) 2019 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/11/23 (Sat) | オリジナルバージョン。
// ============================================================================

using Microsoft.Xaml.Behaviors;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Shinta.Behaviors
{
	public class UIElementBindingSupportBehavior : Behavior<UIElement>
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ウィンドウ座標系での左位置をバインド可能にする
		public static readonly DependencyProperty LeftProperty =
				DependencyProperty.Register("Left", typeof(Double), typeof(UIElementBindingSupportBehavior),
				new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null));
		public Double Left
		{
			get => (Double)GetValue(LeftProperty);
			set => SetValue(LeftProperty, value);
		}

		// ウィンドウ座標系での上位置をバインド可能にする
		public static readonly DependencyProperty TopProperty =
				DependencyProperty.Register("Top", typeof(Double), typeof(UIElementBindingSupportBehavior),
				new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null));
		public Double Top
		{
			get => (Double)GetValue(TopProperty);
			set => SetValue(TopProperty, value);
		}

		// ====================================================================
		// public メンバー変数
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

			AssociatedObject.LayoutUpdated += ControlLayoutUpdated;
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// View 側でレイアウトが変更された
		// --------------------------------------------------------------------
		private void ControlLayoutUpdated(Object? oSender, EventArgs oArgs)
		{
			try
			{
				// 親ウィンドウを探す
				Window? aWindow = null;
				DependencyObject? aDep = AssociatedObject;
				while (aDep != null)
				{
					aDep = VisualTreeHelper.GetParent(aDep);
					aWindow = aDep as Window;
					if (aWindow != null)
					{
						break;
					}
				}
				if (aWindow == null)
				{
					return;
				}

				// 相対位置
				Point aPoint = AssociatedObject.TranslatePoint(new Point(0.0, 0.0), aWindow);
				Left = aPoint.X;
				Top = aPoint.Y;
			}
			catch (Exception)
			{
			}
		}
	}
	// public class UIElementBindingSupportBehavior ___END___

}
// namespace Shinta.Behaviors ___END___
