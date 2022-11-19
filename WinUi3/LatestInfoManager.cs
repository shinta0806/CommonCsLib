// ============================================================================
// 
// 最新情報を解析・管理するクラス
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
//  -.--  | 2022/11/19 (Sat) | WPF 版を元に作成開始。
//  1.00  | 2022/11/19 (Sat) | ファーストバージョン。
// ============================================================================

using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;
using Windows.UI.Popups;

namespace Shinta.WinUi3
{
	public class LatestInfoManager
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public LatestInfoManager(String rssUrl, Boolean forceShow, Int32 wait, String appVer, CancellationToken cancellationToken, WindowEx window, String? settingsPath = null)
		{
			_rssUrl = rssUrl;
			_forceShow = forceShow;
			_wait = wait;
			_appVer = appVer;
			_cancellationToken = cancellationToken;
			_window = window;
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
						Log.Information("最新情報確認前に " + _wait.ToString() + " 秒待機します...");
						await Task.Delay(_wait * 1000);
					}
					await PrepareLatestAsync();
					if (_newItems.Count == 0)
					{
						await ShowLogMessageDialogIfNeededAsync(LogEventLevel.Information, "最新情報はありませんでした。");
					}
					else
					{
						Log.Information("最新情報が " + _newItems.Count.ToString() + " 件見つかりました。");
						await AskDisplayLatestAsync();
						DisplayLatest();
					}
					success = true;
				}
				catch (Exception ex)
				{
					await ShowLogMessageDialogIfNeededAsync(LogEventLevel.Error, "最新情報確認時エラー：\n" + ex.Message);
					Log.Information("スタックトレース：\n" + ex.StackTrace);
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

		// 最新情報保存パス
		private readonly String _settingsPath;

		// 最新情報
		private List<RssItem> _newItems = new();

		// メッセージ表示用の親ウィンドウ
		private WindowEx _window;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 最新情報の確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async Task AskDisplayLatestAsync()
		{
			MessageDialog messageDialog = _window.CreateMessageDialog("最新情報が " + _newItems.Count.ToString() + " 件見つかりました。\n表示しますか？", "質問");
			messageDialog.Commands.Add(new UICommand("はい"));
			messageDialog.Commands.Add(new UICommand("いいえ"));
			IUICommand cmd = await messageDialog.ShowAsync();
			if (cmd.Label != "はい")
			{
				throw new Exception("最新情報の表示を中止しました。");
			}
		}

		// --------------------------------------------------------------------
		// RSS マネージャーを生成
		// --------------------------------------------------------------------
		private RssManager CreateRssManager()
		{
			RssManager rssManager = new(_settingsPath);

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
			Log.Debug(guids);
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
				Log.Information(_newItems.Count.ToString() + " 件の最新情報を表示完了。");
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
			Log.Information("最新情報を確認中...");

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

		/// <summary>
		/// ログの記録と表示
		/// </summary>
		/// <param name="logEventLevel"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public Task ShowLogMessageDialogIfNeededAsync(LogEventLevel logEventLevel, String message)
		{
			Log.Write(logEventLevel, message);
			if (_forceShow)
			{
				return _window.CreateMessageDialog(message, logEventLevel.ToString().ToLocalized()).ShowAsync().AsTask();
			}
			else
			{
				return Task.CompletedTask;
			}
		}

	}
}
