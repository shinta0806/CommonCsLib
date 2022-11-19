// ============================================================================
// 
// ウィンドウの基底用 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 基礎的な OK、キャンセル処理に対応
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2022/02/06 (Sun) | オリジナルバージョン。
//  1.10  | 2022/02/20 (Sun) | PathByOpeningDialog() を作成。
//  1.20  | 2022/02/20 (Sun) | PathBySavingDialog を作成。
// (1.21) | 2022/03/05 (Sat) |   PathByOpeningDialog() 等を仮想化。
// ============================================================================

using Livet;
using Livet.Commands;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Shinta.Wpf.ViewModels
{
	internal class BasicWindowViewModel : ViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public BasicWindowViewModel(LogWriter? logWriter = null)
		{
			_logWriter = logWriter;
			_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 生成中...");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ウィンドウタイトル（デフォルトが null だと実行時にエラーが発生するので Empty にしておく）
		// Window.Title にバインドされる想定
		private String _title = String.Empty;
		public String Title
		{
			get => _title;
			set
			{
				String title = value;
#if DEBUG
				title = "［デバッグ］" + title;
#endif
#if TEST
				title = "［テスト］" + title;
#endif
				RaisePropertyChangedIfSet(ref _title, title);
			}
		}

		// ウィンドウ左端
		// Window.Left に TwoWay でバインドされる想定
		private Double _left;
		public Double Left
		{
			get => _left;
			set => RaisePropertyChangedIfSet(ref _left, value);
		}

		// ウィンドウ上端
		// Window.Top に TwoWay でバインドされる想定
		private Double _top;
		public Double Top
		{
			get => _top;
			set => RaisePropertyChangedIfSet(ref _top, value);
		}

		// ウィンドウ幅
		// Window.Width に TwoWay でバインドされる想定
		private Double _width;
		public Double Width
		{
			get => _width;
			set => RaisePropertyChangedIfSet(ref _width, value);
		}

		// ウィンドウ高さ
		// Window.Height に TwoWay でバインドされる想定
		private Double _height;
		public Double Height
		{
			get => _height;
			set => RaisePropertyChangedIfSet(ref _height, value);
		}

		// カーソル
		// Window.Cursor にバインドされる想定
		private Cursor? _cursor;
		public Cursor? Cursor
		{
			get => _cursor;
			set => RaisePropertyChangedIfSet(ref _cursor, value);
		}

		// アクティブ
		// WindowBindingSupportBehavior.IsActive にバインドされる想定
		private Boolean _isActive;
		public Boolean IsActive
		{
			get => _isActive;
			set => RaisePropertyChangedIfSet(ref _isActive, value);
		}

		// OK ボタンフォーカス
		// OK ボタンの IsFocusedAttachedBehavior.IsFocused にバインドされる想定
		private Boolean _isButtonOkFocused;
		public Boolean IsButtonOkFocused
		{
			get => _isButtonOkFocused;
			set
			{
				// 再度フォーカスを当てられるように強制伝播
				_isButtonOkFocused = value;
				RaisePropertyChanged(nameof(IsButtonOkFocused));
			}
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// OK、Cancel、Yes、No 等の結果
		public MessageBoxResult Result { get; protected set; } = MessageBoxResult.None;

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region OK ボタンの制御
		private ViewModelCommand? _buttonOkClickedCommand;

		public ViewModelCommand ButtonOkClickedCommand
		{
			get
			{
				if (_buttonOkClickedCommand == null)
				{
					_buttonOkClickedCommand = new ViewModelCommand(ButtonOkClicked);
				}
				return _buttonOkClickedCommand;
			}
		}

		public void ButtonOkClicked()
		{
			try
			{
				// Enter キーでボタンが押された場合はテキストボックスからフォーカスが移らずプロパティーが更新されないため強制フォーカス
				IsButtonOkFocused = true;

				CheckProperties();
				PropertiesToSettings();
				SaveSettings();
				Result = MessageBoxResult.OK;
				Close();
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "OK ボタンクリック時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ウィンドウをアクティブ化する
		// IsActive がバインドされている必要がある
		// --------------------------------------------------------------------
		public virtual void Activate()
		{
			try
			{
				IsActive = true;
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "アクティブ化時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// ウィンドウを閉じる
		// ビューの InteractionMessageTrigger が設定されている必要がある
		// --------------------------------------------------------------------
		public virtual void Close()
		{
			Messenger.Raise(new WindowActionMessage(Common.MESSAGE_KEY_WINDOW_CLOSE));
		}

		// --------------------------------------------------------------------
		// 初期化
		// Initialize() との順序を制御するために SettingsToProperties() は派生クラスで呼びだすものとする
		// --------------------------------------------------------------------
		public virtual void Initialize()
		{
			_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 初期化中...");
		}

		// --------------------------------------------------------------------
		// 開くダイアログを表示し、ファイルパスを取得
		// ビューの InteractionMessageTrigger が設定されている必要がある
		// --------------------------------------------------------------------
		public virtual String? PathByOpeningDialog(String title, String filter, String? path)
		{
			OpeningFileSelectionMessage message = new(Common.MESSAGE_KEY_OPEN_OPEN_FILE_DIALOG);
			message.Title = title;
			message.Filter = filter + ADDITIONAL_FILTER;
			if (!String.IsNullOrEmpty(path))
			{
				message.InitialDirectory = Path.GetDirectoryName(path);
				message.FileName = Path.GetFileName(path);
			}
			Messenger.Raise(message);
			if (message.Response == null)
			{
				return null;
			}

			return message.Response[0];
		}

		// --------------------------------------------------------------------
		// 保存ダイアログを表示し、ファイルパスを取得
		// ビューの InteractionMessageTrigger が設定されている必要がある
		// --------------------------------------------------------------------
		public virtual String? PathBySavingDialog(String title, String filter, String? path)
		{
			SavingFileSelectionMessage message = new(Common.MESSAGE_KEY_OPEN_SAVE_FILE_DIALOG);
			message.Title = title;
			message.Filter = filter + ADDITIONAL_FILTER;
			if (!String.IsNullOrEmpty(path))
			{
				message.InitialDirectory = Path.GetDirectoryName(path);
				message.FileName = Path.GetFileName(path);
			}
			Messenger.Raise(message);
			if (message.Response == null)
			{
				return null;
			}

			return message.Response[0];
		}

		// ====================================================================
		// protected 変数
		// ====================================================================

		// ログ
		protected LogWriter? _logWriter;

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力されているプロパティーの妥当性を確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected virtual void CheckProperties()
		{
		}

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			base.Dispose(isDisposing);

			_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 破棄中...");
		}

		// --------------------------------------------------------------------
		// プロパティーを設定に反映
		// --------------------------------------------------------------------
		protected virtual void PropertiesToSettings()
		{
		}

		// --------------------------------------------------------------------
		// 設定を保存
		// --------------------------------------------------------------------
		protected virtual void SaveSettings()
		{
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		protected virtual void SettingsToProperties()
		{
		}

		// ====================================================================
		// protected 定数
		// ====================================================================

		// 開くダイアログ・保存ダイアログ用の追加フィルター
		protected const String ADDITIONAL_FILTER = "|すべてのファイル|*.*";
	}
}
