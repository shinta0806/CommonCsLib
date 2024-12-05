// ============================================================================
// 
// prefix.map を解釈・保持するクラス
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
// ============================================================================

using System.Text;

namespace Shinta.LibUtau;

internal class PrefixMap
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public PrefixMap()
	{
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// ノート番号とプレフィックスの対比
	/// </summary>
	public Dictionary<Int32, String> Prefixes => _prefixMap;

	/// <summary>
	/// ノート番号とサフィックスの対比
	/// </summary>
	public Dictionary<Int32, String> Suffixes => _suffixMap;

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// prefix.map の適用
	/// </summary>
	/// <param name="noteNum"></param>
	/// <param name="lyric"></param>
	/// <returns></returns>
	public String PrefixedLyric(Int32 noteNum, String lyric)
	{
		String? value;
		if (_prefixMap.TryGetValue(noteNum, out value))
		{
			lyric = value + lyric;
		}
		if (_suffixMap.TryGetValue(noteNum, out value))
		{
			lyric += value;
		}
		return lyric;
	}

	/// <summary>
	/// prefix.map を読み込む
	/// </summary>
	/// <param name="prefixMapPath"></param>
	/// <returns></returns>
	public Int32 SetTo(String prefixMapPath)
	{
		try
		{
			String[] lines = File.ReadAllLines(prefixMapPath, Encoding.GetEncoding(Common.CODE_PAGE_SHIFT_JIS));
			foreach (String line in lines)
			{
				// prefix.map の一行のフォーマット：音名<tab>プレフィックス<tab>サフィックス
				if (!String.IsNullOrEmpty(line))
				{
					String[] prefixStrings = line.Split('\t');
					if (prefixStrings.Length >= 3)
					{
						_prefixMap[UtauUtils.ToneNameToToneNumber(prefixStrings[0])] = prefixStrings[1];
						_suffixMap[UtauUtils.ToneNameToToneNumber(prefixStrings[0])] = prefixStrings[2];
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

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// ノート番号とプレフィックスの対比
	/// </summary>
	private readonly Dictionary<Int32, String> _prefixMap = new();

	/// <summary>
	/// ノート番号とサフィックスの対比
	/// </summary>
	private readonly Dictionary<Int32, String> _suffixMap = new();
}

