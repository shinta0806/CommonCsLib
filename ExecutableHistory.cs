// ============================================================================
// 
// ログから過去バージョンの実行ファイルのパスを取得するクラス
// Copyright (C) 2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/01/23 (Sun) | 作成開始。
//  1.00  | 2022/01/23 (Sun) | オリジナルバージョン。
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;

namespace Shinta
{
	internal class ExecutableHistory
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public ExecutableHistory(String? logPath = null)
		{
			if (!String.IsNullOrEmpty(logPath))
			{
				// ログファイルのフルパスが指定されている場合はセット
				_simpleTraceListener.LogFileName = logPath;
			}
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 過去バージョンの実行ファイルのパスを取得
		// 新しいログに記録されている実行ファイルのほうが結果の添字は若い
		// ＜引数＞ includeStore: ストアアプリとしての実行ファイルのパスも含める
		//          includeNoExist: ファイルとして存在していないパスも含める
		//          truncateLeaf: 実行ファイルではなくフォルダーパスにする
		// --------------------------------------------------------------------
		public List<String> GetHistories(Boolean includeStore, Boolean includeNoExist, Boolean truncateLeaf)
		{
			List<String> histories = new();

			// 現行ログから取得
			AddHistories(histories, _simpleTraceListener.LogFileName, includeStore, includeNoExist, truncateLeaf);

			// 過去ログから取得
			Int32 generation = 1;
			for (; ; )
			{
				String logPath = _simpleTraceListener.OldLogFileName(generation);
				if (!File.Exists(logPath))
				{
					break;
				}
				AddHistories(histories, logPath, includeStore, includeNoExist, truncateLeaf);
				generation++;
			}

			return histories;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ストアアプリがインストールされるフォルダー
		// API では取得できない（少なくとも Environment.GetFolderPath() では取得できなかった）ようなので決め打ち
		// ユーザー設定によりドライブは変更されることがあるが、その場合でも WindowsApps は含まれる模様
		private const String STORE_FOLDER = "WindowsApps\\";

		// ====================================================================
		// private 変数
		// ====================================================================

		// ログファイル名管理用のトレースリスナー（内容のアクセスには使用しない）
		private readonly SimpleTraceListener _simpleTraceListener = new();

		// ====================================================================
		// private 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 過去バージョンの実行ファイルのパスを追加
		// 新しいログに記録されている実行ファイルのほうが結果の添字は若い
		// --------------------------------------------------------------------
		private void AddHistories(List<String> histories, String logPath, Boolean includeStore, Boolean includeNoExist, Boolean truncateLeaf)
		{
			try
			{
				String[] lines = File.ReadAllLines(logPath);
				for (Int32 i = lines.Length - 1; i >= 0; i--)
				{
					// パス記載行かどうか調べる
					Int32 pos = lines[i].IndexOf(SystemEnvironment.LOG_PREFIX_SYSTEM_ENV + SystemEnvironment.LOG_ITEM_NAME_PATH);
					if (pos < 0)
					{
						continue;
					}

					// 実行ファイルもしくはフォルダーパス
					String exePath = lines[i][(pos + SystemEnvironment.LOG_PREFIX_SYSTEM_ENV.Length + SystemEnvironment.LOG_ITEM_NAME_PATH.Length)..].Trim('"');
					if (truncateLeaf)
					{
						exePath = Path.GetDirectoryName(exePath) + "\\";
					}

					// 以下を除外する
					// ・現行パス
					// ・ストアを含まない要求の場合にストアパス
					// ・追加済パス
					// ・存在しないパスを含まない要求の場合に存在しないパス
					if (Environment.GetCommandLineArgs()[0].StartsWith(exePath)
							|| !includeStore && exePath.Contains(STORE_FOLDER)
							|| histories.Contains(exePath)
							|| !includeNoExist && (truncateLeaf && !Directory.Exists(exePath) || !truncateLeaf && !File.Exists(exePath)))
					{
						continue;
					}

					// 追加
					histories.Add(exePath);
				}
			}
			catch
			{
			}
		}
	}
}
