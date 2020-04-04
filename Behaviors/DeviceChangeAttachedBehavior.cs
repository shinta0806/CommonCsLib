// ============================================================================
// 
// リムーバブルメディアの着脱時にコマンドを発行する添付ビヘイビア
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
// (1.01) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

#nullable enable

namespace Shinta.Behaviors
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
		// 設定されたコマンドが実行可能な場合にそのコマンドを返す
		// --------------------------------------------------------------------
		private static ICommand? ExecutableCommand(Object? oSender)
		{
			if (oSender is UIElement aElement)
			{
				ICommand? aCommand = GetCommand(aElement);
				if (aCommand != null && aCommand.CanExecute(null))
				{
					return aCommand;
				}
			}

			return null;
		}

		// --------------------------------------------------------------------
		// ViewModel 側で Command が変更された
		// --------------------------------------------------------------------
		private static void SourceCommandChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			if (!(oObject is Window aWindow))
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
		// HWnd から Window を取得
		// --------------------------------------------------------------------
		private static Window? WindowFromHWnd(IntPtr oHWnd)
		{
			HwndSource aWndSource = HwndSource.FromHwnd(oHWnd);
			return aWndSource.RootVisual as Window;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// USB メモリ等の着脱により呼び出される
		// --------------------------------------------------------------------
		private static void WmDeviceChange(IntPtr oHWnd, IntPtr oWParam, IntPtr oLParam)
		{
			ICommand? aCommand = ExecutableCommand(WindowFromHWnd(oHWnd));
			if (aCommand == null)
			{
				return;
			}

			switch ((DBT)oWParam.ToInt32())
			{
				case DBT.DBT_DEVICEARRIVAL:
				case DBT.DBT_DEVICEREMOVECOMPLETE:
					break;
				default:
					return;
			}
			if (oLParam == IntPtr.Zero)
			{
				return;
			}

			WindowsApi.DEV_BROADCAST_HDR aHdr = (WindowsApi.DEV_BROADCAST_HDR)Marshal.PtrToStructure(oLParam, typeof(WindowsApi.DEV_BROADCAST_HDR));
			if (aHdr.dbch_devicetype != (Int32)DBT_DEVTYP.DBT_DEVTYP_VOLUME)
			{
				return;
			}

			WindowsApi.DEV_BROADCAST_VOLUME aVolume = (WindowsApi.DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(oLParam, typeof(WindowsApi.DEV_BROADCAST_VOLUME));
			UInt32 aUnitMask = aVolume.dbcv_unitmask;
			if (aUnitMask == 0)
			{
				return;
			}

			Char aNumShift = (Char)0;
			String aDriveLetter;
			while (aUnitMask != 1)
			{
				aUnitMask >>= 1;
				aNumShift++;
			}
			aDriveLetter = new String((Char)('A' + aNumShift), 1) + ":";

			// 着脱情報を引数としてコマンドを実行
			DeviceChangeInfo aInfo = new DeviceChangeInfo();
			aInfo.Kind = (DBT)oWParam.ToInt32();
			aInfo.DriveLetter = aDriveLetter;
			aCommand.Execute(aInfo);
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// SD カード等の着脱により呼び出される
		// --------------------------------------------------------------------
		private static void WmShNotify(IntPtr oHWnd, IntPtr oWParam, IntPtr oLParam)
		{
			ICommand? aCommand = ExecutableCommand(WindowFromHWnd(oHWnd));
			if (aCommand == null)
			{
				return;
			}

			switch ((SHCNE)oLParam)
			{
				case SHCNE.SHCNE_MEDIAINSERTED:
				case SHCNE.SHCNE_MEDIAREMOVED:
					break;
				default:
					return;
			}

			WindowsApi.SHNOTIFYSTRUCT aShNotifyStruct = (WindowsApi.SHNOTIFYSTRUCT)Marshal.PtrToStructure(oWParam, typeof(WindowsApi.SHNOTIFYSTRUCT));
			StringBuilder aDriveRoot = new StringBuilder();
			WindowsApi.SHGetPathFromIDList((IntPtr)aShNotifyStruct.dwItem1, aDriveRoot);
			String aDriveLetter = aDriveRoot.ToString().Substring(0, 2);

			// 着脱情報を引数としてコマンドを実行
			DeviceChangeInfo aInfo = new DeviceChangeInfo();
			aInfo.Kind = (SHCNE)oLParam == SHCNE.SHCNE_MEDIAINSERTED ? DBT.DBT_DEVICEARRIVAL : DBT.DBT_DEVICEREMOVECOMPLETE;
			aInfo.DriveLetter = aDriveLetter;
			aCommand.Execute(aInfo);
		}

		// --------------------------------------------------------------------
		// メッセージハンドラ
		// --------------------------------------------------------------------
		private static IntPtr WndProc(IntPtr oHWnd, Int32 oMsg, IntPtr oWParam, IntPtr oLParam, ref Boolean oHandled)
		{
			try
			{
				switch ((UInt32)oMsg)
				{
					case WindowsApi.WM_DEVICECHANGE:
						WmDeviceChange(oHWnd, oWParam, oLParam);
						oHandled = true;
						break;
					case WindowsApi.WM_SHNOTIFY:
						WmShNotify(oHWnd, oWParam, oLParam);
						oHandled = true;
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
