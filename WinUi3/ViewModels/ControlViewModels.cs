// ============================================================================
// 
// ページ内の各パネル（NavigationBar で切り替えられるページ）の ViewModel を保持するクラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2024/01/21 (Sun) | 作成開始。
//  1.00  | 2024/01/21 (Sun) | ファーストバージョン。
// (1.01) | 2024/11/25 (Mon) |   AOT 対応のため Enum.GetValues(typeof) を代替。
// ============================================================================

namespace Shinta.WinUi3.ViewModels;

internal class ControlViewModels<TViewModel, TEnum>
	where TViewModel : ControlViewModel, new()
	where TEnum : struct, Enum
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public ControlViewModels(TEnum numItems)
	{
		Items = new TViewModel[Convert.ToInt32(numItems)];

		// ダミー代入
		TViewModel viewModel = new();
		foreach (TEnum t in Enum.GetValues<TEnum>())
		{
			if (Convert.ToInt32(t) < Items.Length)
			{
				this[t] = viewModel;
			}
		}
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 制作パネルの ViewModel（実体）
	/// </summary>
	public TViewModel[] Items
	{
		get;
		set;
	}

	/// <summary>
	/// 制作パネルの ViewModel（インデクサー）
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public TViewModel this[TEnum index]
	{
		get => Items[Convert.ToInt32(index)];
		set => Items[Convert.ToInt32(index)] = value;
	}
}
