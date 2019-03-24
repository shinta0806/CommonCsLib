// ============================================================================
// 
// メッセージおよび付加情報を CSV 形式でログファイルに保存
// Copyright (C) 2014-2019 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 文字コードは UTF-8
// アプリケーションアセンブリ情報（保存フォルダ名になる）を設定した状態で使用すること
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
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Shinta
{
	public class SimpleTraceListener : TextWriterTraceListener
	{
		// ====================================================================
		// public プロパティ
		// ====================================================================

		// ログファイル名
		public String LogFileName { get; set; }

		// ログファイルの最大サイズ（バイト：目安）
		public Int64 MaxSize { get; set; }

		// ログファイルの旧版を何世代保存するか
		public Int32 MaxOldGenerations { get; set; }

		// スレッドセーフ
		public Boolean ThreadSafe { get; set; }

		// ====================================================================
		// public 定数
		// ====================================================================

		// 古いログファイル名に付けるサフィックス
		public const String FILE_SUFFIX_OLD_LOG = "_Old_";

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public SimpleTraceListener()
		{
			// プロパティのデフォルト値
			LogFileName = Common.UserAppDataFolderPath()
					+ Path.ChangeExtension(Path.GetFileName(Assembly.GetEntryAssembly().Location), Common.FILE_EXT_LOG);
			MaxSize = MAX_LOG_SIZE_DEFAULT;
			MaxOldGenerations = 3;
			ThreadSafe = false;
		}

		// --------------------------------------------------------------------
		// 旧世代のログファイル名
		// ＜引数＞ oGeneraion: 世代
		// --------------------------------------------------------------------
		public String OldLogFileName(Int32 oGeneraion)
		{
			return Path.GetDirectoryName(LogFileName) + "\\"
					+ Path.ChangeExtension(Path.GetFileNameWithoutExtension(LogFileName) + FILE_SUFFIX_OLD_LOG + oGeneraion.ToString("D2"), Common.FILE_EXT_LOG);
		}

		// --------------------------------------------------------------------
		// ログ記録
		// ＜引数＞ oSource: TraceSource 生成時に指定された値
		// --------------------------------------------------------------------
		public override void TraceEvent(TraceEventCache oEventCache, String oSource, TraceEventType oEventType, int oID, String oMessage)
		{
			String aContents;
			String aEventType;

			// イベントタイプ
			switch (oEventType)
			{
				case Common.TRACE_EVENT_TYPE_STATUS:
					aEventType = "Status";
					break;
				default:
					aEventType = oEventType.ToString();
					break;
			}

			// メッセージ
			oMessage = oMessage.Replace("\r\n", BR_SYMBOL);
			oMessage = oMessage.Replace("\r", BR_SYMBOL);
			oMessage = oMessage.Replace("\n", BR_SYMBOL);
			aContents = DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss") + "," + Process.GetCurrentProcess().Id.ToString() + ",N"
					+ WindowsApi.GetCurrentThreadId() + "/M" + Thread.CurrentThread.ManagedThreadId.ToString() + "," + oSource + "," + aEventType + ",\"" + oMessage + "\"";

			// 書き込み（最大 MAX_WRITE_TRY 回試行する）
			for (Int32 i = 0; i < MAX_WRITE_TRY; i++)
			{
				try
				{
					using (Writer = CreateTextWriter())
					{
						WriteLine(aContents);
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
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 書き出し用オブジェクトの作成
		// --------------------------------------------------------------------
		private TextWriter CreateTextWriter()
		{
			// StreamWriter オブジェクトの設定
			StreamWriter aSW = new StreamWriter(LogFileName, true);
			aSW.AutoFlush = true;

			if (ThreadSafe)
			{
				// スレッドセーフラッパを作成して返す
				return TextWriter.Synchronized(aSW);
			}
			else
			{
				// そのまま返す
				return aSW;
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
				FileInfo aFI = new FileInfo(LogFileName);
				if (aFI.Length <= MaxSize)
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

#if USE_STATUS_T
		// --------------------------------------------------------------------
		// ファイルサイズが一定値を越えたらローテーション
		// --------------------------------------------------------------------
		private StatusT LotateLogFile()
		{
			// ローテーション設定確認（設定サイズが 0 ならローテーション不要）
			if (MaxSize <= 0)
			{
				return StatusT.Ok;
			}

			// ファイル名の確認
			if (String.IsNullOrEmpty(LogFileName))
			{
				return StatusT.Error;
			}

			try
			{
				// ファイルサイズの調査
				FileInfo aFI = new FileInfo(LogFileName);
				if (aFI.Length <= MaxSize)
				{
					// ローテーション不要
					return StatusT.Ok;
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
				return StatusT.Error;
			}

			return StatusT.Ok;
		}
#endif

	}
}

