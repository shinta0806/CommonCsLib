// ============================================================================
// 
// 最新情報を解析・管理するクラス
// Copyright (C) 2021-2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2021/12/19 (Sun) | 作成開始。
//  1.00  | 2021/12/19 (Sun) | オリジナルバージョン。
// (1.01) | 2022/01/09 (Sun) |   軽微なリファクタリング。
//  1.10  | 2022/02/26 (Sat) | アプリケーションバージョンによる選別機能を付けた。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Shinta
{
	public class LatestInfoManager
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public LatestInfoManager(String rssUrl, Boolean forceShow, Int32 wait, String appVer, CancellationToken cancellationToken, LogWriter? logWriter = null, String? settingsPath = null)
		{
			_rssUrl = rssUrl;
			_forceShow = forceShow;
			_wait = wait;
			_appVer = appVer;
			_cancellationToken = cancellationToken;
			_logWriter = logWriter;
			if (String.IsNullOrEmpty(settingsPath))
			{
				_settingsPath = Common.UserAppDataFolderPath() + FILE_NAME_LATEST_INFO;
			}
			else
			{
				_settingsPath = settingsPath;
			}
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 最新情報の確認と、必要に応じて結果表示
		// ＜返値＞ true: 成功, false: 失敗
		// --------------------------------------------------------------------
		public Task<Boolean> CheckAsync()
		{
			return Task.Run(async () =>
			{
				Boolean success = false;
				try
				{
					if (_wait > 0)
					{
						_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "最新情報確認前に " + _wait.ToString() + " 秒待機します...");
						await Task.Delay(_wait * 1000);
					}
					await PrepareLatestAsync();
					if (_newItems.Count == 0)
					{
						_logWriter?.ShowLogMessage(TraceEventType.Information, "最新情報はありませんでした。", !_forceShow);
					}
					else
					{
						_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "最新情報が " + _newItems.Count.ToString() + " 件見つかりました。");
						AskDisplayLatest();
						DisplayLatest();
					}
					success = true;
				}
				catch (Exception excep)
				{
					_logWriter?.ShowLogMessage(TraceEventType.Error, "最新情報確認時エラー：\n" + excep.Message, !_forceShow);
					_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
				return success;
			});
		}

		// ====================================================================
		// private メンバー定数
		// ====================================================================

		// 最新情報保存ファイル名
		private const String FILE_NAME_LATEST_INFO = "LatestInfo" + Common.FILE_EXT_CONFIG;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 最新情報を保持している RSS の URL
		private readonly String _rssUrl;

		// 最新情報が無くてもユーザーに通知
		private readonly Boolean _forceShow;

		// チェック開始までの待ち時間 [s]
		private readonly Int32 _wait;

		// アプリのバージョン
		private readonly String _appVer;

		// 中断制御
		private readonly CancellationToken _cancellationToken;

		// ログ
		private readonly LogWriter? _logWriter;

		// 最新情報保存パス
		private readonly String _settingsPath;

		// 最新情報
		private List<RssItem> _newItems = new();

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 最新情報の確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void AskDisplayLatest()
		{
			if (MessageBox.Show("最新情報が " + _newItems.Count.ToString() + " 件見つかりました。\n表示しますか？",
					"質問", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
			{
				throw new Exception("最新情報の表示を中止しました。");
			}
		}

		// --------------------------------------------------------------------
		// RSS マネージャーを生成
		// --------------------------------------------------------------------
		private RssManager CreateRssManager()
		{
			RssManager rssManager = new(_logWriter, _settingsPath);

			// 既存設定の読込
			rssManager.Load();

			// 中断制御
			rssManager.CancellationToken = _cancellationToken;

#if DEBUG
			String guids = "SetRssManager() PastRssGuids:\n";
			foreach (String guid in rssManager.PastRssGuids)
			{
				guids += guid + "\n";
			}
			_logWriter?.LogMessage(TraceEventType.Verbose, guids);
#endif

			return rssManager;
		}

		// --------------------------------------------------------------------
		// 最新情報の確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void DisplayLatest()
		{
			Int32 numErrors = 0;

			foreach (RssItem newItem in _newItems)
			{
				try
				{
					Common.ShellExecute(newItem.Elements[RssManager.NODE_NAME_LINK]);
				}
				catch
				{
					// エラーでもとりあえずは続行
					numErrors++;
				}
			}
			if (numErrors == 0)
			{
				// 正常終了
				_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, _newItems.Count.ToString() + " 件の最新情報を表示完了。");
			}
			else if (numErrors < _newItems.Count)
			{
				throw new Exception("一部の最新情報を表示できませんでした。");
			}
			else
			{
				throw new Exception("最新情報を表示できませんでした。");
			}
		}

		// --------------------------------------------------------------------
		// 最新情報の確認と表示準備
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async Task PrepareLatestAsync()
		{
			_logWriter?.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "最新情報を確認中...");

			// RSS チェック
			RssManager rssManager = CreateRssManager();
			(Boolean result, String? errorMessage) = await rssManager.ReadLatestRssAsync(_rssUrl, _appVer);
			if (!result)
			{
				throw new Exception(errorMessage);
			}
			_newItems = rssManager.GetNewItems();

			// 更新
			rssManager.UpdatePastRss();
			rssManager.Save();
		}

	}
}
