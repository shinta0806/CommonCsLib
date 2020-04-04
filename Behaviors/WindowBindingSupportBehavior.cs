// ============================================================================
// 
// Window のバインド可能なプロパティーを増やすためのビヘイビア
// Copyright (C) 2019-2020 by SHINTA
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
// (1.11) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.12) | 2020/03/29 (Sun) |   null 許容参照型の対応強化。
// ============================================================================

using Microsoft.Xaml.Behaviors;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

#nullable enable

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

		// ウィンドウのキャプションバーに最小化ボタンを表示するかどうか
		public Boolean MinimizeBox
		{
			get => (Boolean)GetValue(MinimizeBoxProperty);
			set => SetValue(MinimizeBoxProperty, value);
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

		// ウィンドウのキャプションバーに最小化ボタンを表示するかどうか
		public static readonly DependencyProperty MinimizeBoxProperty =
				DependencyProperty.Register("MinimizeBox", typeof(Boolean), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceMinimizeBoxChanged));

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
			Double delta = SystemParameters.CaptionHeight + SystemParameters.WindowResizeBorderThickness.Top;
			Double newLeft = AssociatedObject.Owner.Left + delta;
			Double newTop = AssociatedObject.Owner.Top + delta;

			// ディスプレイからはみ出さないように調整
			// ToDo: オーナーウィンドウと同じディスプレイからはみ出さないように調整
			if (newLeft + AssociatedObject.ActualWidth > SystemParameters.VirtualScreenWidth)
			{
				newLeft = AssociatedObject.Owner.Left;
			}
			if (newTop + AssociatedObject.ActualHeight > SystemParameters.VirtualScreenHeight)
			{
				newTop = AssociatedObject.Owner.Top;
			}

			AssociatedObject.Left = newLeft;
			AssociatedObject.Top = newTop;
		}

		// --------------------------------------------------------------------
		// View 側で IsActive が変更された
		// --------------------------------------------------------------------
		private void ControlActivated(Object? sender, EventArgs args)
		{
			IsActive = true;
		}

		// --------------------------------------------------------------------
		// View 側で Closing された
		// --------------------------------------------------------------------
		private void ControlClosing(Object sender, CancelEventArgs cancelEventArgs)
		{
			if (ClosingCommand == null || !ClosingCommand.CanExecute(null))
			{
				return;
			}

			// イベント引数を引数としてコマンドを実行
			ClosingCommand.Execute(cancelEventArgs);
		}

		// --------------------------------------------------------------------
		// View 側で IsActive が変更された
		// --------------------------------------------------------------------
		private void ControlDeactivated(Object? sender, EventArgs args)
		{
			IsActive = false;
		}

		// --------------------------------------------------------------------
		// View 側で Loaded された
		// --------------------------------------------------------------------
		private void ControlLoaded(Object sender, RoutedEventArgs routedEventArgs)
		{
			if (AssociatedObject != null && AssociatedObject.SizeToContent != SizeToContent.Manual)
			{
				// SizeToContent が WidthAndHeight の場合は SourceInitialized よりも Loaded が先に呼び出されるのでここで処理を行う
				CascadeWindowIfNeeded();
				UpdateMinimizeBox();
			}
		}

		// --------------------------------------------------------------------
		// View 側で SourceInitialized された
		// --------------------------------------------------------------------
		private void ControlSourceInitialized(Object? sender, EventArgs args)
		{
			if (AssociatedObject != null && AssociatedObject.SizeToContent == SizeToContent.Manual)
			{
				// SizeToContent が Manual の場合は Loaded よりも SourceInitialized が先に呼び出されるのでここで処理を行う
				CascadeWindowIfNeeded();
				UpdateMinimizeBox();
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で ClosingCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceClosingCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			WindowBindingSupportBehavior? thisObject = obj as WindowBindingSupportBehavior;
			if (thisObject == null || thisObject.AssociatedObject == null)
			{
				return;
			}

			if (args.NewValue != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				thisObject.AssociatedObject.Closing += thisObject.ControlClosing;
			}
			else
			{
				// コマンドが解除された場合はイベントハンドラーを無効にする
				thisObject.AssociatedObject.Closing -= thisObject.ControlClosing;
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で IsActive が変更された
		// --------------------------------------------------------------------
		private static void SourceIsActiveChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			WindowBindingSupportBehavior? thisObject = obj as WindowBindingSupportBehavior;
			if (thisObject == null || thisObject.AssociatedObject == null)
			{
				return;
			}

			if ((Boolean)args.NewValue)
			{
				thisObject.AssociatedObject.Activate();
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で IsCascade が変更された
		// --------------------------------------------------------------------
		private static void SourceIsCascadeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			WindowBindingSupportBehavior? thisObject = obj as WindowBindingSupportBehavior;
			thisObject?.CascadeWindowIfNeeded();
		}

		// --------------------------------------------------------------------
		// ViewModel 側で MinimizeBox が変更された
		// --------------------------------------------------------------------
		private static void SourceMinimizeBoxChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			WindowBindingSupportBehavior? thisObject = obj as WindowBindingSupportBehavior;
			thisObject?.UpdateMinimizeBox();
		}

		// --------------------------------------------------------------------
		// 最小化ボタンの状態を更新
		// --------------------------------------------------------------------
		private void UpdateMinimizeBox()
		{
			if (AssociatedObject == null)
			{
				return;
			}

			WindowInteropHelper helper = new WindowInteropHelper(AssociatedObject);
			Int64 style = (Int64)WindowsApi.GetWindowLong(helper.Handle, (Int32)GWL.GWL_STYLE);

			if (MinimizeBox)
			{
				WindowsApi.SetWindowLong(helper.Handle, (Int32)GWL.GWL_STYLE, (IntPtr)(style | ((Int64)WS.WS_MINIMIZEBOX)));
			}
			else
			{
				WindowsApi.SetWindowLong(helper.Handle, (Int32)GWL.GWL_STYLE, (IntPtr)(style & ~((Int64)WS.WS_MINIMIZEBOX)));
			}
		}

	}
	// public class WindowBindingSupportBehavior ___END___

}
// namespace Shinta.Behaviors ___END___
