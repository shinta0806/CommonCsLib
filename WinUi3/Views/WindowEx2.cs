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
// ============================================================================

using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

using System.Reflection;
using System.Text;

using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Popups;
using Windows.Win32;
using Windows.Win32.Foundation;
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
	public Boolean AddVeil(String? childName = null, Object? childDataContext = null)
	{
		if (_veiledElement != null)
		{
			return false;
		}
		Page page = MainPage();
		_veiledElement = page.Content;

		// いったん切り離し
		page.Content = null;

		// 再構築
		Grid veilGrid = (Grid)LoadDynamicXaml("VeilGrid");
		veilGrid.Children.Add(_veiledElement);
		if (!String.IsNullOrEmpty(childName))
		{
			FrameworkElement element = (FrameworkElement)LoadDynamicXaml(childName);
			element.DataContext = childDataContext;
			veilGrid.Children.Add(element);
		}
		page.Content = veilGrid;
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
	/// ベール除去
	/// </summary>
	/// <returns>除去したかどうか（既に除去されている場合は false）</returns>
	public Boolean RemoveVeil()
	{
		if (_veiledElement == null)
		{
			return false;
		}

		Page page = MainPage();
		Grid veilGrid = (Grid)page.Content;
		veilGrid.Children.Clear();
		page.Content = _veiledElement;
		_veiledElement = null;
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
			throw new Exception("内部エラー：既にダイアログが開いています。");
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
	/// UI スレッド以外からも実行可能
	/// </summary>
	/// <param name="caption"></param>
	/// <param name="ex"></param>
	/// <returns></returns>
	public async Task<ContentDialogResult> ShowExceptionLogContentDialogAsync(String caption, Exception ex)
	{
		ContentDialogResult result = await ShowLogContentDialogAsync(LogEventLevel.Error, caption + "：\n" + ex.Message);
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
		IUICommand command = await ShowLogMessageDialogAsync(LogEventLevel.Error, caption + "：\n" + ex.Message);
		SerilogUtils.LogStackTrace(ex);
		return command;
	}

	/// <summary>
	/// ログの記録と表示（ContentDialog 版）
	/// UI スレッド以外からも実行可能
	/// </summary>
	/// <param name="logEventLevel"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	public async Task<ContentDialogResult> ShowLogContentDialogAsync(LogEventLevel logEventLevel, String message)
	{
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
	// protected 関数
	// ====================================================================

	/// <summary>
	/// LoadDynamicXaml() で使用するネームスペース
	/// </summary>
	/// <returns></returns>
	protected virtual String DynamicXamlNamespace()
	{
		return String.Empty;
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
	/// ベールに覆われている UIElement
	/// </summary>
	private UIElement? _veiledElement;

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
		Log.Debug("WindowEx2.AppWindowClosing() " + args.Cancel);
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
	/// 初期化
	/// </summary>
	private void Initialize()
	{
		Page page = MainPage();
		page.GettingFocus += MainPageGettingFocus;
		_initialized = true;
	}

	/// <summary>
	/// 実行バイナリ内の XAML を読み込んでコントロールを作成
	/// XAML のビルドアクションは「埋め込みリソース」である必要がある
	/// </summary>
	/// <returns></returns>
	private Object LoadDynamicXaml(String name)
	{
#if false
        // StorageFile を使う方が今時っぽいが（ビルドアクション：コンテンツ）、非パッケージ時にうまく動かないため現時点では使用しない
        Uri uri = new("ms-appx:///Views/Dynamics/" + name + Common.FILE_EXT_XAML);
        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
        using StreamReader streamReader = new StreamReader(await file.OpenStreamForReadAsync());
        String xaml = await streamReader.ReadToEndAsync();
#endif
		Assembly assembly = Assembly.GetExecutingAssembly();
		using Stream stream = assembly.GetManifestResourceStream(DynamicXamlNamespace() + "." + name + Common.FILE_EXT_XAML)
				?? throw new Exception("内部エラー：コントロールリソースが見つかりません。");
		Byte[] data = new Byte[stream.Length];
		stream.Read(data);
		String xaml = Encoding.UTF8.GetString(data);
		return XamlReader.Load(xaml);
	}

	/// <summary>
	/// メインページ（ウィンドウ内のページ）
	/// </summary>
	/// <returns></returns>
	private Page MainPage()
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
	/// イベントハンドラー：メインページのフォーカスを取得しようとしている
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void MainPageGettingFocus(UIElement sender, GettingFocusEventArgs args)
	{
		try
		{
			if (_veiledElement == null)
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
			Log.Error(GetType().Name + " メインページフォーカス時エラー：\n" + ex.Message);
			Log.Information("スタックトレース：\n" + ex.StackTrace);
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
