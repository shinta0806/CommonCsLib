// ============================================================================
// 
// Serilog 利用関数
// Copyright (C) 2022-2024 by SHINTA
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
//  1.10  | 2023/04/02 (Sun) | LogException() を作成。
// (1.11) | 2023/07/17 (Mon) |   ログアクセスを共有にした。
// (1.12) | 2024/09/28 (Sat) |   LogException() を改善。
// ============================================================================

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
			.WriteTo.File(path, rollOnFileSizeLimit: true, fileSizeLimitBytes: flleSizeLimit, retainedFileCountLimit: generations, shared: true,
			outputTemplate: "{Timestamp:yyyy/MM/dd HH:mm:ss.fff}\t{ProcessId}/M{ThreadId}\t{Level:u3}\t{Message:lj}{NewLine}{Exception}")
			.CreateLogger();
	}

	/// <summary>
	/// 例外をログする
	/// </summary>
	/// <param name="caption"></param>
	/// <param name="ex"></param>
	public static void LogException(String caption, Exception ex)
	{
		Log.Error(Common.ExceptionMessage(caption, ex));
		LogStackTrace(ex);
	}

	/// <summary>
	/// スタックトレースをログする
	/// </summary>
	/// <param name="ex"></param>
	public static void LogStackTrace(Exception ex)
	{
		Log.Information("スタックトレース：\n" + ex.StackTrace);
	}
}
