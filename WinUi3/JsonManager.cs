// ============================================================================
// 
// JSON 形式で設定の保存と読み込みを管理
// Copyright (C) 2023 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 保存場所は WinUI 3 のテンプレートに倣う
// 同じオプションの場合は JsonSerializerOptions を使い回す必要がある
// https://learn.microsoft.com/ja-jp/dotnet/standard/serialization/system-text-json/configure-options?pivots=dotnet-7-0#reuse-jsonserializeroptions-instances
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2023/08/19 (Sat) | ファーストバージョン。
// ============================================================================

using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Shinta.WinUi3;

internal class JsonManager
{
	// ====================================================================
	// public 関数
	// ====================================================================

#pragma warning disable CA1822
	/// <summary>
	/// 設定を読み込み
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="path">相対パスの場合はデフォルトのフォルダー配下から読み込み</param>
	/// <param name="decompress"></param>
	/// <param name="options">同じオプションの場合は同じインスタンスを渡すこと</param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public T Load<T>(String path, Boolean decompress, JsonSerializerOptions options)
	{
		String json;
		if (decompress)
		{
			json = Decompress(path);
		}
		else
		{
			json = File.ReadAllText(AdjustPath(path), Encoding.UTF8);
		}
		return JsonSerializer.Deserialize<T>(json, options) ?? throw new Exception("設定を復元できませんでした：" + path);
	}

	/// <summary>
	/// 設定を保存
	/// </summary>
	/// <param name="settings"></param>
	/// <param name="path">相対パスの場合はデフォルトのフォルダー配下に保存</param>
	/// <param name="compress"></param>
	/// <param name="options">同じオプションの場合は同じインスタンスを渡すこと</param>
	/// <exception cref="Exception"></exception>
	public void Save(Object settings, String path, Boolean compress, JsonSerializerOptions options)
	{
		String json = JsonSerializer.Serialize(settings, options);
		path = AdjustPath(path);
		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new Exception("設定保存先が不正です：" + path));
		if (compress)
		{
			Compress(json, path);
		}
		else
		{
			File.WriteAllText(path, json, Encoding.UTF8);
		}
	}
#pragma warning restore CA1822

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// Zip ファイル中のエントリ名（特に意味があるわけではない名前なので短くて良い）
	/// </summary>
	private const String FILE_NAME_COMPRESS = "0";

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// 保存先を調整
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private static String AdjustPath(String path)
	{
		if (Path.IsPathRooted(path))
		{
			// 絶対パスの場合はそのパスを使用
			return path;
		}
		else
		{
			// 相対パスの場合は設定フォルダー配下
			return WinUi3Common.SettingsFolder() + path;
		}
	}

	/// <summary>
	/// json を圧縮して保存
	/// </summary>
	/// <param name="json"></param>
	/// <param name="path"></param>
	private static void Compress(String json, String path)
	{
		String folder = Common.TempPath();
		Directory.CreateDirectory(folder);
		File.WriteAllText(folder + "\\" + FILE_NAME_COMPRESS, json, Encoding.UTF8);
		try
		{
			File.Delete(path);
		}
		catch (Exception)
		{
		}
		ZipFile.CreateFromDirectory(folder, path, CompressionLevel.SmallestSize, false);
	}

	/// <summary>
	/// zip から json を展開
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private static String Decompress(String path)
	{
		String folder = Common.TempPath();
		ZipFile.ExtractToDirectory(path, folder);
		return File.ReadAllText(folder + "\\" + FILE_NAME_COMPRESS, Encoding.UTF8);
	}
}
