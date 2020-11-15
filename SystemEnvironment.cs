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
// (1.34) | 2020/06/27 (Sat) |   null 許容参照型の対応強化。
// (1.35) | 2020/06/27 (Sat) |   GetClrVersionName() の強化。
// (1.36) | 2020/11/15 (Sun) |   null 許容参照型の対応強化。
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
		public Boolean GetClrVersionName(out String clrVersion)
		{
			Int32 registry;
			if (!GetClrVersionRegistryNumber(out registry))
			{
				clrVersion = "4.5 or ealier";
				return false;
			}

			if (registry >= 528209)
			{
				clrVersion = "4.8 or later";
			}
			else if (registry >= 461808)
			{
				clrVersion = "4.7.2";
			}
			else if (registry >= 461308)
			{
				clrVersion = "4.7.1";
			}
			else if (registry >= 460798)
			{
				clrVersion = "4.7";
			}
			else if (registry >= 394802)
			{
				clrVersion = "4.6.2";
			}
			else if (registry >= 394254)
			{
				clrVersion = "4.6.1";
			}
			else if (registry >= 393295)
			{
				clrVersion = "4.6";
			}
			else if (registry >= 379893)
			{
				clrVersion = "4.5.2";
			}
			else if (registry >= 378675)
			{
				clrVersion = "4.5.1";
			}
			else if (registry >= 378389)
			{
				clrVersion = "4.5";
			}
			else
			{
				// 仕様上はここに到達することはない
				clrVersion = "Unknown";
				return false;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// CLR バージョンレジストリ番号の取得（4.5 以降のみ対応）
		// https://docs.microsoft.com/ja-jp/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#net_d
		// --------------------------------------------------------------------
		public Boolean GetClrVersionRegistryNumber(out Int32 clrVersion)
		{
			clrVersion = 0;
			using RegistryKey? key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\");
			if (key == null)
			{
				return false;
			}
			Object? value = key.GetValue("Release");
			if (value == null)
			{
				return false;
			}

			clrVersion = (Int32)value;
			return true;
		}

		// --------------------------------------------------------------------
		// CPU 名の取得
		// --------------------------------------------------------------------
		public Boolean GetCpuBrandName(out String vendorIdString, out String brandName)
		{
			vendorIdString = ManagementValue(WMI_CLASS_PROCESSOR, "Manufacturer");
			brandName = ManagementValue(WMI_CLASS_PROCESSOR, "Name");
			return true;
		}

		// --------------------------------------------------------------------
		// 論理プロセッサ数（スレッド数）の取得
		// --------------------------------------------------------------------
		public Boolean GetNumLogicalProcessors(out Int32 numProcessors)
		{
			if (!Int32.TryParse(ManagementValue(WMI_CLASS_PROCESSOR, "NumberOfLogicalProcessors"), out numProcessors))
			{
				return false;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// OS 名の取得
		// --------------------------------------------------------------------
		public Boolean GetOSName(out String osName, out Int32 osBit)
		{
			osName = ManagementValue(WMI_CLASS_OS, "Caption");
			if (Environment.Is64BitOperatingSystem)
			{
				osBit = 64;
			}
			else
			{
				osBit = 32;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// OS バージョン番号の取得
		// --------------------------------------------------------------------
		public Boolean GetOSVersion(out Double osVersion)
		{
			String aVerAndBuild = ManagementValue(WMI_CLASS_OS, "Version");

			if (!Double.TryParse(aVerAndBuild.Substring(0, aVerAndBuild.LastIndexOf(".")), out osVersion))
			{
				return false;
			}
			return true;
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
				Int32 numProcessors;
				String cpuVendor;
				String cpuName;
				GetCpuBrandName(out cpuVendor, out cpuName);
				GetNumLogicalProcessors(out numProcessors);
				logWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "CPU: " + cpuVendor
						+ " / " + cpuName + " / " + numProcessors + " スレッド");

				// OS 情報
				Int32 osBit;
				String osName;
				GetOSName(out osName, out osBit);
				logWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "OS: " + osName
						+ "（" + osBit.ToString() + " ビット）");

				// .NET CLR 情報
				Int32 clrVerNum;
				GetClrVersionRegistryNumber(out clrVerNum);
				String clrVerName;
				GetClrVersionName(out clrVerName);
				logWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "CLR: " + Environment.Version.ToString()
						+ " / " + clrVerNum.ToString() + " (" + clrVerName + ")");

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
							family += folderName.Substring(0, periPos);
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
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 指定された情報を取得
		// --------------------------------------------------------------------
		private String ManagementValue(String className, String propertyName)
		{
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT " + propertyName + " FROM " + className);

			// クエリの結果が 1 つのみであることを前提としている
			foreach (ManagementObject obj in searcher.Get())
			{
				foreach (PropertyData property in obj.Properties)
				{
					return property.Value.ToString() ?? String.Empty;
				}
			}
			return String.Empty;
		}
	}
	// public class SystemEnvironment ___END___

}
// namespace Shinta ___END___

