// ============================================================================
// 
// 動作環境を取得する
// Copyright (C) 2014-2022 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/12/06 (Sat) | 作成開始。
//  1.00  | 2014/12/06 (Sat) | オリジナルバージョン。
//  1.10  | 2014/12/28 (Sun) | GetOSVersion() を作成した。
// (1.11) | 2015/05/19 (Tue) | 外部で別項目のログができるよう、LOG_PREFIX_SYSTEM_ENV を public にした。
// (1.12) | 2015/09/12 (Sat) | .NET CLR の情報も記録するようにした。
// (1.13) | 2016/09/17 (Sat) | .NET CLR 4.6 に対応。
// (1.14) | 2016/09/24 (Sat) | LogWriter を使うように変更。
// (1.15) | 2017/11/18 (Sat) | StatusT の使用を廃止。
//  1.20  | 2018/04/02 (Mon) | GetClrVersionRegistryNumber() を作成した。
//  1.30  | 2018/04/02 (Mon) | GetClrVersionName() を作成した。
// (1.31) | 2019/01/20 (Sun) | WPF アプリケーションでも使用可能にした。
// (1.32) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.33) | 2020/05/05 (Tue) |   null 許容参照型を無効化できるようにした。
// (1.34) | 2020/06/27 (Sat) |   null 許容参照型の対応強化。
// (1.35) | 2020/06/27 (Sat) |   GetClrVersionName() の強化。
// (1.36) | 2020/11/15 (Sun) |   null 許容参照型の対応強化。
// (1.37) | 2022/01/09 (Sun) |   LogEnvironment() を改善。
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace Shinta
{
	public class SystemEnvironment
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public SystemEnvironment()
		{
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		// ログの動作環境表記
		public const String LOG_PREFIX_SYSTEM_ENV = "［動作環境］";

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// CPU 名の取得
		// ＜返値＞ ベンダー文字列, CPU 名文字列
		// --------------------------------------------------------------------
		public (String? vendorIdString, String? brandName) GetCpuBrandName()
		{
			return (ManagementValue(WMI_CLASS_PROCESSOR, "Manufacturer"), ManagementValue(WMI_CLASS_PROCESSOR, "Name"));
		}

		// --------------------------------------------------------------------
		// 環境をログに記録
		// --------------------------------------------------------------------
		public Boolean LogEnvironment(LogWriter? logWriter)
		{
			if (logWriter == null)
			{
				return false;
			}

			Boolean success = false;
			try
			{
				// CPU 情報
				(String? cpuVendor, String? cpuName) = GetCpuBrandName();
				logWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "CPU: " + cpuVendor + " / " + cpuName
						+ " / " + Environment.ProcessorCount + " スレッド");

				// OS 情報
				Int32 osBit;
				if (Environment.Is64BitOperatingSystem)
				{
					osBit = 64;
				}
				else
				{
					osBit = 32;
				}
				logWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "OS: " + Environment.OSVersion.VersionString
						+ "（" + osBit.ToString() + " ビット）");

				// .NET CLR 情報
				logWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "CLR: " + Environment.Version.ToString());

				// 自身のパス
				logWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "Path: " + Environment.GetCommandLineArgs()[0]);

				// ファミリー
				try
				{
					String family = String.Empty;
					String[] folders;
					folders = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
							+ "\\" + Common.FOLDER_NAME_SHINTA);
					foreach (String folder in folders)
					{
						if (!String.IsNullOrEmpty(family))
						{
							family += "、";
						}
						String folderName = Path.GetFileName(folder);
						Int32 periPos = folderName.IndexOf(".");
						if (periPos < 0)
						{
							family += folderName;
						}
						else
						{
							family += folderName[0..periPos];
						}
					}
					logWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "Family: " + family);
				}
				catch (Exception)
				{
				}

				success = true;
			}
			catch (Exception)
			{
			}

			return success;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		private const String WMI_CLASS_OS = "Win32_OperatingSystem";    // WMI OS 情報取得
		private const String WMI_CLASS_PROCESSOR = "Win32_Processor";   // WMI CPU 情報取得

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

#pragma warning disable CA1822
		// --------------------------------------------------------------------
		// 指定された情報を取得
		// ＜返値＞ 取得できなかった場合は null
		// --------------------------------------------------------------------
		private static String? ManagementValue(String className, String propertyName)
		{
			try
			{
				ManagementObjectSearcher searcher = new("SELECT " + propertyName + " FROM " + className);

				// クエリの結果が 1 つのみであることを前提としている
				foreach (ManagementObject obj in searcher.Get())
				{
					foreach (PropertyData property in obj.Properties)
					{
						return property.Value.ToString();
					}
				}
			}
			catch (Exception)
			{
				// MSIX の x64 化がうまくいっていないと ManagementObjectSearcher を生成できない
			}
			return null;
		}
#pragma warning restore CA1822
	}
}
