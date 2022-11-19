// ============================================================================
// 
// Serilog 利用関数
// Copyright (C) 2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Serilog.Sinks.File
//   Serilog.Sinks.Debug
//   Serilog.Enrichers.Process
//   Serilog.Enrichers.Thread
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/11/03 (Thu) | 作成開始。
//  1.00  | 2022/11/03 (Thu) | ファーストバージョン。
// (1.01) | 2022/11/05 (Sat) |   CreateLogger() の path を必須にした。
// ============================================================================

using Serilog;

namespace Shinta;

internal class SerilogUtils
{
	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// ロガー生成
	/// </summary>
	/// <param name="flleSizeLimit">1 つのログファイルの上限サイズ [Bytes]</param>
	/// <param name="generations">保存する世代（現行世代を含む）</param>
	/// <param name="path">ログファイルのパス</param>
	public static void CreateLogger(Int32 flleSizeLimit, Int32 generations, String path)
	{
		Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Information()
#if DEBUG
				.MinimumLevel.Debug()
				.WriteTo.Debug()
#endif
				.Enrich.WithProcessId()
				.Enrich.WithThreadId()
				.WriteTo.File(path, rollOnFileSizeLimit: true, fileSizeLimitBytes: flleSizeLimit, retainedFileCountLimit: generations,
				outputTemplate: "{Timestamp:yyyy/MM/dd HH:mm:ss.fff}\t{ProcessId}/M{ThreadId}\t{Level:u3}\t{Message:lj}{NewLine}{Exception}")
				.CreateLogger();
	}
}
