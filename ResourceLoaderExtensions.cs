﻿// ============================================================================
// 
// 言語リソースからの文字列取得
// Copyright (C) 2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Serilog.Sinks.File
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
//  
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/11/19 (Sat) | 作成開始。
//  1.00  | 2022/11/19 (Sat) | ファーストバージョン。
// (1.01) | 2022/11/19 (Sat) |   Serilog の使用を前提にした。
// ============================================================================

using Microsoft.Windows.ApplicationModel.Resources;

using Serilog;

namespace Shinta;

internal static class ResourceLoaderExtensions
{
	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// 文字列取得
	/// </summary>
	/// <param name="resourceKey"></param>
	/// <returns></returns>
	public static String ToLocalized(this String resourceKey)
	{
		try
		{
			return _resourceLoader.GetString(resourceKey);
		}
		catch (Exception)
		{
			Log.Error("言語リソースが見つかりません：" + resourceKey);
			return resourceKey;
		}
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// リソース
	/// </summary>
	private static readonly ResourceLoader _resourceLoader = new();
}
