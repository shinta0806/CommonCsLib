// ============================================================================
// 
// JSON 形式で設定の保存と読み込みを管理
// Copyright (C) 2023-2024 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 保存場所は MSIX かどうかによって変わる
// 同じオプションの場合は JsonSerializerOptions を使い回す必要がある
// https://learn.microsoft.com/ja-jp/dotnet/standard/serialization/system-text-json/configure-options?pivots=dotnet-7-0#reuse-jsonserializeroptions-instances
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2023/08/19 (Sat) | ファーストバージョン。
// (1.01) | 2023/09/17 (Sun) |   Load() のオーバーロードを作成。
// (1.04) | 2024/04/08 (Mon) |   MSIX ではない馬合の保存先を変更。
// (1.05) | 2024/05/20 (Mon) |   Load() のパスを改善。
//  1.10  | 2025/02/08 (Sat) | AOT 対応。
// ============================================================================

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Shinta;

internal class JsonManager
{
	// ====================================================================
	// public 関数
	// ====================================================================

#pragma warning disable CA1822
#if !USE_AOT
	/// <summary>
	/// 設定を読み込み（AOT 非対応）
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="path">相対パスの場合はデフォルトのフォルダー配下から読み込み</param>
	/// <param name="decompress"></param>
	/// <param name="options">同じオプションの場合は同じインスタンスを渡すこと</param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public T Load<T>(String path, Boolean decompress, JsonSerializerOptions? options)
	{
		String json = LoadJsonString(path, decompress);
		return JsonSerializer.Deserialize<T>(json, options) ?? throw new Exception("設定を復元できませんでした：" + path);
	}

	/// <summary>
	/// 設定を読み込み（AOT 非対応）
	/// </summary>
	/// <param name="type"></param>
	/// <param name="path"></param>
	/// <param name="decompress"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public Object Load(Type type, String path, Boolean decompress, JsonSerializerOptions? options)
	{
		String json = LoadJsonString(path, decompress);
		return JsonSerializer.Deserialize(json, type, options) ?? throw new Exception("設定を復元できませんでした：" + path);
	}
#endif

	/// <summary>
	/// 設定を読み込み（AOT 対応）
	/// AOT 非対応版で保存したデータを AOT 対応版で読み込むことは可能
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="path"></param>
	/// <param name="decompress"></param>
	/// <param name="jsonTypeInfo"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public T LoadAot<T>(String path, Boolean decompress, JsonTypeInfo<T> jsonTypeInfo)
	{
		String json = LoadJsonString(path, decompress);
		return JsonSerializer.Deserialize(json, jsonTypeInfo) ?? throw new Exception("設定を復元できませんでした：" + path);
	}

#if !USE_AOT
	/// <summary>
	/// 設定を保存（AOT 非対応）
	/// </summary>
	/// <param name="settings"></param>
	/// <param name="path">相対パスの場合はデフォルトのフォルダー配下に保存</param>
	/// <param name="compress"></param>
	/// <param name="options">同じオプションの場合は同じインスタンスを渡すこと</param>
	/// <exception cref="Exception"></exception>
	public void Save(Object settings, String path, Boolean compress, JsonSerializerOptions? options = default)
	{
		String json = JsonSerializer.Serialize(settings, options);
		SaveJsonString(json, path, compress);
	}
#endif

	/// <summary>
	/// 設定を保存（AOT 対応）
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="settings"></param>
	/// <param name="path"></param>
	/// <param name="decompress"></param>
	/// <param name="jsonTypeInfo"></param>
	public void SaveAot<T>(Object settings, String path, Boolean compress, JsonTypeInfo<T> jsonTypeInfo)
	{
		String json = JsonSerializer.Serialize(settings, jsonTypeInfo);
		SaveJsonString(json, path, compress);
	}
#pragma warning restore CA1822

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// zip ファイル中のエントリ名（特に意味があるわけではない名前なので短くて良い）
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
			return CommonWindows.SettingsFolder() + path;
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

	/// <summary>
	/// JSON 文字列の読み込み
	/// </summary>
	/// <param name="path"></param>
	/// <param name="decompress"></param>
	/// <returns></returns>
	private static String LoadJsonString(String path, Boolean decompress)
	{
		path = AdjustPath(path);
		if (decompress)
		{
			return Decompress(path);
		}
		else
		{
			return File.ReadAllText(path, Encoding.UTF8);
		}
	}

	/// <summary>
	/// JSON 文字列の保存
	/// </summary>
	/// <param name="json"></param>
	/// <param name="path"></param>
	/// <param name="compress"></param>
	private static void SaveJsonString(String json, String path, Boolean compress)
	{
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
}
