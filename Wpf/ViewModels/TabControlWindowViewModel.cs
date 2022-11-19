// ============================================================================
// 
// タブコントロールを持つウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// T はタブコントロールで扱いたい設定の型
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2022/02/06 (Sun) | オリジナルバージョン。
//  1.10  | 2022/02/13 (Sun) | ジェネリッククラスにした。
//  1.20  | 2022/02/20 (Sun) | SelectedTabIndex を作成。
//  1.30  | 2022/02/20 (Sun) | ファイルドロップに対応。
//  1.40  | 2022/03/05 (Sat) | TabIndexOf() を作成。
// (1.41) | 2022/03/06 (Sun) |   設定をクラスメンバーとして保持するようにした。
// ============================================================================

using Livet.Commands;

using System;
using System.Diagnostics;

namespace Shinta.Wpf.ViewModels
{
	internal abstract class TabControlWindowViewModel<T> : BasicWindowViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public TabControlWindowViewModel(T settings, LogWriter? logWriter = null)
				: base(logWriter)
		{
			// タブアイテムの ViewModel 初期化
			_tabItemViewModels = CreateTabItemViewModels();
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				CompositeDisposable.Add(_tabItemViewModels[i]);
			}

			// 設定
			_settings = settings;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 選択されているタブ
		// TabControl.SelectedIndex にバインドされる想定
		private Int32 _selectedTabIndex = -1;
		public Int32 SelectedTabIndex
		{
			get => _selectedTabIndex;
			set
			{
				Int32 prevSelectedTabIndex = _selectedTabIndex;
				if (RaisePropertyChangedIfSet(ref _selectedTabIndex, value))
				{
					SelectedTabIndexChanged(prevSelectedTabIndex, _selectedTabIndex);
				}
			}
		}

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region ファイルドロップの制御
		private ListenerCommand<String[]>? _tabControlFileDropCommand;

		public ListenerCommand<String[]> TabControlFileDropCommand
		{
			get
			{
				if (_tabControlFileDropCommand == null)
				{
					_tabControlFileDropCommand = new ListenerCommand<String[]>(TabControlFileDrop);
				}
				return _tabControlFileDropCommand;
			}
		}

		public void TabControlFileDrop(String[] pathes)
		{
			try
			{
				if (SelectedTabIndex < 0 || SelectedTabIndex >= _tabItemViewModels.Length)
				{
					return;
				}
				_tabItemViewModels[SelectedTabIndex].PathDropped(pathes);
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "タブコントロールファイルドロップ時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// Initialize() との順序を制御するために SettingsToProperties() は派生クラスで呼びだすものとする
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			try
			{
				for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
				{
					_tabItemViewModels[i].Initialize();
				}
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "タブコントロールウィンドウ初期化時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}

		// --------------------------------------------------------------------
		// 何番目のタブアイテムか
		// --------------------------------------------------------------------
		public Int32 TabIndexOf(TabItemViewModel<T> tabItemViewModel)
		{
			return Array.IndexOf(_tabItemViewModels, tabItemViewModel);
		}

		// ====================================================================
		// protected 変数
		// ====================================================================

		// タブアイテムの ViewModel
		protected readonly TabItemViewModel<T>[] _tabItemViewModels;

		// 設定
		protected T _settings;

		// ====================================================================
		// protected 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力されているプロパティーの妥当性を確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		protected override void CheckProperties()
		{
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				_tabItemViewModels[i].CheckProperties();
			}
		}

		// --------------------------------------------------------------------
		// タブアイテムの ViewModel を生成
		// --------------------------------------------------------------------
		protected abstract TabItemViewModel<T>[] CreateTabItemViewModels();

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		protected override void PropertiesToSettings()
		{
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				_tabItemViewModels[i].PropertiesToSettings(_settings);
			}
		}

		// --------------------------------------------------------------------
		// タブ選択が変更された
		// --------------------------------------------------------------------
		protected virtual void SelectedTabIndexChanged(Int32 prevIndex, Int32 newIndex)
		{
			if (0 <= prevIndex && prevIndex < _tabItemViewModels.Length)
			{
				_tabItemViewModels[prevIndex].Deselected();
			}
			if (0 <= newIndex && newIndex < _tabItemViewModels.Length)
			{
				_tabItemViewModels[newIndex].Selected();
			}
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		protected override void SettingsToProperties()
		{
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				_tabItemViewModels[i].SettingsToProperties(_settings);
			}
		}
	}
}
