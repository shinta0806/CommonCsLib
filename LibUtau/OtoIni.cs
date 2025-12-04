// ============================================================================
// 
// oto.ini を管理するクラス
// Copyright (C) 2024-2025 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2024/04/22 (Mon) | C++ 版 Lib UTAU から移植。
// (1.01) | 2024/12/05 (Thu) |   namespace を変更。
// (1.02) | 2024/12/07 (Sat) |   FileName の不具合を修正。
// (1.03) | 2025/12/04 (Thu) |   原音設定の保持を SortedDictionary から Dictoinary に変更。
// (1.04) | 2025/12/04 (Thu) |   原音設定の保持を後優先から先優先に変更。
// ============================================================================

using System.Text;

namespace Shinta.LibUtau;

internal class OtoIni
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public OtoIni()
	{
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 原音設定の解析結果（エイリアス文字と原音設定の対応）
	/// </summary>
	public Dictionary<String, GenonSettings> GenonSettings => _genonSettings;

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// oto.ini を読み込む
	/// </summary>
	/// <param name="otoIniPath"></param>
	/// <param name="recursive"></param>
	/// <returns>0: 正常, -1: 異常</returns>
	public Int32 SetTo(String otoIniPath, Boolean recursive = true)
	{
		// 既存のデータをクリア
		_genonSettings.Clear();

		// oto.ini を検索
		// 将来的に oto.ini 以外のファイル名が許容される場合に備えて、
		// 検索ファイル名は oto.ini 決め打ちではなく、otoIniPath で指定されたファイル名とする
		String? folder = Path.GetDirectoryName(otoIniPath);
		if (String.IsNullOrEmpty(folder))
		{
			return -1;
		}
		String[] otoInis = Directory.GetFiles(folder, Path.GetFileName(otoIniPath), recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

		// 読込
		foreach (String oneIni in otoInis)
		{
			// 個々の oto.ini ではエラー判定しない（ルートが 0 バイトの oto.ini ということもあるため）
			SetToCore(oneIni);
		}

		if (_genonSettings.Count == 0)
		{
			return -1;
		}
		return 0;
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// 原音設定の解析結果（エイリアス文字と原音設定の対応）
	/// </summary>
	private readonly Dictionary<String, GenonSettings> _genonSettings = new();

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// 1 つの oto.ini を読み込む
	/// </summary>
	/// <param name="otoIniPath"></param>
	/// <returns></returns>
	private Int32 SetToCore(String otoIniPath)
	{
		try
		{
			String[] lines = File.ReadAllLines(otoIniPath, Encoding.GetEncoding(Common.CODE_PAGE_SHIFT_JIS));

			// ファイル名＋エイリアスで容量確保
			_genonSettings.EnsureCapacity(_genonSettings.Count + lines.Length * 2);

			foreach (String line in lines)
			{
				if (String.IsNullOrEmpty(line))
				{
					continue;
				}

				// oto.ini の一行：_ああいあう.wav=- あE4,473,450,-660,300,100
				// プレフィックス（という名のサフィックス）はエイリアス部分に付いている前提（多音階でも付いていない音源もある？）
				String[] settingsStrings = line.Split(',');
				String[] nameStrings = settingsStrings[0].Split('=');
				String? alias;
				if (nameStrings.Length >= 2)
				{
					// "=" のすぐ右がエイリアス
					alias = nameStrings[1];
				}
				else
				{
					// エイリアスの指定が無い
					alias = null;
				}
				// ピリオドの左側がファイル名
				String fileBody = nameStrings[0].Split('.')[0];

				// 原音パラメータをファイル名とエイリアス名の両方で登録する
				if (settingsStrings.Length < 6)
				{
					Array.Resize(ref settingsStrings, 6);
				}
				Double doubleValue;
				GenonSettings genonSettings = new()
				{
					// ファイル名
					FileName = Path.GetDirectoryName(otoIniPath) + '\\' + nameStrings[0]
				};

				// オフセット
				_ = Double.TryParse(settingsStrings[1], out doubleValue);
				genonSettings.Offset = doubleValue;

				// 子音速度
				_ = Double.TryParse(settingsStrings[2], out doubleValue);
				genonSettings.Shiin = doubleValue;

				// 右ブランク
				_ = Double.TryParse(settingsStrings[3], out doubleValue);
				genonSettings.Blank = doubleValue;

				// 先行発声
				_ = Double.TryParse(settingsStrings[4], out doubleValue);
				genonSettings.PreUtterance = doubleValue;

				// オーバーラップ
				_ = Double.TryParse(settingsStrings[5], out doubleValue);
				genonSettings.VoiceOverlap = doubleValue;

				// 連続音 wav でなければファイル名で原音登録
				if (!String.IsNullOrEmpty(fileBody) && fileBody[0] != '_')
				{
					if (!_genonSettings.TryAdd(fileBody, genonSettings))
					{
						//Debug.WriteLine("SetToCore() 原音設定が既に存在：" + fileBody);
					}
				}
				// エイリアスが指定されていれば登録
				if (!String.IsNullOrEmpty(alias))
				{
					if (!_genonSettings.TryAdd(alias, genonSettings))
					{
						//Debug.WriteLine("SetToCore() 原音設定が既に存在：" + alias);
					}
				}
			}
		}
		catch
		{
			return -1;
		}
		return 0;
	}
}
