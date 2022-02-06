// ============================================================================
// 
// マルチディスプレイを管理するクラス
// Copyright (C) 2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ・スレッドセーフではない。
//     複数スレッドで使用したい場合はスレッドごとにインスタンスを作成する必要がある。
// ・マニフェストで高 DPI 対応宣言がされている前提で実装している。
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/02/06 (Sun) | 作成開始。
//  1.00  | 2022/02/06 (Sun) | オリジナルバージョン。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Shinta
{
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
		// マルチモニター環境で各モニターの領域を取得
		// --------------------------------------------------------------------
		public List<Rect> GetMonitorRects()
		{
			_monitorRawRects = new();
			_monitorHandles = new();
			WindowsApi.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, GetMonitorRectsCallback, IntPtr.Zero);
			Debug.Assert(_monitorRawRects.Count == _monitorHandles.Count, "GetMonitorRects() bad list counts");

			// High DPI 換算
			for (Int32 i = 0; i < _monitorRawRects.Count; i++)
			{
				WindowsApi.GetDpiForMonitor(_monitorHandles[i], MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out UInt32 dpiX, out UInt32 dpiY);
#if DEBUGz
				MessageBox.Show("GetMonitorRects() " + i + ": " + dpiX + ", " + dpiY);
#endif
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
		private Boolean GetMonitorRectsCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref WindowsApi.RECT lprcMonitor, IntPtr dwData)
		{
			Debug.Assert(_monitorRawRects != null, "GetMonitorRectsCallback() _monitorRawRects null");
			Debug.Assert(_monitorHandles != null, "GetMonitorRectsCallback() _monitorHandles null");
			Rect rect = new Rect(lprcMonitor.left, lprcMonitor.top, lprcMonitor.right - lprcMonitor.left, lprcMonitor.bottom - lprcMonitor.top);
			_monitorRawRects.Add(rect);
			_monitorHandles.Add(hMonitor);
			return true;
		}
	}
}
