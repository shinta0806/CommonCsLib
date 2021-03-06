﻿// ============================================================================
// 
// Windows API を C# で使えるようにするための記述
// Copyright (C) 2015-2019 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2015/02/21 (Sat) | 作成開始。
//  1.00  | 2015/02/21 (Sat) | オリジナルバージョン。
//  1.01  | 2015/03/15 (Sun) |   DeleteFile() を追加。
//  1.02  | 2015/05/24 (Sun) |   GetCurrentThreadId() を追加。
//  1.03  | 2016/01/31 (Sun) |   GetRunningObjectTable() を追加。
//  1.04  | 2016/01/31 (Sun) |   CreateItemMoniker() を追加。
//  1.05  | 2016/02/06 (Sat) |   HRESULT を追加。
//  1.06  | 2016/06/10 (Fri) |   FAILED() を追加。
//  1.07  | 2016/06/11 (Sat) |   SUCCEEDED() を追加。
//  1.08  | 2017/06/28 (Wed) |   GetWindowText() を追加。
//  1.09  | 2018/03/23 (Fri) |   SHChangeNotifyRegister() を追加。
//  1.10  | 2018/08/10 (Fri) | IsIconic() を追加。
//  1.11  | 2018/08/10 (Fri) |   ShowWindowAsync() を追加。
//  1.12  | 2018/08/10 (Fri) |   SetForegroundWindow() を追加。
//  1.13  | 2019/02/09 (Sat) |   GetWindowLong() を追加。
//  1.14  | 2019/02/09 (Sat) |   SetWindowLong() を追加。
//  1.15  | 2019/10/08 (Tue) |   CopyMemory() を追加。
//  1.16  | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
//  1.17  | 2019/12/22 (Sun) |   null 許容参照型を無効化できるようにした。
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

#if !NULLABLE_DISABLED
#nullable enable
#endif

namespace Shinta
{
	// ====================================================================
	// public 列挙子
	// ====================================================================

	// --------------------------------------------------------------------
	// DBT
	// --------------------------------------------------------------------
	public enum DBT : Int32
	{
		DBT_CONFIGCHANGECANCELED = 0x0019,
		DBT_CONFIGCHANGED = 0x0018,
		DBT_CUSTOMEVENT = 0x8006,
		DBT_DEVICEARRIVAL = 0x8000,
		DBT_DEVICEQUERYREMOVE = 0x8001,
		DBT_DEVICEQUERYREMOVEFAILED = 0x8002,
		DBT_DEVICEREMOVECOMPLETE = 0x8004,
		DBT_DEVICEREMOVEPENDING = 0x8003,
		DBT_DEVICETYPESPECIFIC = 0x8005,
		DBT_DEVNODES_CHANGED = 0x0007,
		DBT_QUERYCHANGECONFIG = 0x0017,
		DBT_USERDEFINED = 0xFFFF,
	}

	// --------------------------------------------------------------------
	// DBT_DEVTYP
	// --------------------------------------------------------------------
	public enum DBT_DEVTYP : Int32
	{
		DBT_DEVTYP_DEVICEINTERFACE = 0x00000005,
		DBT_DEVTYP_HANDLE = 0x00000006,
		DBT_DEVTYP_OEM = 0x00000000,
		DBT_DEVTYP_PORT = 0x00000003,
		DBT_DEVTYP_VOLUME = 0x00000002,
	}

	// --------------------------------------------------------------------
	// GWL (GetWindowLong)
	// --------------------------------------------------------------------
	public enum GWL : Int32
	{
		GWL_WNDPROC = -4,
		GWL_HINSTANCE = -6,
		GWL_HWNDPARENT = -8,
		GWL_STYLE = -16,
		GWL_EXSTYLE = -20,
		GWL_USERDATA = -21,
		GWL_ID = -12,
	}

	// --------------------------------------------------------------------
	// HRESULT
	// --------------------------------------------------------------------
	public enum HRESULT : Int32
	{
		S_FALSE = 0x0001,
		S_OK = 0x0000,

		E_FAIL = unchecked((Int32)0x80004005),

		E_INVALIDARG = unchecked((Int32)0x80070057),
		E_OUTOFMEMORY = unchecked((Int32)0x8007000E),

		VFW_E_ALREADY_CONNECTED = unchecked((Int32)0x80040204),
		VFW_E_INVALID_DIRECTION = unchecked((Int32)0x80040208),
		VFW_E_NO_ACCEPTABLE_TYPES = unchecked((Int32)0x80040207),
		VFW_E_NO_CLOCK = unchecked((Int32)0x80040213),
		VFW_E_NOT_STOPPED = unchecked((Int32)0x80040224),
		VFW_E_TYPE_NOT_ACCEPTED = unchecked((Int32)0x8004022A),
	}

	// --------------------------------------------------------------------
	// SHCNRE
	// --------------------------------------------------------------------
	[Flags]
	public enum SHCNE
	{
		SHCNE_RENAMEITEM = 0x00000001,
		SHCNE_CREATE = 0x00000002,
		SHCNE_DELETE = 0x00000004,
		SHCNE_MKDIR = 0x00000008,
		SHCNE_RMDIR = 0x00000010,
		SHCNE_MEDIAINSERTED = 0x00000020,
		SHCNE_MEDIAREMOVED = 0x00000040,
		SHCNE_DRIVEREMOVED = 0x00000080,
		SHCNE_DRIVEADD = 0x00000100,
		SHCNE_NETSHARE = 0x00000200,
		SHCNE_NETUNSHARE = 0x00000400,
		SHCNE_ATTRIBUTES = 0x00000800,
		SHCNE_UPDATEDIR = 0x00001000,
		SHCNE_UPDATEITEM = 0x00002000,
		SHCNE_SERVERDISCONNECT = 0x00004000,
		SHCNE_UPDATEIMAGE = 0x00008000,
		SHCNE_DRIVEADDGUI = 0x00010000,
		SHCNE_RENAMEFOLDER = 0x00020000,
		SHCNE_FREESPACE = 0x00040000,
		SHCNE_EXTENDED_EVENT = 0x04000000,
		SHCNE_ASSOCCHANGED = 0x08000000,
		SHCNE_DISKEVENTS = 0x0002381F,
		SHCNE_GLOBALEVENTS = 0x0C0581E0,
		SHCNE_ALLEVENTS = 0x7FFFFFFF,
		SHCNE_INTERRUPT = unchecked((Int32)0x80000000),
	}

	// --------------------------------------------------------------------
	// SHCNRF
	// --------------------------------------------------------------------
	[Flags]
	public enum SHCNRF
	{
		SHCNRF_InterruptLevel = 0x0001,
		SHCNRF_ShellLevel = 0x0002,
		SHCNRF_RecursiveInterrupt = 0x1000,
		SHCNRF_NewDelivery = 0x8000,
	}

	// --------------------------------------------------------------------
	// ShowWindow()::nCmdShow
	// --------------------------------------------------------------------
	public enum ShowWindowCommands : Int32
	{
		SW_HIDE = 0,
		SW_SHOWNORMAL = 1,
		SW_NORMAL = 1,
		SW_SHOWMINIMIZED = 2,
		SW_SHOWMAXIMIZED = 3,
		SW_MAXIMIZE = 3,
		SW_SHOWNOACTIVATE = 4,
		SW_SHOW = 5,
		SW_MINIMIZE = 6,
		SW_SHOWMINNOACTIVE = 7,
		SW_SHOWNA = 8,
		SW_RESTORE = 9
	}

	// --------------------------------------------------------------------
	// WS (Window Style)
	// --------------------------------------------------------------------
	[Flags]
	public enum WS : UInt32
	{
		WS_BORDER = 0x800000,
		WS_CAPTION = 0xc00000,
		WS_CHILD = 0x40000000,
		WS_CLIPCHILDREN = 0x2000000,
		WS_CLIPSIBLINGS = 0x4000000,
		WS_DISABLED = 0x8000000,
		WS_DLGFRAME = 0x400000,
		WS_GROUP = 0x20000,
		WS_HSCROLL = 0x100000,
		WS_MAXIMIZE = 0x1000000,
		WS_MAXIMIZEBOX = 0x10000,
		WS_MINIMIZE = 0x20000000,
		WS_MINIMIZEBOX = 0x20000,
		WS_OVERLAPPED = 0x0,
		WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
		WS_POPUP = 0x80000000u,
		WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
		WS_SIZEFRAME = 0x40000,
		WS_SYSMENU = 0x80000,
		WS_TABSTOP = 0x10000,
		WS_VISIBLE = 0x10000000,
		WS_VSCROLL = 0x200000,
	}

	// ====================================================================
	// Windows API
	// ====================================================================

	public class WindowsApi
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		// --------------------------------------------------------------------
		// ウィンドウメッセージ
		// --------------------------------------------------------------------
		public const UInt32 WM_APP = 0x8000;
		public const UInt32 WM_CLOSE = 0x0010;
		public const UInt32 WM_COPYDATA = 0x4A;
		public const UInt32 WM_DEVICECHANGE = 0x0219;
		public const UInt32 WM_PAINT = 0x000F;
		public const UInt32 WM_QUIT = 0x0012;
		public const UInt32 WM_SHNOTIFY = 0x0401;

		// --------------------------------------------------------------------
		// ChangeWindowMessageFilter()
		// --------------------------------------------------------------------
		public const UInt32 MSGFLT_ADD = 1;

		// --------------------------------------------------------------------
		// IRunningObjectTable::Register()
		// --------------------------------------------------------------------
		public const Int32 ROTFLAGS_REGISTRATIONKEEPSALIVE = 0x1;
		public const Int32 ROTFLAGS_ALLOWANYCLIENT = 0x2;

		// --------------------------------------------------------------------
		// ファイル名
		// --------------------------------------------------------------------
		public const String FILE_NAME_KERNEL32_DLL = "kernel32.dll";
		public const String FILE_NAME_OLE32_DLL = "ole32.dll";
		public const String FILE_NAME_SHELL32_DLL = "shell32.dll";
		public const String FILE_NAME_USER32_DLL = "user32.dll";

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------
		public const UInt32 INFINITE = 0xFFFFFFFF;      // Infinite timeout

		// ====================================================================
		// public 構造体
		// ====================================================================

		// --------------------------------------------------------------------
		// COPYDATASTRUCT
		// --------------------------------------------------------------------
		[StructLayout(LayoutKind.Sequential)]
		public struct COPYDATASTRUCT_String
		{
			public IntPtr dwData;
			public Int32 cbData;
			[MarshalAs(UnmanagedType.LPWStr)]
			public String lpData;
		}

		// --------------------------------------------------------------------
		// DEV_BROADCAST_HDR
		// --------------------------------------------------------------------
		[StructLayout(LayoutKind.Sequential)]
		public struct DEV_BROADCAST_HDR
		{
			public UInt32 dbch_size;
			public UInt32 dbch_devicetype;
			public UInt32 dbch_reserved;
		}

		// --------------------------------------------------------------------
		// DEV_BROADCAST_VOLUME
		// --------------------------------------------------------------------
		[StructLayout(LayoutKind.Sequential)]
		public struct DEV_BROADCAST_VOLUME
		{
			public UInt32 dbcv_size;
			public UInt32 dbcv_devicetype;
			public UInt32 dbcv_reserved;
			public UInt32 dbcv_unitmask;
			public UInt16 dbcv_flags;
		}

		// --------------------------------------------------------------------
		// SHChangeNotifyEntry
		// --------------------------------------------------------------------
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct SHChangeNotifyEntry
		{
			public IntPtr pidl;
			[MarshalAs(UnmanagedType.Bool)]
			public Boolean fRecursive;
		}

		// --------------------------------------------------------------------
		// SHNOTIFYSTRUCT
		// --------------------------------------------------------------------
		[StructLayout(LayoutKind.Sequential)]
		public struct SHNOTIFYSTRUCT
		{
			public IntPtr dwItem1;
			public IntPtr dwItem2;
		}

		// ====================================================================
		// マクロ
		// ====================================================================

		// --------------------------------------------------------------------
		// FAILED (COM)
		// --------------------------------------------------------------------
		public static Boolean FAILED(HRESULT oHResult)
		{
			return oHResult < 0;
		}

		// --------------------------------------------------------------------
		// SUCCEEDED (COM)
		// --------------------------------------------------------------------
		public static Boolean SUCCEEDED(HRESULT oHResult)
		{
			return oHResult >= 0;
		}

		// ====================================================================
		// DLL インポート
		// ====================================================================

		// --------------------------------------------------------------------
		// BringWindowToTop
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		public static extern Boolean BringWindowToTop(IntPtr oWnd);

		// --------------------------------------------------------------------
		// ChangeWindowMessageFilter
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean ChangeWindowMessageFilter(UInt32 oMsg, UInt32 oFlag);

		// --------------------------------------------------------------------
		// CopyMemory
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL, SetLastError = false)]

		public static extern void CopyMemory(IntPtr oDest, IntPtr oSrc, UInt32 oCount);

		// --------------------------------------------------------------------
		// CreateItemMoniker
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_OLE32_DLL)]
		public static extern Int32 CreateItemMoniker([MarshalAs(UnmanagedType.LPWStr)] String oDelim,
				[MarshalAs(UnmanagedType.LPWStr)] String oItem, out IMoniker oPpmk);

		// --------------------------------------------------------------------
		// DeleteFile
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean DeleteFile(String oFileName);

		// --------------------------------------------------------------------
		// FindWindow
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		public static extern IntPtr FindWindow(String oClassName, String oWindowName);

		// --------------------------------------------------------------------
		// GetClassName
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern Int32 GetClassName(IntPtr oWnd, StringBuilder oClassName, int oMaxCount);

		// --------------------------------------------------------------------
		// GetCurrentThread
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL)]
		public static extern UInt32 GetCurrentThreadId();

		// --------------------------------------------------------------------
		// GetCurrentThreadId
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL)]
		public static extern IntPtr GetCurrentThread();

		// --------------------------------------------------------------------
		// GetForegroundWindow
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		public static extern IntPtr GetForegroundWindow();

		// --------------------------------------------------------------------
		// GetRunningObjectTable
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_OLE32_DLL)]
		public static extern Int32 GetRunningObjectTable(UInt32 oReserved, out IRunningObjectTable oPprot);

		// --------------------------------------------------------------------
		// GetWindowLong
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		public static extern IntPtr GetWindowLong(IntPtr oHWnd, Int32 oIndex);

		// --------------------------------------------------------------------
		// GetWindowText
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern Int32 GetWindowText(IntPtr oHWnd, StringBuilder oString, Int32 oMaxCount);

		// --------------------------------------------------------------------
		// GetWindowTextLength
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern Int32 GetWindowTextLength(IntPtr oHWnd);

		// --------------------------------------------------------------------
		// IsIconic
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean IsIconic(IntPtr oHWnd);

		// --------------------------------------------------------------------
		// PostMessage
		// 非同期通信
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean PostMessage(IntPtr oHWnd, UInt32 oMsg, IntPtr oWParam, IntPtr oLParam);
		//[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		//[return: MarshalAs(UnmanagedType.Bool)]
		//public static extern Boolean PostMessage(IntPtr oHWnd, UInt32 oMsg, IntPtr oWParam, ref COPYDATASTRUCT_String oLParam);

		// --------------------------------------------------------------------
		// ReplyMessage
		// MSDN（英語版含む）には拡張エラーに関する記述が無いので、恐らく SetLastError = false でいいのかなとは思う
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean ReplyMessage(IntPtr oLResult);

		// --------------------------------------------------------------------
		// SendMessage
		// 同期通信
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr oHWnd, UInt32 oMsg, IntPtr oWParam, IntPtr oLParam);
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr oHWnd, UInt32 oMsg, IntPtr oWParam, ref COPYDATASTRUCT_String oLParam);

		// --------------------------------------------------------------------
		// SetForegroundWindow
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean SetForegroundWindow(IntPtr oWnd);

		// --------------------------------------------------------------------
		// SetThreadDescription：うまく動かない？
		// Win10 Ver 1607 以降
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL, CharSet = CharSet.Auto)]
		public static extern Int32 SetThreadDescription(IntPtr hThread, StringBuilder description);

		// --------------------------------------------------------------------
		// SetWindowLong
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		public static extern IntPtr SetWindowLong(IntPtr oHWnd, Int32 oIndex, IntPtr oNewLong);

		// --------------------------------------------------------------------
		// SHChangeNotifyRegister
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_SHELL32_DLL, SetLastError = true, EntryPoint = "#2", CharSet = CharSet.Auto)]
		public static extern UInt32 SHChangeNotifyRegister(IntPtr oHWnd, SHCNRF oSources, SHCNE oEvents, UInt32 oMsg, Int32 oEntries, ref SHChangeNotifyEntry oShCne);

		// --------------------------------------------------------------------
		// SHGetPathFromIDList
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_SHELL32_DLL, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean SHGetPathFromIDList(IntPtr oIdl, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder oPath);

		// --------------------------------------------------------------------
		// ShowWindowAsync
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern Boolean ShowWindowAsync(IntPtr oHWnd, Int32 oCmdShow);
	}
	// public class WindowsApi ___END___

}
// namespace Shinta ___END___

