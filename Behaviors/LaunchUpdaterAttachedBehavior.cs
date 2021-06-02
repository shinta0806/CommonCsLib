// ============================================================================
// 
// ちょちょいと自動更新との通信を行う添付ビヘイビア
// Copyright (C) 2019-2020 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// UpdaterLauncher を設定するとちょちょいと自動更新を起動し、結果がコマンドで返る
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/06/24 (Mon) | オリジナルバージョン。
// (1.01) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.02) | 2020/09/13 (Sun) |   リファクタリング。
// (1.03) | 2020/09/14 (Mon) |   起動失敗時にコマンドを発行するようにした。
// ============================================================================

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Shinta.Behaviors
{
	public class LaunchUpdaterAttachedBehavior
	{
		// ====================================================================
		// public メンバー変数
		// ====================================================================

		// UpdaterLauncher 添付プロパティー
		public static readonly DependencyProperty UpdaterLauncherProperty =
				DependencyProperty.RegisterAttached("UpdaterLauncher", typeof(UpdaterLauncher), typeof(LaunchUpdaterAttachedBehavior),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceUpdaterLauncherChanged));

		// コマンド添付プロパティー
		public static readonly DependencyProperty CommandProperty =
				DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(LaunchUpdaterAttachedBehavior),
				new PropertyMetadata(null, SourceCommandChanged));

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// UpdaterLauncher 添付プロパティー GET
		// --------------------------------------------------------------------
		public static UpdaterLauncher GetUpdaterLauncher(DependencyObject obj)
		{
			return (UpdaterLauncher)obj.GetValue(UpdaterLauncherProperty);
		}

		// --------------------------------------------------------------------
		// UpdaterLauncher 添付プロパティー SET
		// --------------------------------------------------------------------
		public static void SetUpdaterLauncher(DependencyObject obj, UpdaterLauncher value)
		{
			obj.SetValue(UpdaterLauncherProperty, value);
		}

		// --------------------------------------------------------------------
		// コマンド添付プロパティー GET
		// --------------------------------------------------------------------
		public static ICommand GetCommand(DependencyObject obj)
		{
			return (ICommand)obj.GetValue(CommandProperty);
		}

		// --------------------------------------------------------------------
		// コマンド添付プロパティー SET
		// --------------------------------------------------------------------
		public static void SetCommand(DependencyObject obj, ICommand value)
		{
			obj.SetValue(CommandProperty, value);
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// WndProc
		private static readonly HwndSourceHook _wndProc = new(WndProc);

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定されたコマンドが実行可能な場合にそのコマンドを返す
		// --------------------------------------------------------------------
		private static ICommand? ExecutableCommand(Object? sender)
		{
			if (sender is UIElement element)
			{
				ICommand? command = GetCommand(element);
				if (command != null && command.CanExecute(null))
				{
					return command;
				}
			}

			return null;
		}

		// --------------------------------------------------------------------
		// ViewModel 側で Command が変更された
		// --------------------------------------------------------------------
		private static void SourceCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (obj is not Window window)
			{
				return;
			}

			if (GetCommand(window) != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				WindowInteropHelper helper = new(window);
				HwndSource wndSource = HwndSource.FromHwnd(helper.Handle);
				wndSource.AddHook(_wndProc);
			}
			else
			{
				// コマンドが解除された場合はイベントハンドラーを無効にする
				WindowInteropHelper helper = new(window);
				HwndSource aWndSource = HwndSource.FromHwnd(helper.Handle);
				aWndSource.RemoveHook(_wndProc);
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で UpdaterLauncher が変更された
		// --------------------------------------------------------------------
		private static void SourceUpdaterLauncherChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (obj is not Window window)
			{
				return;
			}

			if (args.NewValue is not UpdaterLauncher launcher)
			{
				return;
			}

			// ウィンドウハンドル設定
			WindowInteropHelper helper = new(window);
			launcher.NotifyHWnd = helper.Handle;

			// ちょちょいと自動更新を起動
			if (!launcher.Launch(launcher.ForceShow))
			{
				WMUpdaterUIDisplayed(launcher.NotifyHWnd);
			}
		}

		// --------------------------------------------------------------------
		// HWnd から Window を取得
		// --------------------------------------------------------------------
		private static Window? WindowFromHWnd(IntPtr hWnd)
		{
			HwndSource wndSource = HwndSource.FromHwnd(hWnd);
			return wndSource.RootVisual as Window;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー（ちょちょいと自動更新からのメッセージを受信）
		// --------------------------------------------------------------------
		private static void WMUpdaterUIDisplayed(IntPtr hWnd)
		{
			ICommand? command = ExecutableCommand(WindowFromHWnd(hWnd));

			// コマンドを実行
			command?.Execute(null);
		}

		// --------------------------------------------------------------------
		// メッセージハンドラ
		// --------------------------------------------------------------------
		private static IntPtr WndProc(IntPtr hWnd, Int32 msg, IntPtr wParam, IntPtr lParam, ref Boolean handled)
		{
			switch (msg)
			{
				case UpdaterLauncher.WM_UPDATER_UI_DISPLAYED:
					WMUpdaterUIDisplayed(hWnd);
					handled = true;
					break;
			}

			return IntPtr.Zero;
		}
	}
}
