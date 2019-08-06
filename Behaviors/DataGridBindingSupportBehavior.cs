// ============================================================================
// 
// DataGrid のバインド可能なプロパティーを増やすためのビヘイビア
// Copyright (C) 2019 by SHINTA
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
//  1.01  | 2019/08/06 (Tue) |   SelectedItem の動作を改善。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Shinta.Behaviors
{
	public class DataGridBindingSupportBehavior : Behavior<DataGrid>
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// DataGrid.Columns をバインド可能にする
		public ObservableCollection<DataGridColumn> Columns
		{
			get => (ObservableCollection<DataGridColumn>)GetValue(ColumnsProperty);
			set => SetValue(ColumnsProperty, value);
		}

		// DataGrid.CurrentCell をバインド可能にする
		public DataGridCellInfo CurrentCell
		{
			get => (DataGridCellInfo)GetValue(CurrentCellProperty);
			set => SetValue(CurrentCellProperty, value);
		}

		// DataGrid.CurrentCell を列インデックスと行インデックスで扱えるようにする
		// System.Windows.Point（Double）ではなく System.Drawing.Point（Int32）であることに注意
		public System.Drawing.Point CurrentCellLocation
		{
			get => (System.Drawing.Point)GetValue(CurrentCellLocationProperty);
			set => SetValue(CurrentCellLocationProperty, value);
		}

		// DataGrid.SelectedCells をバインド可能にする
		public List<DataGridCellInfo> SelectedCells
		{
			get => (List<DataGridCellInfo>)GetValue(SelectedCellsProperty);
			set => SetValue(SelectedCellsProperty, value);
		}

		// DataGrid.SelectedItem 設定時にスクロールする
		public Object SelectedItem
		{
			get => GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		// DataGrid.Sorting をコマンドで扱えるようにする
		public ICommand SortingCommand
		{
			get => (ICommand)GetValue(SortingCommandProperty);
			set => SetValue(SortingCommandProperty, value);
		}

		// ====================================================================
		// public メンバー変数
		// ====================================================================

		// DataGrid.Columns は読み取り専用だが内容を ViewModel 側で変更するのでコールバックは登録する
		public static readonly DependencyProperty ColumnsProperty =
				DependencyProperty.Register("Columns", typeof(ObservableCollection<DataGridColumn>), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceColumnsChanged));

		// DataGrid.CurrentCell
		public static readonly DependencyProperty CurrentCellProperty
				= DependencyProperty.RegisterAttached("CurrentCell", typeof(DataGridCellInfo), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(new DataGridCellInfo(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceCurrentCellChanged));

		// CurrentCellLocation
		public static readonly DependencyProperty CurrentCellLocationProperty
				= DependencyProperty.RegisterAttached("CurrentCellLocation", typeof(System.Drawing.Point), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(System.Drawing.Point.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceCurrentCellLocationChanged));

		// DataGrid.SelectedCells は読み取り専用なのでコールバックは登録しない
		public static readonly DependencyProperty SelectedCellsProperty
				= DependencyProperty.Register("SelectedCells", typeof(List<DataGridCellInfo>), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(new List<DataGridCellInfo>()));

		// DataGrid.SelectedItem
		public static readonly DependencyProperty SelectedItemProperty
				= DependencyProperty.RegisterAttached("SelectedItem", typeof(Object), typeof(DataGridBindingSupportBehavior),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceSelectedItemChanged));

		// DataGird.Sorting
		public static readonly DependencyProperty SortingCommandProperty
				= DependencyProperty.RegisterAttached("SortingCommand", typeof(ICommand), typeof(DataGridBindingSupportBehavior),
				new PropertyMetadata(null, SourceSortingCommandChanged));

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
		private void ControlCurrentCellChanged(Object oSender, EventArgs oArgs)
		{
			DataGrid aDataGrid = oSender as DataGrid;
			if (aDataGrid != null)
			{
				CurrentCell = aDataGrid.CurrentCell;
				if (CurrentCell.Column != null)
				{
					CurrentCellLocation = new System.Drawing.Point(CurrentCell.Column.DisplayIndex, aDataGrid.Items.IndexOf(CurrentCell.Item));
				}
			}
		}

		// --------------------------------------------------------------------
		// View 側で SelectedCells が変更された
		// --------------------------------------------------------------------
		private void ControlSelectedCellsChanged(Object oSender, SelectedCellsChangedEventArgs oSelectedCellsChangedEventArgs)
		{
			DataGrid aDataGrid = oSender as DataGrid;
			if (aDataGrid != null)
			{
				SelectedCells = aDataGrid.SelectedCells.ToList();
			}
		}

		// --------------------------------------------------------------------
		// View 側で Selector 選択項目が変更された
		// --------------------------------------------------------------------
		private void ControlSelectionChanged(Object oSender, SelectionChangedEventArgs oSelectionChangedEventArgs)
		{
			DataGrid aDataGrid = oSender as DataGrid;
			if (aDataGrid != null)
			{
				SelectedItem = aDataGrid.SelectedItem;
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private void OnSorting(Object oSender, DataGridSortingEventArgs oDataGridSortingEventArgs)
		{
			if (SortingCommand == null || !SortingCommand.CanExecute(null))
			{
				return;
			}

			// イベント引数を引数としてコマンドを実行
			SortingCommand.Execute(oDataGridSortingEventArgs);
		}

		// --------------------------------------------------------------------
		// ViewModel 側で Columns が変更された
		// --------------------------------------------------------------------
		private static void SourceColumnsChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			ObservableCollection<DataGridColumn> aNewColumns = oArgs.NewValue as ObservableCollection<DataGridColumn>;
			if (aNewColumns == null)
			{
				return;
			}

			DataGridBindingSupportBehavior aThisObject = oObject as DataGridBindingSupportBehavior;
			if (aThisObject == null || aThisObject.AssociatedObject == null)
			{
				return;
			}

			// カラム設定
			aThisObject.AssociatedObject.Columns.Clear();
			foreach (DataGridColumn aDataGridColumn in aNewColumns)
			{
				aThisObject.AssociatedObject.Columns.Add(aDataGridColumn);
			}

			// カラム変更に対応できるようにする
			aNewColumns.CollectionChanged
					+= delegate (Object oSender, NotifyCollectionChangedEventArgs oNotifyCollectionChangedEventArgs)
			{
				if (oNotifyCollectionChangedEventArgs.NewItems != null)
				{
					foreach (DataGridColumn aDataGridColumn in oNotifyCollectionChangedEventArgs.NewItems.Cast<DataGridColumn>())
					{
						aThisObject.AssociatedObject.Columns.Add(aDataGridColumn);
					}
				}
				if (oNotifyCollectionChangedEventArgs.OldItems != null)
				{
					foreach (DataGridColumn aDataGridColumn in oNotifyCollectionChangedEventArgs.OldItems.Cast<DataGridColumn>())
					{
						aThisObject.AssociatedObject.Columns.Remove(aDataGridColumn);
					}
				}
			};
		}

		// --------------------------------------------------------------------
		// ViewModel 側で CurrentCell が変更された
		// --------------------------------------------------------------------
		private static void SourceCurrentCellChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			DataGridBindingSupportBehavior aThisObject = oObject as DataGridBindingSupportBehavior;
			if (aThisObject == null || aThisObject.AssociatedObject == null)
			{
				return;
			}

			DataGridCellInfo aNewCellInfo = (DataGridCellInfo)oArgs.NewValue;
			if (aThisObject.AssociatedObject.CurrentCell == aNewCellInfo)
			{
				return;
			}

			// 先にフォーカスを当てないと選択状態にならない
			aThisObject.AssociatedObject.Focus();
			if (aThisObject.AssociatedObject.SelectionUnit == DataGridSelectionUnit.FullRow)
			{
				aThisObject.AssociatedObject.SelectedIndex = -1;
			}
			else
			{
				aThisObject.AssociatedObject.SelectedCells.Clear();
			}

			// セル選択
			aThisObject.AssociatedObject.CurrentCell = aNewCellInfo;
		}

		// --------------------------------------------------------------------
		// ViewModel 側で CurrentCellLocation が変更された
		// --------------------------------------------------------------------
		private static void SourceCurrentCellLocationChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			DataGridBindingSupportBehavior aThisObject = oObject as DataGridBindingSupportBehavior;
			if (aThisObject == null || aThisObject.AssociatedObject == null)
			{
				return;
			}

			System.Drawing.Point aNewPoint = (System.Drawing.Point)oArgs.NewValue;
			if (aNewPoint.X < 0 || aNewPoint.X >= aThisObject.AssociatedObject.Columns.Count
					|| aNewPoint.Y < 0 || aNewPoint.Y >= aThisObject.AssociatedObject.Items.Count)
			{
				return;
			}

			// 先にフォーカスを当てないと選択状態にならない
			aThisObject.AssociatedObject.Focus();
			if (aThisObject.AssociatedObject.SelectionUnit == DataGridSelectionUnit.FullRow)
			{
				aThisObject.AssociatedObject.SelectedIndex = aNewPoint.Y;
			}

			// セル選択
			aThisObject.AssociatedObject.CurrentCell = new DataGridCellInfo(aThisObject.AssociatedObject.Items[aNewPoint.Y], aThisObject.AssociatedObject.Columns[aNewPoint.X]);
		}

		// --------------------------------------------------------------------
		// ViewModel 側で SelectedItem が変更された
		// --------------------------------------------------------------------
		private static void SourceSelectedItemChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			if (oArgs.NewValue == null)
			{
				return;
			}

			DataGridBindingSupportBehavior aThisObject = oObject as DataGridBindingSupportBehavior;
			if (aThisObject != null && aThisObject.AssociatedObject != null)
			{
				aThisObject.AssociatedObject.SelectedItem = oArgs.NewValue;
				aThisObject.AssociatedObject.ScrollIntoView(oArgs.NewValue);
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で SortingCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceSortingCommandChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			DataGridBindingSupportBehavior aThisObject = oObject as DataGridBindingSupportBehavior;
			if (aThisObject == null || aThisObject.AssociatedObject == null)
			{
				return;
			}

			if (oArgs.NewValue != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				aThisObject.AssociatedObject.Sorting += aThisObject.OnSorting;
			}
			else
			{
				// コマンドが解除された場合はイベントハンドラーを無効にする
				aThisObject.AssociatedObject.Sorting -= aThisObject.OnSorting;
			}

		}

	}
	// public class DataGridBindingSupportBehavior ___END___

}
// namespace Shinta.Behaviors ___END___
