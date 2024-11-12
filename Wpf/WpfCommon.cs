// ============================================================================
// 
// WPF 環境で使用する共通関数群
// Copyright (C) 2021-2024 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
//  
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2021/03/28 (Sun) | CommonWindows 作成開始。
//  1.00  | 2024/11/12 (Tue) | CommonWindows より独立。
// ============================================================================

using System.Windows;

namespace Shinta.Wpf;

internal class WpfCommon
{
	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// ウィンドウがスクリーンから完全にはみ出している場合はスクリーン内に移動する
	/// 必要であれば WPF 用のライブラリに移す
	/// </summary>
	/// <param name="windowRect"></param>
	/// <returns></returns>
	public static Rect AdjustWindowRect(Rect windowRect)
	{
		Rect screenRect = new(0, 0, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

		// ウィンドウとスクリーンがぴったりの場合は移動不要
		if (screenRect == windowRect)
		{
			return windowRect;
		}

		// ウィンドウとスクリーンが一部重なっている場合は移動不要
		// ※ウィンドウがスクリーンより完全に大きい場合を除く
		Rect intersect = Rect.Intersect(screenRect, windowRect);
		if (!intersect.IsEmpty && intersect != screenRect)
		{
			return windowRect;
		}

		// 移動の必要がある
		return new Rect(0, 0, windowRect.Width, windowRect.Height);
	}
}