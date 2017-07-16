// ============================================================================
// 
// Windows API を C# で使えるようにするための記述
// Copyright (C) 2015-2017 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2015/02/21 (Sat) | 作成開始。
//  1.00  | 2015/02/21 (Sat) | オリジナルバージョン。
//  1.01  | 2015/03/15 (Sun) | DeleteFile() を追加。
//  1.02  | 2015/05/24 (Sun) | GetCurrentThreadId() を追加。
//  1.03  | 2016/01/31 (Sun) | GetRunningObjectTable() を追加。
//  1.04  | 2016/01/31 (Sun) | CreateItemMoniker() を追加。
//  1.05  | 2016/02/06 (Sat) | HRESULT を追加。
//  1.06  | 2016/06/10 (Fri) | FAILED() を追加。
//  1.07  | 2016/06/11 (Sat) | SUCCEEDED() を追加。
//  1.08  | 2017/06/28 (Wed) | GetWindowText() を追加。
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Shinta
{
	// ====================================================================
	// public 列挙子
	// ====================================================================

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
		public const UInt32 WM_QUIT = 0x0012;

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
		public struct COPYDATASTRUCT_String
		{
			public IntPtr dwData;
			public Int32 cbData;
			[MarshalAs(UnmanagedType.LPWStr)]
			public String lpData;
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
		// GetCurrentThreadId
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_KERNEL32_DLL)]
		public static extern UInt32 GetCurrentThreadId();

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
		// GetWindowText
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern Int32 GetWindowText(IntPtr oWnd, StringBuilder oString, Int32 oMaxCount);

		// --------------------------------------------------------------------
		// GetWindowTextLength
		// --------------------------------------------------------------------
		[DllImport(FILE_NAME_USER32_DLL, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern Int32 GetWindowTextLength(IntPtr oWnd);

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

	}


}

