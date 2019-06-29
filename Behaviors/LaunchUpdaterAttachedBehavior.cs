// ============================================================================
// 
// ちょちょいと自動更新との通信を行う添付ビヘイビア
// Copyright (C) 2019 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// UpdaterLauncher を設定するとちょちょいと自動更新を起動し、結果がコマンドで返る
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/06/24 (Mon) | オリジナルバージョン。
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
		public static UpdaterLauncher GetUpdaterLauncher(DependencyObject oObject)
		{
			return (UpdaterLauncher)oObject.GetValue(UpdaterLauncherProperty);
		}

		// --------------------------------------------------------------------
		// UpdaterLauncher 添付プロパティー SET
		// --------------------------------------------------------------------
		public static void SetUpdaterLauncher(DependencyObject oObject, UpdaterLauncher oValue)
		{
			oObject.SetValue(UpdaterLauncherProperty, oValue);
		}

		// --------------------------------------------------------------------
		// コマンド添付プロパティー GET
		// --------------------------------------------------------------------
		public static ICommand GetCommand(DependencyObject oObject)
		{
			return (ICommand)oObject.GetValue(CommandProperty);
		}

		// --------------------------------------------------------------------
		// コマンド添付プロパティー SET
		// --------------------------------------------------------------------
		public static void SetCommand(DependencyObject oObject, ICommand oValue)
		{
			oObject.SetValue(CommandProperty, oValue);
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// WndProc
		private static HwndSourceHook smWndProc = new HwndSourceHook(WndProc);

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定されたコマンドが実行可能かどうか
		// --------------------------------------------------------------------
		private static Boolean CanExecuteCommand(Object oSender, out ICommand oCommand)
		{
			oCommand = null;

			UIElement aElement = oSender as UIElement;
			if (aElement == null)
			{
				return false;
			}

			oCommand = GetCommand(aElement);
			if (oCommand == null || !oCommand.CanExecute(null))
			{
				return false;
			}

			return true;
		}

		// --------------------------------------------------------------------
		// ViewModel 側で Command が変更された
		// --------------------------------------------------------------------
		private static void SourceCommandChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			Window aWindow = oObject as Window;
			if (aWindow == null)
			{
				return;
			}

			if (GetCommand(aWindow) != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				WindowInteropHelper aHelper = new WindowInteropHelper(aWindow);
				HwndSource aWndSource = HwndSource.FromHwnd(aHelper.Handle);
				aWndSource.AddHook(smWndProc);
			}
			else
			{
				// コマンドが解除された場合はイベントハンドラーを無効にする
				WindowInteropHelper aHelper = new WindowInteropHelper(aWindow);
				HwndSource aWndSource = HwndSource.FromHwnd(aHelper.Handle);
				aWndSource.RemoveHook(smWndProc);
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で UpdaterLauncher が変更された
		// --------------------------------------------------------------------
		private static void SourceUpdaterLauncherChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			Window aWindow = oObject as Window;
			if (aWindow == null)
			{
				return;
			}

			UpdaterLauncher aLauncher = oArgs.NewValue as UpdaterLauncher;
			if (aLauncher == null)
			{
				return;
			}

			// ウィンドウハンドル設定
			WindowInteropHelper aHelper = new WindowInteropHelper(aWindow);
			aLauncher.NotifyHWnd = aHelper.Handle;

			// ちょちょいと自動更新を起動
			aLauncher.Launch(aLauncher.ForceShow);
		}

		// --------------------------------------------------------------------
		// HWnd から Window を取得
		// --------------------------------------------------------------------
		private static Window WindowFromHWnd(IntPtr oHWnd)
		{
			HwndSource aWndSource = HwndSource.FromHwnd(oHWnd);
			return aWndSource.RootVisual as Window;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー（ちょちょいと自動更新からのメッセージを受信）
		// --------------------------------------------------------------------
		private static void WMUpdaterUIDisplayed(IntPtr oHWnd)
		{
			ICommand aCommand;
			if (!CanExecuteCommand(WindowFromHWnd(oHWnd), out aCommand))
			{
				return;
			}

			// コマンドを実行
			aCommand.Execute(null);
		}

		// --------------------------------------------------------------------
		// メッセージハンドラ
		// --------------------------------------------------------------------
		private static IntPtr WndProc(IntPtr oHWnd, Int32 oMsg, IntPtr oWParam, IntPtr oLParam, ref Boolean oHandled)
		{
			switch (oMsg)
			{
				case UpdaterLauncher.WM_UPDATER_UI_DISPLAYED:
					WMUpdaterUIDisplayed(oHWnd);
					oHandled = true;
					break;
			}

			return IntPtr.Zero;
		}

	}
	// public class LaunchUpdaterAttachedBehavior ___END___

}
// namespace Shinta.Behaviors ___END___
