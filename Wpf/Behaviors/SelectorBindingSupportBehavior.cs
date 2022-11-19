// ============================================================================
// 
// Selector のバインド可能なプロパティーを増やすためのビヘイビア
// Copyright (C) 2021 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2021/09/20 (Mon) | オリジナルバージョン。
// (1.01) | 2021/09/20 (Mon) |   メインウィンドウでの使用対策を実装。
//  1.10  | 2021/09/20 (Mon) | SelectedIndex 対応。
// ============================================================================

using Microsoft.Xaml.Behaviors;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Shinta.Wpf.Behaviors
{
	public abstract class SelectorBindingSupportBehavior<T> : Behavior<T> where T : Selector
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// SelectedIndex 設定時にスクロールする（スクロール処理の実装は派生クラスで行う）
		public static readonly DependencyProperty SelectedIndexProperty
				= DependencyProperty.RegisterAttached(nameof(SelectedIndex), typeof(Int32), typeof(SelectorBindingSupportBehavior<T>),
				new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceSelectedIndexChanged));
		public Int32 SelectedIndex
		{
			get => (Int32)GetValue(SelectedIndexProperty);
			set => SetValue(SelectedIndexProperty, value);
		}

		// SelectedItem 設定時にスクロールする（スクロール処理の実装は派生クラスで行う）
		public static readonly DependencyProperty SelectedItemProperty
				= DependencyProperty.RegisterAttached(nameof(SelectedItem), typeof(Object), typeof(SelectorBindingSupportBehavior<T>),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceSelectedItemChanged));
		public Object SelectedItem
		{
			get => GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		// SelectionChanged をコマンドで扱えるようにする
		public static readonly DependencyProperty SelectionChangedCommandProperty
				= DependencyProperty.RegisterAttached(nameof(SelectionChangedCommand), typeof(ICommand), typeof(SelectorBindingSupportBehavior<T>),
				new PropertyMetadata(null, SourceSelectionChangedCommandChanged));
		public ICommand SelectionChangedCommand
		{
			get => (ICommand)GetValue(SelectionChangedCommandProperty);
			set => SetValue(SelectionChangedCommandProperty, value);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// アタッチ時の準備作業
		// --------------------------------------------------------------------
		protected override void OnAttached()
		{
			base.OnAttached();

			AssociatedObject.SelectionChanged += ControlSelectionChanged;

			// メインウィンドウで使用される際、フレームワークから呼ばれる XXXXChanged() では AssociatedObject が null になるため、ここで再度呼びだす
			SourceSelectionChangedCommandChanged(this, new DependencyPropertyChangedEventArgs(SelectionChangedCommandProperty, null, SelectionChangedCommand));
		}

		// --------------------------------------------------------------------
		// item の位置までスクロールさせる
		// --------------------------------------------------------------------
		protected abstract void ScrollIntoView(T selector, Object item);

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// View 側で Selector 選択項目が変更された
		// --------------------------------------------------------------------
		private void ControlSelectionChanged(Object sender, SelectionChangedEventArgs selectionChangedEventArgs)
		{
			if (sender is Selector selector)
			{
				SelectedIndex = selector.SelectedIndex;
				SelectedItem = selector.SelectedItem;
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
		// ViewModel 側で SelectedIndex が変更された
		// --------------------------------------------------------------------
		private static void SourceSelectedIndexChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Int32 newIndex = (Int32)args.NewValue;
			if (obj is SelectorBindingSupportBehavior<T> thisObject && thisObject.AssociatedObject != null)
			{
				thisObject.AssociatedObject.SelectedIndex = newIndex;
				if (0 <= newIndex && newIndex < thisObject.AssociatedObject.Items.Count)
				{
					thisObject.ScrollIntoView(thisObject.AssociatedObject, thisObject.AssociatedObject.Items[newIndex]);
				}
			}
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

			if (obj is SelectorBindingSupportBehavior<T> thisObject && thisObject.AssociatedObject != null)
			{
				thisObject.AssociatedObject.SelectedItem = args.NewValue;
				thisObject.ScrollIntoView(thisObject.AssociatedObject, args.NewValue);
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で SelectionChangedCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceSelectionChangedCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if ((obj is not SelectorBindingSupportBehavior<T> thisObject) || thisObject.AssociatedObject == null)
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
	}
}
