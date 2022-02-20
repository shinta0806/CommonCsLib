// ============================================================================
// 
// DataGrid のバインド可能なプロパティーを増やすためのビヘイビア
// Copyright (C) 2019-2021 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// ToDo
// DataGrid.SelectedCells は IList<DataGridCellInfo> だが、DataGridBindingSupportBehavior.SelectedCells を
// IList<DataGridCellInfo> とするとなぜか SetValue() でプロパティー値がセットされない。
// 仕方ないので、ひとまず List<DataGridCellInfo> 型としている。
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/06/24 (Mon) | オリジナルバージョン。
// (1.01) | 2019/08/06 (Tue) |   SelectedItem の動作を改善。
// (1.02) | 2019/11/09 (Sat) |   null 許容参照型を有効化した。
// (1.03) | 2020/01/20 (Mon) |   SelectedItems をバインド可能にした。
// (1.04) | 2020/01/21 (Tue) |   SelectionChangedCommand をバインド可能にした。
// (1.05) | 2020/11/15 (Sun) |   null 許容参照型の対応強化。
//  1.10  | 2021/09/20 (Mon) | SelectorBindingSupportBehavior の派生クラスにした。
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shinta.Behaviors
{
	public class DataGridBindingSupportBehavior : SelectorBindingSupportBehavior<DataGrid>
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// DataGrid.Columns をバインド可能にする
		// DataGrid.Columns は読み取り専用だが内容を ViewModel 側で変更するのでコールバックは登録する
		public static readonly DependencyProperty ColumnsProperty =
				DependencyProperty.Register("Columns", typeof(ObservableCollection<DataGridColumn>), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceColumnsChanged));
		public ObservableCollection<DataGridColumn> Columns
		{
			get => (ObservableCollection<DataGridColumn>)GetValue(ColumnsProperty);
			set => SetValue(ColumnsProperty, value);
		}

		// DataGrid.CurrentCell をバインド可能にする
		public static readonly DependencyProperty CurrentCellProperty
				= DependencyProperty.RegisterAttached("CurrentCell", typeof(DataGridCellInfo), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(new DataGridCellInfo(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceCurrentCellChanged));
		public DataGridCellInfo CurrentCell
		{
			get => (DataGridCellInfo)GetValue(CurrentCellProperty);
			set => SetValue(CurrentCellProperty, value);
		}

		// DataGrid.CurrentCell を列インデックスと行インデックスで扱えるようにする
		// System.Windows.Point（Double）になった
		public static readonly DependencyProperty CurrentCellLocationProperty
				= DependencyProperty.RegisterAttached("CurrentCellLocation", typeof(Point), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(new Point(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceCurrentCellLocationChanged));
		public Point CurrentCellLocation
		{
			get => (Point)GetValue(CurrentCellLocationProperty);
			set => SetValue(CurrentCellLocationProperty, value);
		}

		// DataGrid.SelectedCells をバインド可能にする
		// DataGrid.SelectedCells は読み取り専用なのでコールバックは登録しない
		public static readonly DependencyProperty SelectedCellsProperty
				= DependencyProperty.Register("SelectedCells", typeof(List<DataGridCellInfo>), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(new List<DataGridCellInfo>()));
		public List<DataGridCellInfo> SelectedCells
		{
			get => (List<DataGridCellInfo>)GetValue(SelectedCellsProperty);
			set => SetValue(SelectedCellsProperty, value);
		}

		// DataGrid.SelectedItems をバインド可能にする
		// DataGrid.SelectedItems は読み取り専用なのでコールバックは登録しない
		public static readonly DependencyProperty SelectedItemsProperty
				= DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public IList SelectedItems
		{
			get => (IList)GetValue(SelectedItemsProperty);
			set => SetValue(SelectedItemsProperty, value);
		}

		// DataGrid.Sorting をコマンドで扱えるようにする
		public static readonly DependencyProperty SortingCommandProperty
				= DependencyProperty.RegisterAttached("SortingCommand", typeof(ICommand), typeof(DataGridBindingSupportBehavior),
				new PropertyMetadata(null, SourceSortingCommandChanged));
		public ICommand SortingCommand
		{
			get => (ICommand)GetValue(SortingCommandProperty);
			set => SetValue(SortingCommandProperty, value);
		}

		// ====================================================================
		// public メンバー変数
		// ====================================================================

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// item の位置までスクロールさせる
		// --------------------------------------------------------------------
		protected override void ScrollIntoView(DataGrid dataGrid, Object item)
		{
			dataGrid.ScrollIntoView(item);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// アタッチ時の準備作業
		// --------------------------------------------------------------------
		protected override void OnAttached()
		{
			AssociatedObject.CurrentCellChanged += ControlCurrentCellChanged;
			AssociatedObject.SelectedCellsChanged += ControlSelectedCellsChanged;
			AssociatedObject.SelectionChanged += ControlSelectionChanged;

			// メインウィンドウで使用される際、フレームワークから呼ばれる XXXXChanged() では AssociatedObject が null になるため、ここで再度呼びだす
			SourceSortingCommandChanged(this, new DependencyPropertyChangedEventArgs(SortingCommandProperty, null, SortingCommand));

			// 基底の ControlSelectionChanged をここの ControlSelectionChanged より後に呼ばれるようにする（SelectedItems の更新が終わってから通知が飛ぶように）
			base.OnAttached();
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// View 側で CurrentCell が変更された
		// --------------------------------------------------------------------
		private void ControlCurrentCellChanged(Object? sender, EventArgs args)
		{
			if (sender is DataGrid dataGrid)
			{
				CurrentCell = dataGrid.CurrentCell;
				if (CurrentCell.Column != null)
				{
					CurrentCellLocation = new Point(CurrentCell.Column.DisplayIndex, dataGrid.Items.IndexOf(CurrentCell.Item));
				}
			}
		}

		// --------------------------------------------------------------------
		// View 側で SelectedCells が変更された
		// --------------------------------------------------------------------
		private void ControlSelectedCellsChanged(Object sender, SelectedCellsChangedEventArgs selectedCellsChangedEventArgs)
		{
			if (sender is DataGrid dataGrid)
			{
				SelectedCells = dataGrid.SelectedCells.ToList();
			}
		}

		// --------------------------------------------------------------------
		// View 側で Selector 選択項目が変更された
		// --------------------------------------------------------------------
		private void ControlSelectionChanged(Object sender, SelectionChangedEventArgs selectionChangedEventArgs)
		{
			if (SelectedItems != null)
			{
				foreach (Object? addedItem in selectionChangedEventArgs.AddedItems)
				{
					SelectedItems.Add(addedItem);
				}
				foreach (Object? removedItem in selectionChangedEventArgs.RemovedItems)
				{
					SelectedItems.Remove(removedItem);
				}
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void OnSorting(Object sender, DataGridSortingEventArgs dataGridSortingEventArgs)
		{
			if (SortingCommand == null || !SortingCommand.CanExecute(null))
			{
				return;
			}

			// イベント引数を引数としてコマンドを実行
			SortingCommand.Execute(dataGridSortingEventArgs);
		}

		// --------------------------------------------------------------------
		// ViewModel 側で Columns が変更された
		// --------------------------------------------------------------------
		private static void SourceColumnsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (args.NewValue is not ObservableCollection<DataGridColumn> newColumns)
			{
				return;
			}

			if ((obj is not DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			// カラム設定
			thisObject.AssociatedObject.Columns.Clear();
			foreach (DataGridColumn dataGridColumn in newColumns)
			{
				thisObject.AssociatedObject.Columns.Add(dataGridColumn);
			}

			// カラム変更に対応できるようにする
			newColumns.CollectionChanged
					+= delegate (Object? sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
			{
				if (notifyCollectionChangedEventArgs.NewItems != null)
				{
					foreach (DataGridColumn dataGridColumn in notifyCollectionChangedEventArgs.NewItems.Cast<DataGridColumn>())
					{
						thisObject.AssociatedObject.Columns.Add(dataGridColumn);
					}
				}
				if (notifyCollectionChangedEventArgs.OldItems != null)
				{
					foreach (DataGridColumn dataGridColumn in notifyCollectionChangedEventArgs.OldItems.Cast<DataGridColumn>())
					{
						thisObject.AssociatedObject.Columns.Remove(dataGridColumn);
					}
				}
			};
		}

		// --------------------------------------------------------------------
		// ViewModel 側で CurrentCell が変更された
		// --------------------------------------------------------------------
		private static void SourceCurrentCellChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if ((obj is not DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			if ((args.NewValue is not DataGridCellInfo newCellInfo) || thisObject.AssociatedObject.CurrentCell == newCellInfo)
			{
				return;
			}

			// 先にフォーカスを当てないと選択状態にならない
			thisObject.AssociatedObject.Focus();
			if (thisObject.AssociatedObject.SelectionUnit == DataGridSelectionUnit.FullRow)
			{
				thisObject.AssociatedObject.SelectedIndex = -1;
			}
			else
			{
				thisObject.AssociatedObject.SelectedCells.Clear();
			}

			// セル選択
			thisObject.AssociatedObject.CurrentCell = newCellInfo;
		}

		// --------------------------------------------------------------------
		// ViewModel 側で CurrentCellLocation が変更された
		// --------------------------------------------------------------------
		private static void SourceCurrentCellLocationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if ((obj is not DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			Point newPoint = (Point)args.NewValue;
			if (newPoint.X < 0 || newPoint.X >= thisObject.AssociatedObject.Columns.Count
					|| newPoint.Y < 0 || newPoint.Y >= thisObject.AssociatedObject.Items.Count)
			{
				return;
			}

			// 先にフォーカスを当てないと選択状態にならない
			thisObject.AssociatedObject.Focus();
			if (thisObject.AssociatedObject.SelectionUnit == DataGridSelectionUnit.FullRow)
			{
				thisObject.AssociatedObject.SelectedIndex = (Int32)newPoint.Y;
			}

			// セル選択
			thisObject.AssociatedObject.CurrentCell = new DataGridCellInfo(thisObject.AssociatedObject.Items[(Int32)newPoint.Y], thisObject.AssociatedObject.Columns[(Int32)newPoint.X]);
		}

		// --------------------------------------------------------------------
		// ViewModel 側で SortingCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceSortingCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if ((obj is not DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			if (args.NewValue != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				thisObject.AssociatedObject.Sorting += thisObject.OnSorting;
			}
			else
			{
				// コマンドが解除された場合はイベントハンドラーを無効にする
				thisObject.AssociatedObject.Sorting -= thisObject.OnSorting;
			}
		}
	}
}
