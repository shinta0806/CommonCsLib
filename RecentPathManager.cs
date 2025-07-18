// ============================================================================
// 
// 最近使用したファイル・フォルダーを管理
// Copyright (C) 2022-2025 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2022/12/31 (Sat) | 作成開始。
//  1.00  | 2022/12/31 (Sat) | ファーストバージョン。
// (1.01) | 2023/06/25 (Sun) |   軽微なリファクタリング。
// (1.02) | 2025/07/18 (Fri) |   軽微なリファクタリング。
// ============================================================================

using System.Collections.ObjectModel;

namespace Shinta;

// ====================================================================
// public 列挙子
// ====================================================================

/// <summary>
/// 履歴種別（ファイル／フォルダー）
/// </summary>
[Flags]
public enum RecentItemType
{
	File = 0x01,
	Folder = 0x02,
	FileAndFolder = File | Folder,
}


internal class RecentPathManager
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	/// <param name="recentItemType">履歴種別</param>
	/// <param name="capacity">保持する履歴の数</param>
	public RecentPathManager(RecentItemType recentItemType, Int32 capacity)
	{
		_recentItemType = recentItemType;
		_capacity = Math.Max(capacity, 1);
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// 履歴を追加
	/// </summary>
	/// <param name="path">履歴のパス</param>
	/// <returns>追加したら true</returns>
	public Boolean Add(String path)
	{
		ConfirmRecentExist();

		if (!Exists(path))
		{
			return false;
		}

		Int32 existIndex = FindIndex(path);
		if (existIndex >= 0)
		{
			// 既に追加されている場合はいったん削除
			_recentPathes.RemoveAt(existIndex);
		}

		// 先頭に追加
		_recentPathes.Insert(0, path);

		// 溢れた履歴を削除
		Truncate();

		return true;
	}

	/// <summary>
	/// 履歴
	/// </summary>
	/// <returns></returns>
	public ReadOnlyCollection<String> RecentPathes()
	{
		return _recentPathes.AsReadOnly();
	}

	/// <summary>
	/// 履歴を指定されたパス群で置き換える
	/// </summary>
	/// <param name="pathes"></param>
	public void SetPathes(IEnumerable<String> pathes)
	{
		_recentPathes = [.. pathes];
		ConfirmRecentExist();
		Truncate();
	}

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// 履歴種別
	/// </summary>
	private readonly RecentItemType _recentItemType;

	/// <summary>
	/// 保持する履歴の数
	/// </summary>
	private readonly Int32 _capacity;

	/// <summary>
	/// 履歴
	/// 先頭が最新
	/// </summary>
	private List<String> _recentPathes = [];

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// 履歴のファイル・フォルダーが現存しているか確認し、現存していない場合は履歴から削除
	/// </summary>
	private void ConfirmRecentExist()
	{
		for (Int32 i = _recentPathes.Count - 1; i >= 0; i--)
		{
			if (!Exists(_recentPathes[i]))
			{
				_recentPathes.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// 履歴種別に合致したものが存在しているか
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private Boolean Exists(String path)
	{
		if (_recentItemType.HasFlag(RecentItemType.File) && File.Exists(path))
		{
			return true;
		}
		if (_recentItemType.HasFlag(RecentItemType.Folder) && Directory.Exists(path))
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// 指定されたパスが履歴の何番目にあるか
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private Int32 FindIndex(String path)
	{
		return _recentPathes.FindIndex(x => String.Compare(x, path, true) == 0);
	}

	/// <summary>
	/// 溢れた履歴を削除
	/// </summary>
	private void Truncate()
	{
		if (_recentPathes.Count > _capacity)
		{
			_recentPathes.RemoveRange(_capacity, _recentPathes.Count - _capacity);
		}
	}
}
