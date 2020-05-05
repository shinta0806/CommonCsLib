// ============================================================================
// 
// 動作環境を取得する
// Copyright (C) 2014-2020 by SHINTA
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
// ============================================================================

using Microsoft.Win32;

using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;

#if !NULLABLE_DISABLED
#nullable enable
#endif

namespace Shinta
{
	public class SystemEnvironment
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		// ログの動作環境表記
		public const String LOG_PREFIX_SYSTEM_ENV = "［動作環境］";   

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public SystemEnvironment()
		{
		}

		// --------------------------------------------------------------------
		// CLR バージョン名の取得（4.5 以降のみ対応）
		// https://docs.microsoft.com/ja-jp/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#net_d
		// --------------------------------------------------------------------
		public Boolean GetClrVersionName(out String oClrVersion)
		{
			Int32 aRegistry;
			if (!GetClrVersionRegistryNumber(out aRegistry))
			{
				oClrVersion = "4.5 or ealier";
				return false;
			}

			if (aRegistry >= 461308)
			{
				oClrVersion = "4.7.1 or later";
			}
			else if (aRegistry >= 460798)
			{
				oClrVersion = "4.7";
			}
			else if (aRegistry >= 394802)
			{
				oClrVersion = "4.6.2";
			}
			else if (aRegistry >= 394254)
			{
				oClrVersion = "4.6.1";
			}
			else if (aRegistry >= 393295)
			{
				oClrVersion = "4.6";
			}
			else if (aRegistry >= 379893)
			{
				oClrVersion = "4.5.2";
			}
			else if (aRegistry >= 378675)
			{
				oClrVersion = "4.5.1";
			}
			else if (aRegistry >= 378389)
			{
				oClrVersion = "4.5";
			}
			else
			{
				// 仕様上はここに到達することはない
				oClrVersion = "Unknown";
				return false;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// CLR バージョンレジストリ番号の取得（4.5 以降のみ対応）
		// https://docs.microsoft.com/ja-jp/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#net_d
		// --------------------------------------------------------------------
		public Boolean GetClrVersionRegistryNumber(out Int32 oClrVersion)
		{
			using (RegistryKey aKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
			{
				if (aKey != null && aKey.GetValue("Release") != null)
				{
					oClrVersion = (Int32)aKey.GetValue("Release");
					return true;
				}
				else
				{
					oClrVersion = 0;
					return false;
				}
			}
		}

		// --------------------------------------------------------------------
		// CPU 名の取得
		// --------------------------------------------------------------------
		public Boolean GetCpuBrandName(out String oVendorIDString, out String oBrandName)
		{
			oVendorIDString = ManagementValue(WMI_CLASS_PROCESSOR, "Manufacturer");
			oBrandName = ManagementValue(WMI_CLASS_PROCESSOR, "Name");
			return true;
		}

		// --------------------------------------------------------------------
		// 論理プロセッサ数（スレッド数）の取得
		// --------------------------------------------------------------------
		public Boolean GetNumLogicalProcessors(out Int32 oNumProcessors)
		{
			if (!Int32.TryParse(ManagementValue(WMI_CLASS_PROCESSOR, "NumberOfLogicalProcessors"), out oNumProcessors))
			{
				return false;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// OS 名の取得
		// --------------------------------------------------------------------
		public Boolean GetOSName(out String oOSName, out Int32 oOSBit)
		{
			oOSName = ManagementValue(WMI_CLASS_OS, "Caption");
			if (Environment.Is64BitOperatingSystem)
			{
				oOSBit = 64;
			}
			else
			{
				oOSBit = 32;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// OS バージョン番号の取得
		// --------------------------------------------------------------------
		public Boolean GetOSVersion(out Double oOSVersion)
		{
			String aVerAndBuild = ManagementValue(WMI_CLASS_OS, "Version");

			if (!Double.TryParse(aVerAndBuild.Substring(0, aVerAndBuild.LastIndexOf(".")), out oOSVersion))
			{
				return false;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// 環境をログに記録
		// --------------------------------------------------------------------
		public Boolean LogEnvironment(LogWriter oLogWriter)
		{
			if (oLogWriter == null)
			{
				return false;
			}

			// CPU 情報
			Int32 aNumProcessors;
			String aCpuVendor;
			String aCpuName;
			GetCpuBrandName(out aCpuVendor, out aCpuName);
			GetNumLogicalProcessors(out aNumProcessors);
			oLogWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "CPU: " + aCpuVendor
					+ " / " + aCpuName + " / " + aNumProcessors + " スレッド");

			// OS 情報
			Int32 aOSBit;
			String aOSName;
			GetOSName(out aOSName, out aOSBit);
			oLogWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "OS: " + aOSName
					+ "（" + aOSBit.ToString() + " ビット）");

			// .NET CLR 情報
			Int32 aClrVerNum;
			GetClrVersionRegistryNumber(out aClrVerNum);
			String aClrVerName;
			GetClrVersionName(out aClrVerName);
			oLogWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "CLR: " + Environment.Version.ToString()
					+ " / " + aClrVerNum.ToString() + " (" + aClrVerName + ")");

			// 自身のパス
			oLogWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "Path: " + Assembly.GetEntryAssembly().Location);

			// ファミリー
			try
			{
				String aFamily = String.Empty;
				String[] aFolders;
				aFolders = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
						+ "\\" + Common.FOLDER_NAME_SHINTA);
				foreach (String aFolder in aFolders)
				{
					if (!String.IsNullOrEmpty(aFamily))
					{
						aFamily += "、";
					}
					String aFolderName = Path.GetFileName(aFolder);
					Int32 aPeriPos = aFolderName.IndexOf(".");
					if (aPeriPos < 0)
					{
						aFamily += aFolderName;
					}
					else
					{
						aFamily += aFolderName.Substring(0, aPeriPos);
					}
				}
				oLogWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "Family: " + aFamily);
			}
			catch (Exception)
			{
			}

			return true;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		private const String WMI_CLASS_OS = "Win32_OperatingSystem";    // WMI OS 情報取得
		private const String WMI_CLASS_PROCESSOR = "Win32_Processor";   // WMI CPU 情報取得

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 指定された CPU の情報を取得
		// --------------------------------------------------------------------
		private String ManagementValue(String oClass, String oName)
		{
			ManagementObjectSearcher aSearcher = new ManagementObjectSearcher("SELECT " + oName + " FROM " + oClass);

			// クエリの結果が 1 つのみであることを前提としている
			foreach (ManagementObject aObj in aSearcher.Get())
			{
				foreach (PropertyData aProperty in aObj.Properties)
				{
					return aProperty.Value.ToString();
				}
			}
			return String.Empty;
		}
	}
	// public class SystemEnvironment ___END___

}
// namespace Shinta ___END___

