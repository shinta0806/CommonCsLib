// ============================================================================
// 
// UTAU プラグインのためのユーティリティー関数群
// 
// ============================================================================

// ----------------------------------------------------------------------------
// UtauUtils.[cpp|h] の内容および、UtauConstants.h の内容を一部移植
// その他新規作成したものもあり
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2024/04/22 (Mon) | C++ 版 Lib UTAU から移植。
// (1.01) | 2024/12/05 (Thu) |   namespace を変更。
//  1.10  | 2025/01/22 (Wed) | ToneNameToToneNumber() を作成。
//  1.20  | 2025/02/11 (Tue) | DetectToneKind() を作成。
//  1.30  | 2025/02/11 (Tue) | LoadCharacterText() を作成。
//  1.40  | 2025/02/11 (Tue) | VoiceName() を作成。
//  1.50  | 2025/02/11 (Tue) | VoiceIconPath() を作成。
// ============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace Shinta.LibUtau;

/// <summary>
/// 音素種別
/// </summary>
public enum ToneKind
{
	Unknown,
	Tandoku,
	Renzoku,
	Cvvc,
}

internal partial class UtauUtils
{
	// ====================================================================
	// public 定数
	// ====================================================================

	/// <summary>
	/// 音名
	/// </summary>
	public const Char TONE_NAME_SHARP = '#';

	/// <summary>
	/// 設定可能な低い音：C1
	/// </summary>
	public const Int32 TONE_NUMBER_BASE = 24;

	/// <summary>
	/// UTAU の GUI で指定できるオクターブ最高値
	/// </summary>
	public const Int32 TONE_OCTAVE_MAX = 7;

	/// <summary>
	/// UTAU の GUI で指定できるオクターブ最低値
	/// </summary>
	public const Int32 TONE_OCTAVE_MIN = 1;

	/// <summary>
	/// 1 オクターブの音階数
	/// </summary>
	public const Int32 TONE_OCTAVE_STEPS = 12;

	/// <summary>
	/// resampler
	/// </summary>
	public const String FILE_NAME_RESAMPLER = "resampler" + Common.FILE_EXT_EXE;

	/// <summary>
	/// wavtool
	/// </summary>
	public const String FILE_NAME_WAV_TOOL = "wavtool" + Common.FILE_EXT_EXE;

	/// <summary>
	/// 音源キャラ情報ファイル
	/// </summary>
	public const String FILE_NAME_CHARACTER = "character" + Common.FILE_EXT_TXT;

	/// <summary>
	/// 音源キャラ情報ファイル（スペルミス救済用）
	/// </summary>
	public const String FILE_NAME_CHARACTER_SPELL_MISS_1 = "charactar" + Common.FILE_EXT_TXT;

	/// <summary>
	/// 音源キャラ情報ファイル（スペルミス救済用）
	/// </summary>
	public const String FILE_NAME_CHARACTER_SPELL_MISS_2 = "charactor" + Common.FILE_EXT_TXT;

	/// <summary>
	/// prefix.map
	/// </summary>
	public const String FILE_NAME_PREFIX_MAP = "prefix.map";

	/// <summary>
	/// UTAU
	/// </summary>
	public const String FILE_NAME_UTAU = "utau" + Common.FILE_EXT_EXE;

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// 音素種別を判定
	/// </summary>
	/// <param name="otoIniPath"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public static ToneKind DetectToneKind(String otoIniPath)
	{
		// oto.ini
		OtoIni otoIni = new();
		if (otoIni.SetTo(otoIniPath) < 0)
		{
			throw new Exception("oto.ini を読み込めませんでした。");
		}

		// プレフィックス・サフィックスを取得
		List<String> prefixes = new();
		List<String> suffixes = new();
		PrefixMap prefixMap = new();
		prefixMap.SetTo(Path.GetDirectoryName(otoIniPath) + "\\" + FILE_NAME_PREFIX_MAP);
		foreach (String value in prefixMap.Prefixes.Values)
		{
			if (!prefixes.Contains(value))
			{
				prefixes.Add(value);
			}
		}
		prefixes.Sort();
		foreach (String value in prefixMap.Suffixes.Values)
		{
			if (!suffixes.Contains(value))
			{
				suffixes.Add(value);
			}
		}
		suffixes.Sort();

		// プレフィックス・サフィックス抜きの原音設定
		List<String> stripAliases = new();
		foreach (KeyValuePair<String, GenonSettings> kvp in otoIni.GenonSettings)
		{
			// プレフィックス除外
			// 例えば "↑↑" は "↑" より優先的に引っかけたいので、逆順で探索
			Int32 beginPos = 0;
			for (Int32 i = prefixes.Count - 1; i >= 0; i--)
			{
				if (!String.IsNullOrEmpty(prefixes[i]) && String.Compare(prefixes[i], 0, kvp.Key, 0, prefixes[i].Length, true) == 0)
				{
					beginPos = prefixes[i].Length;
					break;
				}
			}
			// サフィックス除外
			Int32 endPos = -1;
			for (Int32 i = suffixes.Count - 1; i >= 0; i--)
			{
				if (!String.IsNullOrEmpty(suffixes[i]))
				{
					endPos = kvp.Key.IndexOf(suffixes[i], beginPos);
					if (endPos >= 0)
					{
						break;
					}
				}
			}
			// 除外後の文字
			String alias;
			if (endPos < 0)
			{
				alias = kvp.Key[beginPos..];
			}
			else
			{
				alias = kvp.Key[beginPos..endPos];
			}
			if (!stripAliases.Contains(alias))
			{
				stripAliases.Add(alias);
			}
		}
		stripAliases.Sort();

		// 連続音 → CVVC → 単独音、の順に判定
		// 連続音音源のエイリアスがあるか（簡易判定）
		if (stripAliases.Contains("a か") || stripAliases.Contains("i し") || stripAliases.Contains("u つ")
				|| stripAliases.Contains("e ね") || stripAliases.Contains("o ほ"))
		{
			return ToneKind.Renzoku;
		}

		// CVVC のエイリアスがあるか（簡易判定）
		if (stripAliases.Contains("a k") || stripAliases.Contains("i s") || stripAliases.Contains("u ts")
				|| stripAliases.Contains("e n") || stripAliases.Contains("o h"))
		{
			return ToneKind.Cvvc;
		}

		// 単独音音源のエイリアスがあるか
		Int32 lower = LowerBound(stripAliases, "あ");
		Int32 upper = UpperBound(stripAliases, "ん");
		while (lower < upper)
		{
			// 単独音のエイリアスは「きゃ」など長くても長さは 2。途中にスペースを含まない
			if (stripAliases[lower].Length <= 2 && stripAliases[lower].IndexOf(' ') < 0)
			{
				return ToneKind.Tandoku;
			}
			lower++;
		}

		return ToneKind.Unknown;
	}

	/// <summary>
	/// oto.ini と同じフォルダーにある character.txt を読み込む
	/// 予め Encoding.RegisterProvider(CodePagesEncodingProvider.Instance) されている前提
	/// </summary>
	/// <returns>character.txt の内容</returns>
	public static String LoadCharacterText(String otoIniPath)
	{
		Encoding encoding = Encoding.GetEncoding(Common.CODE_PAGE_SHIFT_JIS);
		String folder = Path.GetDirectoryName(otoIniPath) + "\\";

		// まずは正しいファイル名で読み込む
		try
		{
			return File.ReadAllText(folder + FILE_NAME_CHARACTER, encoding);
		}
		catch (Exception)
		{
		}

		// 間違ったファイル名を救済
		try
		{
			return File.ReadAllText(folder + FILE_NAME_CHARACTER_SPELL_MISS_1, encoding);
		}
		catch (Exception)
		{
		}
		try
		{
			return File.ReadAllText(folder + FILE_NAME_CHARACTER_SPELL_MISS_2, encoding);
		}
		catch (Exception)
		{
		}

		Log.Error("音源キャラ情報ファイルを読み込めませんでした：" + folder + FILE_NAME_CHARACTER);
		return String.Empty;
	}

	/// <summary>
	/// 音名→UTAU 音番号
	/// C1 = 24, C4 = 60
	/// </summary>
	/// <param name="toneName"></param>
	/// <returns></returns>
	public static Int32 ToneNameToToneNumber(String toneName)
	{
		if (toneName.Length < 2)
		{
			return TONE_NUMBER_BASE;
		}

		// 音名
		Int32 aToneNameIndex = TONE_NAMES.IndexOf(toneName[0]);
		if (aToneNameIndex < 0)
		{
			aToneNameIndex = 0;
		}

		// オクターブ
		_ = Int32.TryParse(toneName.AsSpan(toneName.Length - 1), out Int32 octave);
		if (octave < TONE_OCTAVE_MIN)
		{
			octave = TONE_OCTAVE_MIN;
		}
		else if (octave > TONE_OCTAVE_MAX)
		{
			octave = TONE_OCTAVE_MAX;
		}

		return TONE_NUMBER_BASE + (octave - 1) * TONE_OCTAVE_STEPS + aToneNameIndex + Convert.ToInt32(toneName[1] == TONE_NAME_SHARP);
	}

	/// <summary>
	/// UTAU 音番号→音名
	/// </summary>
	/// <param name="toneNumber"></param>
	/// <returns></returns>
	public static String ToneNumberToToneName(Int32 toneNumber)
	{
		return ToneNumberToToneName(toneNumber % TONE_OCTAVE_STEPS, toneNumber / TONE_OCTAVE_STEPS - 2);
	}

	/// <summary>
	/// UTAU 音番号→音名
	/// nameIndex, octaveIndex は 0 ベース
	/// </summary>
	/// <param name="nameIndex"></param>
	/// <param name="octaveIndex"></param>
	/// <returns></returns>
	public static String ToneNumberToToneName(Int32 nameIndex, Int32 octaveIndex)
	{
		String toneName;

		toneName = TONE_NAMES[nameIndex].ToString();
		if (nameIndex > 0 && TONE_NAMES[nameIndex] == TONE_NAMES[nameIndex - 1])
		{
			toneName += TONE_NAME_SHARP;
		}
		toneName += (octaveIndex + 1).ToString();
		return toneName;
	}

	/// <summary>
	/// oto.ini と同じフォルダーにある character.txt から音源アイコンファイルのフルパスを取得する
	/// </summary>
	/// <param name="otoIniPath"></param>
	/// <returns></returns>
	public static String VoiceIconPath(String otoIniPath)
	{
		if (String.IsNullOrEmpty(otoIniPath))
		{
			return String.Empty;
		}
		String otoIniFolderPath = Path.GetDirectoryName(otoIniPath) + "\\";

		String characterText = LoadCharacterText(otoIniPath);
		foreach (Match match in RegexVoiceIconFileName().Matches(characterText).Cast<Match>())
		{
			if (match.Groups.Count >= 2)
			{
				String charPath = otoIniFolderPath + match.Groups[1].Value.Trim();
				if (File.Exists(charPath))
				{
					return charPath;
				}
			}
		}

		// 指示がない場合は、よく使われていそうなファイル名で救済
		String defaultPath = otoIniFolderPath + "image" + Common.FILE_EXT_BMP;
		if (File.Exists(defaultPath))
		{
			return defaultPath;
		}

		// それもない場合や、指示が間違っている場合は、とにかく存在している bmp で救済
		String? anyBmpPath = Directory.EnumerateFiles(otoIniFolderPath, "*" + Common.FILE_EXT_BMP).FirstOrDefault();
		if (File.Exists(anyBmpPath))
		{
			return anyBmpPath;
		}

		return String.Empty;
	}

	/// <summary>
	/// oto.ini から音源名を取得する
	/// </summary>
	/// <param name="otoIniPath"></param>
	/// <returns></returns>
	public static String VoiceName(String otoIniPath)
	{
		// oto.ini の name エントリー
		String characterText = LoadCharacterText(otoIniPath);
		foreach (Match match in RegexVoiceName().Matches(characterText).Cast<Match>())
		{
			if (match.Groups.Count >= 2)
			{
				return match.Groups[1].Value.Trim();
			}
		}

		// 見つからない場合はフォルダー名を音源名とする
		return Path.GetFileName(Path.GetDirectoryName(otoIniPath)) ?? String.Empty;
	}

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// 音名の先頭一文字を並べた物
	/// </summary>
	private const String TONE_NAMES = "CCDDEFFGGAAB";

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// key 以上の値を持つ最初の要素を指すインデックスを返す
	/// 手抜き実装：O(n)
	/// </summary>
	/// <param name="container"></param>
	/// <param name="key"></param>
	/// <returns>見つからない場合は Count</returns>
	private static Int32 LowerBound(List<String> container, String key)
	{
		Int32 index = 0;
		while (index < container.Count)
		{
			if (String.Compare(container[index], key) >= 0)
			{
				break;
			}
			index++;
		}
		return index;
	}

	/// <summary>
	/// 音源アイコンのファイル名を抽出するための正規表現
	/// </summary>
	/// <returns></returns>
	[GeneratedRegex(@"^image=(.*?)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
	private static partial Regex RegexVoiceIconFileName();

	/// <summary>
	/// 音源名を抽出するための正規表現
	/// </summary>
	/// <returns></returns>
	[GeneratedRegex(@"^name=(.*?)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
	private static partial Regex RegexVoiceName();

	/// <summary>
	/// key を越える値を持つ最初の要素を指すインデックスを返す
	/// 手抜き実装：O(n)
	/// </summary>
	/// <param name="container"></param>
	/// <param name="key"></param>
	/// <returns>見つからない場合は Count</returns>
	private static Int32 UpperBound(List<String> container, String key)
	{
		Int32 index = 0;
		while (index < container.Count)
		{
			if (String.Compare(container[index], key) > 0)
			{
				break;
			}
			index++;
		}
		return index;
	}
}
