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
//   PInvoke.User32, PInvoke.SHCore
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2023/02/11 (Sat) | 作成開始。
//  1.00  | 2023/02/11 (Sat) | オリジナルバージョン。
// ============================================================================

using PInvoke;

using System.Diagnostics;

using Windows.Foundation;

namespace Shinta;

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
	public List<Rect> GetRawMonitorRects()
	{
		GetMonitorRectsCore();
		Debug.Assert(_monitorRawRects != null, "GetRawMonitorRects() _monitorRawRects null");
		return _monitorRawRects;
	}

	// --------------------------------------------------------------------
	// マルチモニター環境で各モニターの領域を取得（各ディスプレイの拡大率に合わせてスケーリング）
	// --------------------------------------------------------------------
	public List<Rect> GetScaledMonitorRects()
	{
		GetMonitorRectsCore();
		Debug.Assert(_monitorRawRects != null, "GetScaledMonitorRects() _monitorRawRects null");
		Debug.Assert(_monitorHandles != null, "GetScaledMonitorRects() _monitorHandles null");

		for (Int32 i = 0; i < _monitorRawRects.Count; i++)
		{
			if (WindowsApi.FAILED(SHCore.GetDpiForMonitor(_monitorHandles[i], MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out Int32 dpiX, out Int32 dpiY)))
			{
				continue;
			}

			Double scaleX = Common.DEFAULT_DPI / dpiX;
			Double scaleY = Common.DEFAULT_DPI / dpiY;
			Rect rect = new(_monitorRawRects[i].Left * scaleX, _monitorRawRects[i].Top * scaleY, _monitorRawRects[i].Width * scaleX, _monitorRawRects[i].Height * scaleY);
			_monitorRawRects[i] = rect;
		}
		return _monitorRawRects;
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	// ディスプレイ領域格納用
	private List<Rect>? _monitorRawRects;

	// ディスプレイハンドル格納用
	private List<IntPtr>? _monitorHandles;

	// ====================================================================
	// private 関数
	// ====================================================================

	// --------------------------------------------------------------------
	// マルチモニター環境で各モニターの領域を取得（コールバック）
	// --------------------------------------------------------------------
	private Boolean GetMonitorRectsCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref PInvoke.RECT lprcMonitor, IntPtr dwData)
	{
		Debug.Assert(_monitorRawRects != null, "GetMonitorRectsCallback() _monitorRawRects null");
		Debug.Assert(_monitorHandles != null, "GetMonitorRectsCallback() _monitorHandles null");
		Rect rect = new(lprcMonitor.left, lprcMonitor.top, lprcMonitor.right - lprcMonitor.left, lprcMonitor.bottom - lprcMonitor.top);
		_monitorRawRects.Add(rect);
		_monitorHandles.Add(hMonitor);
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
