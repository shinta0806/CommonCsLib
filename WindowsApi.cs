// ============================================================================
// 
// Windows API を C# で使えるようにするための記述
// Copyright (C) 2015-2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージで使用可能になるものは除外
//   PInvoke.*
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2015/02/21 (Sat) | 作成開始。
//  1.00  | 2015/02/21 (Sat) | オリジナルバージョン。
// (1.01) | 2015/03/15 (Sun) |   DeleteFile() を追加。
// (1.02) | 2015/05/24 (Sun) |   GetCurrentThreadId() を追加。
// (1.03) | 2016/01/31 (Sun) |   GetRunningObjectTable() を追加。
// (1.04) | 2016/01/31 (Sun) |   CreateItemMoniker() を追加。
// (1.05) | 2016/02/06 (Sat) |   HRESULT を追加。
// (1.06) | 2016/06/10 (Fri) |   FAILED() を追加。
// (1.07) | 2016/06/11 (Sat) |   SUCCEEDED() を追加。
// (1.08) | 2017/06/28 (Wed) |   GetWindowText() を追加。
// (1.09) | 2018/03/23 (Fri) |   SHChangeNotifyRegister() を追加。
//  1.10  | 2018/08/10 (Fri) | マイナーバージョンアップの積み重ね。
//                               IsIconic() を追加。
// (1.11) | 2018/08/10 (Fri) |   ShowWindowAsync() を追加。
// (1.12) | 2018/08/10 (Fri) |   SetForegroundWindow() を追加。
// (1.13) | 2019/02/09 (Sat) |   GetWindowLong() を追加。
// (1.14) | 2019/02/09 (Sat) |   SetWindowLong() を追加。
// (1.15) | 2019/10/08 (Tue) |   CopyMemory() を追加。
// (1.16) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.17) | 2019/12/22 (Sun) |   null 許容参照型を無効化できるようにした。
// (1.18) | 2021/03/06 (Sat) |   DllImport 属性の関数のアクセスレベルを public から internal に変更。
// (1.19) | 2021/03/06 (Sat) |   文字列を引数とする関数に CharSet.Unicode を指定。
//  1.20  | 2021/03/27 (Sat) | マイナーバージョンアップの積み重ね。
//                               GetVolumeInformation() を追加。
// (1.21) | 2022/02/02 (Wed) |   EnumDisplayMonitors() を追加。
// (1.22) | 2022/02/05 (Sat) |   GetWindowRect() を追加。
// (1.23) | 2022/02/06 (Sun) |   GetDpiForMonitor() を追加。
// (1.24) | 2022/02/06 (Sun) |   MoveWindow() を追加。
// (1.25) | 2022/02/26 (Sat) |   WS_EX を追加。
// (1.26) | 2022/02/26 (Sat) |   WM を列挙子に変更。
//  2.00  | 2022/12/08 (Thu) | PInvoke パッケージと重複するものを除外。
// (2.01) | 2022/12/08 (Thu) |   SetWindowSubclass() を追加。
// (2.02) | 2022/12/08 (Thu) |   DefSubclassProc() を追加。
// ============================================================================

using PInvoke;

using System.Runtime.InteropServices;

namespace Shinta
{
	// ====================================================================
	// public 列挙子
	// ====================================================================

#if false
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
	// File System Flags
	// --------------------------------------------------------------------
	[Flags]
	public enum FSF : UInt32
	{
		FILE_CASE_PRESERVED_NAMES = 2,
		FILE_CASE_SENSITIVE_SEARCH = 1,
		FILE_DAX_VOLUME = 0x20000000,
		FILE_FILE_COMPRESSION = 0x10,
		FILE_NAMED_STREAMS = 0x40000,
		FILE_PERSISTENT_ACLS = 8,
		FILE_READ_ONLY_VOLUME = 0x80000,
		FILE_SEQUENTIAL_WRITE_ONCE = 0x100000,
		FILE_SUPPORTS_ENCRYPTION = 0x20000,
		FILE_SUPPORTS_EXTENDED_ATTRIBUTES = 0x00800000,
		FILE_SUPPORTS_HARD_LINKS = 0x00400000,
		FILE_SUPPORTS_OBJECT_IDS = 0x10000,
		FILE_SUPPORTS_OPEN_BY_FILE_ID = 0x01000000,
		FILE_SUPPORTS_REPARSE_POINTS = 0x80,
		FILE_SUPPORTS_SPARSE_FILES = 0x40,
		FILE_SUPPORTS_TRANSACTIONS = 0x200000,
		FILE_SUPPORTS_USN_JOURNAL = 0x02000000,
		FILE_UNICODE_ON_DISK = 4,
		FILE_VOLUME_IS_COMPRESSED = 0x8000,
		FILE_VOLUME_QUOTAS = 0x20,
		FILE_SUPPORTS_BLOCK_REFCOUNTING = 0x08000000,
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
	// MONITOR_DPI_TYPE
	// --------------------------------------------------------------------
	public enum MONITOR_DPI_TYPE
	{
		MDT_EFFECTIVE_DPI = 0,
		MDT_ANGULAR_DPI = 1,
		MDT_RAW_DPI = 2,
		MDT_DEFAULT
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
		SW_NORMAL = SW_SHOWNORMAL,
		SW_SHOWMINIMIZED = 2,
		SW_SHOWMAXIMIZED = 3,
		SW_MAXIMIZE = SW_SHOWMAXIMIZED,
		SW_SHOWNOACTIVATE = 4,
		SW_SHOW = 5,
		SW_MINIMIZE = 6,
		SW_SHOWMINNOACTIVE = 7,
		SW_SHOWNA = 8,
		SW_RESTORE = 9
	}

	// --------------------------------------------------------------------
	// ウィンドウメッセージ
	// --------------------------------------------------------------------
	public enum WM : UInt32
	{
		WM_APP = 0x8000,
		WM_CLOSE = 0x0010,
		WM_COPYDATA = 0x4A,
		WM_DEVICECHANGE = 0x0219,
		WM_PAINT = 0x000F,
		WM_QUIT = 0x0012,
		WM_SHNOTIFY = 0x0401,
		WM_SYSCOMMAND = 0x0112,
	}

	// --------------------------------------------------------------------
	// WM_SYSCOMMAND wParam
	// --------------------------------------------------------------------
	public enum WM_SYSCOMMAND_WPARAM : UInt32
	{
		SC_CLOSE = 0xF060,
		SC_CONTEXTHELP = 0xF180,
	}

	// --------------------------------------------------------------------
	// WS (Window Style)
	// https://docs.microsoft.com/en-us/windows/win32/winmsg/window-styles
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
		WS_MINIMIZEBOX = WS_GROUP,
		WS_OVERLAPPED = 0x0,
		WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
		WS_POPUP = 0x80000000u,
		WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
		WS_SIZEFRAME = 0x40000,
		WS_SYSMENU = 0x80000,
		WS_TABSTOP = WS_MAXIMIZEBOX,
		WS_VISIBLE = 0x10000000,
		WS_VSCROLL = 0x200000,
	}

	// --------------------------------------------------------------------
	// WS_EX (Extended Window Styles)
	// https://docs.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles
	// --------------------------------------------------------------------
	[Flags]
	public enum WS_EX : UInt32
	{
		WS_EX_ACCEPTFILES = 0x00000010,
		WS_EX_APPWINDOW = 0x00040000,
		WS_EX_CLIENTEDGE = 0x00000200,
		WS_EX_COMPOSITED = 0x02000000,
		WS_EX_CONTEXTHELP = 0x00000400,
		WS_EX_CONTROLPARENT = 0x00010000,
		WS_EX_DLGMODALFRAME = 0x00000001,
		WS_EX_LAYERED = 0x00080000,
		WS_EX_LAYOUTRTL = 0x00400000,
		WS_EX_LEFT = 0x00000000,
		WS_EX_LEFTSCROLLBAR = 0x00004000,
		WS_EX_LTRREADING = WS_EX_LEFT,
		WS_EX_MDICHILD = 0x00000040,
		WS_EX_NOACTIVATE = 0x08000000,
		WS_EX_NOINHERITLAYOUT = 0x00100000,
		WS_EX_NOPARENTNOTIFY = 0x00000004,
		WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
		WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE),
		WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST),
		WS_EX_RIGHT = 0x00001000,
		WS_EX_RIGHTSCROLLBAR = WS_EX_LEFT,
		WS_EX_RTLREADING = 0x00002000,
		WS_EX_STATICEDGE = 0x00020000,
		WS_EX_TOOLWINDOW = 0x00000080,
		WS_EX_TOPMOST = 0x00000008,
		WS_EX_TRANSPARENT = 0x00000020,
		WS_EX_WINDOWEDGE = 0x00000100,
	}
#endif

	// ====================================================================
	// Windows API
	// ====================================================================

	public class WindowsApi
	{
		// ====================================================================
		// デリゲート
		// ====================================================================

#if false
		internal delegate Boolean EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
#endif
		internal delegate IntPtr SubclassProc(IntPtr hWnd, User32.WindowMessage msg, IntPtr wPalam, IntPtr lParam, IntPtr idSubclass, IntPtr refData);

		// ====================================================================
		// public 定数
		// ====================================================================

#if false
		// --------------------------------------------------------------------
		// ChangeWindowMessageFilter()
		// --------------------------------------------------------------------
		public const UInt32 MSGFLT_ADD = 1;

		// --------------------------------------------------------------------
		// IRunningObjectTable::Register()
		// --------------------------------------------------------------------
		public const Int32 ROTFLAGS_REGISTRATIONKEEPSALIVE = 0x1;
		public const Int32 ROTFLAGS_ALLOWANYCLIENT = 0x2;
#endif

		// --------------------------------------------------------------------
		// ファイル名
		// --------------------------------------------------------------------
		public const String FILE_NAME_COMCTL32_DLL = "Comctl32.dll";
#if false
		public const String FILE_NAME_KERNEL32_DLL = "kernel32.dll";
		public const String FILE_NAME_OLE32_DLL = "ole32.dll";
		public const String FILE_NAME_SHCORE_DLL = "SHCore.dll";
		public const String FILE_NAME_SHELL32_DLL = "shell32.dll";
#endif
		public const String FILE_NAME_USER32_DLL = "user32.dll";

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------
#if false
		public const UInt32 INFINITE = 0xFFFFFFFF;      // Infinite timeout
#endif

		// ====================================================================
		// public 構造体
		// ====================================================================

#if false
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
		// RECT
		// --------------------------------------------------------------------
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public Int32 left;
			public Int32 top;
			public Int32 right;
			public Int32 bottom;
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
#endif

		// ====================================================================
		// マクロ
		// ====================================================================

#if false
		// --------------------------------------------------------------------
		// FAILED (COM)
		// --------------------------------------------------------------------
		public static Boolean FAILED(HRESULT hResult)
		{
			return hResult < 0;
		}

		// --------------------------------------------------------------------
		// SUCCEEDED (COM)
		// --------------------------------------------------------------------
		public static Boolean SUCCEEDED(HRESULT hResult)
		{
			return hResult >= 0;
		}
#endif

		// ====================================================================
		// DLL インポート
		// public にすると CA1401 となるため internal にする
		// ====================================================================

#if false
		// --------------------------------------------------------------------
		// BringWindowToTop
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		internal static extern Boolean BringWindowToTop(IntPtr windowHandle);
#endif
#if false
		// --------------------------------------------------------------------
		// ChangeWindowMessageFilter
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean ChangeWindowMessageFilter(UInt32 oMsg, UInt32 oFlag);

		// --------------------------------------------------------------------
		// CopyMemory
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL, SetLastError = false)]

		internal static extern void CopyMemory(IntPtr oDest, IntPtr oSrc, UInt32 oCount);

		// --------------------------------------------------------------------
		// CreateItemMoniker
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_OLE32_DLL)]
		internal static extern Int32 CreateItemMoniker([MarshalAs(UnmanagedType.LPWStr)] String oDelim,
				[MarshalAs(UnmanagedType.LPWStr)] String oItem, out IMoniker oPpmk);
#endif

		[DllImport(FILE_NAME_COMCTL32_DLL)]
		internal static extern IntPtr DefSubclassProc(IntPtr hwnd, User32.WindowMessage msg, IntPtr wPalam, IntPtr lParam);

#if false
		// --------------------------------------------------------------------
		// DeleteFile
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL, SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean DeleteFile(String fileName);

		// --------------------------------------------------------------------
		// EnumDisplayMonitors
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean EnumDisplayMonitors(IntPtr hdc, IntPtr clip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

		// --------------------------------------------------------------------
		// FindWindow
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern IntPtr FindWindow(String className, String windowName);

		// --------------------------------------------------------------------
		// GetClassName
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern Int32 GetClassName(IntPtr wnd, StringBuilder className, int maxCount);

		// --------------------------------------------------------------------
		// GetCurrentThread
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL)]
		internal static extern UInt32 GetCurrentThreadId();

		// --------------------------------------------------------------------
		// GetCurrentThreadId
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL)]
		internal static extern IntPtr GetCurrentThread();

		// --------------------------------------------------------------------
		// GetDpiForMonitor
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_SHCORE_DLL)]
		internal static extern HRESULT GetDpiForMonitor(IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out UInt32 dpiX, out UInt32 dpiY);

		// --------------------------------------------------------------------
		// GetForegroundWindow
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		internal static extern IntPtr GetForegroundWindow();

		// --------------------------------------------------------------------
		// GetRunningObjectTable
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_OLE32_DLL)]
		internal static extern Int32 GetRunningObjectTable(UInt32 oReserved, out IRunningObjectTable oPprot);

		// --------------------------------------------------------------------
		// GetVolumeInformation
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL, SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean GetVolumeInformation(String rootPathName, StringBuilder? volumeNameBuffer, Int32 volumeNameSize,
				out UInt32 volumeSerialNumber, out UInt32 maximumComponentLength, out FSF fileSystemFlags, StringBuilder? fileSystemNameBuffer, Int32 fileSystemNameSize);

		// --------------------------------------------------------------------
		// GetWindowLong
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		internal static extern IntPtr GetWindowLong(IntPtr oHWnd, Int32 oIndex);

		// --------------------------------------------------------------------
		// GetWindowRect
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean GetWindowRect(IntPtr hwnd, out RECT lpRect);

		// --------------------------------------------------------------------
		// GetWindowText
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern Int32 GetWindowText(IntPtr wnd, StringBuilder str, Int32 maxCount);

		// --------------------------------------------------------------------
		// GetWindowTextLength
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern Int32 GetWindowTextLength(IntPtr oHWnd);

		// --------------------------------------------------------------------
		// IsIconic
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean IsIconic(IntPtr oHWnd);

		// --------------------------------------------------------------------
		// IsIconic
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean MoveWindow(IntPtr hWnd, Int32 x, Int32 y, Int32 width, Int32 height, Boolean repaint);

		// --------------------------------------------------------------------
		// PostMessage
		// 非同期通信
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean PostMessage(IntPtr oHWnd, UInt32 oMsg, IntPtr oWParam, IntPtr oLParam);

		// --------------------------------------------------------------------
		// ReplyMessage
		// MSDN（英語版含む）には拡張エラーに関する記述が無いので、恐らく SetLastError = false でいいのかなとは思う
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean ReplyMessage(IntPtr oLResult);

		// --------------------------------------------------------------------
		// SendMessage
		// 同期通信
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		internal static extern IntPtr SendMessage(IntPtr oHWnd, UInt32 oMsg, IntPtr oWParam, IntPtr oLParam);
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		internal static extern IntPtr SendMessage(IntPtr oHWnd, UInt32 oMsg, IntPtr oWParam, ref COPYDATASTRUCT_String oLParam);

		// --------------------------------------------------------------------
		// SetForegroundWindow
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean SetForegroundWindow(IntPtr oWnd);

		// --------------------------------------------------------------------
		// SetThreadDescription：うまく動かない？
		// Win10 Ver 1607 以降
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL, CharSet = CharSet.Unicode)]
		internal static extern Int32 SetThreadDescription(IntPtr thread, StringBuilder description);

		// --------------------------------------------------------------------
		// SetWindowLong
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL)]
		internal static extern IntPtr SetWindowLong(IntPtr oHWnd, Int32 oIndex, IntPtr oNewLong);
#endif

		/// <summary>
		/// SetWindowSubclass
		/// </summary>
		/// <param name="hwnd"></param>
		/// <param name="subclassProc"></param>
		/// <param name="idSubclass"></param>
		/// <param name="refData"></param>
		/// <returns></returns>
		[DllImport(FILE_NAME_COMCTL32_DLL)]
		internal static extern Boolean SetWindowSubclass(IntPtr hWnd, SubclassProc subclassProc, IntPtr idSubclass, IntPtr refData);

#if false
		// --------------------------------------------------------------------
		// SHChangeNotifyRegister
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_SHELL32_DLL, SetLastError = true, EntryPoint = "#2", CharSet = CharSet.Auto)]
		internal static extern UInt32 SHChangeNotifyRegister(IntPtr oHWnd, SHCNRF oSources, SHCNE oEvents, UInt32 oMsg, Int32 oEntries, ref SHChangeNotifyEntry oShCne);

		// --------------------------------------------------------------------
		// SHGetPathFromIDList
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_SHELL32_DLL, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean SHGetPathFromIDList(IntPtr idList, StringBuilder path);

		// --------------------------------------------------------------------
		// ShowWindowAsync
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern Boolean ShowWindowAsync(IntPtr oHWnd, Int32 oCmdShow);
#endif
	}
}

