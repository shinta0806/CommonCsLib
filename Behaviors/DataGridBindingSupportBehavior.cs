// ============================================================================
// 
// DataGrid のバインド可能なプロパティーを増やすためのビヘイビア
// Copyright (C) 2019-2020 by SHINTA
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
// ============================================================================

using Microsoft.Xaml.Behaviors;
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

#nullable enable

namespace Shinta.Behaviors
{
	public class DataGridBindingSupportBehavior : Behavior<DataGrid>
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

		// DataGrid.SelectedItem 設定時にスクロールする
		public static readonly DependencyProperty SelectedItemProperty
				= DependencyProperty.RegisterAttached("SelectedItem", typeof(Object), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceSelectedItemChanged));
		public Object SelectedItem
		{
			get => GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		// DataGrid.SelectedItems をバインド可能にする
		// DataGrid.SelectedItems は読み取り専用なのでコールバックは登録しない
		public static readonly DependencyProperty SelectedItemsProperty
				= DependencyProperty.RegisterAttached("SelectedItems", typeof(List<Object>), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(new List<Object>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public List<Object> SelectedItems
		{
			get => (List<Object>)GetValue(SelectedItemsProperty);
			set => SetValue(SelectedItemsProperty, value);
		}

		// DataGrid.SelectionChanged をコマンドで扱えるようにする
		public static readonly DependencyProperty SelectionChangedCommandProperty
				= DependencyProperty.RegisterAttached("SelectionChangedCommand", typeof(ICommand), typeof(DataGridBindingSupportBehavior),
				new PropertyMetadata(null, SourceSelectionChangedCommandChanged));
		public ICommand SelectionChangedCommand
		{
			get => (ICommand)GetValue(SelectionChangedCommandProperty);
			set => SetValue(SelectionChangedCommandProperty, value);
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

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// アタッチ時の準備作業
		// --------------------------------------------------------------------
		protected override void OnAttached()
		{
			base.OnAttached();

			AssociatedObject.CurrentCellChanged += ControlCurrentCellChanged;
			AssociatedObject.SelectedCellsChanged += ControlSelectedCellsChanged;
			AssociatedObject.SelectionChanged += ControlSelectionChanged;
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
			if (sender is DataGrid dataGrid)
			{
				SelectedItem = dataGrid.SelectedItem;

				List<Object> list = new List<Object>();
				for (Int32 i = 0; i < dataGrid.SelectedItems.Count; i++)
				{
					Object? obj = dataGrid.SelectedItems[i];
					if (obj != null)
					{
						list.Add(obj);
					}
				}
				SelectedItems = list;
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void OnSelectionChanged(Object sender, SelectionChangedEventArgs args)
		{
			if (SelectionChangedCommand == null || !SelectionChangedCommand.CanExecute(null))
			{
				return;
			}

			// イベント引数を引数としてコマンドを実行
			SelectionChangedCommand.Execute(args);
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
			if (!(args.NewValue is ObservableCollection<DataGridColumn> newColumns))
			{
				return;
			}

			if (!(obj is DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
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
					+= delegate (Object oSender, NotifyCollectionChangedEventArgs oNotifyCollectionChangedEventArgs)
			{
				if (oNotifyCollectionChangedEventArgs.NewItems != null)
				{
					foreach (DataGridColumn aDataGridColumn in oNotifyCollectionChangedEventArgs.NewItems.Cast<DataGridColumn>())
					{
						thisObject.AssociatedObject.Columns.Add(aDataGridColumn);
					}
				}
				if (oNotifyCollectionChangedEventArgs.OldItems != null)
				{
					foreach (DataGridColumn aDataGridColumn in oNotifyCollectionChangedEventArgs.OldItems.Cast<DataGridColumn>())
					{
						thisObject.AssociatedObject.Columns.Remove(aDataGridColumn);
					}
				}
			};
		}

		// --------------------------------------------------------------------
		// ViewModel 側で CurrentCell が変更された
		// --------------------------------------------------------------------
		private static void SourceCurrentCellChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (!(obj is DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			if (!(args.NewValue is DataGridCellInfo newCellInfo) || thisObject.AssociatedObject.CurrentCell == newCellInfo)
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
			if (!(obj is DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
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
		// ViewModel 側で SelectedItem が変更された
		// --------------------------------------------------------------------
		private static void SourceSelectedItemChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (args.NewValue == null)
			{
				return;
			}

			if (obj is DataGridBindingSupportBehavior thisObject && thisObject.AssociatedObject != null)
			{
				thisObject.AssociatedObject.SelectedItem = args.NewValue;
				thisObject.AssociatedObject.ScrollIntoView(args.NewValue);
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で SelectionChangedCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceSelectionChangedCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (!(obj is DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			if (args.NewValue != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				thisObject.AssociatedObject.SelectionChanged += thisObject.OnSelectionChanged;
			}
			else
			{
				// コマンドが解除された場合はイベントハンドラーを無効にする
				thisObject.AssociatedObject.SelectionChanged -= thisObject.OnSelectionChanged;
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で SortingCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceSortingCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (!(obj is DataGridBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
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
	// public class DataGridBindingSupportBehavior ___END___

}
// namespace Shinta.Behaviors ___END___
