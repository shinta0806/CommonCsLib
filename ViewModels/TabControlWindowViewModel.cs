﻿// ============================================================================
// 
// タブコントロールを持つウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Shinta.ViewModels
{
	internal abstract class TabControlWindowViewModel : BasicWindowViewModel
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
		// public 関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
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

				SettingsToProperties();
			}
			catch (Exception ex)
			{
				_logWriter?.ShowLogMessage(TraceEventType.Error, "タブコントロールウィンドウ初期化時エラー：\n" + ex.Message);
				_logWriter?.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + ex.StackTrace);
			}
		}

		// ====================================================================
		// protected 変数
		// ====================================================================

		// タブアイテムの ViewModel
		protected readonly TabItemViewModel[] _tabItemViewModels;

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
		protected abstract TabItemViewModel[] CreateTabItemViewModels();

		// --------------------------------------------------------------------
		// プロパティーから設定に反映
		// --------------------------------------------------------------------
		protected override void PropertiesToSettings()
		{
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				_tabItemViewModels[i].PropertiesToSettings();
			}
		}

		// --------------------------------------------------------------------
		// 設定をプロパティーに反映
		// --------------------------------------------------------------------
		protected override void SettingsToProperties()
		{
			for (Int32 i = 0; i < _tabItemViewModels.Length; i++)
			{
				_tabItemViewModels[i].SettingsToProperties();
			}
		}
	}
}