// ============================================================================
// 
// ウィンドウの拡張
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Serilog
//   CsWin32
//     comInterop: preserveSigMethods
//     Ole32
//     Shell32
//     FileOpenDialog
//     FileSaveDialog
//     IFileDialog
//     IFileOpenDialog
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
//  1.70  | 2025/03/31 (Mon) | ShowOpenFileDialogMulti() を作成。
//  1.80  | 2025/03/31 (Mon) | ShowOpenFileDialog() を作成。
//  1.90  | 2025/03/31 (Mon) | ShowSaveFileDialogMulti() を作成。
//  2.00  | 2025/03/31 (Mon) | ShowSaveFileDialog() を作成。
//  2.10  | 2025/04/02 (Wed) | ShowFileOpenDialogMulti() を作成し、ShowOpenFileDialogMulti() を廃止。
//  2.20  | 2025/04/02 (Wed) | ShowFileOpenDialog() を作成し、ShowOpenFileDialog() を廃止。
//  2.30  | 2025/04/02 (Wed) | ShowFileSaveDialog() を作成し、ShowSaveFileDialog() および ShowSaveFileDialogMulti() を廃止。
// (2.31) | 2025/11/14 (Fri) |   ShowFileOpenDialogCore() を改善。
// (2.32) | 2025/11/14 (Fri) |   ShowFileXxxxDialog() の引数順番を変更。
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
using Windows.Win32.UI.WindowsAndMessaging;

#if !USE_AOT
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using WinRT.Interop;
#endif

#if USE_AOT && USE_UNSAFE
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using WinRT.Interop;
#endif

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

#if USE_AOT && USE_UNSAFE
		HWND hWnd = (HWND)WindowNative.GetWindowHandle(this);
		_hWndMap[hWnd] = this;
#endif

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
		List<Rect> monitorRects = [];
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
		ContentDialogResult result = await ShowLogContentDialogAsync(LogEventLevel.Error, Common.ExceptionMessage(caption, ex));
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
		IUICommand command = await ShowLogMessageDialogAsync(LogEventLevel.Error, Common.ExceptionMessage(caption, ex));
		SerilogUtils.LogStackTrace(ex);
		return command;
	}

#if USE_UNSAFE
	/// <summary>
	/// ファイルを開くダイアログを表示
	/// </summary>
	/// <param name="filter">説明|拡張子（例："画像ファイル|*.jpg;*.jpeg"）、説明に拡張子を含めなくても自動的に OS 側で付与される</param>
	/// <param name="filterIndex"></param>
	/// <param name="options">FOS_PICKFOLDERS でフォルダーを開く（その際、通常は filter = String.Empty を指定）</param>
	/// <param name="initialPath"></param>
	/// <returns></returns>
	public String? ShowFileOpenDialog(String filter, ref Int32 filterIndex, FILEOPENDIALOGOPTIONS options = 0, Guid? guid = null, String? initialPath = null)
	{
		String[]? result = ShowFileOpenDialogCore(filter, ref filterIndex, options, guid, initialPath);
		if (result == null)
		{
			return null;
		}
		return result[0];
	}

	/// <summary>
	/// ファイルを開くダイアログを表示（複数選択可能）
	/// </summary>
	/// <param name="filter"></param>
	/// <param name="filterIndex"></param>
	/// <param name="options"></param>
	/// <param name="initialPath"></param>
	/// <returns></returns>
	public String[]? ShowFileOpenDialogMulti(String filter, ref Int32 filterIndex, FILEOPENDIALOGOPTIONS options = 0, Guid? guid = null, String? initialPath = null)
	{
		options |= FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT;
		return ShowFileOpenDialogCore(filter, ref filterIndex, options, guid, initialPath);
	}

	/// <summary>
	/// 名前を付けて保存ダイアログを表示
	/// </summary>
	/// <param name="filter">「すべてのファイル」は含めないほうが良い（拡張子が * になる）</param>
	/// <param name="filterIndex"></param>
	/// <param name="options"></param>
	/// <param name="initialPath"></param>
	/// <returns></returns>
	public unsafe String? ShowFileSaveDialog(String filter, ref Int32 filterIndex, FILEOPENDIALOGOPTIONS options = 0, Guid? guid = null, String? initialPath = null)
	{
		IFileDialog* saveDialog = null;
		IShellItem* shellResult = null;

		// finally 用の try
		try
		{
			// ダイアログ生成
			HRESULT result = PInvoke.CoCreateInstance(typeof(FileSaveDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out saveDialog);
			result.ThrowOnFailure();

			// 表示
			if (!ShowFileDialogCore(saveDialog, filter, ref filterIndex, options, guid, initialPath))
			{
				return null;
			}

			// 結果取得
			result = saveDialog->GetResult(&shellResult);
			if (result.Failed)
			{
				return null;
			}
			return ShellItemToPath(shellResult, filter, filterIndex);
		}
		finally
		{
			if (shellResult != null)
			{
				shellResult->Release();
			}
			if (saveDialog != null)
			{
				saveDialog->Release();
			}
		}
	}
#endif

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

#if USE_UNSAFEz
	/// <summary>
	/// Win32 API ファイル選択ダイアログを開く（WinUI 3 のは管理者権限で開けないので）
	/// </summary>
	/// <param name="filter">"|" で説明と拡張子パターンを区切ったペアを連結、パターンにスペース不可 
	/// "すべてのファイル (*.*)|*.*|動画ファイル (*.mp4;*.wmv)|*.mp4;*.wmv|テキストファイル (*.txt)|*.txt"</param>
	/// <param name="filterIndex">初期選択、および、ユーザーが選択したフィルターのインデックス（0 オリジン）</param>
	/// <param name="initialPath">初期選択されるファイルのパス（ファイル名のみでも可）</param>
	/// <param name="flags">OPEN_FILENAME_FLAGS</param>
	/// <returns>パス（ユーザーが拡張子を入力しない場合はそのまま）、キャンセルされた場合は null</returns>
	public unsafe String? ShowOpenFileDialog(String filter, ref Int32 filterIndex, OPEN_FILENAME_FLAGS flags = 0, String? initialPath = null)
	{
		String[]? result = ShowOpenFileDialogCore(filter, ref filterIndex, flags, initialPath);
		if (result == null)
		{
			return null;
		}
		return result[0];
	}

	/// <summary>
	/// Win32 API ファイル選択ダイアログを開く（WinUI 3 のは管理者権限で開けないので）
	/// </summary>
	/// <param name="filter">"|" で説明と拡張子パターンを区切ったペアを連結、パターンにスペース不可 
	/// "すべてのファイル (*.*)|*.*|動画ファイル (*.mp4;*.wmv)|*.mp4;*.wmv|テキストファイル (*.txt)|*.txt"</param>
	/// <param name="filterIndex">初期選択、および、ユーザーが選択したフィルターのインデックス（0 オリジン）</param>
	/// <param name="initialPath">初期選択されるファイルのパス（ファイル名のみでも可）</param>
	/// <param name="flags">OPEN_FILENAME_FLAGS</param>
	/// <returns>パス群（ユーザーが拡張子を入力しない場合はそのまま）、キャンセルされた場合は null</returns>
	public unsafe String[]? ShowOpenFileDialogMulti(String filter, ref Int32 filterIndex, OPEN_FILENAME_FLAGS flags = 0, String? initialPath = null)
	{
		flags |= OPEN_FILENAME_FLAGS.OFN_ALLOWMULTISELECT;
		return ShowOpenFileDialogCore(filter, ref filterIndex, flags, initialPath);
	}

	/// <summary>
	/// Win32 API ファイル保存ダイアログを開く（WinUI 3 のは管理者権限で開けないので）
	/// </summary>
	/// <param name="filter">"|" で説明と拡張子パターンを区切ったペアを連結、パターンにスペース不可 
	/// "すべてのファイル (*.*)|*.*|動画ファイル (*.mp4;*.wmv)|*.mp4;*.wmv|テキストファイル (*.txt)|*.txt"</param>
	/// <param name="filterIndex">初期選択、および、ユーザーが選択したフィルターのインデックス（0 オリジン）</param>
	/// <param name="initialPath">初期選択されるファイルのパス（ファイル名のみでも可）</param>
	/// <param name="flags">OPEN_FILENAME_FLAGS</param>
	/// <returns>パス（ユーザーが拡張子を入力しない場合はそのまま）、キャンセルされた場合は null</returns>
	public unsafe String? ShowSaveFileDialog(String filter, ref Int32 filterIndex, OPEN_FILENAME_FLAGS flags = 0, String? initialPath = null)
	{
		String[]? result = ShowSaveFileDialogCore(filter, ref filterIndex, flags, initialPath);
		if (result == null)
		{
			return null;
		}
		return result[0];
	}

	/// <summary>
	/// Win32 API ファイル保存ダイアログを開く（WinUI 3 のは管理者権限で開けないので）
	/// </summary>
	/// <param name="filter">"|" で説明と拡張子パターンを区切ったペアを連結、パターンにスペース不可 
	/// "すべてのファイル (*.*)|*.*|動画ファイル (*.mp4;*.wmv)|*.mp4;*.wmv|テキストファイル (*.txt)|*.txt"</param>
	/// <param name="filterIndex">初期選択、および、ユーザーが選択したフィルターのインデックス（0 オリジン）</param>
	/// <param name="initialPath">初期選択されるファイルのパス（ファイル名のみでも可）</param>
	/// <param name="flags">OPEN_FILENAME_FLAGS</param>
	/// <returns>パス群（ユーザーが拡張子を入力しない場合はそのまま）、キャンセルされた場合は null</returns>
	public unsafe String[]? ShowSaveFileDialogMulti(String filter, ref Int32 filterIndex, OPEN_FILENAME_FLAGS flags = 0, String? initialPath = null)
	{
		flags |= OPEN_FILENAME_FLAGS.OFN_ALLOWMULTISELECT;
		return ShowSaveFileDialogCore(filter, ref filterIndex, flags, initialPath);
	}
#endif

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
	// private 定数
	// ====================================================================

	/// <summary>
	/// OPENFILENAMEW の lpstrFile 用バッファーサイズ
	/// バッファーは最小なら MAX_PATH * 2（Unicode）
	/// しかし拡張パスは 32,767 * 2 だし、複数ファイル選択の場合もあるため、多めに取っておく
	/// </summary>
	private const UInt32 PATH_BUF_SIZE = 80 * 1024;

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

#if USE_AOT && USE_UNSAFE
	/// <summary>
	/// static なウィンドウプロシージャーがウィンドウを識別するための変数
	/// </summary>
	private static readonly Dictionary<HWND, WindowEx2> _hWndMap = new();
#endif

#if !USE_AOT && !NO_SUBCLASSPROC
	/// <summary>
	/// ウィンドウプロシージャー
	/// </summary>
	private SUBCLASSPROC? _subclassProc;
#endif

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

#if USE_UNSAFE
	/// <summary>
	/// 背後バッファが持続する PCWSTR を作成
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	private static unsafe (PCWSTR, nint) CreateCoMemPcwstr(String str)
	{
		nint bufPtr = Marshal.StringToCoTaskMemUni(str);
		PCWSTR pcwstr = new((Char*)bufPtr);
		return (pcwstr, bufPtr);
	}
#endif

#if USE_UNSAFEz
	private unsafe OPENFILENAMEW CreateOpenFileNameW(String filter, Int32 filterIndex, OPEN_FILENAME_FLAGS flags, String? initialPath,
		ref char* filterPtr, ref char* pathPtr)
	{
		// OPENFILENAMEW の準備
		// https://learn.microsoft.com/ja-jp/windows/win32/api/commdlg/ns-commdlg-openfilenamew

		// lStructSize: アンマネージドの構造体サイズ
		UInt32 structSize = (UInt32)Marshal.SizeOf<OPENFILENAMEW>();

		// hwndOwner: 指定しないとモードレスになるので指定する
		nint hWnd = WindowNative.GetWindowHandle(this);

		// hInstance: テンプレートを使用しない

		// lpstrFilter: ペアの区切りは null Char 1 文字、フィルターの末尾は null Char 2 文字
		filter = (filter + "||").Replace('|', (Char)0);
		Byte[] filterBytes = Encoding.Unicode.GetBytes(filter);
		filterPtr = (char*)NativeMemory.Alloc((UInt32)filterBytes.Length);
		Marshal.Copy(filterBytes, 0, (nint)filterPtr, filterBytes.Length);

		// lpstrCustomFilter: ユーザー定義フィルターを使用しない

		// nMaxCustFilter: ユーザー定義フィルターを使用しない

		// nFilterIndex: 本関数は 0 オリジンだが、Win32 API は 1 オリジン
		filterIndex++;

		// lpstrFile: 初期パス兼ユーザー選択結果
		pathPtr = (char*)NativeMemory.AllocZeroed(PATH_BUF_SIZE);
		if (!String.IsNullOrEmpty(initialPath))
		{
			Byte[] initialPathBytes = Encoding.Unicode.GetBytes(initialPath);
			Marshal.Copy(initialPathBytes, 0, (nint)pathPtr, initialPathBytes.Length);
		}

		// nMaxFile: pathBufSize

		// lpstrFileTitle: lpstrFile で用は足りる

		// nMaxFileTitle: lpstrFile で用は足りる

		// lpstrInitialDir: lpstrFile で用は足りる

		// lpstrTitle: ダイアログタイトルは使用しない

		// Flags: OFN_EXPLORER 強制付与
		// OFN_ALLOWMULTISELECT の時は OFN_EXPLORER がないと見た目が古いだけではなく lpstrFile の扱い方も変わってしまう
		// OFN_ALLOWMULTISELECT が指定されていない場合は OFN_ALLOWMULTISELECT があってもなくても影響ない
		flags |= OPEN_FILENAME_FLAGS.OFN_EXPLORER;

		return new()
		{
			lStructSize = structSize,
			hwndOwner = new(hWnd),
			lpstrFilter = new(filterPtr),
			nFilterIndex = (UInt32)filterIndex,
			lpstrFile = new(pathPtr),
			nMaxFile = PATH_BUF_SIZE,
			Flags = flags,
		};
	}
#endif

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

#if USE_UNSAFEz
	private unsafe String[] FileDialogPathes(ref Int32 filterIndex, char* pathPtr, OPENFILENAMEW openFileNameW)
	{
		// 選択されたフィルター（1 オリジン → 0 オリジン）
		filterIndex = (Int32)openFileNameW.nFilterIndex - 1;

		// 選択ファイルが 1 つの場合、firstPath にフルパスが入る
		// 選択ファイルが 2 つ以上の場合、firstPath にフォルダーが入り、null Char 0 以降にパスの無いファイル名が入る（末尾はダブル null Char 0）
		String firstPath = new(pathPtr);
		if (!Directory.Exists(firstPath))
		{
			// フォルダーではない場合はすべて単独ファイル扱い
			return [firstPath];
		}

		ReadOnlySpan<Char> spanChar = new ReadOnlySpan<Char>(pathPtr, (Int32)PATH_BUF_SIZE / sizeof(Char)).Slice(openFileNameW.nFileOffset);
		Int32 endPos = spanChar.IndexOf("\0\0".AsSpan());
		String[] fileNames = new String(spanChar.Slice(0, endPos)).Split('\0');
		String[] pathes = new String[fileNames.Length];
		for (Int32 i = 0; i < fileNames.Length; i++)
		{
			pathes[i] = firstPath + "\\" + fileNames[i];
		}
		return pathes;
	}
#endif

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
#if USE_AOT && USE_UNSAFE
			unsafe
			{
				WinUi3Common.EnableContextHelp(this, &SubclassProcAot);
			}
#endif
#if !USE_AOT && !NO_SUBCLASSPROC
			_subclassProc = new SUBCLASSPROC(SubclassProc);
			WinUi3Common.EnableContextHelp(this, _subclassProc);
#endif
		}
		else
		{
			// 未実装
		}
	}

#if USE_UNSAFE
	/// <summary>
	/// IShellItem からパスを取得
	/// </summary>
	/// <param name="iShellResult"></param>
	/// <returns></returns>
	private unsafe String? ShellItemToPath(IShellItem* iShellResult, String filter, Int32 filterIndex)
	{
		PWSTR pathPwstr;
		HRESULT result = iShellResult->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &pathPwstr);
		if (result.Failed)
		{
			return null;
		}
		String path = pathPwstr.ToString();
		Marshal.FreeCoTaskMem((nint)pathPwstr.Value);

		// 拡張子が入力されておらず、指定ファイルが存在しない場合は、拡張子を付与
		// FOS_STRICTFILETYPES は動作していないように見える
		// フォルダーの場合は存在しないがフィルターもないはずなので該当しない
		(String[] filters, Boolean checkFilter) = ShowFileDialogCheckFilter(filter);
		if (String.IsNullOrEmpty(Path.GetExtension(path)) && !File.Exists(path) && checkFilter)
		{
			if (filterIndex < filters.Length / 2)
			{
				String ext = Path.GetExtension(filters[filterIndex * 2 + 1].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0]);
				path = Path.ChangeExtension(path, ext);
			}
		}

		return path;
	}

	/// <summary>
	/// '|' で区切られたフィルター文字列（2 つでペア）を分解
	/// </summary>
	/// <param name="filter"></param>
	/// <returns>filters: フィルター（[n] が説明、[n+1] が拡張子）, check: ペアになっていたか</returns>
	private static (String[], Boolean) ShowFileDialogCheckFilter(String filter)
	{
		String[] filters = filter.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		Boolean check = filters.Length > 0 && filters.Length % 2 == 0;
		return (filters, check);
	}

	/// <summary>
	/// Win32 API ファイル選択ダイアログを開く
	/// </summary>
	/// <param name="fileDialog"></param>
	/// <param name="filter"></param>
	/// <param name="filterIndex"></param>
	/// <param name="options"></param>
	/// <param name="initialPath"></param>
	/// <returns></returns>
	private unsafe Boolean ShowFileDialogCore(IFileDialog* fileDialog, String filter, ref Int32 filterIndex, FILEOPENDIALOGOPTIONS options, Guid? guid, String? initialPath)
	{
		// IShellItem.GetDisplayName() が CoTaskMem なのでみんなそれに合わせる
		List<nint> coTaskMemories = [];
		void* shellInitial = null;

		// finally 用の try
		try
		{
			// GUID
			HRESULT result;
			if (guid.HasValue)
			{
				result = fileDialog->SetClientGuid(guid.Value);
				if (result.Failed)
				{
					Log.Error("ShowFileDialogCore() SetClientGuid: " + guid.Value.ToString());
				}
			}

			// オプション
			if ((options & ~(FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT | FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS)) == 0)
			{
				// FOS_ALLOWMULTISELECT, FOS_PICKFOLDERS のみ指定の場合は、既定のオプションを生かす
				result = fileDialog->GetOptions(out FILEOPENDIALOGOPTIONS defaultOptions);
				if (result.Succeeded)
				{
					options |= defaultOptions;
				}
			}
			result = fileDialog->SetOptions(options);
			if (result.Failed)
			{
				Log.Error("ShowFileDialogCore() SetOptions: " + options.ToString());
			}

			// フィルター
			(String[] filters, Boolean checkFilter) = ShowFileDialogCheckFilter(filter);
			if (checkFilter)
			{
				Int32 numFilters = filters.Length / 2;
				nint specs = Marshal.AllocCoTaskMem(Marshal.SizeOf<COMDLG_FILTERSPEC>() * numFilters);
				coTaskMemories.Add(specs);
				for (Int32 i = 0; i < numFilters; i++)
				{
					(PCWSTR namePcwstr, nint namePtr) = CreateCoMemPcwstr(filters[i * 2]);
					coTaskMemories.Add(namePtr);
					(PCWSTR specPcwstr, nint specPtr) = CreateCoMemPcwstr(filters[i * 2 + 1]);
					coTaskMemories.Add(specPtr);
					COMDLG_FILTERSPEC spec = new() { pszName = namePcwstr, pszSpec = specPcwstr };
					Marshal.StructureToPtr(spec, specs + Marshal.SizeOf<COMDLG_FILTERSPEC>() * i, false);
				}
				result = fileDialog->SetFileTypes((UInt32)numFilters, (COMDLG_FILTERSPEC*)specs);
				if (result.Failed)
				{
					Log.Error("ShowFileDialogCore() SetFileTypes: " + filter);
				}

				// 本関数は 0 オリジンだが openDialog は 1 オリジン
				fileDialog->SetFileTypeIndex((UInt32)filterIndex + 1);
			}

			// 初期ファイル・フォルダー
			if (!String.IsNullOrEmpty(initialPath))
			{
				if (Directory.Exists(initialPath))
				{
					// フォルダーのみ指定
					result = PInvoke.SHCreateItemFromParsingName(initialPath, null, typeof(IShellItem).GUID, out shellInitial);
					if (result.Succeeded)
					{
						fileDialog->SetFolder((IShellItem*)shellInitial);
					}
				}
				else
				{
					// フォルダーとファイルを指定
					String? folderPath = Path.GetDirectoryName(initialPath);
					if (Directory.Exists(folderPath))
					{
						result = PInvoke.SHCreateItemFromParsingName(folderPath, null, typeof(IShellItem).GUID, out shellInitial);
						if (result.Succeeded)
						{
							fileDialog->SetFolder((IShellItem*)shellInitial);
						}
					}
					fileDialog->SetFileName(Path.GetFileName(initialPath));
				}
			}

			// ダイアログを表示
			HWND hWnd = (HWND)WindowNative.GetWindowHandle(this);
			result = fileDialog->Show(hWnd);
			if (result.Failed)
			{
				return false;
			}

			// フィルターインデックス書き戻し
			result = fileDialog->GetFileTypeIndex(out UInt32 filterType);
			if (result.Succeeded)
			{
				filterIndex = (Int32)filterType - 1;
			}
			return true;
		}
		finally
		{
			if (shellInitial != null)
			{
				((IShellItem*)shellInitial)->Release();
			}
			foreach (nint ptr in coTaskMemories)
			{
				Marshal.FreeCoTaskMem(ptr);
			}
		}
	}

	/// <summary>
	/// ShowFileOpenDialog() の共通処理
	/// </summary>
	/// <param name="filter"></param>
	/// <param name="filterIndex"></param>
	/// <param name="options"></param>
	/// <param name="guid"></param>
	/// <param name="initialPath"></param>
	/// <returns></returns>
	private unsafe String[]? ShowFileOpenDialogCore(String filter, ref Int32 filterIndex, FILEOPENDIALOGOPTIONS options, Guid? guid, String? initialPath)
	{
		IFileOpenDialog* openDialog = null;
		IShellItemArray* shellItemArray = null;

		// finally 用の try
		try
		{
			// ダイアログ生成
			HRESULT result = PInvoke.CoCreateInstance(typeof(FileOpenDialog).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out openDialog);
			result.ThrowOnFailure();

			// 表示
			if (!ShowFileDialogCore((IFileDialog*)openDialog, filter, ref filterIndex, options, guid, initialPath))
			{
				return null;
			}

			// 結果取得
			result = openDialog->GetResults(&shellItemArray);
			if (result.Failed)
			{
				return null;
			}
			result = shellItemArray->GetCount(out UInt32 numPathes);
			if (result.Failed)
			{
				return null;
			}
			String[] pathes = new String[numPathes];
			for (UInt32 i = 0; i < numPathes; i++)
			{
				IShellItem* iShellResult;
				result = shellItemArray->GetItemAt(i, &iShellResult);
				if (result.Failed)
				{
					continue;
				}
				String? path = ShellItemToPath(iShellResult, filter, filterIndex);
				iShellResult->Release();
				if (String.IsNullOrEmpty(path))
				{
					continue;
				}
				pathes[i] = path;
			}
			return pathes;
		}
		finally
		{
			if (shellItemArray != null)
			{
				shellItemArray->Release();
			}
			if (openDialog != null)
			{
				openDialog->Release();
			}
		}
	}
#endif

#if USE_UNSAFEz
	/// <summary>
	/// Win32 API ファイル選択ダイアログを開くコア
	/// </summary>
	/// <param name="filter"></param>
	/// <param name="filterIndex"></param>
	/// <param name="initialPath"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	private unsafe String[]? ShowOpenFileDialogCore(String filter, ref Int32 filterIndex, OPEN_FILENAME_FLAGS flags, String? initialPath)
	{
		char* filterPtr = null;
		char* pathPtr = null;

		// finally 用の try
		try
		{
			OPENFILENAMEW openFileNameW = CreateOpenFileNameW(filter, filterIndex, flags, initialPath, ref filterPtr, ref pathPtr);

			// https://learn.microsoft.com/ja-jp/windows/win32/api/commdlg/nf-commdlg-getopenfilenamew
			BOOL result = PInvoke.GetOpenFileName(ref openFileNameW);
			if (!result)
			{
				return null;
			}
			return FileDialogPathes(ref filterIndex, pathPtr, openFileNameW);
		}
		finally
		{
			NativeMemory.Free(pathPtr);
			NativeMemory.Free(filterPtr);
		}
	}

	/// <summary>
	/// Win32 API ファイル保存ダイアログを開くコア
	/// </summary>
	/// <param name="filter"></param>
	/// <param name="filterIndex"></param>
	/// <param name="initialPath"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	private unsafe String[]? ShowSaveFileDialogCore(String filter, ref Int32 filterIndex, OPEN_FILENAME_FLAGS flags, String? initialPath)
	{
		char* filterPtr = null;
		char* pathPtr = null;

		// finally 用の try
		try
		{
			OPENFILENAMEW openFileNameW = CreateOpenFileNameW(filter, filterIndex, flags, initialPath, ref filterPtr, ref pathPtr);

			// https://learn.microsoft.com/ja-jp/windows/win32/api/commdlg/nf-commdlg-getsavefilenamew
			BOOL result = PInvoke.GetSaveFileName(ref openFileNameW);
			if (!result)
			{
				return null;
			}
			return FileDialogPathes(ref filterIndex, pathPtr, openFileNameW);
		}
		finally
		{
			NativeMemory.Free(pathPtr);
			NativeMemory.Free(filterPtr);
		}
	}
#endif

#if USE_AOT && USE_UNSAFE
	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static LRESULT SubclassProcAot(HWND hWnd, UInt32 msg, WPARAM wPalam, LPARAM lParam, nuint _1, nuint _2)
	{
		switch (msg)
		{
			case PInvoke.WM_SYSCOMMAND:
				if ((UInt32)wPalam == PInvoke.SC_CONTEXTHELP)
				{
					if (_hWndMap.TryGetValue(hWnd, out WindowEx2? window))
					{
						window.ShowHelp(window.HelpButtonParameter);
					}
					return (LRESULT)IntPtr.Zero;
				}

				// ヘルプボタン以外は次のハンドラーにお任せ
				return PInvoke.DefSubclassProc(hWnd, msg, wPalam, lParam);
			default:
				// WM_SYSCOMMAND 以外は次のハンドラーにお任せ
				return PInvoke.DefSubclassProc(hWnd, msg, wPalam, lParam);
		}
	}
#endif

#if !USE_AOT
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
#endif

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
