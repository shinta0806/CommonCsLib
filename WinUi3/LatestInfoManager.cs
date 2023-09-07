// ============================================================================
// 
// 最新情報を解析・管理するクラス
// Copyright (C) 2022-2023 by SHINTA
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
// (1.01) | 2022/11/19 (Sat) |   軽微なリファクタリング。
//  1.10  | 2022/12/11 (Sun) | 多言語対応。
//  1.20  | 2023/08/19 (Sat) | RssManager の派生にした。
// (1.21) | 2023/09/05 (Tue) |   Task に関する内部変更。
// ============================================================================

using Windows.UI.Popups;

namespace Shinta.WinUi3;

internal class LatestInfoManager : RssManager
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	/// <param name="rssUrl"></param>
	/// <param name="forceShow"></param>
	/// <param name="wait"></param>
	/// <param name="appVer"></param>
	/// <param name="cancellationToken"></param>
	/// <param name="window"></param>
	/// <param name="settingsPath">絶対パスでも相対パスでも可</param>
	public LatestInfoManager(String rssUrl, Boolean forceShow, Int32 wait, String appVer, WindowEx window, String? settingsPath = null)
			: base(String.IsNullOrEmpty(settingsPath) ? FILE_NAME_LATEST_INFO : settingsPath)
	{
		_rssUrl = rssUrl;
		_forceShow = forceShow;
		_wait = wait;
		_appVer = appVer;
		_window = window;
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// 最新情報の確認と、必要に応じて結果表示
	/// </summary>
	/// <returns>true: 成功, false: 失敗</returns>
	public async Task<Boolean> CheckAsync()
	{
		return await Task.Run(async () =>
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
					await ShowLogMessageDialogIfNeededAsync(LogEventLevel.Information, "LatestInfoManager_CheckAsync_NoInfo".ToLocalized());
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
				await ShowLogMessageDialogIfNeededAsync(LogEventLevel.Error, "LatestInfoManager_CheckAsync_Error".ToLocalized() + "\n" + ex.Message);
				SerilogUtils.LogStackTrace(ex);
			}
			return success;
		});
	}

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// 最新情報保存ファイル名
	/// </summary>
	private const String FILE_NAME_LATEST_INFO = "LatestInfo" + Common.FILE_EXT_JSON;

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// 最新情報を保持している RSS の URL
	/// </summary>
	private readonly String _rssUrl;

	/// <summary>
	/// 最新情報が無くてもユーザーに通知
	/// </summary>
	private readonly Boolean _forceShow;

	/// <summary>
	/// チェック開始までの待ち時間 [s]
	/// </summary>
	private readonly Int32 _wait;

	/// <summary>
	/// アプリのバージョン
	/// </summary>
	private readonly String _appVer;

	/// <summary>
	/// 今回の最新情報
	/// </summary>
	private List<RssItem> _newItems = new();

	/// <summary>
	/// メッセージ表示用の親ウィンドウ
	/// </summary>
	private readonly WindowEx _window;

	// ====================================================================
	// private メンバー関数
	// ====================================================================

	/// <summary>
	/// 最新情報の確認
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	private async Task AskDisplayLatestAsync()
	{
		MessageDialog messageDialog = _window.CreateMessageDialog(String.Format("LatestInfoManager_AskDisplayLatestAsync_Ask".ToLocalized(),
				_newItems.Count), Common.LK_GENERAL_LABEL_CONFIRM.ToLocalized());
		messageDialog.Commands.Add(new UICommand(Common.LK_GENERAL_LABEL_YES.ToLocalized()));
		messageDialog.Commands.Add(new UICommand(Common.LK_GENERAL_LABEL_NO.ToLocalized()));
		IUICommand cmd = await messageDialog.ShowAsync();
		if (cmd.Label != Common.LK_GENERAL_LABEL_YES.ToLocalized())
		{
			throw new Exception("LatestInfoManager_AskDisplayLatestAsync_Cancel".ToLocalized());
		}
	}

	/// <summary>
	/// 最新情報の確認
	/// </summary>
	/// <exception cref="Exception"></exception>
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
			throw new Exception("LatestInfoManager_DisplayLatest_Error_Part".ToLocalized());
		}
		else
		{
			throw new Exception("LatestInfoManager_DisplayLatest_Error_Not".ToLocalized());
		}
	}

	/// <summary>
	/// 最新情報の確認と表示準備
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	private async Task PrepareLatestAsync()
	{
		Log.Information("最新情報を確認中...");

		// RSS チェック
		try
		{
			Load();
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("過去の最新情報を読み込めませんでした", ex);
		}

		(Boolean result, String? errorMessage) = await ReadLatestRssAsync(_rssUrl, _appVer);
		if (!result)
		{
			throw new Exception(errorMessage);
		}
		_newItems = GetNewItems();

		// 更新
		UpdatePastRss();
		Save();
	}

	/// <summary>
	/// ログの記録と表示
	/// </summary>
	/// <param name="logEventLevel"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	private async Task ShowLogMessageDialogIfNeededAsync(LogEventLevel logEventLevel, String message)
	{
		Log.Write(logEventLevel, message);
		if (_forceShow)
		{
			await _window.CreateMessageDialog(message, logEventLevel.ToString().ToLocalized()).ShowAsync();
		}
	}
}
