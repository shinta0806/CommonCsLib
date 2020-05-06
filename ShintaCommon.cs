// ============================================================================
// 
// よく使う一般的な定数や関数
// Copyright (C) 2014-2020 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2014/11/29 (Sat) | 作成開始。
//  1.00  | 2014/11/29 (Sat) | Constants クラスを作成。
//  1.10  | 2014/12/06 (Sat) | ShowLogMessage() を作成。
//  1.20  | 2014/12/22 (Mon) | PostMessage() Windows API をインポート。
// (1.21) | 2014/12/28 (Sun) |   ShowLogMessage() でメッセージが空の場合に何もしないようにした。
// (1.22) | 2014/12/28 (Sun) |   ShowLogMessage() でログもできるようにした。
//  1.30  | 2015/01/10 (Sat) | TraceEventTypeToCaption() を作成。
// (1.31) | 2015/01/31 (Sat) |   TRACE_SOURCE_DEFAULT_LISTENER_NAME を定義。
//  1.40  | 2015/02/15 (Sun) | FindWindow() Windows API をインポート。
//  1.50  | 2015/02/21 (Sat) | Windows API 関連を分離。
//  1.60  | 2015/03/15 (Sun) | DeleteZoneID() を作成。
//  1.70  | 2015/05/15 (Fri) | LoadKeyAndValue() を作成。
//  1.80  | 2015/05/15 (Fri) | DeepClone() を作成。
//  1.90  | 2015/05/15 (Fri) | Swap() を作成。
//  2.00  | 2015/05/23 (Sat) | SerializableKeyValuePair を作成。
//  2.01  | 2015/10/03 (Sat) | ShowLogMessage() のオーバーロードを作成。
//  2.10  | 2016/02/20 (Sat) | ContainsControl() を作成。
//  2.20  | 2016/04/17 (Sun) | DetectEncoding() を作成。
// (2.21) | 2016/04/17 (Sun) |   精度が悪いため DetectEncoding() を廃止。
//  2.30  | 2016/05/03 (Tue) | MakeRelativePath() を作成。
//  2.40  | 2016/05/03 (Tue) | MakeAbsolutePath() を作成。
// (2.41) | 2017/11/17 (Fri) |   StatusT の使用を廃止。
//  2.50  | 2017/11/18 (Sat) | Serialize()、Deserialize() を作成。
//  2.60  | 2018/01/18 (Thu) | CompareVersionString() を作成。
//  2.70  | 2018/07/07 (Sat) | StringToInt32() を作成。
//  2.80  | 2018/08/07 (Tue) | SameNameProcesses() を作成。
//  2.90  | 2018/08/11 (Sat) | ActivateExternalWindow() を作成。
//  3.00  | 2018/09/09 (Sun) | ContainFormIfNeeded() を作成。
// (3.01) | 2019/01/14 (Mon) |   Windows フォームアプリケーション用の関数を #if で隔離。
//  3.10  | 2019/01/14 (Mon) | ActivateSameNameProcessWindow() を作成。
//  3.20  | 2019/01/19 (Sat) | UserAppDataFolderPath() を作成。
//  3.30  | 2019/02/10 (Sun) | SelectDataGridCell() を作成。
//  3.40  | 2019/06/27 (Thu) | フォーム・WPF 特有のものを別ファイルに分離。
//  3.50  | 2019/11/10 (Sun) | null 許容参照型を有効化した。
// (3.51) | 2019/12/22 (Sun) |   null 許容参照型を無効化できるようにした。
// (3.52) | 2020/05/05 (Tue) |   MakeAbsolutePath() のnull 許容参照型を無効化できるようにした。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

#if !NULLABLE_DISABLED
#nullable enable
#endif

namespace Shinta
{
	// ====================================================================
	// 共用ユーティリティークラス
	// ====================================================================

	public class Common
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		// --------------------------------------------------------------------
		// ログ用定数（正規の TraceEventType への追加分）
		// --------------------------------------------------------------------

		// Information より重要度の低い情報（ShowLogMessage() で表示しない）
		public const TraceEventType TRACE_EVENT_TYPE_STATUS = (TraceEventType)1001;

		// --------------------------------------------------------------------
		// デバッグ用バイナリかどうかを見分けるための印
		// --------------------------------------------------------------------
#if DEBUG
		public const String DEBUG_ENABLED_MARK = "DEBUG_MARK_SHINTA_COMMON";
#endif

		// --------------------------------------------------------------------
		// Boolean 値の文字列表記（DefaultSettingValue などで使用）
		// --------------------------------------------------------------------
		public const String BOOLEAN_STRING_TRUE = "True";
		public const String BOOLEAN_STRING_FALSE = "False";

		// --------------------------------------------------------------------
		// よく使うフォルダ
		// --------------------------------------------------------------------
		public const String FOLDER_NAME_SHINTA = SHINTA + "\\";

		// --------------------------------------------------------------------
		// よく使う拡張子
		// --------------------------------------------------------------------
		public const String FILE_EXT_AVI = ".avi";
		public const String FILE_EXT_BAK = ".bak";
		public const String FILE_EXT_CONFIG = ".config";
		public const String FILE_EXT_CSS = ".css";
		public const String FILE_EXT_CSV = ".csv";
		public const String FILE_EXT_DLL = ".dll";
		public const String FILE_EXT_EXE = ".exe";
		public const String FILE_EXT_FLV = ".flv";
		public const String FILE_EXT_HTML = ".html";
		public const String FILE_EXT_INI = ".ini";
		public const String FILE_EXT_KRA = ".kra";
		public const String FILE_EXT_LOCK = ".lock";
		public const String FILE_EXT_LOG = ".log";
		public const String FILE_EXT_LGA = ".lga";
		public const String FILE_EXT_LRC = ".lrc";
		public const String FILE_EXT_M4A = ".m4a";
		public const String FILE_EXT_MKV = ".mkv";
		public const String FILE_EXT_MOV = ".mov";
		public const String FILE_EXT_MP3 = ".mp3";
		public const String FILE_EXT_MP4 = ".mp4";
		public const String FILE_EXT_MPEG = ".mpeg";
		public const String FILE_EXT_MPG = ".mpg";
		public const String FILE_EXT_PHP = ".php";
		public const String FILE_EXT_PNG = ".png";
		public const String FILE_EXT_REG = ".reg";
		public const String FILE_EXT_SQLITE3 = ".sqlite3";
		public const String FILE_EXT_TPL = ".tpl";
		public const String FILE_EXT_TXT = ".txt";
		public const String FILE_EXT_WAV = ".wav";
		public const String FILE_EXT_WMA = ".wma";
		public const String FILE_EXT_WMV = ".wmv";
		public const String FILE_EXT_XAML = ".xaml";
		public const String FILE_EXT_ZIP = ".zip";

		// --------------------------------------------------------------------
		// Encoding 用コードページ
		// --------------------------------------------------------------------
		public const Int32 CODE_PAGE_EUC_JP = 20932;
		public const Int32 CODE_PAGE_JIS = 50220;
		public const Int32 CODE_PAGE_SHIFT_JIS = 932;
		public const Int32 CODE_PAGE_UTF_16_BE = 1201;

		// --------------------------------------------------------------------
		// その他
		// --------------------------------------------------------------------

		// 一般的なスレッドスリープ時間 [ms]
		public const Int32 GENERAL_SLEEP_TIME = 20;

		// SHINTA
		public const String SHINTA = "SHINTA";

		// TraceSource のデフォルトリスナー
		public const String TRACE_SOURCE_DEFAULT_LISTENER_NAME = "Default";

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ミューテックスを取得できない場合は、同名のプロセスのウィンドウをアクティベートする
		// ＜返値＞ 既存プロセスが存在しミューテックスが取得できなかった場合：null
		//          既存プロセスが存在せずミューテックスが取得できた場合：取得したミューテックス（使い終わった後で呼び出し元にて解放する必要がある）
		// --------------------------------------------------------------------
#if !NULLABLE_DISABLED
		public static Mutex? ActivateAnotherProcessWindowIfNeeded(String oMutexName)
#else
		public static Mutex ActivateAnotherProcessWindowIfNeeded(String oMutexName)
#endif
		{
			// ミューテックスを取得する
			Mutex aOwnedMutex = new Mutex(false, oMutexName);
			try
			{
				if (aOwnedMutex.WaitOne(0))
				{
					// ミューテックスを取得できた
					return aOwnedMutex;
				}
			}
			catch (AbandonedMutexException)
			{
				// ミューテックスが放棄されていた場合にこの例外となるが、取得自体はできている
				return aOwnedMutex;
			}

			// ミューテックスが取得できなかったため、同名プロセスを探し、そのウィンドウをアクティベートする
			ActivateSameNameProcessWindow();
			return null;
		}

		// --------------------------------------------------------------------
		// 外部プロセスのウィンドウをアクティベートする
		// --------------------------------------------------------------------
		public static void ActivateExternalWindow(IntPtr oHWnd)
		{
			if (oHWnd == IntPtr.Zero)
			{
				return;
			}

			// ウィンドウが最小化されていれば元に戻す
			if (WindowsApi.IsIconic(oHWnd))
			{
				WindowsApi.ShowWindowAsync(oHWnd, (Int32)ShowWindowCommands.SW_RESTORE);
			}

			// アクティベート
			WindowsApi.SetForegroundWindow(oHWnd);
		}

		// --------------------------------------------------------------------
		// 指定プロセスと同名プロセスのウィンドウをアクティベートする
		// --------------------------------------------------------------------
#if !NULLABLE_DISABLED
		public static void ActivateSameNameProcessWindow(Process? oSpecifyProcess = null)
#else
		public static void ActivateSameNameProcessWindow(Process oSpecifyProcess = null)
#endif
		{
			List<Process> aSameNameProcesses = SameNameProcesses(oSpecifyProcess);
			if (aSameNameProcesses.Count > 0)
			{
				ActivateExternalWindow(aSameNameProcesses[0].MainWindowHandle);
			}
		}

		// --------------------------------------------------------------------
		// バージョン文字列を比較（大文字小文字は区別しない）
		// --------------------------------------------------------------------
		public static Int32 CompareVersionString(String oVerA, String oVerB)
		{
			// 最初に同じ文字列かどうか確認
			if (String.Compare(oVerA, oVerB, true) == 0)
			{
				return 0;
			}
			if (String.IsNullOrEmpty(oVerA) && String.IsNullOrEmpty(oVerB))
			{
				return 0;
			}

			// いずれかが IsNullOrEmpty() ならそちらが小さいとする
			if (String.IsNullOrEmpty(oVerA))
			{
				return -1;
			}
			if (String.IsNullOrEmpty(oVerB))
			{
				return 1;
			}

			// 解析
			Match aMatchA = Regex.Match(oVerA, COMPARE_VERSION_STRING_REGEX, RegexOptions.IgnoreCase);
			Match aMatchB = Regex.Match(oVerB, COMPARE_VERSION_STRING_REGEX, RegexOptions.IgnoreCase);

			if (!aMatchA.Success || !aMatchB.Success)
			{
				// バージョン文字列ではない場合は、通常の文字列比較
				return String.Compare(oVerA, oVerB, true);
			}

			// バージョン番号部分の比較
			Double aVerNumA = Double.Parse(aMatchA.Groups[1].Value);
			Double aVerNumB = Double.Parse(aMatchB.Groups[1].Value);
			if (aVerNumA < aVerNumB)
			{
				return -1;
			}
			if (aVerNumA > aVerNumB)
			{
				return 1;
			}

			// 後続文字列（α, β）の比較
			String aSuffixA = aMatchA.Groups[2].Value.Trim();
			String aSuffixB = aMatchB.Groups[2].Value.Trim();
			if (String.IsNullOrEmpty(aSuffixA) && String.IsNullOrEmpty(aSuffixB))
			{
				return 0;
			}

			// 片方に後続文字列がある場合は、後続文字列の無い方（正式版）が大きい
			if (String.IsNullOrEmpty(aSuffixA))
			{
				return 1;
			}
			if (String.IsNullOrEmpty(aSuffixB))
			{
				return -1;
			}

			// 後続文字列同士を比較
			return String.Compare(aSuffixA, aSuffixB, true);
		}

		// --------------------------------------------------------------------
		// Base64 エンコード済みの暗号化データを復号し、元の文字列を返す
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static String DecryptString(String oSrc, String oPassword, String oSalt)
		{
			RijndaelManaged aRijndael = CreateRijndaelManaged(oPassword, oSalt);

			// Base64 デコード
			Byte[] aSrcBytes = Convert.FromBase64String(oSrc);

			// 復号化
			Byte[] aDecBytes;
			ICryptoTransform aDecryptor = aRijndael.CreateDecryptor();
			try
			{
				aDecBytes = aDecryptor.TransformFinalBlock(aSrcBytes, 0, aSrcBytes.Length);
			}
			finally
			{
				aDecryptor.Dispose();
			}

			return Encoding.Unicode.GetString(aDecBytes);
		}

		// --------------------------------------------------------------------
		// 深いコピーを行う
		// 対象は SerializableAttribute が付いているクラス
		// --------------------------------------------------------------------
		public static T DeepClone<T>(T oSrc)
		{
			Object aClone;
			using (MemoryStream aStream = new MemoryStream())
			{
				BinaryFormatter aFormatter = new BinaryFormatter();
				aFormatter.Serialize(aStream, oSrc);
				aStream.Position = 0;
				aClone = aFormatter.Deserialize(aStream);
			}
			return (T)aClone;
		}

		// --------------------------------------------------------------------
		// ZoneID を削除
		// ＜返値＞削除できたら true
		// --------------------------------------------------------------------
		public static Boolean DeleteZoneID(String oPath)
		{
			return WindowsApi.DeleteFile(oPath + STREAM_NAME_ZONE_ID);
		}

		// --------------------------------------------------------------------
		// ZoneID を削除（フォルダ配下のすべてのファイル）
		// ＜返値＞ファイル列挙で何らかのエラーが発生したら Error、削除できなくても Ok は返る
		// --------------------------------------------------------------------
		public static Boolean DeleteZoneID(String oFolder, SearchOption oOption)
		{
			try
			{
				String[] aFiles = Directory.GetFiles(oFolder, "*", oOption);
				foreach (String aFile in aFiles)
				{
					DeleteZoneID(aFile);
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// オブジェクトをデシリアライズして読み出し
		// クラスコンストラクタで List に要素を追加している場合、読み出した要素が置換ではなくさらに追加になることに注意
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static T Deserialize<T>(String oPath)
		{
			XmlSerializer aSerializer = new XmlSerializer(typeof(T));
			using (StreamReader aSR = new StreamReader(oPath, new UTF8Encoding(false)))
			{
				return (T)aSerializer.Deserialize(aSR);
			}
		}

		// --------------------------------------------------------------------
		// 文字列を暗号化し、文字列（Base64）で返す
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static String EncryptString(String oSrc, String oPassword, String oSalt)
		{
			RijndaelManaged aRijndael = CreateRijndaelManaged(oPassword, oSalt);

			// 暗号化
			Byte[] aSrcBytes = Encoding.Unicode.GetBytes(oSrc);
			Byte[] aEncBytes;
			ICryptoTransform aEncryptor = aRijndael.CreateEncryptor();
			try
			{
				aEncBytes = aEncryptor.TransformFinalBlock(aSrcBytes, 0, aSrcBytes.Length);
			}
			finally
			{
				aEncryptor.Dispose();
			}

			// Base64 エンコード
			return Convert.ToBase64String(aEncBytes);
		}

		// --------------------------------------------------------------------
		// セクションのない ini ファイルからペアを読み取る
		// ＜返値＞ ペア
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static SortedDictionary<String, String> LoadKeyAndValue(String oIniPath)
		{
			SortedDictionary<String, String> aKeyValuePairs = new SortedDictionary<String, String>();
			String[] aLines = File.ReadAllLines(oIniPath, Encoding.GetEncoding(Common.CODE_PAGE_SHIFT_JIS));
			foreach (String aLine in aLines)
			{
				Int32 aEqPos = aLine.IndexOf('=');
				if (aEqPos < 0)
				{
					aKeyValuePairs[aLine.Trim().ToLower()] = String.Empty;
				}
				else
				{
					aKeyValuePairs[aLine.Substring(0, aEqPos).Trim().ToLower()] = aLine.Substring(aEqPos + 1).Trim();
				}
			}
			return aKeyValuePairs;
		}

		// --------------------------------------------------------------------
		// oBase を基準とした oRelativePath の絶対パスを取得
		// ＜返値＞ 絶対パス
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
#if !NULLABLE_DISABLED
		public static String MakeAbsolutePath(String oBasePath, String? oRelativePath)
#else
		public static String MakeAbsolutePath(String oBasePath, String oRelativePath)
#endif
		{
			if (String.IsNullOrEmpty(oRelativePath))
			{
				return oBasePath;
			}

			// oBasePath の末尾が '\\' 1 つでないとうまく動作しない
			if (!String.IsNullOrEmpty(oBasePath) && oBasePath[oBasePath.Length - 1] != '\\')
			{
				oBasePath = oBasePath + "\\";
			}

			// Uri クラスのコンストラクターが勝手にデコードするので、予め "%" を "%25" にしておく
			oBasePath = oBasePath.Replace("%", "%25");
			oRelativePath = oRelativePath.Replace("%", "%25");

			// 絶対パス
			Uri aBaseUri = new Uri(oBasePath);
			Uri aAbsoluteUri = new Uri(aBaseUri, oRelativePath);
			String aAbsolutePath = aAbsoluteUri.LocalPath;

			// "%25" を "%" に戻す
			aAbsolutePath = aAbsolutePath.Replace("%25", "%");

			return aAbsolutePath;
		}

		// --------------------------------------------------------------------
		// oBase を基準とした相対パスを取得
		// ＜返値＞ 相対パス
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
#if !NULLABLE_DISABLED
		public static String? MakeRelativePath(String oBasePath, String oAbsolutePath)
#else
		public static String MakeRelativePath(String oBasePath, String oAbsolutePath)
#endif
		{
			if (String.IsNullOrEmpty(oAbsolutePath))
			{
				return null;
			}

			// oBasePath の末尾が '\\' 1 つでないとうまく動作しない
			if (oBasePath[oBasePath.Length - 1] != '\\')
			{
				oBasePath = oBasePath + "\\";
			}

			// Uri クラスのコンストラクターが勝手にデコードするので、予め "%" を "%25" にしておく
			oBasePath = oBasePath.Replace("%", "%25");
			oAbsolutePath = oAbsolutePath.Replace("%", "%25");

			// 相対パス
			Uri aBaseUri = new Uri(oBasePath);
			String aRelativePath = aBaseUri.MakeRelativeUri(new Uri(oAbsolutePath)).ToString();

			// 勝手に URL エンコードされるのでデコードする
			aRelativePath = Uri.UnescapeDataString(aRelativePath);

			// '/' を '\\' にする
			aRelativePath = aRelativePath.Replace('/', '\\');

			// "%25" を "%" に戻す
			aRelativePath = aRelativePath.Replace("%25", "%");

			return aRelativePath;
		}

#if USE_UNSAFE
		// --------------------------------------------------------------------
		// 2 つのバイト列が等しいかどうか
		// Int32 ではなく Boolean を返すので、MemCmp() とはしない
		// ＜返値＞ 等しければ true、双方 null なら無条件で true
		// --------------------------------------------------------------------
		public static Boolean MemEquals(Byte[] oBuf1, Byte[] oBuf2)
		{
			// 同じ実体を参照していれば true、双方 null ならここで true が返る
			if (Object.ReferenceEquals(oBuf1, oBuf2))
			{
				return true;
			}

			// 明らかにアンバランスなケース
			if (oBuf1 == null || oBuf2 == null || oBuf1.Length != oBuf2.Length)
			{
				return false;
			}

			// 比較
			unsafe
			{
				fixed (Byte* aFixBuf1 = oBuf1, aFixBuf2 = oBuf2)
				{
					return MemEqualsCore(aFixBuf1, aFixBuf2, oBuf1.Length);
				}

			}
		}

#endif

#if USE_UNSAFE
		// --------------------------------------------------------------------
		// バイト列 oHayStack の中から、目的のバイト列 oNeedle を探す
		// 検索は指定した位置から開始され、指定した数の位置が検査される（oStartIndex + oCount - 1 まで）
		// ＜返値＞ oNeedle の位置（oHayStack の先頭から数えて）。見つからない場合は -1
		// --------------------------------------------------------------------
		public static Int32 MemIndexOf(Byte[] oHayStack, Byte[] oNeedle, Int32 oStartIndex = 0)
		{
			if (oHayStack == null || oNeedle == null)
			{
				return -1;
			}
			return MemIndexOf(oHayStack, oNeedle, oStartIndex, oHayStack.Length - oNeedle.Length + 1 - oStartIndex);
		}

		// --------------------------------------------------------------------
		// バイト列 oHayStack の中から、目的のバイト列 oNeedle を探す
		// 検索は指定した位置から開始され、指定した数の位置が検査される（oStartIndex + oCount - 1 まで）
		// ＜返値＞ oNeedle の位置（oHayStack の先頭から数えて）。見つからない場合は -1
		// --------------------------------------------------------------------
		public static Int32 MemIndexOf(Byte[] oHayStack, Byte[] oNeedle, Int32 oStartIndex, Int32 oCount)
		{
			// 範囲チェック
			if (oHayStack == null || oHayStack.Length == 0 || oNeedle == null || oNeedle.Length == 0)
			{
				return -1;
			}
			if (oStartIndex < 0 || oCount <= 0)
			{
				return -1;
			}
			if (oStartIndex + oCount - 1 + oNeedle.Length > oHayStack.Length)
			{
				return -1;
			}

			// 検索
			unsafe
			{
				fixed (Byte* aFixHayStack = oHayStack, aFixNeedle = oNeedle)
				{
					for (Int32 i = 0; i < oCount; i++)
					{
						if (MemEqualsCore(aFixHayStack + oStartIndex + i, aFixNeedle, oNeedle.Length))
						{
							return oStartIndex + i;
						}
					}
				}
			}
			return -1;
		}
#endif

		// --------------------------------------------------------------------
		// 指定されたプロセスと同じ名前のプロセス（指定されたプロセスを除く）を列挙する
		// ＜返値＞ プロセス群（見つからない場合は空のリスト
		// --------------------------------------------------------------------
#if !NULLABLE_DISABLED
		public static List<Process> SameNameProcesses(Process? oSpecifyProcess = null)
#else
		public static List<Process> SameNameProcesses(Process oSpecifyProcess = null)
#endif
		{
			// プロセスが指定されていない場合は実行中のプロセスが指定されたものとする
			if (oSpecifyProcess == null)
			{
				oSpecifyProcess = Process.GetCurrentProcess();
			}

			Process[] aAllProcesses = Process.GetProcessesByName(oSpecifyProcess.ProcessName);
			List<Process> aSameNameProcesses = new List<Process>();

			foreach (Process aProcess in aAllProcesses)
			{
				if (aProcess.Id != oSpecifyProcess.Id)
				{
					aSameNameProcesses.Add(aProcess);
				}
			}

			return aSameNameProcesses;
		}

		// --------------------------------------------------------------------
		// オブジェクトをシリアライズして保存
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		public static void Serialize(String oPath, Object oObject)
		{
			XmlSerializer aSerializer = new XmlSerializer(oObject.GetType());
			using (StreamWriter aSW = new StreamWriter(oPath, false, new UTF8Encoding(false)))
			{
				aSerializer.Serialize(aSW, oObject);
			}
		}

		// --------------------------------------------------------------------
		// 全メンバを浅くコピーする
		// 新規インスタンスを作るのではなく、既存のインスタンスにコピーする
		// ApplicationSettingsBase 派生のクラスに対してはフィールドが取得できないためコピーできない
		// （this[] は取得できないのか？）
		// --------------------------------------------------------------------
		public static void ShallowCopy<T>(T oSrc, T oDest)
		{
			FieldInfo[] aFields = typeof(T).GetFields(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			foreach (FieldInfo aField in aFields)
			{
				aField.SetValue(oDest, aField.GetValue(oSrc));
			}
		}

		// --------------------------------------------------------------------
		// 文字列のうち、数値に見える部分を数値に変換
		// --------------------------------------------------------------------
#if !NULLABLE_DISABLED
		public static Int32 StringToInt32(String? oString)
#else
		public static Int32 StringToInt32(String oString)
#endif
		{
			if (String.IsNullOrEmpty(oString))
			{
				return 0;
			}

			Match aMatch = Regex.Match(oString, "-?[0-9]+");
			if (String.IsNullOrEmpty(aMatch.Value))
			{
				return 0;
			}
			return Int32.Parse(aMatch.Value);
		}

		// --------------------------------------------------------------------
		// 参照の入替
		// --------------------------------------------------------------------
		public static void Swap<T>(ref T oLhs, ref T oRhs)
		{
			T aTemp = oLhs;
			oLhs = oRhs;
			oRhs = aTemp;
		}

		// --------------------------------------------------------------------
		// 設定保存用フォルダーのパス（末尾 '\\'）
		// フォルダーが存在しない場合は作成する
		// --------------------------------------------------------------------
		public static String UserAppDataFolderPath()
		{
			String path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify)
					+ "\\" + Common.FOLDER_NAME_SHINTA + Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location) + "\\";
			try
			{
				Directory.CreateDirectory(path);
			}
			catch (Exception)
			{
			}
			return path;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		private const String COMPARE_VERSION_STRING_REGEX = @"Ver ([0-9]+\.[0-9]+)(.*)";
		private const String REG_KEY_DOT_NET_45_VERSION = "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\";
		private const String STREAM_NAME_ZONE_ID = ":Zone.Identifier";

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化済みラインダールオブジェクトを返す
		// EncryptString / DecryptString で使用
		// oSalt は 8 バイト以上（4 文字以上）である必要がある
		// ＜例外＞
		// --------------------------------------------------------------------
		private static RijndaelManaged CreateRijndaelManaged(String oPassword, String oSalt)
		{
			RijndaelManaged aRijndael = new RijndaelManaged();

			// salt をバイト化
			Byte[] aSaltBytes = Encoding.Unicode.GetBytes(oSalt);

			// パスワードから共有キーと初期化ベクタを作成する
			Rfc2898DeriveBytes aDeriveBytes = new Rfc2898DeriveBytes(oPassword, aSaltBytes);
			aDeriveBytes.IterationCount = 1000;
			aRijndael.Key = aDeriveBytes.GetBytes(aRijndael.KeySize / 8);
			aRijndael.IV = aDeriveBytes.GetBytes(aRijndael.BlockSize / 8);

			return aRijndael;
		}

#if USE_UNSAFE
		// --------------------------------------------------------------------
		// メモリ比較
		// oBuf1、obuf2 の長さは oLength 以上である前提
		// --------------------------------------------------------------------
		private static unsafe Boolean MemEqualsCore(Byte* oBuf1, Byte* oBuf2, Int32 oLength)
		{
			// 8 バイトずつ比較
			Int64* aBuf164 = (Int64*)oBuf1;
			Int64* aBuf264 = (Int64*)oBuf2;
			while (oLength >= 8)
			{
				if (*aBuf164 != *aBuf264)
				{
					return false;
				}
				aBuf164++;
				aBuf264++;
				oLength -= 8;
			}

			// 1 バイトずつ比較
			Byte* aBuf18 = (Byte*)aBuf164;
			Byte* aBuf28 = (Byte*)aBuf264;
			while (oLength > 0)
			{
				if (*aBuf18 != *aBuf28)
				{
					return false;
				}
				aBuf18++;
				aBuf28++;
				oLength--;
			}

			return true;
		}
#endif
	}
	// public class Common ___END___

	// ====================================================================
	// シリアライズ用構造体
	// ====================================================================

	// --------------------------------------------------------------------
	// シリアライズ可能なキーと値のペア
	// NameValueCollection、Dictionary などはシリアライズできないため、
	// 本クラスでペアを作成した上で、シリアライズ可能な List に詰め込む
	// なぜか System.Collections.Generic.KeyValuePair を使うとうまくシリアライズできない
	// --------------------------------------------------------------------
	[Serializable]
	public struct SerializableKeyValuePair<TKey, TValue>
	{
		public TKey Key { get; set; }
		public TValue Value { get; set; }

		public SerializableKeyValuePair(TKey oKey, TValue oValue)
			: this()
		{
			Key = oKey;
			Value = oValue;
		}
	}
	// public struct SerializableKeyValuePair<TKey, TValue> ___END___

	// ====================================================================
	// 多重起動例外
	// ====================================================================

	public class MultiInstanceException : Exception
	{
	}
	// public class MultiInstanceException ___END___

}
// namespace Shinta ___END___

