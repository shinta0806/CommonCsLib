// ============================================================================
// 
// アプリケーション設定を XML 形式でファイルに保存するための SettingsProvider 基底クラス
// Copyright (C) 2014-2020 by SHINTA
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
// (1.22) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.23) | 2019/12/22 (Sun) |   null 許容参照型を無効化できるようにした。
// (1.24) | 2020/04/05 (Sun) |   null 許容参照型の対応強化。
// (1.25) | 2020/05/05 (Tue) |   null 許容参照型の対応強化。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

#if !NULLABLE_DISABLED
#nullable enable
#endif

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
			get => Assembly.GetExecutingAssembly().GetName().Name ?? String.Empty;
			set { }
		}

		// 設定の保存先ファイル
#if !NULLABLE_DISABLED
		public String? FileName { get; set; }
#else
		public String FileName 
		{ 
			get; 
			set; 
		}
#endif

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
		public SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property)
		{
			throw new NotImplementedException();
		}

		// --------------------------------------------------------------------
		// プロパティ群をファイルから読み込み
		// --------------------------------------------------------------------
		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection propertyCollection)
		{
			SettingsPropertyValueCollection valueCollection = new SettingsPropertyValueCollection();
			List<SerializableKeyValuePair<String, Object>> pairs = new List<SerializableKeyValuePair<String, Object>>();
			try
			{
				if (String.IsNullOrEmpty(FileName))
				{
					throw new Exception();
				}

				// 逆シリアライズ
				XmlSerializer serializer = new XmlSerializer(pairs.GetType());

				// UTF-8、BOM 無しで読込
				using (StreamReader sr = new StreamReader(FileName, new UTF8Encoding(false)))
				{
					pairs = (List<SerializableKeyValuePair<String, Object>>)serializer.Deserialize(sr);
				}
			}
			catch (Exception)
			{
			}

			// リストから valueCollection にコピー
			SortedDictionary<String, Object> dic = new SortedDictionary<String, Object>();
			foreach (SerializableKeyValuePair<String, Object> pair in pairs)
			{
				if (pair.Key != null && pair.Value != null)
				{
					dic[pair.Key] = pair.Value;
				}
			}
#if !NULLABLE_DISABLED
			foreach (SettingsProperty? property in propertyCollection)
#else
			foreach (SettingsProperty property in propertyCollection)
#endif
			{
				if (property == null)
				{
					continue;
				}
				SettingsPropertyValue value = new SettingsPropertyValue(property);
				value.SerializedValue = dic.ContainsKey(property.Name) ? dic[property.Name] : property.DefaultValue;
				value.IsDirty = false;
				valueCollection.Add(value);
			}

			return valueCollection;
		}

		// --------------------------------------------------------------------
		// IApplicationSettingsProvider 実装
		// プロパティ値をデフォルトに戻す
		// --------------------------------------------------------------------
		public void Reset(SettingsContext context)
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
		public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
		{
			if (String.IsNullOrEmpty(FileName))
			{
				return;
			}

			List<SerializableKeyValuePair<String, Object>> pairs = new List<SerializableKeyValuePair<String, Object>>();
			XmlSerializer serializer = new XmlSerializer(pairs.GetType());

			// collection からリストにコピー
#if !NULLABLE_DISABLED
			foreach (SettingsPropertyValue? value in collection)
#else
			foreach (SettingsPropertyValue value in collection)
#endif
			{
				if (value == null)
				{
					continue;
				}
				pairs.Add(new SerializableKeyValuePair<String, Object>(value.Name, value.SerializedValue));
			}

			// シリアライズ
			try
			{
				// UTF-8、BOM 無しで保存
				using (StreamWriter sw = new StreamWriter(FileName, false, new UTF8Encoding(false)))
				{
					serializer.Serialize(sw, pairs);

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
		public void Upgrade(SettingsContext context, SettingsPropertyCollection properties)
		{
			throw new NotImplementedException();
		}

	}
	// public class ApplicationSettingsProviderBase ___END___

}
// namespace Shinta ___END___

