// ============================================================================
// 
// 動作環境を取得する
// Copyright (C) 2014-2016 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/12/06 (Sat) | 作成開始。
//  1.00  | 2014/12/06 (Sat) | オリジナルバージョン。
//  1.10  | 2014/12/28 (Sun) | GetOSVersion() を作成した。
//  1.11  | 2015/05/19 (Tue) | 外部で別項目のログができるよう、LOG_PREFIX_SYSTEM_ENV を public にした。
//  1.12  | 2015/09/12 (Sat) | .NET CLR の情報も記録するようにした。
//  1.13  | 2016/09/17 (Sat) | .NET CLR 4.6 に対応。
//  1.14  | 2016/09/24 (Sat) | LogWriter を使うように変更。
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows.Forms;

namespace Shinta
{
	public class SystemEnvironment
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		public const String LOG_PREFIX_SYSTEM_ENV = "［動作環境］";   // ログの動作環境表記

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
		// CPU 名の取得
		// --------------------------------------------------------------------
		public StatusT GetCpuBrandName(out String oVendorIDString, out String oBrandName)
		{
			oVendorIDString = ManagementValue(WMI_CLASS_PROCESSOR, "Manufacturer");
			oBrandName = ManagementValue(WMI_CLASS_PROCESSOR, "Name");
			return StatusT.Ok;
		}

		// --------------------------------------------------------------------
		// 論理プロセッサ数（スレッド数）の取得
		// --------------------------------------------------------------------
		public StatusT GetNumLogicalProcessors(out Int32 oNumProcessors)
		{
			if (!Int32.TryParse(ManagementValue(WMI_CLASS_PROCESSOR, "NumberOfLogicalProcessors"), out oNumProcessors))
			{
				return StatusT.Error;
			}
			return StatusT.Ok;
		}

		// --------------------------------------------------------------------
		// OS 名の取得
		// --------------------------------------------------------------------
		public StatusT GetOSName(out String oOSName, out Int32 oOSBit)
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
			return StatusT.Ok;
		}

		// --------------------------------------------------------------------
		// OS バージョン番号の取得
		// --------------------------------------------------------------------
		public StatusT GetOSVersion(out Double oOSVersion)
		{
			String aVerAndBuild = ManagementValue(WMI_CLASS_OS, "Version");

			if (!Double.TryParse(aVerAndBuild.Substring(0, aVerAndBuild.LastIndexOf(".")), out oOSVersion))
			{
				return StatusT.Error;
			}
			return StatusT.Ok;
		}

		// --------------------------------------------------------------------
		// 環境をログに記録
		// --------------------------------------------------------------------
		public StatusT LogEnvironment(LogWriter oLogWriter)
		{
			if (oLogWriter == null)
			{
				return StatusT.Error;
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
			// http://stackoverflow.com/questions/12971881/how-to-reliably-detect-the-actual-net-4-5-version-installed
			String aNetVer = Environment.Version.ToString();
			Int32 aNetRev;
			Int32.TryParse(aNetVer.Substring(aNetVer.LastIndexOf('.') + 1), out aNetRev);
			if (aNetVer.IndexOf("4.0.30319.") == 0)
			{
				// .NET 4.x
				if (aNetRev == 0)
				{
					// 詳細不明
					aNetVer += " (Unknown 4.x)";
				}
				else if (aNetRev <= 2034)
				{
					aNetVer += " (4.0)";
				}
				else if (aNetRev <= 18063)
				{
					aNetVer += " (4.5)";
				}
				else if (aNetRev <= 34014)
				{
					aNetVer += " (4.5.1)";
				}
				else if (aNetRev <= 34209)
				{
					aNetVer += " (4.5.2)";
				}
				else if (aNetRev <= 42000)
				{
					aNetVer += " (4.6)";
				}
				else
				{
					// 詳細不明
					aNetVer += " (Unknown 4.x)";
				}
			}
			oLogWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "CLR: " + aNetVer);

			// 自身のパス
			oLogWriter.LogMessage(TraceEventType.Information, LOG_PREFIX_SYSTEM_ENV + "Path: " + Application.ExecutablePath);

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

			return StatusT.Ok;
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


}

