// ============================================================================
// 
// メッセージおよび付加情報を CSV 形式でログファイルに保存
// Copyright (C) 2014-2023 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ログの文字コードは UTF-8
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Microsoft.Windows.CsWin32
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/11/30 (Sun) | 作成開始。
//  1.00  | 2014/12/06 (Sat) | オリジナルバージョン。
// (1.01) | 2014/12/07 (Sun) |   FILE_EXT_LOG のクラス名を修正。
// (1.02) | 2014/12/17 (Wed) |   ログ保存場所のデフォルトを設定フォルダにした。
//  1.10  | 2014/12/17 (Wed) | ローテーション機能を付けた。
// (1.11) | 2014/12/17 (Wed) |   書き込み時のみログファイルをロックするようにした。
// (1.12) | 2015/01/01 (Thu) |   改行を "/" で表記するようにした。
// (1.13) | 2015/01/12 (Mon) |   改行を "<br>" で表記するようにした。
// (1.14) | 2015/05/13 (Wed) |   Dispose() から using に変更。
// (1.15) | 2015/11/08 (Sun) |   メッセージを "" で括るようにした。
//  1.20  | 2015/11/22 (Sun) | 複数世代を残せるようにした。
// (1.21) | 2018/01/07 (Sun) |   MaxOldGenerations のデフォルト値を 3 にした。
// (1.22) | 2019/01/19 (Sat) |   System.Windows.Forms を使用しないようにした。
// (1.23) | 2019/06/27 (Thu) |   StatusT を廃止。
//  1.30  | 2019/11/10 (Sun) | null 許容参照型を有効化した。
// (1.31) | 2019/12/22 (Sun) |   null 許容参照型を無効化できるようにした。
// (1.32) | 2020/11/15 (Sun) |   null 許容参照型の対応強化。
// (1.33) | 2020/11/15 (Sun) |   .NET 5 の単一ファイルに対応。
// (1.34) | 2021/03/06 (Sat) |   TraceEvent() の表記を簡略化。
// (1.35) | 2021/06/27 (Sun) |   Quote プロパティーを作成。
// (1.36) | 2022/01/23 (Sun) |   軽微なリファクタリング。
// (1.37) | 2023/11/19 (Sun) |   Microsoft.Windows.CsWin32 パッケージを導入。
// ============================================================================

using Windows.Win32;

namespace Shinta.Wpf;

public class SimpleTraceListener : TextWriterTraceListener
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	// --------------------------------------------------------------------
	// メインコンストラクター
	// --------------------------------------------------------------------
	public SimpleTraceListener()
	{
		// プロパティのデフォルト値
		LogFileName = Common.UserAppDataFolderPath()
				+ Path.ChangeExtension(Path.GetFileName(Environment.GetCommandLineArgs()[0]), Common.FILE_EXT_LOG);
		MaxSize = MAX_LOG_SIZE_DEFAULT;
		MaxOldGenerations = 3;
		Quote = true;
		ThreadSafe = false;
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	// ログファイル名
	public String LogFileName { get; set; }

	// ログファイルの最大サイズ（バイト：目安）
	public Int64 MaxSize { get; set; }

	// ログファイルの旧版を何世代保存するか
	public Int32 MaxOldGenerations { get; set; }

	// メッセージ本体をダブルクオートで囲むか
	private Boolean _quote;
	public Boolean Quote
	{
		get => _quote;
		set
		{
			_quote = value;
			_quoteString = _quote ? "\"" : null;
		}
	}

	// スレッドセーフ
	public Boolean ThreadSafe { get; set; }

	// ====================================================================
	// public 定数
	// ====================================================================

	// 古いログファイル名に付けるサフィックス
	public const String FILE_SUFFIX_OLD_LOG = "_Old_";

	// ====================================================================
	// public 関数
	// ====================================================================

	// --------------------------------------------------------------------
	// 旧世代のログファイル名
	// ＜引数＞ generaion: 世代
	// --------------------------------------------------------------------
	public String OldLogFileName(Int32 generaion)
	{
		return Path.GetDirectoryName(LogFileName) + "\\"
				+ Path.ChangeExtension(Path.GetFileNameWithoutExtension(LogFileName) + FILE_SUFFIX_OLD_LOG + generaion.ToString("D2"), Common.FILE_EXT_LOG);
	}

	// --------------------------------------------------------------------
	// ログ記録
	// ＜引数＞ source: TraceSource 生成時に指定された値
	// --------------------------------------------------------------------
	public override void TraceEvent(TraceEventCache? eventCache, String source, TraceEventType eventType, int id, String? message)
	{
		String contents;
		String eventTypeString;

		// イベントタイプ
		eventTypeString = eventType switch
		{
			Common.TRACE_EVENT_TYPE_STATUS => "Status",
			_ => eventType.ToString(),
		};

		// メッセージ
		if (!String.IsNullOrEmpty(message))
		{
			message = message.Replace("\r\n", BR_SYMBOL);
			message = message.Replace("\r", BR_SYMBOL);
			message = message.Replace("\n", BR_SYMBOL);
		}
		contents = DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss") + "," + Environment.ProcessId.ToString() + ",N"
				+ PInvoke.GetCurrentThreadId() + "/M" + Environment.CurrentManagedThreadId.ToString() + "," + source + "," + eventTypeString + "," + _quoteString + message + _quoteString;

		// 書き込み（最大 MAX_WRITE_TRY 回試行する）
		for (Int32 i = 0; i < MAX_WRITE_TRY; i++)
		{
			try
			{
				using (Writer = CreateTextWriter())
				{
					WriteLine(contents);
				}
				// 書き込みに成功したらループを抜ける
				break;
			}
			catch
			{
				// エラーが発生したら、一定時間後にリトライする
				Thread.Sleep(Common.GENERAL_SLEEP_TIME);
			}

		}

		LotateLogFile();
	}

	// ====================================================================
	// private 定数
	// ====================================================================

	private const Int32 MAX_WRITE_TRY = 3;
	private const Int64 MAX_LOG_SIZE_DEFAULT = 2 * 1024 * 1024;
	private const String BR_SYMBOL = "<br>";

	// ====================================================================
	// private 変数
	// ====================================================================

	// 引用文字列
	private String? _quoteString;

	// ====================================================================
	// private 関数
	// ====================================================================

	// --------------------------------------------------------------------
	// 書き出し用オブジェクトの作成
	// --------------------------------------------------------------------
	private TextWriter CreateTextWriter()
	{
		// StreamWriter オブジェクトの設定
		StreamWriter sw = new(LogFileName, true);
		sw.AutoFlush = true;

		if (ThreadSafe)
		{
			// スレッドセーフラッパを作成して返す
			return TextWriter.Synchronized(sw);
		}
		else
		{
			// そのまま返す
			return sw;
		}
	}

	// --------------------------------------------------------------------
	// ファイルサイズが一定値を越えたらローテーション
	// ＜返値＞ 正常終了なら true（ローテーションしたかしないかは関係ない）
	// --------------------------------------------------------------------
	private Boolean LotateLogFile()
	{
		// ローテーション設定確認（設定サイズが 0 ならローテーション不要）
		if (MaxSize <= 0)
		{
			return true;
		}

		// ファイル名の確認
		if (String.IsNullOrEmpty(LogFileName))
		{
			return false;
		}

		try
		{
			// ファイルサイズの調査
			FileInfo fi = new(LogFileName);
			if (fi.Length <= MaxSize)
			{
				// ローテーション不要
				return true;
			}

			try
			{
				// ローテーション：まずは最古のファイルを削除
				File.Delete(OldLogFileName(MaxOldGenerations));
			}
			catch (Exception)
			{
			}

			// 順に移動
			for (Int32 i = MaxOldGenerations; i > 1; i--)
			{
				try
				{
					File.Move(OldLogFileName(i - 1), OldLogFileName(i));
				}
				catch (Exception)
				{
				}
			}

			if (MaxOldGenerations <= 0)
			{
				File.Delete(LogFileName);
			}
			else
			{
				// 最新を旧へ
				File.Move(LogFileName, OldLogFileName(1));
			}
		}
		catch (Exception)
		{
			return false;
		}

		return true;
	}

}

