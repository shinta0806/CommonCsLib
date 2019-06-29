// ============================================================================
// 
// Window のバインド可能なプロパティーを増やすためのビヘイビア
// Copyright (C) 2019 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/06/24 (Mon) | オリジナルバージョン。
//  1.10  | 2019/06/29 (Sat) | IsCascade を実装。
// ============================================================================

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Shinta.Behaviors
{
	public class WindowBindingSupportBehavior : Behavior<Window>
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// Window.Closing をコマンドで扱えるようにする
		public ICommand ClosingCommand
		{
			get => (ICommand)GetValue(ClosingCommandProperty);
			set => SetValue(ClosingCommandProperty, value);
		}

		// Window.IsActive をバインド可能にする
		public Boolean IsActive
		{
			get => (Boolean)GetValue(IsActiveProperty);
			set => SetValue(IsActiveProperty, value);
		}

		// オーナーウィンドウが設定されている場合、オーナーウィンドウの位置に対してカスケードするかどうか
		public Boolean IsCascade
		{
			get => (Boolean)GetValue(IsCascadeProperty);
			set => SetValue(IsCascadeProperty, value);
		}

		// ====================================================================
		// public メンバー変数
		// ====================================================================

		// Window.Closing
		public static readonly DependencyProperty ClosingCommandProperty
				= DependencyProperty.RegisterAttached("ClosingCommand", typeof(ICommand), typeof(WindowBindingSupportBehavior),
				new PropertyMetadata(null, SourceClosingCommandChanged));

		// Window.IsActive は元々読み取り専用だが変更可能とするためにコールバックを登録する
		public static readonly DependencyProperty IsActiveProperty =
				DependencyProperty.Register("IsActive", typeof(Boolean), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceIsActiveChanged));

		// オーナーウィンドウの位置に対してカスケードするかどうか
		public static readonly DependencyProperty IsCascadeProperty =
				DependencyProperty.Register("IsCascade", typeof(Boolean), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceIsCascadeChanged));

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// アタッチ時の準備作業
		// --------------------------------------------------------------------
		protected override void OnAttached()
		{
			base.OnAttached();

			AssociatedObject.Activated += ControlActivated;
			AssociatedObject.Deactivated += ControlDeactivated;
			AssociatedObject.Loaded += ControlLoaded;
			AssociatedObject.SourceInitialized += ControlSourceInitialized;
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ウィンドウの位置をオーナーウィンドウに対してカスケードする
		// --------------------------------------------------------------------
		private void CascadeWindowIfNeeded()
		{
			if (!IsCascade || AssociatedObject?.Owner == null)
			{
				return;
			}

			// 位置をずらす
			Double aDelta = SystemParameters.CaptionHeight + SystemParameters.WindowResizeBorderThickness.Top;
			Double aNewLeft = AssociatedObject.Owner.Left + aDelta;
			Double aNewTop = AssociatedObject.Owner.Top + aDelta;

			// ディスプレイからはみ出さないように調整
			// ToDo: オーナーウィンドウと同じディスプレイからはみ出さないように調整
			if (aNewLeft + AssociatedObject.ActualWidth > SystemParameters.VirtualScreenWidth)
			{
				aNewLeft = AssociatedObject.Owner.Left;
			}
			if (aNewTop + AssociatedObject.ActualHeight > SystemParameters.VirtualScreenHeight)
			{
				aNewTop = AssociatedObject.Owner.Top;
			}

			AssociatedObject.Left = aNewLeft;
			AssociatedObject.Top = aNewTop;
		}

		// --------------------------------------------------------------------
		// View 側で IsActive が変更された
		// --------------------------------------------------------------------
		private void ControlActivated(Object oSender, EventArgs oArgs)
		{
			IsActive = true;
		}

		// --------------------------------------------------------------------
		// View 側で Closing された
		// --------------------------------------------------------------------
		private void ControlClosing(Object oSender, CancelEventArgs oCancelEventArgs)
		{
			if (ClosingCommand == null || !ClosingCommand.CanExecute(null))
			{
				return;
			}

			// イベント引数を引数としてコマンドを実行
			ClosingCommand.Execute(oCancelEventArgs);
		}

		// --------------------------------------------------------------------
		// View 側で IsActive が変更された
		// --------------------------------------------------------------------
		private void ControlDeactivated(Object oSender, EventArgs oArgs)
		{
			IsActive = false;
		}

		// --------------------------------------------------------------------
		// View 側で Loaded された
		// --------------------------------------------------------------------
		private void ControlLoaded(Object oSender, RoutedEventArgs oRoutedEventArgs)
		{
			if (AssociatedObject != null && AssociatedObject.SizeToContent != SizeToContent.Manual)
			{
				// SizeToContent が WidthAndHeight の場合は SourceInitialized よりも Loaded が先に呼び出されるのでここでカスケードする
				CascadeWindowIfNeeded();
			}
		}

		// --------------------------------------------------------------------
		// View 側で SourceInitialized された
		// --------------------------------------------------------------------
		private void ControlSourceInitialized(Object oSender, EventArgs oArgs)
		{
			if (AssociatedObject != null && AssociatedObject.SizeToContent == SizeToContent.Manual)
			{
				// SizeToContent が Manual の場合は Loaded よりも SourceInitialized が先に呼び出されるのでここでカスケードする
				CascadeWindowIfNeeded();
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で ClosingCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceClosingCommandChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			WindowBindingSupportBehavior aThisObject = oObject as WindowBindingSupportBehavior;
			if (aThisObject == null || aThisObject.AssociatedObject == null)
			{
				return;
			}

			if (oArgs.NewValue != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				aThisObject.AssociatedObject.Closing += aThisObject.ControlClosing;
			}
			else
			{
				// コマンドが解除された場合はイベントハンドラーを無効にする
				aThisObject.AssociatedObject.Closing -= aThisObject.ControlClosing;
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で IsActive が変更された
		// --------------------------------------------------------------------
		private static void SourceIsActiveChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			WindowBindingSupportBehavior aThisObject = oObject as WindowBindingSupportBehavior;
			if (aThisObject == null || aThisObject.AssociatedObject == null)
			{
				return;
			}

			if ((Boolean)oArgs.NewValue)
			{
				aThisObject.AssociatedObject.Activate();
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で IsCascade が変更された
		// --------------------------------------------------------------------
		private static void SourceIsCascadeChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			WindowBindingSupportBehavior aThisObject = oObject as WindowBindingSupportBehavior;
			aThisObject?.CascadeWindowIfNeeded();
		}

	}
	// public class WindowBindingSupportBehavior ___END___

}
// namespace Shinta.Behaviors ___END___
