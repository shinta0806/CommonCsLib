// ============================================================================
// 
// 動作環境を取得する
// Copyright (C) 2022-2024 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Microsoft.Windows.Compatibility
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
// (1.01) | 2023/08/19 (Sat) |   ManagementValue() を改善。
// (1.02) | 2024/04/18 (Thu) |   LogEnvironment() を改善。
// ============================================================================

using System.Management;

using Windows.Storage;

namespace Shinta;

public class SystemEnvironment
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public SystemEnvironment()
	{
	}

	// ====================================================================
	// public 定数
	// ====================================================================

	/// <summary>
	/// ログの動作環境表記
	/// </summary>
	public const String LOG_PREFIX_SYSTEM_ENV = "［動作環境］";

	/// <summary>
	/// ログ項目名
	/// </summary>
	public const String LOG_ITEM_NAME_PATH = "Path: ";

	// ====================================================================
	// public 関数
	// ====================================================================

#pragma warning disable CA1822
	/// <summary>
	/// CPU 名の取得
	/// </summary>
	/// <returns>ベンダー文字列, CPU 名文字列</returns>
	public (String? vendorIdString, String? brandName) GetCpuBrandName()
	{
		return (ManagementValue(WMI_CLASS_PROCESSOR, "Manufacturer"), ManagementValue(WMI_CLASS_PROCESSOR, "Name"));
	}
#pragma warning restore CA1822

	/// <summary>
	/// 環境をログに記録
	/// </summary>
	/// <returns></returns>
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

			// ファミリー（設定が保存されている SHINTA のアプリ）
			try
			{
				List<String> families = new();
				String[] folders;
				if (CommonWindows.IsMsix())
				{
					folders = Directory.GetDirectories(Path.GetDirectoryName(Path.GetDirectoryName(ApplicationData.Current.LocalFolder.Path))!, "22724SHINTA*");
				}
				else
				{
					folders = Directory.GetDirectories(Path.GetDirectoryName(CommonWindows.SettingsFolder())!);
				}
				foreach (String folder in folders)
				{
					String folderName = Path.GetFileName(folder);
					Int32 periPos = folderName.IndexOf('.');
					Int32 underPos = folderName.IndexOf('_', periPos);
					if (periPos >= 0 && underPos >= 0)
					{
						families.Add(folderName[(periPos + 1)..underPos]);
					}
					else
					{
						families.Add(folderName);
					}
				}
				Log.Information(LOG_PREFIX_SYSTEM_ENV + "Family: " + String.Join('、', families));
			}
			catch (Exception ex)
			{
				SerilogUtils.LogException("ファミリー検索時エラー", ex);
			}

			success = true;
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException("環境ログ時エラー", ex);
		}

		return success;
	}

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// WMI CPU 情報取得
	/// </summary>
	private const String WMI_CLASS_PROCESSOR = "Win32_Processor";

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// 指定された情報を取得
	/// </summary>
	/// <param name="className"></param>
	/// <param name="propertyName"></param>
	/// <returns>取得できなかった場合は null</returns>
	private static String? ManagementValue(String className, String propertyName)
	{
		try
		{
			ManagementObjectSearcher searcher = new("SELECT " + propertyName + " FROM " + className);

			// クエリの結果が 1 つのみであることを前提としている
			foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
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
