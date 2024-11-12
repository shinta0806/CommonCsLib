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

using System.Runtime.InteropServices;

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

		if (_ime.GetPhonetic(kanji, 1, -1, out String hiragana) != 0)
		{
			return null;
		}

		return hiragana;
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
	Int32 GetMorphResult(UInt32 request, UInt32 cmode, Int32 cwchInput, [MarshalAs(UnmanagedType.LPWStr)] String pwchInput, IntPtr cinfo, out Object result);

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
