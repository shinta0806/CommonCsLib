﻿// ============================================================================
// 
// タブアイテムの基底用 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ウィンドウではないため、BaseViewModel を継承しない
// ----------------------------------------------------------------------------

using Livet;

namespace Shinta.ViewModels
{
	internal class TabItemViewModel : ViewModel
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public TabItemViewModel(TabControlWindowViewModel tabControlWindowViewModel, LogWriter? logWriter = null)
		{
			_tabControlWindowViewModel = tabControlWindowViewModel;
			_logWriter = logWriter;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター（Visual Studio・TransitionMessage 用）
		// --------------------------------------------------------------------
		public TabItemViewModel()
		{
			_tabControlWindowViewModel = null!;
		}

		// ====================================================================
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 入力されているプロパティーの妥当性を確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public virtual void CheckProperties()
		{
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public virtual void Initialize()
		{
			_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 初期化中...");
			SettingsToProperties();
		}

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		public virtual void PropertiesToSettings()
		{
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		public virtual void SettingsToProperties()
		{
		}

		// ====================================================================
		// protected 変数
		// ====================================================================

		// ウィンドウのビューモデル
		protected TabControlWindowViewModel _tabControlWindowViewModel;

		// ログ
		protected LogWriter? _logWriter;
	}
}