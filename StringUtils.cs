// ============================================================================
// 
// 文字列操作関連の関数群
// Copyright (C) 2015-2021 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2015/01/03 (Sat) | 作成開始。
//  1.00  | 2015/01/03 (Sat) | オリジナルバージョン。
// (1.01) | 2021/10/28 (Thu) |   軽微なリファクタリング。
// ============================================================================

using System;

namespace Shinta
{
	public class StringUtils
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 文字列を数値部分と文字列部分に分けてから比較する
		// 例えば AB12CD34 なら、AB, 12, CD, 34 に分けてから、それぞれの部分で相手と比較する
		// AB12CD と AB123CD は、12 < 123 なので AB12CD < AB123CD
		// 先頭の型が異なる場合は、文字列の方が大きいとする
		// ハイフン、プラス、ピリオドは、後ろに数値が続くなら数値と見なす
		// -1-2 のような数値表記としておかしい文字列に対する結果は不定
		// --------------------------------------------------------------------
		public static Int32 StrAndNumCmp(String str1, String str2, Boolean ignoreCase)
		{
			Int32 str1Begin = 0;
			Int32 str2Begin = 0;

			while (str1Begin < str1.Length || str2Begin < str2.Length)
			{
				// 文字列部分の比較
				String str1Cmp = StrAndNumCmpSubStr(str1, ref str1Begin);
				String str2Cmp = StrAndNumCmpSubStr(str2, ref str2Begin);
				Int32 cmpResult = String.Compare(str1Cmp, str2Cmp, ignoreCase);
				if (cmpResult != 0)
				{
					return cmpResult;
				}

				// 数値部分の比較
				Double str1Num = StrAndNumCmpSubNum(str1, ref str1Begin);
				Double str2Num = StrAndNumCmpSubNum(str2, ref str2Begin);
				if (str1Num > str2Num)
				{
					return 1;
				}
				else if (str1Num < str2Num)
				{
					return -1;
				}
			}
			return 0;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ====================================================================
		// private static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 数値かどうか
		// --------------------------------------------------------------------
		private static Boolean StrAndNumCmpSubIsDigit(String str, Int32 pos)
		{
			// ハイフンやピリオドの後ろに数値が続く場合は、ハイフンは数値扱い
			if (pos + 1 < str.Length
					&& (str[pos] == '-' || str[pos] == '+' || str[pos] == '.')
					&& Char.IsDigit(str[pos + 1]))
			{
				return true;
			}

			// その他は通常の判定
			return Char.IsDigit(str[pos]);
		}

		// --------------------------------------------------------------------
		// 数値部分を抜き出す
		// --------------------------------------------------------------------
		private static Double StrAndNumCmpSubNum(String str, ref Int32 pos)
		{
			Int32 beginPos = pos;

			while (pos < str.Length && StrAndNumCmpSubIsDigit(str, pos))
			{
				pos++;
			}
			if (!Double.TryParse(str.Substring(beginPos, pos - beginPos), out Double result))
			{
				return 0.0;
			}

			return result;
		}

		// --------------------------------------------------------------------
		// 文字列部分（数値以外の部分）を抜き出す
		// --------------------------------------------------------------------
		private static String StrAndNumCmpSubStr(String str, ref Int32 pos)
		{
			Int32 beginPos = pos;

			while (pos < str.Length && !StrAndNumCmpSubIsDigit(str, pos))
			{
				pos++;
			}
			return str.Substring(beginPos, pos - beginPos);
		}
	}
}

