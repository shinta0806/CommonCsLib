// ============================================================================
// 
// アプリケーション設定を任意のパスに保存する
// Copyright (C) 2014 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 使用時は、ファイル名の設定と設定のリロードが必要
// SomeSettings aSettings = new SomeSettings();
// VariableSettingsProvider aProvider = (VariableSettingsProvider)aSettings.Providers[VariableSettingsProvider.PROVIDER_NAME_VARIABLE_SETTINGS];
// aProvider.FileName = Path.GetDirectoryName(Application.UserAppDataPath) + "\\" + "Some" + Common.FILE_EXT_CONFIG;
// aSettings.Reload();
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/12/28 (Sun) | 作成開始。
//  1.00  | 2014/12/28 (Sun) | オリジナルバージョン。
// ============================================================================

using System;
using System.Collections.Specialized;

namespace Shinta
{
	public class VariableSettingsProvider : ApplicationSettingsProviderBase
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		public const String PROVIDER_NAME_VARIABLE_SETTINGS = "VariableSettingsProvider";
	
		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public VariableSettingsProvider()
		{
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize(String oName, NameValueCollection oConfig)
		{
			// 設定プロバイダ名を設定
			base.Initialize(PROVIDER_NAME_VARIABLE_SETTINGS, oConfig);
		}

		// ====================================================================
		// private 定数
		// ====================================================================

	}
}

