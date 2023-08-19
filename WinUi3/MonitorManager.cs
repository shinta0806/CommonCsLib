// ============================================================================
// 
// マルチディスプレイを管理するクラス
// Copyright (C) 2023 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ・スレッドセーフではない。
//     複数スレッドで使用したい場合はスレッドごとにインスタンスを作成する必要がある。
// ・マニフェストで高 DPI 対応宣言がされている前提で実装している。
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   CsWin32
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2023/02/11 (Sat) | 作成開始。
//  1.00  | 2023/02/11 (Sat) | オリジナルバージョン。
//  1.10  | 2023/08/19 (Sat) | CsWin32 導入に伴う仕様変更。
// ============================================================================

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace Shinta.WinUi3;

internal class MonitorManager
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	// --------------------------------------------------------------------
	// メインコンストラクター
	// --------------------------------------------------------------------
	public MonitorManager()
	{
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	// --------------------------------------------------------------------
	// マルチモニター環境で各モニターの領域を取得（API 値のままスケーリングしない）
	// --------------------------------------------------------------------
	public List<RECT> GetRawMonitorRects()
	{
		GetMonitorRectsCore();
		Debug.Assert(_monitorRawRects != null, "GetRawMonitorRects() _monitorRawRects null");
		return _monitorRawRects;
	}

	// --------------------------------------------------------------------
	// マルチモニター環境で各モニターの領域を取得（各ディスプレイの拡大率に合わせてスケーリング）
	// --------------------------------------------------------------------
	public List<RECT> GetScaledMonitorRects()
	{
		GetMonitorRectsCore();
		Debug.Assert(_monitorRawRects != null, "GetScaledMonitorRects() _monitorRawRects null");
		Debug.Assert(_monitorHandles != null, "GetScaledMonitorRects() _monitorHandles null");

		for (Int32 i = 0; i < _monitorRawRects.Count; i++)
		{
			if (PInvoke.GetDpiForMonitor(_monitorHandles[i], MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out UInt32 dpiX, out UInt32 dpiY).Failed)
			{
				continue;
			}

			Double scaleX = Common.DEFAULT_DPI / dpiX;
			Double scaleY = Common.DEFAULT_DPI / dpiY;
			RECT rect = new((Int32)(_monitorRawRects[i].left * scaleX), (Int32)(_monitorRawRects[i].top * scaleY),
					(Int32)(_monitorRawRects[i].right * scaleX), (Int32)(_monitorRawRects[i].bottom * scaleY));
			_monitorRawRects[i] = rect;
		}
		return _monitorRawRects;
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	// ディスプレイ領域格納用
	private List<RECT>? _monitorRawRects;

	// ディスプレイハンドル格納用
	private List<HMONITOR>? _monitorHandles;

	// ====================================================================
	// private 関数
	// ====================================================================

	// --------------------------------------------------------------------
	// マルチモニター環境で各モニターの領域を取得（コールバック）
	// --------------------------------------------------------------------
	private Boolean GetMonitorRectsCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
	{
		Debug.Assert(_monitorRawRects != null, "GetMonitorRectsCallback() _monitorRawRects null");
		Debug.Assert(_monitorHandles != null, "GetMonitorRectsCallback() _monitorHandles null");
		_monitorRawRects.Add(lprcMonitor);
		_monitorHandles.Add((HMONITOR)hMonitor);
		return true;
	}

	// --------------------------------------------------------------------
	// マルチモニター環境で各モニターの領域を取得（基本部分）
	// --------------------------------------------------------------------
	private void GetMonitorRectsCore()
	{
		_monitorRawRects = new();
		_monitorHandles = new();
		WindowsApi.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, GetMonitorRectsCallback, IntPtr.Zero);
		Debug.Assert(_monitorRawRects.Count == _monitorHandles.Count, "GetMonitorRectsCore() bad list counts");
	}
}
