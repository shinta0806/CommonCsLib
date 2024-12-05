// ============================================================================
// 
// UTAU プラグインのためのユーティリティー関数群
// 
// ============================================================================

// ----------------------------------------------------------------------------
// UtauUtils.[cpp|h] の内容および、UtauConstants.h の内容を移植
// ごく一部のみ移植済み（必要になったところしか移植してない）
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2024/04/22 (Mon) | C++ 版 Lib UTAU から移植。
// (1.01) | 2024/12/05 (Thu) |   namespace を変更。
// ============================================================================

namespace Shinta.LibUtau;

internal class UtauUtils
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

	// ====================================================================
	// public 関数
	// ====================================================================

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

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// 音名の先頭一文字を並べた物
	/// </summary>
	private const String TONE_NAMES = "CCDDEFFGGAAB";
}
