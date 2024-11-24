// ============================================================================
// 
// 漢字からフリガナを得る
// Copyright (C) 2021-2024 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// Input Method Editor Reference
// https://learn.microsoft.com/en-us/previous-versions/office/developer/office-2007/ee828920(v=office.12)?redirectedfrom=MSDN
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2021/06/13 (Sun) | ゆかりすたー 4 NEBULA のファーストバージョン。
// (1.01) | 2024/11/12 (Tue) |   SHINTA 共通 C# ライブラリー化。
//  1.10  | 2024/11/13 (Wed) | IsReady() を作成。
// ============================================================================

using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Input.Ime;

namespace Shinta;

internal class RubyReconverter : IDisposable
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public RubyReconverter()
	{
		Type? type = Type.GetTypeFromProgID("MSIME.Japan");
		if (type != null)
		{
			_ime = Activator.CreateInstance(type) as IFELanguage2;
			if (_ime != null)
			{
				if (_ime.Open() != 0)
				{
					_ime.Close();
					_ime = null;
				}
			}
		}
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// IDisposable.Dispose()
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// 変換準備が整っているか
	/// </summary>
	/// <returns></returns>
	public Boolean IsReady()
	{
		return _ime != null;
	}

	/// <summary>
	/// 漢字をひらがなに変換
	/// </summary>
	/// <param name="kanji">漢字</param>
	/// <returns>変換できない場合は null</returns>
	public String? Reconvert(String? kanji)
	{
		if (_ime == null || String.IsNullOrEmpty(kanji))
		{
			return null;
		}

		// GetPhonetic() の start の先頭文字は 1（0 ではないことに注意）
		if (_ime.GetPhonetic(kanji, 1, -1, out String hiragana) != 0)
		{
			return null;
		}

		return hiragana;
	}

#if USE_UNSAFE
	/// <summary>
	/// 漢字をひらがなに変換（ブロックごとの対応付き）
	/// </summary>
	/// <param name="kanji">漢字</param>
	/// <returns>ひらがな, ブロックごとの漢字, ブロックごとのひらがな</returns>
	public (String?, String[], String[]) ReconvertDetail(String? kanji)
	{
		if (_ime == null || String.IsNullOrEmpty(kanji))
		{
			return (null, Array.Empty<String>(), Array.Empty<String>());
		}

		if (_ime.GetMorphResult(PInvoke.FELANG_REQ_REV, PInvoke.FELANG_CMODE_HIRAGANAOUT, kanji.Length, kanji, IntPtr.Zero, out IntPtr result) != 0)
		{
			return (null, Array.Empty<String>(), Array.Empty<String>());
		}
		MORRSLT morResult = Marshal.PtrToStructure<MORRSLT>(result);

		// 返値準備
		String hiragana;
		String[] kanjiBlocks = new String[morResult.cWDD];
		String[] hiraganaBlocks = new String[morResult.cWDD];

		unsafe
		{
			hiragana = new(morResult.pwchOutput, 0, morResult.cchOutput);
			for (Int32 i = 0; i < morResult.cWDD; i++)
			{
				WDD wdd = Marshal.PtrToStructure<WDD>((nint)(morResult.pWDD) + Marshal.SizeOf<WDD>() * i);
				kanjiBlocks[i] = kanji[wdd.Anonymous1.wReadPos..(wdd.Anonymous1.wReadPos + wdd.Anonymous2.cchRead)];
				hiraganaBlocks[i] = hiragana[wdd.wDispPos..(wdd.wDispPos + wdd.cchDisp)];
			}

		}
		Marshal.FreeCoTaskMem(result);
		return (hiragana, kanjiBlocks, hiraganaBlocks);
	}
#endif

	public void test()
	{
		if (_ime == null)
		{
			return;
		}

		String kanji = "今夜は月が綺麗";
		_ime.GetMorphResult(PInvoke.FELANG_REQ_REV, PInvoke.FELANG_CMODE_HIRAGANAOUT, kanji.Length, kanji, IntPtr.Zero, out IntPtr result);
#if false
		MORRSLT[] morResults = new MORRSLT[kanji.Length];
		for (Int32 i = 0; i < kanji.Length; i++) {
			morResults[i] = Marshal.PtrToStructure<MORRSLT>(result + Marshal.SizeOf<MORRSLT>() * i);
		}
#endif
		// result は配列ではないっぽい
		MORRSLT morResult = Marshal.PtrToStructure<MORRSLT>(result);

		unsafe
		{
			String srcString = new(morResult.Anonymous1.pwchRead, 0, morResult.Anonymous2.cchRead);
			Debug.WriteLine("test() 元文章：" + srcString);
			String resultString = new(morResult.pwchOutput, 0, morResult.cchOutput);
			Debug.WriteLine("test() 逆変換：" + resultString);
		}

		unsafe
		{
			for (Int32 i = 0; i < morResult.cWDD; i++)
			{
				WDD wdd = Marshal.PtrToStructure<WDD>((nint)(morResult.pWDD) + Marshal.SizeOf<WDD>() * i);
				Debug.WriteLine("test() wdd " + i + " wDispPos: " + wdd.wDispPos + " wReadPos: " + wdd.Anonymous1.wReadPos);
			}
		}

		Marshal.FreeCoTaskMem(result);
	}

	// ====================================================================
	// protected 関数
	// ====================================================================

	/// <summary>
	/// リソース解放
	/// </summary>
	/// <param name="isDisposing"></param>
	protected virtual void Dispose(Boolean isDisposing)
	{
		if (_isDisposed)
		{
			return;
		}

		// マネージドリソース解放
		if (isDisposing)
		{
			_ime?.Close();
		}

		// アンマネージドリソース解放
		// 今のところ無し
		// アンマネージドリソースを持つことになった場合、ファイナライザの実装が必要

		// 解放完了
		_isDisposed = true;
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// IME
	/// </summary>
	private readonly IFELanguage2? _ime;

	/// <summary>
	/// Dispose フラグ
	/// </summary>
	private Boolean _isDisposed;
}

// ====================================================================
// IFE Language 2 Interface
// https://learn.microsoft.com/en-us/previous-versions/office/developer/office-2007/ee828899(v=office.12)
// ====================================================================

[ComImport]
[Guid("21164102-C24A-11d1-851A-00C04FCC6B14")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IFELanguage2
{
	// --------------------------------------------------------------------
	// IFE Language 1
	// --------------------------------------------------------------------

	/// <summary>
	/// 初期化
	/// </summary>
	/// <returns></returns>
	Int32 Open();

	/// <summary>
	/// 後始末
	/// </summary>
	/// <returns></returns>
	Int32 Close();

	// --------------------------------------------------------------------
	// IFE Language 2
	// --------------------------------------------------------------------

	/// <summary>
	/// モーフ解析
	/// </summary>
	/// <param name="request"></param>
	/// <param name="cmode"></param>
	/// <param name="cwchInput"></param>
	/// <param name="pwchInput"></param>
	/// <param name="cinfo"></param>
	/// <param name="result"></param>
	/// <returns></returns>
	Int32 GetMorphResult(UInt32 request, UInt32 cmode, Int32 cwchInput, [MarshalAs(UnmanagedType.LPWStr)] String pwchInput, IntPtr cinfo, out IntPtr result);

	/// <summary>
	/// 変換モード
	/// </summary>
	/// <param name="caps"></param>
	/// <returns></returns>
	Int32 GetConversionModeCaps(ref UInt32 caps);

	/// <summary>
	/// 漢字→ひらがな
	/// </summary>
	/// <param name="str"></param>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <param name="result"></param>
	/// <returns></returns>
	Int32 GetPhonetic([MarshalAs(UnmanagedType.BStr)] String str, Int32 start, Int32 length, [MarshalAs(UnmanagedType.BStr)] out String result);

	/// <summary>
	/// ひらがな→漢字
	/// </summary>
	/// <param name="str"></param>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <param name="result"></param>
	/// <returns></returns>
	Int32 GetConversion([MarshalAs(UnmanagedType.BStr)] String str, Int32 start, Int32 length, [MarshalAs(UnmanagedType.BStr)] out String result);
}
