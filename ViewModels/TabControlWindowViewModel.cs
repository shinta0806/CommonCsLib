// ============================================================================
// 
// タブコントロールを持つウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// T はタブコントロールで扱いたい設定の型
// ----------------------------------------------------------------------------

using Livet.Commands;

using System;
using System.Diagnostics;

namespace Shinta.ViewModels
{
	internal abstract class TabControlWindowViewModel<T> : BasicWindowViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public TabControlWindowViewModel(LogWriter? logWriter = null)
				: base(logWriter)
		{
			// タブアイテムの ViewModel 初期化
			_tabItemViewModels = CreateTabItemViewModels();
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				CompositeDisposable.Add(_tabItemViewModels[i]);
			}
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
			catch (Exception excep)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "タブコントロールファイルドロップ時エラー：\n" + excep.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// srcSettings を定型化できないために SettingsToProperties() は派生クラスで呼びだすものとする
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
		protected virtual void PropertiesToSettings(T destSettings)
		{
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				_tabItemViewModels[i].PropertiesToSettings(destSettings);
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
		protected virtual void SettingsToProperties(T srcSettings)
		{
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				_tabItemViewModels[i].SettingsToProperties(srcSettings);
			}
		}
	}
}
