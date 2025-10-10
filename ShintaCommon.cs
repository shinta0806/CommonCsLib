// ============================================================================
// 
// よく使う一般的な定数や関数（OS に依存しないもの）
// Copyright (C) 2014-2025 by SHINTA
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
// (3.52) | 2020/05/05 (Tue) |   MakeAbsolutePath() の null 許容参照型を無効化できるようにした。
// (3.53) | 2020/05/16 (Sat) |   Deserialize() 引数の多態性に対応。
// (3.54) | 2020/05/16 (Sat) |   MakeRelativePath() の引数チェックを強化。
// (3.55) | 2020/05/19 (Tue) |   Deserialize() スペースをデシリアライズできるようにした。
// (3.56) | 2020/11/15 (Sun) |   null 許容参照型の対応強化。
// (3.57) | 2020/11/15 (Sun) |   UserAppDataFolderPath() .NET 5 の単一ファイルに対応。
// (3.58) | 2021/03/28 (Sun) |   一部の関数を ShintaCommonWindows に移管。
// (3.59) | 2021/04/04 (Sun) |   ShallowCopy() 実体が派生クラスの場合もコピーできるようにした。
//  3.60  | 2021/05/24 (Mon) | ShallowCopyProperties() を作成。
//  3.70  | 2021/09/04 (Sat) | TempFolderPath() を作成。
// (3.71) | 2021/09/04 (Sat) |   null 許容参照型を必須にした。
//  3.80  | 2021/10/23 (Sat) | ShellExecute() を作成。
// (3.81) | 2021/11/14 (Sun) |   拡張子を追加。
// (3.82) | 2022/02/20 (Sun) |   MessageKey を追加。
//  3.90  | 2022/02/20 (Sun) | SelectFiles() を作成。
//  4.00  | 2022/02/20 (Sun) | SelectFolder() を作成。
//  4.10  | 2022/02/20 (Sun) | SelectFolders() を作成。
//  4.20  | 2022/02/25 (Fri) | OpenMicrosoftStore() を作成。
// (4.21) | 2022/05/22 (Sun) |   拡張子を追加。
//  4.30  | 2022/05/22 (Sun) | TempPath() を作成。
// (4.31) | 2022/12/04 (Sun) |   拡張子を追加。
//  4.40  | 2023/05/27 (Sat) | ShallowCopyFieldsDeclaredOnly() を作成。
//  4.50  | 2023/06/10 (Sat) | DeleteFileIfEmpty() を作成。
//  4.60  | 2023/08/19 (Sat) | AppId() を作成。
// (4.61) | 2024/01/13 (Sat) |   StringToInt32() を改善。
// (4.62) | 2024/05/09 (Thu) |   拡張子を追加。
//  4.70  | 2024/09/28 (Sat) | ExceptionMessage() を作成。
// (4.71) | 2025/10/10 (Fri) |   メモリ系を obsolete にした。
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   CsWin32
// ----------------------------------------------------------------------------

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Shinta;

// ====================================================================
// 共用ユーティリティークラス
// ====================================================================

public partial class Common
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
	public const String FOLDER_NAME_SETTINGS = "Settings\\";
	public const String FOLDER_NAME_SHINTA = SHINTA + "\\";

	// --------------------------------------------------------------------
	// よく使うファイル
	// --------------------------------------------------------------------
	public const String FILE_NAME_USER_CONFIG = "user" + FILE_EXT_CONFIG;

	// --------------------------------------------------------------------
	// よく使う拡張子
	// --------------------------------------------------------------------
	public const String FILE_EXT_AAC = ".aac";
	public const String FILE_EXT_AVI = ".avi";
	public const String FILE_EXT_BAK = ".bak";
	public const String FILE_EXT_BMP = ".bmp";
	public const String FILE_EXT_CONFIG = ".config";
	public const String FILE_EXT_CS = ".cs";
	public const String FILE_EXT_CSS = ".css";
	public const String FILE_EXT_CSV = ".csv";
	public const String FILE_EXT_DDS = ".dds";
	public const String FILE_EXT_DLL = ".dll";
	public const String FILE_EXT_DNG = ".dng";
	public const String FILE_EXT_EXE = ".exe";
	public const String FILE_EXT_FLV = ".flv";
	public const String FILE_EXT_GIF = ".gif";
	public const String FILE_EXT_HTML = ".html";
	public const String FILE_EXT_ICO = ".ico";
	public const String FILE_EXT_INI = ".ini";
	public const String FILE_EXT_JPEG = ".jpeg";
	public const String FILE_EXT_JPG = ".jpg";
	public const String FILE_EXT_JS = ".js";
	public const String FILE_EXT_JSON = ".json";
	public const String FILE_EXT_JXR = ".jxr";
	public const String FILE_EXT_KRA = ".kra";
	public const String FILE_EXT_LNK = ".lnk";
	public const String FILE_EXT_LOCK = ".lock";
	public const String FILE_EXT_LOG_ARCHIVE = ".lga";
	public const String FILE_EXT_LOG = ".log";
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
	public const String FILE_EXT_RESW = ".resw";
	public const String FILE_EXT_SQLITE3 = ".sqlite3";
	public const String FILE_EXT_SQLITE3_SHM = ".sqlite3-shm";
	public const String FILE_EXT_SQLITE3_WAL = ".sqlite3-wal";
	public const String FILE_EXT_SETTINGS_ARCHIVE = ".sta";
	public const String FILE_EXT_TIF = ".tif";
	public const String FILE_EXT_TIFF = ".tiff";
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
	// MessageKey
	// --------------------------------------------------------------------

	// ウィンドウをアクティブ化する
	public const String MESSAGE_KEY_WINDOW_ACTIVATE = "Activate";

	// ウィンドウを閉じる
	public const String MESSAGE_KEY_WINDOW_CLOSE = "Close";

	// 開くダイアログを開く
	public const String MESSAGE_KEY_OPEN_OPEN_FILE_DIALOG = "OpenOpenFileDialog";

	// 保存ダイアログを開く
	public const String MESSAGE_KEY_OPEN_SAVE_FILE_DIALOG = "OpenSaveFileDialog";

	// --------------------------------------------------------------------
	// その他
	// --------------------------------------------------------------------

	// 開くダイアログ・保存ダイアログ用の追加フィルター（旧式）
	public const String OPEN_SAVE_DIALOG_ADDITIONAL_FILTER = "|すべてのファイル|*.*";

	// 開くダイアログ・保存ダイアログ用の先頭フィルター（新式）
	public const String OPEN_SAVE_DIALOG_ALL_TYPE_FILTER = "すべてのファイル|*.*|";

	// 一般的なスレッドスリープ時間 [ms]
	public const Int32 GENERAL_SLEEP_TIME = 20;

	// SHINTA
	public const String SHINTA = "SHINTA";

	// TraceSource のデフォルトリスナー
	public const String TRACE_SOURCE_DEFAULT_LISTENER_NAME = "Default";

	// DPI 標準値
	public const Double DEFAULT_DPI = 96.0;

	// --------------------------------------------------------------------
	// よく使われるローカライズキー
	// --------------------------------------------------------------------

	/// <summary>
	/// アプリの基本情報
	/// </summary>
	public const String LK_GENERAL_APP_NAME = "0_AppDisplayName";
#if DISTRIB_STORE
	public const String LK_GENERAL_APP_DISTRIB = "0_AppDistributionStore";
#else
	public const String LK_GENERAL_APP_DISTRIB = "0_AppDistributionZip";
#endif

	/// <summary>
	/// ラベル
	/// </summary>
	public const String LK_GENERAL_LABEL_CONFIRM = "Confirm";
	public const String LK_GENERAL_LABEL_NO = "No";
	public const String LK_GENERAL_LABEL_OK = "Ok";
	public const String LK_GENERAL_LABEL_YES = "Yes";

	// ====================================================================
	// public メンバー関数
	// ====================================================================

	/// <summary>
	/// アプリケーション ID
	/// </summary>
	/// <returns></returns>
	public static String AppId()
	{
		return Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
	}

#pragma warning disable SYSLIB1045
	// --------------------------------------------------------------------
	// バージョン文字列を比較（大文字小文字は区別しない）
	// ＜返値＞ -1: verA が小さい, 1: verA が大きい
	// --------------------------------------------------------------------
	public static Int32 CompareVersionString(String verA, String verB)
	{
		// 最初に同じ文字列かどうか確認
		if (String.Compare(verA, verB, true) == 0)
		{
			return 0;
		}
		if (String.IsNullOrEmpty(verA) && String.IsNullOrEmpty(verB))
		{
			return 0;
		}

		// いずれかが IsNullOrEmpty() ならそちらが小さいとする
		if (String.IsNullOrEmpty(verA))
		{
			return -1;
		}
		if (String.IsNullOrEmpty(verB))
		{
			return 1;
		}

		// 解析
		Match matchA = Regex.Match(verA, COMPARE_VERSION_STRING_REGEX, RegexOptions.IgnoreCase);
		Match matchB = Regex.Match(verB, COMPARE_VERSION_STRING_REGEX, RegexOptions.IgnoreCase);

		if (!matchA.Success || !matchB.Success)
		{
			// バージョン文字列ではない場合は、通常の文字列比較
			return String.Compare(verA, verB, true);
		}

		// バージョン番号部分の比較
		Double verNumA = Double.Parse(matchA.Groups[1].Value);
		Double verNumB = Double.Parse(matchB.Groups[1].Value);
		if (verNumA < verNumB)
		{
			return -1;
		}
		if (verNumA > verNumB)
		{
			return 1;
		}

		// 後続文字列（α, β）の比較
		String suffixA = matchA.Groups[2].Value.Trim();
		String suffixB = matchB.Groups[2].Value.Trim();
		if (String.IsNullOrEmpty(suffixA) && String.IsNullOrEmpty(suffixB))
		{
			return 0;
		}

		// 片方に後続文字列がある場合は、後続文字列の無い方（正式版）が大きい
		if (String.IsNullOrEmpty(suffixA))
		{
			return 1;
		}
		if (String.IsNullOrEmpty(suffixB))
		{
			return -1;
		}

		// 後続文字列同士を比較
		return String.Compare(suffixA, suffixB, true);
	}
#pragma warning restore SYSLIB1045

#if USE_OBSOLETE2
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
#endif

	/// <summary>
	/// ファイルが空なら削除
	/// 主に FileSavePicker が勝手に作成するファイルを削除する用途
	/// </summary>
	/// <param name="path"></param>
	/// <returns>ファイルを削除した場合は true</returns>
	public static Boolean DeleteFileIfEmpty(String path)
	{
		Boolean result = false;
		try
		{
			if (new FileInfo(path).Length == 0)
			{
				File.Delete(path);
				result = true;
			}
		}
		catch
		{
		}
		return result;
	}

	// --------------------------------------------------------------------
	// テンポラリフォルダーを削除
	// --------------------------------------------------------------------
	public static Boolean DeleteTempFolder()
	{
		Boolean result = false;
		try
		{
			Directory.Delete(TempFolderPath(), true);
			result = true;
		}
		catch
		{
		}
		return result;
	}

#if !USE_AOT || USE_XML_SERIALIZER
	// --------------------------------------------------------------------
	// オブジェクトをデシリアライズして読み出し
	// オブジェクトのクラスコンストラクターが実行されるため、例えばコンストラクター内で List に要素を追加している場合、読み出した要素が置換ではなくさらに追加になることに注意
	// ＜例外＞ Exception
	// --------------------------------------------------------------------
	public static T Deserialize<T>(String path, T obj) where T : notnull
	{
		XmlSerializer xmlSerializer = new(obj.GetType());
		using StreamReader streamReader = new(path, new UTF8Encoding(false));
		XmlDocument xmlDocument = new()
		{
			PreserveWhitespace = true
		};
		xmlDocument.Load(streamReader);
		if (xmlDocument.DocumentElement == null)
		{
			throw new Exception("xmlDocument.DocumentElement is null");
		}
		using XmlNodeReader xmlNodeReader = new(xmlDocument.DocumentElement);
		Object? des = xmlSerializer.Deserialize(xmlNodeReader) ?? throw new Exception("Deserialize result is null");
		return (T)des;
	}
#endif

#if USE_OBSOLETE2
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
#endif

	/// <summary>
	/// 表示する例外メッセージ
	/// </summary>
	/// <param name="caption"></param>
	/// <param name="ex"></param>
	/// <returns></returns>
	public static String ExceptionMessage(String caption, Exception ex)
	{
		String message = caption + "：\n" + ex.Message;
		if (ex.InnerException != null)
		{
			message += "\n詳細：\n" + ex.InnerException.Message;
		}
		return message;
	}

	// --------------------------------------------------------------------
	// テンポラリフォルダーを初期化
	// --------------------------------------------------------------------
	public static Boolean InitializeTempFolder()
	{
		Boolean result = false;
		try
		{
			String tempFolderPath = Common.TempFolderPath();
			if (Directory.Exists(tempFolderPath))
			{
				// 偶然以前と同じ PID となり、かつ、以前異常終了してテンポラリフォルダーが削除されていない場合に削除
				Directory.Delete(tempFolderPath, true);
			}

			// 空のフォルダーを作成
			Directory.CreateDirectory(tempFolderPath);

			result = true;
		}
		catch
		{
		}
		return result;
	}

	// --------------------------------------------------------------------
	// セクションのない ini ファイルからペアを読み取る
	// ＜返値＞ ペア
	// ＜例外＞ Exception
	// --------------------------------------------------------------------
	public static SortedDictionary<String, String> LoadKeyAndValue(String iniPath)
	{
		SortedDictionary<String, String> keyValuePairs = [];
		String[] lines = File.ReadAllLines(iniPath, Encoding.GetEncoding(Common.CODE_PAGE_SHIFT_JIS));
		foreach (String line in lines)
		{
			Int32 eqPos = line.IndexOf('=');
			if (eqPos < 0)
			{
				keyValuePairs[line.Trim().ToLower()] = String.Empty;
			}
			else
			{
				keyValuePairs[line[0..eqPos].Trim().ToLower()] = line[(eqPos + 1)..^0].Trim();
			}
		}
		return keyValuePairs;
	}

	// --------------------------------------------------------------------
	// basePath を基準とした相対パスを取得
	// ＜返値＞ 相対パス
	// ＜例外＞ Exception
	// --------------------------------------------------------------------
	public static String MakeRelativePath(String? basePath, String? absolutePath)
	{
		basePath ??= String.Empty;
		if (String.IsNullOrEmpty(absolutePath))
		{
			return String.Empty;
		}

		// basePath の末尾が '\\' 1 つでないとうまく動作しない
		if (String.IsNullOrEmpty(basePath) || basePath[^1] != '\\')
		{
			basePath += "\\";
		}

		// Uri クラスのコンストラクターが勝手にデコードするので、予め "%" を "%25" にしておく
		basePath = basePath.Replace("%", "%25");
		absolutePath = absolutePath.Replace("%", "%25");

		// 相対パス
		Uri baseUri = new(basePath);
		String relativePath = baseUri.MakeRelativeUri(new Uri(absolutePath)).ToString();

		// 勝手に URL エンコードされるのでデコードする
		relativePath = Uri.UnescapeDataString(relativePath);

		// '/' を '\\' にする
		relativePath = relativePath.Replace('/', '\\');

		// "%25" を "%" に戻す
		relativePath = relativePath.Replace("%25", "%");

		return relativePath;
	}

#if OBSOLETE
#if USE_UNSAFE
	// --------------------------------------------------------------------
	// 2 つのバイト列が等しいかどうか
	// Int32 ではなく Boolean を返すので、MemCmp() とはしない
	// ＜返値＞ 等しければ true、双方 null なら無条件で true
	// ＜obsolete> oBuf1.SequenceEqual(oBuf2) を使用すれば良い
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
#endif

#if OBSOLETE
#if USE_UNSAFE
	// --------------------------------------------------------------------
	// バイト列 oHayStack の中から、目的のバイト列 oNeedle を探す
	// 検索は指定した位置から開始され、指定した数の位置が検査される（oStartIndex + oCount - 1 まで）
	// ＜返値＞ oNeedle の位置（oHayStack の先頭から数えて）。見つからない場合は -1
	// ＜obsolete> oHayStack.AsSpan(oStartIndex, oCount).IndexOf(oNeedle) を使用すれば良い
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
#endif

	// --------------------------------------------------------------------
	// ストアアプリでアプリページを開く
	// ＜例外＞ Exception
	// --------------------------------------------------------------------
	public static void OpenMicrosoftStore(String productId)
	{
		ShellExecute("ms-windows-store://pdp/?ProductId=" + productId);
	}

	// --------------------------------------------------------------------
	// 指定されたプロセスと同じ名前のプロセス（指定されたプロセスを除く）を列挙する
	// ＜返値＞ プロセス群（見つからない場合は空のリスト
	// --------------------------------------------------------------------
	public static List<Process> SameNameProcesses(Process? specifyProcess = null)
	{
		// プロセスが指定されていない場合は実行中のプロセスが指定されたものとする
		specifyProcess ??= Process.GetCurrentProcess();

		Process[] allProcesses = Process.GetProcessesByName(specifyProcess.ProcessName);
		List<Process> sameNameProcesses = [];

		foreach (Process process in allProcesses)
		{
			if (process.Id != specifyProcess.Id)
			{
				sameNameProcesses.Add(process);
			}
		}

		return sameNameProcesses;
	}

	// --------------------------------------------------------------------
	// フォルダー・ファイル混じりのパスからファイルのみを取得
	// --------------------------------------------------------------------
	public static List<String> SelectFiles(String[] pathes)
	{
		List<String> files = [];
		foreach (String path in pathes)
		{
			if (File.Exists(path))
			{
				files.Add(path);
			}
		}
		return files;
	}

	// --------------------------------------------------------------------
	// フォルダー・ファイル混じりのパスから指定された拡張子のファイルのみを取得
	// ＜引数＞ exts: 小文字前提
	// --------------------------------------------------------------------
	public static List<String> SelectFiles(String[] pathes, IEnumerable<String> exts)
	{
		List<String> files = [];
		foreach (String path in pathes)
		{
			if (!File.Exists(path))
			{
				continue;
			}

			if (exts.Contains(Path.GetExtension(path).ToLower()))
			{
				files.Add(path);
			}
		}
		return files;
	}

	// --------------------------------------------------------------------
	// フォルダー・ファイル混じりのパスからフォルダーを 1 つ取得
	// フォルダーが 1 つもない場合は、ファイルのフォルダーを 1 つ取得
	// --------------------------------------------------------------------
	public static String? SelectFolder(String[] pathes)
	{
		String? folderFromFile = null;
		foreach (String path in pathes)
		{
			if (Directory.Exists(path))
			{
				// フォルダーが見つかった場合は、そのフォルダーで確定する
				return path;
			}
			if (String.IsNullOrEmpty(folderFromFile) && File.Exists(path))
			{
				// ファイルが見つかった場合は、そのファイルを含むフォルダーを候補とする
				// フォルダーが見つかればフォルダー優先のため、ループは継続
				folderFromFile = Path.GetDirectoryName(path);
			}
		}

		// フォルダーが見つからなかった場合はファイルからのフォルダーを返す
		return folderFromFile;
	}

	// --------------------------------------------------------------------
	// フォルダー・ファイル混じりのパスからフォルダーのみを取得
	// --------------------------------------------------------------------
	public static List<String> SelectFolders(String[] pathes)
	{
		List<String> folders = [];
		foreach (String path in pathes)
		{
			if (Directory.Exists(path))
			{
				folders.Add(path);
			}
		}
		return folders;
	}

#if !USE_AOT || USE_XML_SERIALIZER
	// --------------------------------------------------------------------
	// オブジェクトをシリアライズして保存
	// ＜例外＞ Exception
	// --------------------------------------------------------------------
	public static void Serialize(String path, Object obj)
	{
		XmlSerializer xmlSerializer = new(obj.GetType());
		using StreamWriter streamWriter = new(path, false, new UTF8Encoding(false));
		xmlSerializer.Serialize(streamWriter, obj);
	}
#endif

#if !USE_AOT
	// --------------------------------------------------------------------
	// 全フィールドを浅くコピーする
	// インスタンスの実クラスの基底クラスも含めてコピーするが、基底クラスの private フィールドはコピーできないことに注意
	// 新規インスタンスを作るのではなく、既存のインスタンスにコピーする
	// --------------------------------------------------------------------
	public static void ShallowCopyFields<T>(T src, T dest) where T : notnull
	{
		FieldInfo[] fields = src.GetType().GetFields(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		ShallowCopyFieldsCore(src, dest, fields);
	}
#endif

#if !USE_AOT
	// --------------------------------------------------------------------
	// 全フィールドを浅くコピーする（T 型の階層のレベルで宣言されたメンバーのみ）
	// インスタンスの実クラスではなく T としてコピーする。private フィールドもコピーできる
	// 新規インスタンスを作るのではなく、既存のインスタンスにコピーする
	// --------------------------------------------------------------------
	public static void ShallowCopyFieldsDeclaredOnly<T>(T src, T dest) where T : notnull
	{
		FieldInfo[] fields = typeof(T).GetFields(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
				BindingFlags.DeclaredOnly);
		ShallowCopyFieldsCore(src, dest, fields);
	}
#endif

#if !USE_AOT
	// --------------------------------------------------------------------
	// 全プロパティーを浅くコピーする
	// インスタンスの実クラスの基底クラスも含めてコピーするが、基底クラスの private プロパティーはコピーできないことに注意
	// 新規インスタンスを作るのではなく、既存のインスタンスにコピーする
	// --------------------------------------------------------------------
	public static void ShallowCopyProperties<T>(T src, T dest) where T : notnull
	{
		PropertyInfo[] propertyInfos = src.GetType().GetProperties(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in propertyInfos)
		{
			try
			{
				propertyInfo.SetValue(dest, propertyInfo.GetValue(src));
			}
			catch (Exception)
			{
			}
		}
	}
#endif

	// --------------------------------------------------------------------
	// 関連付けられたファイルを開く
	// ＜例外＞ Exception
	// --------------------------------------------------------------------
	public static void ShellExecute(String path)
	{
		ProcessStartInfo psi = new()
		{
			FileName = path,
			UseShellExecute = true,
		};
		Process.Start(psi);
	}

	// --------------------------------------------------------------------
	// 文字列のうち、数値に見える部分を数値に変換
	// --------------------------------------------------------------------
	public static Int32 StringToInt32(String? str)
	{
		if (String.IsNullOrEmpty(str))
		{
			return 0;
		}

		Match match = GeneratedRegexNumbers().Match(str);
		if (String.IsNullOrEmpty(match.Value))
		{
			return 0;
		}
		return Int32.Parse(match.Value);
	}

	// --------------------------------------------------------------------
	// 参照の入替
	// --------------------------------------------------------------------
	public static void Swap<T>(ref T lhs, ref T rhs)
	{
		(rhs, lhs) = (lhs, rhs);
	}

	// --------------------------------------------------------------------
	// テンポラリフォルダーのパス（末尾 '\\'）
	// --------------------------------------------------------------------
	public static String TempFolderPath()
	{
		return Path.GetTempPath() + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]) + '\\' + Environment.ProcessId.ToString() + '\\';
	}

	// --------------------------------------------------------------------
	// テンポラリフォルダー配下のファイル・フォルダー名として使えるパス（呼びだす度に異なるファイル、拡張子なし）
	// --------------------------------------------------------------------
	public static String TempPath()
	{
		// マルチスレッドでも安全にインクリメント
		Int32 counter = Interlocked.Increment(ref _tempPathCounter);
		return TempFolderPath() + counter.ToString() + "_" + Environment.CurrentManagedThreadId.ToString();
	}

	// --------------------------------------------------------------------
	// 設定保存用フォルダーのパス（末尾 '\\'）
	// フォルダーが存在しない場合は作成する
	// --------------------------------------------------------------------
	public static String UserAppDataFolderPath()
	{
		String path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify)
				+ "\\" + FOLDER_NAME_SHINTA + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]) + "\\";
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
	//private const String REG_KEY_DOT_NET_45_VERSION = "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\";

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// StringToInt32() 用 Regex（.NET 7 以降で使用可能）
	/// </summary>
	/// <returns></returns>
	[GeneratedRegex("-?[0-9]+")]
	private static partial Regex GeneratedRegexNumbers();

	// TempPath() 用カウンター（同じスレッドでもファイル名が分かれるようにするため）
	private static Int32 _tempPathCounter;

	// ====================================================================
	// private メンバー関数
	// ====================================================================

#if USE_OBSOLETE2
	// --------------------------------------------------------------------
	// 初期化済みラインダールオブジェクトを返す
	// EncryptString / DecryptString で使用
	// oSalt は 8 バイト以上（4 文字以上）である必要がある
	// ＜例外＞
	// --------------------------------------------------------------------
	private static RijndaelManaged CreateRijndaelManaged(String password, String salt)
	{
		RijndaelManaged rijndael = new();

		// salt をバイト化
		Byte[] saltBytes = Encoding.Unicode.GetBytes(salt);

		// パスワードから共有キーと初期化ベクタを作成する
		Rfc2898DeriveBytes aDeriveBytes = new(password, saltBytes);
		aDeriveBytes.IterationCount = 1000;
		rijndael.Key = aDeriveBytes.GetBytes(rijndael.KeySize / 8);
		rijndael.IV = aDeriveBytes.GetBytes(rijndael.BlockSize / 8);

		return rijndael;
	}
#endif

#if OBSOLETE
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
#endif

	// --------------------------------------------------------------------
	// 全フィールドを浅くコピーする
	// --------------------------------------------------------------------
	private static void ShallowCopyFieldsCore<T>(T src, T dest, FieldInfo[] fields) where T : notnull
	{
		foreach (FieldInfo field in fields)
		{
			field.SetValue(dest, field.GetValue(src));
		}
	}

}
// public class Common ___END___

// ====================================================================
// シリアライズ用構造体
// ====================================================================

// --------------------------------------------------------------------
// シリアライズ可能なキーと値のペア
// NameValueCollection、Dictionary などはシリアライズできないため、
// 本クラスでペアを作成した上で、シリアライズ可能な List に詰め込む
// System.Collections.Generic.KeyValuePair はプロパティーの setter がないためシリアライズできない
// --------------------------------------------------------------------
[Serializable]
public struct SerializableKeyValuePair<TKey, TValue>
{
	public TKey Key { get; set; }
	public TValue Value { get; set; }

	public SerializableKeyValuePair(TKey key, TValue value)
		: this()
	{
		Key = key;
		Value = value;
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
// namespace Shinta ___END___

