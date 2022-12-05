// ============================================================================
// 
// 動作環境を取得する
// Copyright (C) 2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Serilog.Sinks.File
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
//  
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/12/03 (Sat) | WPF 版を元に作成開始。
//  1.00  | 2022/12/03 (Sat) | ファーストバージョン。
// ============================================================================

using Serilog;

using System.Management;

namespace Shinta.WinUi3
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

		// ログ項目名
		public const String LOG_ITEM_NAME_PATH = "Path: ";

		// ====================================================================
		// public メンバー関数
		// ====================================================================

#pragma warning disable CA1822
		// --------------------------------------------------------------------
		// CPU 名の取得
		// ＜返値＞ ベンダー文字列, CPU 名文字列
		// --------------------------------------------------------------------
		public (String? vendorIdString, String? brandName) GetCpuBrandName()
		{
			return (ManagementValue(WMI_CLASS_PROCESSOR, "Manufacturer"), ManagementValue(WMI_CLASS_PROCESSOR, "Name"));
		}
#pragma warning restore CA1822

		// --------------------------------------------------------------------
		// 環境をログに記録
		// --------------------------------------------------------------------
		public Boolean LogEnvironment()
		{
			Boolean success = false;
			try
			{
				// CPU 情報
				(String? cpuVendor, String? cpuName) = GetCpuBrandName();
				Log.Information(LOG_PREFIX_SYSTEM_ENV + "CPU: " + cpuVendor + " / " + cpuName + " / " + Environment.ProcessorCount + " スレッド");

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
				Log.Information(LOG_PREFIX_SYSTEM_ENV + "OS: " + Environment.OSVersion.VersionString + "（" + osBit.ToString() + " ビット）");

				// .NET CLR 情報
				Log.Information(LOG_PREFIX_SYSTEM_ENV + "CLR: " + Environment.Version.ToString());

				// 自身のパス
				Log.Information(LOG_PREFIX_SYSTEM_ENV + LOG_ITEM_NAME_PATH + Environment.GetCommandLineArgs()[0]);

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
					Log.Information(LOG_PREFIX_SYSTEM_ENV + "Family: " + family);
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

		// WMI CPU 情報取得
		private const String WMI_CLASS_PROCESSOR = "Win32_Processor";

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

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
	}
}
