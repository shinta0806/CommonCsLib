// ============================================================================
// 
// Enum を Int32 に変換するコンバーター（バインド用）
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2025/03/12 (Wed) | 作成開始。
//  1.00  | 2025/03/12 (Wed) | ファーストバージョン。
// ============================================================================

using Microsoft.UI.Xaml.Data;

namespace Shinta;

internal partial class EnumToInt32Converter : IValueConverter
{
    // ====================================================================
    // public 関数
    // ====================================================================

    /// <summary>
    /// 変換
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    public Object Convert(Object value, Type targetType, Object parameter, String language)
    {
        return (Int32)value;
    }

    /// <summary>
    /// 逆変換
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Object ConvertBack(Object value, Type targetType, Object parameter, String language)
    {
        return value;
    }
}
