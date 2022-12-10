// ============================================================================
// 
// ウィンドウの拡張
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/12/08 (Thu) | 作成開始。
//  1.00  | 2022/12/08 (Thu) | ファーストバージョン。
// ============================================================================

using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

using Serilog;
using Serilog.Events;

using System.Diagnostics;
using System.Reflection;
using System.Text;

using Windows.Graphics;
using Windows.UI.Popups;

using WinUIEx;

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
	public async Task ShowDialogAsync(WindowEx dialog)
	{
		if (_openingDialog != null)
		{
			throw new Exception("内部エラー：既にダイアログが開いています。");
		}
		_openingDialog = dialog;

		// ディスプレイサイズが不明なのでカスケードしない（はみ出し防止）
		AddVeil();
		dialog.Closed += DialogClosed;
		dialog.AppWindow.Move(new PointInt32(this.AppWindow.Position.X, this.AppWindow.Position.Y));
		dialog.Activate();

		await Task.Run(() =>
		{
			_dialogEvent.WaitOne();
		});
		RemoveVeil();
	}

	/// <summary>
	/// ログの記録と表示
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
		// 開いているダイアログがある場合は閉じる（タスクバーから閉じられた場合などは可能性がある）
		_openingDialog?.Close();
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
		using Stream? stream = assembly.GetManifestResourceStream(DynamicXamlNamespace() + "." + name + Common.FILE_EXT_XAML);
		if (stream == null)
		{
			throw new Exception("内部エラー：コントロールリソースが見つかりません。");
		}
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
	/// イベントハンドラー：メインウィンドウ Activated / Deactivated
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
