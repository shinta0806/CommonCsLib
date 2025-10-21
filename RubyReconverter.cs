// ============================================================================
// 
// 漢字からフリガナを得る
// Copyright (C) 2021-2025 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Microsoft.Windows.CsWin32（AOT 使用時）
// ----------------------------------------------------------------------------

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
// (1.11) | 2024/12/01 (Sun) |   COM 解放漏れを修正。
// (1.12) | 2024/12/04 (Wed) |   AOT に対応。
// (1.13) | 2025/10/11 (Sat) |   AOT の IFELanguage は CsWin32 生成版を使用するようにした。
// (1.14) | 2025/10/12 (Sun) |   AOT の IFELanguage で SafeHandle を使用するようにした。
// ============================================================================

using System.Runtime.InteropServices;
#if USE_AOT || USE_UNSAFE
using Windows.Win32;
#endif
#if USE_AOT
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
#endif
#if USE_UNSAFE
using Windows.Win32.UI.Input.Ime;
#endif

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
#if USE_AOT
		unsafe
		{
			// "MSIME.Japan"
			Guid clsId = new("6a91029e-aa49-471b-aee7-7d332785660d");
			HRESULT result = PInvoke.CoCreateInstance(clsId, null, CLSCTX.CLSCTX_INPROC_SERVER | CLSCTX.CLSCTX_INPROC_HANDLER | CLSCTX.CLSCTX_LOCAL_SERVER, out IFELanguage* imePtr);

			if (result.Succeeded)
			{
				_ime = new IFELanguageSafeHandle(imePtr);
				if (_ime.Ime->Open().Failed)
				{
					_ime.Dispose();
					_ime = null;
				}
			}
		}
#else
		Type? type = Type.GetTypeFromProgID("MSIME.Japan");
		if (type != null)
		{
			_ime = Activator.CreateInstance(type) as IFELanguage2;
		}

		if (_ime != null)
		{
			if (_ime.Open() != 0)
			{
				_ime.Close();
				Marshal.ReleaseComObject(_ime);
				_ime = null;
			}
		}
#endif
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
#if USE_AOT
		return _ime != null && !_ime.IsInvalid;
#else
		return _ime != null;
#endif
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
#if USE_AOT
		using SysFreeStringSafeHandle kanjiBStrHandle = new(Marshal.StringToBSTR(kanji));
		BSTR hiraganaBStr = new();
		unsafe
		{
			if (_ime.Ime->GetPhonetic(kanjiBStrHandle, 1, -1, ref hiraganaBStr).Failed)
			{
				return null;
			}
		}
		using SysFreeStringSafeHandle hiraganaBStrHandle = new(hiraganaBStr);
		return hiraganaBStr.ToString();
#else
		if (_ime.GetPhonetic(kanji, 1, -1, out String hiragana) != 0)
		{
			return null;
		}
		return hiragana;
#endif
	}

#if USE_UNSAFE
	/// <summary>
	/// 漢字をひらがなに変換（ブロックごとの対応付き）
	/// </summary>
	/// <param name="kanji">漢字</param>
	/// <returns>ひらがな, ブロックごとの漢字, ブロックごとのひらがな</returns>
	public unsafe (String?, String[], String[]) ReconvertDetail(String? kanji)
	{
		if (_ime == null || String.IsNullOrEmpty(kanji))
		{
			return (null, Array.Empty<String>(), Array.Empty<String>());
		}

#if USE_AOT
		MORRSLT* result = null;
		HRESULT hResult;
		fixed (Char* kanjiStrPtr = kanji)
		{
			hResult = _ime.Ime->GetJMorphResult(PInvoke.FELANG_REQ_REV, PInvoke.FELANG_CMODE_HIRAGANAOUT, kanji.Length, kanjiStrPtr, null, &result);
		}
		if (hResult.Failed)
#else
		if (_ime.GetMorphResult(PInvoke.FELANG_REQ_REV, PInvoke.FELANG_CMODE_HIRAGANAOUT, kanji.Length, kanji, IntPtr.Zero, out IntPtr result) != 0)
#endif
		{
			return (null, Array.Empty<String>(), Array.Empty<String>());
		}
		MORRSLT morResult = Marshal.PtrToStructure<MORRSLT>((nint)result);

		// 返値準備
		String hiragana;
		String[] kanjiBlocks = new String[morResult.cWDD];
		String[] hiraganaBlocks = new String[morResult.cWDD];

		hiragana = new(morResult.pwchOutput, 0, morResult.cchOutput);
		for (Int32 i = 0; i < morResult.cWDD; i++)
		{
			WDD wdd = Marshal.PtrToStructure<WDD>((nint)(morResult.pWDD) + Marshal.SizeOf<WDD>() * i);
			kanjiBlocks[i] = kanji[wdd.Anonymous1.wReadPos..(wdd.Anonymous1.wReadPos + wdd.Anonymous2.cchRead)];
			hiraganaBlocks[i] = hiragana[wdd.wDispPos..(wdd.wDispPos + wdd.cchDisp)];
		}

		Marshal.FreeCoTaskMem((nint)result);
		return (hiragana, kanjiBlocks, hiraganaBlocks);
	}
#endif

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
		if (isDisposing && _ime != null)
		{
#if USE_AOT
			_ime.Dispose();
#else
			_ime.Close();
			Marshal.ReleaseComObject(_ime);
#endif
			_ime = null;
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
#if USE_AOT
	private IFELanguageSafeHandle? _ime;
#else
	private IFELanguage2? _ime;
#endif

	/// <summary>
	/// Dispose フラグ
	/// </summary>
	private Boolean _isDisposed;
}

#if USE_AOT
// ============================================================================
// CsWin32 の IFELanguage COM ポインタを SafeHandle 化
// ============================================================================
internal unsafe class IFELanguageSafeHandle : SafeHandle
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	/// <param name="preexistingHandle"></param>
	/// <param name="ownsHandle"></param>
	public IFELanguageSafeHandle(IFELanguage* preexistingHandle, Boolean ownsHandle = true)
		: base((nint)preexistingHandle, ownsHandle)
	{
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// ハンドルが無効かどうか
	/// </summary>
	public override Boolean IsInvalid => handle == nint.Zero;

	/// <summary>
	/// 生ポインタ
	/// </summary>
	public IFELanguage* Ime => (IFELanguage*)handle;

	// ====================================================================
	// protected 関数
	// ====================================================================

	/// <summary>
	/// COM 解放
	/// </summary>
	/// <returns></returns>
	protected override bool ReleaseHandle()
	{
		if (!IsInvalid)
		{
			Ime->Close();
			Ime->Release();
			handle = nint.Zero;
		}
		return true;
	}
}
#else
// ====================================================================
// IFE Language 2 Interface
// https://learn.microsoft.com/en-us/previous-versions/office/developer/office-2007/ee828899(v=office.12)
// ====================================================================

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("21164102-C24A-11d1-851A-00C04FCC6B14")]
public partial interface IFELanguage2
{
	// --------------------------------------------------------------------
	// IFE Language 1
	// --------------------------------------------------------------------

	/// <summary>
	/// 初期化
	/// </summary>
	/// <returns></returns>
	[PreserveSig]
	Int32 Open();

	/// <summary>
	/// 後始末
	/// </summary>
	/// <returns></returns>
	[PreserveSig]
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
	[PreserveSig]
	Int32 GetMorphResult(UInt32 request, UInt32 cmode, Int32 cwchInput, [MarshalAs(UnmanagedType.LPWStr)] String pwchInput, IntPtr cinfo, out IntPtr result);

	/// <summary>
	/// 変換モード
	/// </summary>
	/// <param name="caps"></param>
	/// <returns></returns>
	[PreserveSig]
	Int32 GetConversionModeCaps(ref UInt32 caps);

	/// <summary>
	/// 漢字→ひらがな
	/// </summary>
	/// <param name="str"></param>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <param name="result"></param>
	/// <returns></returns>
	[PreserveSig]
	Int32 GetPhonetic([MarshalAs(UnmanagedType.BStr)] String str, Int32 start, Int32 length, [MarshalAs(UnmanagedType.BStr)] out String result);

	/// <summary>
	/// ひらがな→漢字
	/// </summary>
	/// <param name="str"></param>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <param name="result"></param>
	/// <returns></returns>
	[PreserveSig]
	Int32 GetConversion([MarshalAs(UnmanagedType.BStr)] String str, Int32 start, Int32 length, [MarshalAs(UnmanagedType.BStr)] out String result);
}
#endif
