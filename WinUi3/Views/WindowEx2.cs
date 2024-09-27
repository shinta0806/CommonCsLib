// ============================================================================
// 
// ウィンドウの拡張
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   CsWin32
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/12/08 (Thu) | 作成開始。
//  1.00  | 2022/12/08 (Thu) | ファーストバージョン。
//  1.10  | 2023/02/11 (Sat) | AdjustWindowPosition() を作成。
//  1.20  | 2023/04/04 (Tue) | ShowExceptionLogMessageDialogAsync() を作成。
//  1.30  | 2023/04/04 (Tue) | ShowLogContentDialogAsync() を作成。
//  1.40  | 2023/04/04 (Tue) | ShowExceptionLogContentDialogAsync() を作成。
// (1.41) | 2023/11/14 (Tue) |   AddVeil() を改善。
//  1.50  | 2024/06/25 (Tue) | IsHelpButtonEnabled を作成。
//  1.60  | 2024/06/25 (Tue) | HelpClickedCommand を作成。
// ============================================================================

using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Popups;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Shinta.WinUi3.Views;

public class WindowEx2 : WindowEx
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public WindowEx2()
	{
		// コマンド
		HelpClickedCommand = new RelayCommand<String>(HelpClicked);

		// イベントハンドラー
		Activated += WindowActivated;
		AppWindow.Closing += AppWindowClosing;
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	// --------------------------------------------------------------------
	// 一般のプロパティー
	// --------------------------------------------------------------------

	/// <summary>
	/// ヘルプボタンを表示するか
	/// </summary>
	private Boolean _isHelpButtonEnabled;
	public Boolean IsHelpButtonEnabled
	{
		get => _isHelpButtonEnabled;
		set
		{
			if (_isHelpButtonEnabled != value)
			{
				_isHelpButtonEnabled = value;
				OnIsHelpButtonEnabledChanged();
			}
		}
	}

	/// <summary>
	/// ヘルプボタンの引数
	/// </summary>
	public String? HelpButtonParameter
	{
		get;
		set;
	}

	/// <summary>
	/// 内容に合わせてサイズ調整
	/// ページコンテナが StackPanel の場合、HorizontalAlignment="Left" VerticalAlignment="Top" を指定する必要がある。
	/// 関連する操作は PageEx2 にやってもらう
	/// ToDo: Window.SizeToContent が実装されれば不要となるコード
	/// </summary>
	public SizeToContent SizeToContent
	{
		get;
		set;
	}

	// --------------------------------------------------------------------
	// コマンド
	// --------------------------------------------------------------------

	#region ヘルプリンクの制御
	public RelayCommand<String> HelpClickedCommand
	{
		get;
	}

	private void HelpClicked(String? parameter)
	{
		ShowHelp(parameter);
	}
	#endregion

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// ベール追加
	/// 埋め込みリソースとして "VeilGrid" が存在している前提
	/// UI スレッドからのみ実行可能
	/// </summary>
	/// <param name="childName"></param>
	/// <param name="childDataContext"></param>
	/// <returns>追加したかどうか（既に追加されている場合は false）</returns>
	public Boolean AddVeil()
	{
		if (_veilGrid != null)
		{
			// 既に追加されている
			return false;
		}
		UIElement content = MainPage().Content;
		if (content is not Panel panel)
		{
			// ページにパネルがないので追加できない
			return false;
		}
		_veilGrid = CreateVeil(panel);
		panel.Children.Add(_veilGrid);
		return true;
	}

	/// <summary>
	/// ウィンドウがスクリーンからはみ出ないようにする
	/// </summary>
	public void AdjustWindowPosition()
	{
		MonitorManager monitorManager = new();
		List<RECT> monitorRectsOrig = monitorManager.GetRawMonitorRects();
		List<Rect> monitorRects = new();
		foreach (RECT rect in monitorRectsOrig)
		{
			monitorRects.Add(new Rect(rect.X, rect.Y, rect.Width, rect.Height));
		}

		// 上下または左右のボーダー
		Int32 border = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME) * 2;

		// 現在位置
		Rect belongRect = BelongMonitorRect(monitorRects, FrameRect());
		if (belongRect.IsEmpty)
		{
			// どのモニターにも属していない場合はプライマリモニターに表示
			AppWindow.Move(new PointInt32(border, border));
			return;
		}

		Rect frameRect;

		// 下がはみ出ていないか（左下でチェック）
		frameRect = FrameRect();
		if (!monitorRects.Any(x => x.Contains(new Point(frameRect.Left, frameRect.Bottom))))
		{
			AppWindow.Move(new PointInt32((Int32)frameRect.Left, (Int32)(belongRect.Height - frameRect.Height - border)));
		}

		// 右がはみ出ていないか（右上でチェック）
		frameRect = FrameRect();
		if (!monitorRects.Any(x => x.Contains(new Point(frameRect.Right, frameRect.Top))))
		{
			AppWindow.Move(new PointInt32((Int32)(belongRect.Right - frameRect.Width - border), (Int32)frameRect.Top));
		}

		// 上がはみ出ていないか（左上でチェック）
		frameRect = FrameRect();
		if (!monitorRects.Any(x => x.Contains(new Point(frameRect.Left, frameRect.Top))))
		{
			AppWindow.Move(new PointInt32((Int32)frameRect.Left, border));
		}

		// 左がはみ出ていないか（左上でチェック）
		frameRect = FrameRect();
		if (!monitorRects.Any(x => x.Contains(new Point(frameRect.Left, frameRect.Top))))
		{
			AppWindow.Move(new PointInt32(border, (Int32)frameRect.Top));
		}
	}

	/// <summary>
	/// 指定のウィンドウの位置を自身に対してカスケードする
	/// 結果的に画面外にはみ出る場合があることに注意
	/// </summary>
	/// <param name="window"></param>
	public void CascadeWindow(WindowEx window)
	{
		// 位置決め
		// ToDo: 微妙にタイトルバーの高さと異なる
		Int32 delta = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME) + PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYCAPTION);

		// 移動
		window.AppWindow.Move(new PointInt32(this.AppWindow.Position.X + delta, this.AppWindow.Position.Y + delta));
	}

	/// <summary>
	/// スクリーン上でのウィンドウ位置
	/// </summary>
	/// <returns></returns>
	public Rect FrameRect()
	{
		PointInt32 position = AppWindow.Position;
		return new Rect(Bounds.Left + position.X, Bounds.Top + position.Y, Bounds.Width, Bounds.Height);
	}

	/// <summary>
	/// メインページ（ウィンドウ内のページ）
	/// </summary>
	/// <returns></returns>
	public Page MainPage()
	{
		if (Content is Frame frame)
		{
			return (Page)frame.Content;
		}
		else if (Content is Page page)
		{
			return page;
		}
		else
		{
			throw new Exception("内部エラー：予期しないコンテンツクラスです。");
		}
	}

	/// <summary>
	/// ベール除去
	/// </summary>
	/// <returns>除去したかどうか（既に除去されている場合は false）</returns>
	public Boolean RemoveVeil()
	{
		if (_veilGrid == null)
		{
			// 既に除去されている
			return false;
		}
		UIElement content = MainPage().Content;
		if (content is not Panel panel)
		{
			// ページにパネルがないので除去できない
			return false;
		}
		panel.Children.Remove(_veilGrid);
		_veilGrid = null;
		return true;
	}

	/// <summary>
	/// ウィンドウをモーダルで表示
	/// </summary>
	/// <param name="dialog"></param>
	/// <returns></returns>
	public async Task ShowDialogAsync(WindowEx2 dialog)
	{
		if (_openingDialog != null)
		{
			Log.Error("既にダイアログが開いています。：" + dialog.GetType().Name);

			// ToDo: Windows App SDK 1.4 現在、メニューをマウスとキーボードで操作すると 2 回呼ばれてしまうことがあり、ShowDialogAsync() の二重コールがありえる
			// dialog を閉じないとゾンビプロセスになるが、一度表示しないと閉じられない
			dialog.Activated += DialogActivated;
			dialog.Show();
			return;
		}
		_openingDialog = dialog;

		AddVeil();
		dialog.Closed += DialogClosed;
		CascadeWindow(dialog);
		dialog.Activate();
		dialog.AdjustWindowPosition();

		await Task.Run(() =>
		{
			_dialogEvent.WaitOne();
		});
		RemoveVeil();
	}

	/// <summary>
	/// 例外の記録と表示（ContentDialog 版）
	/// UI スレッドのみから実行可能
	/// </summary>
	/// <param name="caption"></param>
	/// <param name="ex"></param>
	/// <returns></returns>
	public async Task<ContentDialogResult> ShowExceptionLogContentDialogAsync(String caption, Exception ex)
	{
		ContentDialogResult result = await ShowLogContentDialogAsync(LogEventLevel.Error, ExceptionMessage(caption, ex));
		SerilogUtils.LogStackTrace(ex);
		return result;
	}

	/// <summary>
	/// 例外の記録と表示（MessageDialog 版）
	/// UI スレッド以外からも実行可能
	/// </summary>
	/// <param name="caption"></param>
	/// <param name="ex"></param>
	/// <returns></returns>
	public async Task<IUICommand> ShowExceptionLogMessageDialogAsync(String caption, Exception ex)
	{
		IUICommand command = await ShowLogMessageDialogAsync(LogEventLevel.Error, ExceptionMessage(caption, ex));
		SerilogUtils.LogStackTrace(ex);
		return command;
	}

	/// <summary>
	/// ログの記録と表示（ContentDialog 版）
	/// UI スレッドのみから実行可能
	/// </summary>
	/// <param name="logEventLevel"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	public async Task<ContentDialogResult> ShowLogContentDialogAsync(LogEventLevel logEventLevel, String message)
	{
		return await WinUi3Common.ShowLogContentDialogAsync(this, logEventLevel, message);
#if false
		// UI スレッド以外から実行したらフリーズした
		ContentDialogResult result = ContentDialogResult.None;
		AutoResetEvent autoResetEvent = new(false);
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			result = await WinUi3Common.ShowLogContentDialogAsync(this, logEventLevel, message);
			autoResetEvent.Set();
		});
		await Task.Run(() =>
		{
			autoResetEvent.WaitOne();
		});
		return result;
#endif
	}

	/// <summary>
	/// ログの記録と表示（MessageDialog 版）
	/// UI スレッド以外からも実行可能
	/// </summary>
	/// <param name="logEventLevel"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	public async Task<IUICommand> ShowLogMessageDialogAsync(LogEventLevel logEventLevel, String message)
	{
		IUICommand? command = null;
		AutoResetEvent autoResetEvent = new(false);
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			Boolean added = AddVeil();
			command = await WinUi3Common.ShowLogMessageDialogAsync(this, logEventLevel, message);
			if (added)
			{
				RemoveVeil();
			}
			autoResetEvent.Set();
		});
		await Task.Run(() =>
		{
			autoResetEvent.WaitOne();
		});
		Debug.Assert(command != null, "ShowLogMessageDialogAsync() command is null");
		return command;
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// ヘルプを表示
	/// </summary>
	/// <returns></returns>
	protected virtual void ShowHelp(String? parameter)
	{
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// 開いているダイアログウィンドウ
	/// </summary>
	private WindowEx? _openingDialog;

	/// <summary>
	/// ダイアログ制御用
	/// </summary>
	private readonly AutoResetEvent _dialogEvent = new(false);

	/// <summary>
	/// ベール
	/// </summary>
	private Grid? _veilGrid;

	/// <summary>
	/// ウィンドウプロシージャー
	/// </summary>
	private SUBCLASSPROC? _subclassProc;

	/// <summary>
	/// 初期化済
	/// </summary>
	private Boolean _initialized;

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// イベントハンドラー：ウィンドウが閉じられようとしている
	/// </summary>
	private void AppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
	{
		if (_openingDialog != null)
		{
			// 開いているダイアログがある場合は閉じない（タスクバーから閉じられた場合などは可能性がある）
			args.Cancel = true;
		}
	}

	/// <summary>
	/// ウィンドウを表示しているモニターの矩形
	/// スクリーン外の場合は Rect.Empty を返す
	/// </summary>
	/// <param name="monitorRects"></param>
	/// <param name="windowRect"></param>
	/// <returns></returns>
	private static Rect BelongMonitorRect(List<Rect> monitorRects, Rect windowRect)
	{
		Rect rect;
		Rect defaultRect = new();

		// まずはウィンドウの中央で検出
		rect = monitorRects.FirstOrDefault(x => x.Contains(new Point((windowRect.Left + windowRect.Right) / 2, (windowRect.Top + windowRect.Bottom) / 2)));
		if (rect != defaultRect)
		{
			return rect;
		}

		// 左上
		rect = monitorRects.FirstOrDefault(x => x.Contains(new Point(windowRect.Left, windowRect.Top)));
		if (rect != defaultRect)
		{
			return rect;
		}

		// 右上
		rect = monitorRects.FirstOrDefault(x => x.Contains(new Point(windowRect.Right, windowRect.Top)));
		if (rect != defaultRect)
		{
			return rect;
		}

		// 左下
		rect = monitorRects.FirstOrDefault(x => x.Contains(new Point(windowRect.Left, windowRect.Bottom)));
		if (rect != defaultRect)
		{
			return rect;
		}

		// 右下
		rect = monitorRects.FirstOrDefault(x => x.Contains(new Point(windowRect.Right, windowRect.Bottom)));
		if (rect != defaultRect)
		{
			return rect;
		}

		return Rect.Empty;
	}

	/// <summary>
	/// ベールとして使う Grid を作成
	/// </summary>
	/// <param name="parent"></param>
	/// <returns></returns>
	private Grid CreateVeil(Panel parent)
	{
		Double width = this.Width;
		Double height = this.Height;
		Double marginLeft = -parent.Margin.Left;
		Double marginTop = -parent.Margin.Top;
		Double marginRight = -parent.Margin.Right;
		Double marginBottom = -parent.Margin.Bottom;
		String add = String.Empty;

		if (parent is Grid)
		{

		}
		else if (parent is RelativePanel relativePanel)
		{
			add = "RelativePanel.AlignLeftWithPanel=\"True\" RelativePanel.AlignRightWithPanel=\"True\" RelativePanel.AlignTopWithPanel=\"True\" RelativePanel.AlignBottomWithPanel=\"True\"";
			marginLeft -= relativePanel.Padding.Left;
			marginTop -= relativePanel.Padding.Top;
			marginRight -= relativePanel.Padding.Right;
			marginBottom -= relativePanel.Padding.Bottom;
		}
		else if (parent is StackPanel stackPanel)
		{
			if (stackPanel.Orientation == Orientation.Vertical)
			{
				height = this.Height * 2;
				marginTop -= this.Height;
			}
			else
			{
				width = this.Width * 2;
				marginLeft -= this.Width;
			}
			marginLeft -= stackPanel.Padding.Left;
			marginTop -= stackPanel.Padding.Top;
			marginRight -= stackPanel.Padding.Right;
			marginBottom -= stackPanel.Padding.Bottom;
		}
		else
		{
			Debug.Assert(false, "CreateVeil() bad parent");
		}
		String margin = "Margin=\"" + marginLeft + "," + marginTop + "," + marginRight + "," + marginBottom + "\"";

		String xaml = $@"
<Grid
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
    Background=""#4D000000"" Canvas.ZIndex=""100"" Width=""{width}"" Height=""{height}"" {margin} {add} 
    >
</Grid>
";
		//Log.Debug(xaml);
		return (Grid)XamlReader.Load(xaml);
	}

	/// <summary>
	/// イベントハンドラー：破棄すべきダイアログがアクティブになった
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private async void DialogActivated(Object sender, WindowActivatedEventArgs args)
	{
		try
		{
			WindowEx2 dialog = (WindowEx2)sender;

			// 多重イベント防止
			dialog.Activated -= DialogActivated;

			// いきなり閉じるとアクセス違反になるので、まず隠して、時間が経ったら閉じる
			dialog.Hide();
			await Task.Delay(1000);
			dialog.Close();
			Log.Debug("DialogActivated() ダイアログを閉じた");
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("破棄すべきダイアログを閉じられませんでした。", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー：ダイアログが閉じられた
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void DialogClosed(object sender, WindowEventArgs args)
	{
		_openingDialog = null;
		_dialogEvent.Set();
	}

	/// <summary>
	/// 表示する例外メッセージ
	/// </summary>
	/// <param name="caption"></param>
	/// <param name="ex"></param>
	/// <returns></returns>
	private String ExceptionMessage(String caption, Exception ex)
	{
		String message = caption + "：\n" + ex.Message;
		if (ex.InnerException != null)
		{
			message += "\n詳細：\n" + ex.InnerException.Message;
		}
		return message;
	}

	/// <summary>
	/// 初期化
	/// </summary>
	private void Initialize()
	{
		Page page = MainPage();
		page.GettingFocus += MainPageGettingFocus;
		_initialized = true;
	}

	/// <summary>
	/// イベントハンドラー：メインページのフォーカスを取得しようとしている
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void MainPageGettingFocus(UIElement sender, GettingFocusEventArgs args)
	{
		try
		{
			if (_veilGrid == null)
			{
				// ベールに覆われていない（なにも作業等をしていない状態）ならそのままフォーカスを取得する
				return;
			}

			// ベールに覆われている場合はフォーカスを取得しない
			// 終了確認後に Cancel を直接いじると落ちるので TryCancel() を使う
			if (args.TryCancel())
			{
				args.Handled = true;
			}
		}
		catch (Exception ex)
		{
			// 終了確認後の可能性もあるので表示せずにログのみ
			SerilogUtils.LogException(GetType().Name + " メインページフォーカス時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	private void OnIsHelpButtonEnabledChanged()
	{
		if (IsHelpButtonEnabled)
		{
			_subclassProc = new SUBCLASSPROC(SubclassProc);
			WinUi3Common.EnableContextHelp(this, _subclassProc);
		}
		else
		{
			// 未実装
		}
	}

	/// <summary>
	/// ウィンドウメッセージ処理
	/// </summary>
	/// <param name="hwnd"></param>
	/// <param name="msg"></param>
	/// <param name="wPalam"></param>
	/// <param name="lParam"></param>
	/// <param name="_1"></param>
	/// <param name="_2"></param>
	/// <returns></returns>
	private LRESULT SubclassProc(HWND hWnd, UInt32 msg, WPARAM wPalam, LPARAM lParam, UIntPtr _1, UIntPtr _2)
	{
		switch (msg)
		{
			case PInvoke.WM_SYSCOMMAND:
				if ((UInt32)wPalam == PInvoke.SC_CONTEXTHELP)
				{
					ShowHelp(HelpButtonParameter);
					return (LRESULT)IntPtr.Zero;
				}

				// ヘルプボタン以外は次のハンドラーにお任せ
				return PInvoke.DefSubclassProc(hWnd, msg, wPalam, lParam);
			default:
				// WM_SYSCOMMAND 以外は次のハンドラーにお任せ
				return PInvoke.DefSubclassProc(hWnd, msg, wPalam, lParam);
		}
	}

	/// <summary>
	/// イベントハンドラー：ウィンドウ Activated / Deactivated
	/// </summary>
	/// <param name="_"></param>
	/// <param name="args"></param>
	private void WindowActivated(Object _, WindowActivatedEventArgs args)
	{
		if (args.WindowActivationState == WindowActivationState.Deactivated)
		{
			return;
		}

		// 初期化
		if (!_initialized)
		{
			Initialize();
		}

		// 開いているダイアログがある場合はダイアログをアクティブにする
		_openingDialog?.Activate();
	}
}
