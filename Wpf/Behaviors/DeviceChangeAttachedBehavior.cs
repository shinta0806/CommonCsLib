// ============================================================================
// 
// リムーバブルメディアの着脱時にコマンドを発行する添付ビヘイビア
// Copyright (C) 2019-2021 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/06/24 (Mon) | オリジナルバージョン。
// (1.01) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.02) | 2021/05/03 (Mon) |   CS8605 警告（null の可能性がある値をボックス化解除しています）に対処。
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

#nullable enable

namespace Shinta.Wpf.Behaviors
{
	public class DeviceChangeAttachedBehavior
	{
		// ====================================================================
		// public メンバー変数
		// ====================================================================

		// コマンド添付プロパティー
		public static readonly DependencyProperty CommandProperty =
				DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(DeviceChangeAttachedBehavior),
				new PropertyMetadata(null, SourceCommandChanged));

		// ====================================================================
		// public メンバー関数
		// ====================================================================

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
		public static void SetCommand(DependencyObject obj, ICommand val)
		{
			obj.SetValue(CommandProperty, val);
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
				HwndSource aWndSource = HwndSource.FromHwnd(helper.Handle);
				aWndSource.AddHook(_wndProc);
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
		// HWnd から Window を取得
		// --------------------------------------------------------------------
		private static Window? WindowFromHWnd(IntPtr hWnd)
		{
			HwndSource wndSource = HwndSource.FromHwnd(hWnd);
			return wndSource.RootVisual as Window;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// USB メモリ等の着脱により呼び出される
		// --------------------------------------------------------------------
		private static void WmDeviceChange(IntPtr hWnd, IntPtr wParam, IntPtr lParam)
		{
			ICommand? command = ExecutableCommand(WindowFromHWnd(hWnd));
			if (command == null)
			{
				return;
			}

			switch ((DBT)wParam.ToInt32())
			{
				case DBT.DBT_DEVICEARRIVAL:
				case DBT.DBT_DEVICEREMOVECOMPLETE:
					break;
				default:
					return;
			}
			if (lParam == IntPtr.Zero)
			{
				return;
			}

			if (Marshal.PtrToStructure(lParam, typeof(WindowsApi.DEV_BROADCAST_HDR)) is not WindowsApi.DEV_BROADCAST_HDR hdr)
			{
				return;
			}
			if (hdr.dbch_devicetype != (Int32)DBT_DEVTYP.DBT_DEVTYP_VOLUME)
			{
				return;
			}

			if (Marshal.PtrToStructure(lParam, typeof(WindowsApi.DEV_BROADCAST_VOLUME)) is not WindowsApi.DEV_BROADCAST_VOLUME volume)
			{
				return;
			}
			UInt32 unitMask = volume.dbcv_unitmask;
			if (unitMask == 0)
			{
				return;
			}

			Char numShift = (Char)0;
			String driveLetter;
			while (unitMask != 1)
			{
				unitMask >>= 1;
				numShift++;
			}
			driveLetter = new String((Char)('A' + numShift), 1) + ":";

			// 着脱情報を引数としてコマンドを実行
			DeviceChangeInfo info = new();
			info.Kind = (DBT)wParam.ToInt32();
			info.DriveLetter = driveLetter;
			command.Execute(info);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// SD カード等の着脱により呼び出される
		// --------------------------------------------------------------------
		private static void WmShNotify(IntPtr hWnd, IntPtr wParam, IntPtr lParam)
		{
			ICommand? command = ExecutableCommand(WindowFromHWnd(hWnd));
			if (command == null)
			{
				return;
			}

			switch ((SHCNE)lParam)
			{
				case SHCNE.SHCNE_MEDIAINSERTED:
				case SHCNE.SHCNE_MEDIAREMOVED:
					break;
				default:
					return;
			}

			if (Marshal.PtrToStructure(wParam, typeof(WindowsApi.SHNOTIFYSTRUCT)) is not WindowsApi.SHNOTIFYSTRUCT shNotifyStruct)
			{
				return;
			}
			StringBuilder driveRoot = new();
			WindowsApi.SHGetPathFromIDList((IntPtr)shNotifyStruct.dwItem1, driveRoot);
			String driveLetter = driveRoot.ToString()[..2];

			// 着脱情報を引数としてコマンドを実行
			DeviceChangeInfo info = new();
			info.Kind = (SHCNE)lParam == SHCNE.SHCNE_MEDIAINSERTED ? DBT.DBT_DEVICEARRIVAL : DBT.DBT_DEVICEREMOVECOMPLETE;
			info.DriveLetter = driveLetter;
			command.Execute(info);
		}

		// --------------------------------------------------------------------
		// メッセージハンドラ
		// --------------------------------------------------------------------
		private static IntPtr WndProc(IntPtr hWnd, Int32 msg, IntPtr wParam, IntPtr lParam, ref Boolean handled)
		{
			try
			{
				switch ((WM)msg)
				{
					case WM.WM_DEVICECHANGE:
						WmDeviceChange(hWnd, wParam, lParam);
						handled = true;
						break;
					case WM.WM_SHNOTIFY:
						WmShNotify(hWnd, wParam, lParam);
						handled = true;
						break;
				}
			}
			catch (Exception)
			{
			}

			return IntPtr.Zero;
		}

	}
	// public class DeviceChangeAttachedBehavior ___END___

	public class DeviceChangeInfo
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// 着脱種別
		public DBT Kind { get; set; }

		// 着脱されたドライブレター（"A:" のようにコロンまで）
		public String? DriveLetter { get; set; }
	}
	// public class DeviceChangeInfo ___END___
}
// namespace Shinta.Behaviors ___END___
