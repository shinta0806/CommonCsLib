// ============================================================================
// 
// CSV ファイルを管理するクラス
// Copyright (C) 2015-2018 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2015/12/19 (Sat) | 作成開始。
//  1.00  | 2015/12/19 (Sat) | オリジナルバージョン。
//  1.10  | 2017/11/18 (Sat) | タイトル行を読み飛ばせるようにした。
// (1.11) | 2017/11/18 (Sat) | \" を読み飛ばせるようにした。
//  1.20  | 2017/12/09 (Sat) | 行番号を付与できるようにした。
//  1.30  | 2018/01/08 (Mon) | 書き込みできるようにした。
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shinta
{
	public class CsvManager
	{
		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 読み込み済みの CSV ファイル内容文字列を解析し、行列に分割
		// 区切りカンマ前後のホワイトスペースは取り除く
		// フィールドがダブルクォートで括られている場合、途中の改行・連続するダブルクォート・\" は文字扱い
		// 行番号を付ける場合、0 ベース。CSV が空行でも行番号列を作成する。
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static List<List<String>> CsvStringToList(String oContents, Boolean oSkipTitleLine = false, Boolean oAddLineIndex = false)
		{
			List<List<String>> aRecords = new List<List<String>>();
			List<String> aFields = new List<String>();

			// 改行コードを LF に統一
			oContents = oContents.Replace("\r\n", "\n");
			oContents = oContents.Replace("\r", "\n");

			Int32 aBeginPos = 0;
			Int32 aEndPos;

			if (oSkipTitleLine)
			{
				aBeginPos = SkipTitleLine(oContents);
			}

			if (oAddLineIndex)
			{
				aFields.Add(aRecords.Count.ToString());
			}

			while (aBeginPos < oContents.Length)
			{
				// 先頭のホワイトスペース読み飛ばし
				SkipWhiteSpace(oContents, ref aBeginPos);

				if (aBeginPos < oContents.Length && oContents[aBeginPos] == '"')
				{
					// ダブルクォートで囲まれている場合
					aEndPos = FindPairQuotePos(oContents, aRecords.Count, aBeginPos);

					// ダブルクォートの中身を抽出
					String aField = oContents.Substring(aBeginPos + 1, aEndPos - aBeginPos - 1);

					// 連続するダブルクォート・\" を 1 つに戻してからフィールド追加
					aFields.Add(aField.Replace("\"\"", "\"").Replace("\\\"", "\""));

					// 末尾のホワイトスペース読み飛ばし
					aEndPos++;
					SkipWhiteSpace(oContents, ref aEndPos);
					if (aEndPos < oContents.Length && oContents[aEndPos] != ',' && oContents[aEndPos] != '\n')
					{
						throw new Exception((aRecords.Count + 1).ToString() + " レコード目のダブルクォートに文字列が続いています。");
					}
				}
				else
				{
					// ダブルクォートで囲まれていない場合
					aEndPos = aBeginPos;

					// 区切り文字を検索
					while (aEndPos < oContents.Length && oContents[aEndPos] != ',' && oContents[aEndPos] != '\n')
					{
						aEndPos++;
					}

					// 区切り文字までの文字を抽出（末尾のホワイトスペース除く）してフィールド追加
					aFields.Add(oContents.Substring(aBeginPos, aEndPos - aBeginPos).TrimEnd());
				}

				// 行末ならレコード追加
				if (aEndPos >= oContents.Length || oContents[aEndPos] == '\n')
				{
					aRecords.Add(aFields);

					// これまでの最大フィールド数＋αのメモリを確保しつつリストを初期化
					aFields = new List<String>(aFields.Capacity);
					if (oAddLineIndex)
					{
						aFields.Add(aRecords.Count.ToString());
					}
				}

				aBeginPos = aEndPos + 1;
			}

			return aRecords;
		}

		// --------------------------------------------------------------------
		// 行列を CSV 文字列に統合
		// oRemoveLineIndex に関わらず、oTitle には行番号列は無いものとする
		// --------------------------------------------------------------------
		public static String ListToCsvString(List<List<String>> oRecords, String oCrCode, List<String> oTitle = null, Boolean oRemoveLineIndex = false)
		{
			StringBuilder aSB = new StringBuilder();

			// タイトル行
			if (oTitle != null)
			{
				AddRecord(aSB, oTitle, 0, oCrCode);
			}

			// 一般行
			Int32 aStartColumnIndex = oRemoveLineIndex ? 1 : 0;
			for (Int32 i = 0; i < oRecords.Count; i++)
			{
				AddRecord(aSB, oRecords[i], aStartColumnIndex, oCrCode);
			}

			return aSB.ToString();
		}

		// --------------------------------------------------------------------
		// ファイルから CSV を読み込む
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static List<List<String>> LoadCsv(String oPath, Encoding oEncoding, Boolean oSkipTitleLine = false, Boolean oAddLineIndex = false)
		{
			return CsvStringToList(File.ReadAllText(oPath, oEncoding), oSkipTitleLine, oAddLineIndex);
		}

		// --------------------------------------------------------------------
		// ファイルに CSV を書き込む
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void SaveCsv(String oPath, List<List<String>> oRecords, String oCrCode, Encoding oEncoding, List<String> oTitle = null, Boolean oRemoveLineIndex = false)
		{
			File.WriteAllText(oPath, ListToCsvString(oRecords, oCrCode, oTitle, oRemoveLineIndex), oEncoding);
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// CSV 1 行分を文字列に変換
		// --------------------------------------------------------------------
		private static void AddRecord(StringBuilder oSB, List<String> oRecord, Int32 oStartColumnIndex, String oCrCode)
		{
			// 右側以外の列を追加
			for (Int32 i = oStartColumnIndex; i < oRecord.Count - 1; i++)
			{
				oSB.Append(Escape(oRecord[i] + ","));
			}

			// 右側の列を追加
			if (oRecord.Count - 1 >= oStartColumnIndex)
			{
				oSB.Append(Escape(oRecord[oRecord.Count - 1]) + oCrCode);
			}
		}

		// --------------------------------------------------------------------
		// 改行・ダブルクオート・\ が含まれる場合はダブルクオートで括る
		// --------------------------------------------------------------------
		private static String Escape(String oString)
		{
			if (String.IsNullOrEmpty(oString))
			{
				return String.Empty;
			}

			if (oString.IndexOfAny(new Char[] { '\r', '\n', '\"', '\\' }) >= 0)
			{
				return "\"" + oString + "\"";
			}

			return oString;
		}

		// --------------------------------------------------------------------
		// 対になるダブルクオートの位置を返す
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private static Int32 FindPairQuotePos(String oContents, Int32 oRecordIndex, Int32 oQuotePos)
		{
			for (; ; )
			{
				// 対になるダブルクォートを探す
				oQuotePos = oContents.IndexOf('"', oQuotePos + 1);
				if (oQuotePos < 0)
				{
					throw new Exception((oRecordIndex + 1).ToString() + " レコード目のダブルクォートと対になるダブルクォートがありません。");
				}
				if (oQuotePos > 0 && oContents[oQuotePos - 1] == '\\')
				{
					// 「\」「"」と連続しているので、内容の一部である
				}
				else if (oQuotePos + 1 < oContents.Length && oContents[oQuotePos + 1] == '"')
				{
					// ダブルクォートが連続しているので、内容の一部である
					oQuotePos++;
				}
				else
				{
					// ダブルクォートが連続していないので、対になるダブルクォートである
					break;
				}
			}
			return oQuotePos;
		}

		// --------------------------------------------------------------------
		// タイトル行を読み飛ばす
		// '\n' の次の位置を返す
		// --------------------------------------------------------------------
		private static Int32 SkipTitleLine(String oContents)
		{
			Int32 aPos = 0;

			while (aPos < oContents.Length)
			{
				if (oContents[aPos] == '\n')
				{
					return aPos + 1;
				}
				if (oContents[aPos] == '\"')
				{
					aPos = FindPairQuotePos(oContents, 0, aPos) + 1;
				}
				else
				{
					aPos++;
				}
			}
			return aPos;
		}

		// --------------------------------------------------------------------
		// 空白を読み飛ばす
		// --------------------------------------------------------------------
		private static void SkipWhiteSpace(String oContents, ref Int32 oPos)
		{
			while (oPos < oContents.Length && (oContents[oPos] == ' ' || oContents[oPos] == '\t'))
			{
				oPos++;
			}
		}



	}
	// public class CsvManager ___END___

}
// namespace Shinta ___END___

