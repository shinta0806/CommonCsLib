// ============================================================================
// 
// CSV ファイルを管理するクラス
// Copyright (C) 2015-2021 by SHINTA
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
// (1.11) | 2017/11/18 (Sat) |   \" を読み飛ばせるようにした。
//  1.20  | 2017/12/09 (Sat) | 行番号を付与できるようにした。
//  1.30  | 2018/01/08 (Mon) | 書き込みできるようにした。
// (1.31) | 2018/04/22 (Sun) |   書き込み時にカンマを含む列をエスケープしていなかった不具合を修正。
// (1.32) | 2018/05/20 (Sun) |   書き込み時に " をエスケープしていなかった不具合を修正。
// (1.33) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.34) | 2019/12/22 (Sun) |   null 許容参照型を無効化できるようにした。
// (1.35) | 2021/05/27 (Thu) |   null 許容参照型を必須にした。
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
		public static List<List<String>> CsvStringToList(String contents, Boolean skipTitleLine = false, Boolean addLineIndex = false)
		{
			List<List<String>> records = new();
			List<String> fields = new();

			// 改行コードを LF に統一
			contents = contents.Replace("\r\n", "\n");
			contents = contents.Replace("\r", "\n");

			Int32 beginPos = 0;
			Int32 endPos;

			if (skipTitleLine)
			{
				beginPos = SkipTitleLine(contents);
			}

			if (addLineIndex)
			{
				fields.Add(records.Count.ToString());
			}

			while (beginPos < contents.Length)
			{
				// 先頭のホワイトスペース読み飛ばし
				SkipWhiteSpace(contents, ref beginPos);

				if (beginPos < contents.Length && contents[beginPos] == '"')
				{
					// ダブルクォートで囲まれている場合
					endPos = FindPairQuotePos(contents, records.Count, beginPos);

					// ダブルクォートの中身を抽出
					String field = contents.Substring(beginPos + 1, endPos - beginPos - 1);

					// 連続するダブルクォート・\" を 1 つに戻してからフィールド追加
					fields.Add(field.Replace("\"\"", "\"").Replace("\\\"", "\""));

					// 末尾のホワイトスペース読み飛ばし
					endPos++;
					SkipWhiteSpace(contents, ref endPos);
					if (endPos < contents.Length && contents[endPos] != ',' && contents[endPos] != '\n')
					{
						throw new Exception((records.Count + 1).ToString() + " レコード目のダブルクォートに文字列が続いています。");
					}
				}
				else
				{
					// ダブルクォートで囲まれていない場合
					endPos = beginPos;

					// 区切り文字を検索
					while (endPos < contents.Length && contents[endPos] != ',' && contents[endPos] != '\n')
					{
						endPos++;
					}

					// 区切り文字までの文字を抽出（末尾のホワイトスペース除く）してフィールド追加
					//fields.Add(contents.Substring(beginPos, endPos - beginPos).TrimEnd());
					fields.Add(contents[beginPos..endPos].TrimEnd());
				}

				// 行末ならレコード追加
				if (endPos >= contents.Length || contents[endPos] == '\n')
				{
					records.Add(fields);

					// これまでの最大フィールド数＋αのメモリを確保しつつリストを初期化
					fields = new List<String>(fields.Capacity);
					if (addLineIndex)
					{
						fields.Add(records.Count.ToString());
					}
				}

				beginPos = endPos + 1;
			}

			return records;
		}

		// --------------------------------------------------------------------
		// 行列を CSV 文字列に統合
		// removeLineIndex に関わらず、title には行番号列は無いものとする
		// --------------------------------------------------------------------
		public static String ListToCsvString(List<List<String>> records, String crCode, List<String>? title = null, Boolean removeLineIndex = false)
		{
			StringBuilder stringBuilder = new();

			// タイトル行
			if (title != null)
			{
				AddRecord(stringBuilder, title, 0, crCode);
			}

			// 一般行
			Int32 aStartColumnIndex = removeLineIndex ? 1 : 0;
			for (Int32 i = 0; i < records.Count; i++)
			{
				AddRecord(stringBuilder, records[i], aStartColumnIndex, crCode);
			}

			return stringBuilder.ToString();
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
		public static void SaveCsv(String path, List<List<String>> records, String crCode, Encoding encoding, List<String>? title = null, Boolean removeLineIndex = false)
		{
			File.WriteAllText(path, ListToCsvString(records, crCode, title, removeLineIndex), encoding);
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
				oSB.Append(Escape(oRecord[i]) + ",");
			}

			// 右側の列を追加
			if (oRecord.Count - 1 >= oStartColumnIndex)
			{
				//oSB.Append(Escape(oRecord[oRecord.Count - 1]) + oCrCode);
				oSB.Append(Escape(oRecord[^1]) + oCrCode);
			}
		}

		// --------------------------------------------------------------------
		// 改行・ダブルクオート・\・カンマ が含まれる場合はダブルクオートで括る
		// --------------------------------------------------------------------
		private static String Escape(String oString)
		{
			if (String.IsNullOrEmpty(oString))
			{
				return String.Empty;
			}

			oString = oString.Replace("\"", "\\\"");

			if (oString.IndexOfAny(new Char[] { '\r', '\n', '\"', '\\', ',' }) >= 0)
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
		private static void SkipWhiteSpace(String contents, ref Int32 pos)
		{
			while (pos < contents.Length && (contents[pos] == ' ' || contents[pos] == '\t'))
			{
				pos++;
			}
		}
	}
}

