// ============================================================================
// 
// アプリケーション設定を XML 形式でファイルに保存するための SettingsProvider 基底クラス
// Copyright (C) 2014-2015 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 組み込みの ApplicationSettingsBase で保存場所の指定ができるようにする。
// 最低限の機能のみ。標準の SettingsProvider よりも機能は少ない。
// 派生クラスですべきこと
// ・コンストラクターで FileName を設定
// ・Initialize() で設定名を設定
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/12/16 (Tue) | 作成開始。
//  1.00  | 2014/12/17 (Wed) | オリジナルバージョン。
//  1.10  | 2014/12/22 (Mon) | 保存ファイル名の決定は派生クラスに委譲した。
//  1.20  | 2015/01/12 (Mon) | Reset() を実装した。
// (1.21) | 2015/05/23 (Sat) |   SerializableKeyValuePair を Common に移動した。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Shinta
{
	public class ApplicationSettingsProviderBase : SettingsProvider, IApplicationSettingsProvider
	{
		// ====================================================================
		// public プロパティ
		// ====================================================================

		// 識別用アプリ名（アセンブリ名を使用）
		public override String ApplicationName
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Name;
			}
			set
			{
			}
		}

		// 設定の保存先ファイル
		public String FileName { get; set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public ApplicationSettingsProviderBase()
		{
		}

		// --------------------------------------------------------------------
		// IApplicationSettingsProvider 実装
		// 以前のバージョンの値を返すのが本来だと思うが、未実装
		// --------------------------------------------------------------------
		public SettingsPropertyValue GetPreviousVersion(SettingsContext oContext, SettingsProperty oProperty)
		{
			return null;
		}


		// --------------------------------------------------------------------
		// プロパティ群をファイルから読み込み
		// --------------------------------------------------------------------
		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext oContext, SettingsPropertyCollection oPropertyCollection)
		{
			SettingsPropertyValueCollection aValueCollection = new SettingsPropertyValueCollection();
			List<SerializableKeyValuePair<String, Object>> aPairs = new List<SerializableKeyValuePair<String, Object>>();
			try
			{
				if (String.IsNullOrEmpty(FileName))
				{
					throw new Exception();
				}

				// 逆シリアライズ
				XmlSerializer aSerializer = aSerializer = new XmlSerializer(aPairs.GetType());
				// UTF-8、BOM 無しで読込
				using (StreamReader aSR = new StreamReader(FileName, new UTF8Encoding(false)))
				{
					aPairs = (List<SerializableKeyValuePair<String, Object>>)aSerializer.Deserialize(aSR);
				}
			}
			catch (Exception)
			{
			}

			// リストから aValueCollection にコピー
			SortedDictionary<String, Object> aDic = new SortedDictionary<String, Object>();
			foreach (SerializableKeyValuePair<String, Object> aPair in aPairs)
			{
				if (aPair.Key != null && aPair.Value != null)
				{
					aDic[aPair.Key] = aPair.Value;
				}
			}
			foreach (SettingsProperty aProperty in oPropertyCollection)
			{
				SettingsPropertyValue aValue = new SettingsPropertyValue(aProperty);
				aValue.SerializedValue = aDic.ContainsKey(aProperty.Name) ? aDic[aProperty.Name] : aProperty.DefaultValue;
				aValue.IsDirty = false;
				aValueCollection.Add(aValue);
			}

			return aValueCollection;
		}

		// --------------------------------------------------------------------
		// IApplicationSettingsProvider 実装
		// プロパティ値をデフォルトに戻す
		// --------------------------------------------------------------------
		public void Reset(SettingsContext oContext)
		{
			try
			{
				// ApplicationSettingsBase.Reset() を呼びだすと、IApplicationSettingsProvider.Reset() が
				// 呼ばれた後に、Reload() が呼ばれるので、設定ファイルを削除するだけで、目的は達成される
				File.Delete(FileName);
			}
			catch
			{

			}
		}



		// --------------------------------------------------------------------
		// プロパティ群をファイルに保存
		// --------------------------------------------------------------------
		public override void SetPropertyValues(SettingsContext oContext, SettingsPropertyValueCollection oCollection)
		{
			if (String.IsNullOrEmpty(FileName))
			{
				return;
			}

			List<SerializableKeyValuePair<String, Object>> aPairs = new List<SerializableKeyValuePair<String, Object>>();
			XmlSerializer aSerializer = new XmlSerializer(aPairs.GetType());

			// oCollection からリストにコピー
			foreach (SettingsPropertyValue aValue in oCollection)
			{
				aPairs.Add(new SerializableKeyValuePair<String, Object>(aValue.Name, aValue.SerializedValue));
			}

			// シリアライズ
			try
			{
				// UTF-8、BOM 無しで保存
				using (StreamWriter aSW = new StreamWriter(FileName, false, new UTF8Encoding(false)))
				{
					aSerializer.Serialize(aSW, aPairs);

				}
			}
			catch (Exception)
			{
				return;
			}

		}

		// --------------------------------------------------------------------
		// IApplicationSettingsProvider 実装
		// 以前のバージョンの値を更新するのが本来だと思うが、未実装
		// --------------------------------------------------------------------
		public void Upgrade(SettingsContext oContext, SettingsPropertyCollection oProperties)
		{

		}

	}
	// public class ApplicationSettingsProviderBase ___END___

}
// namespace Shinta ___END___

