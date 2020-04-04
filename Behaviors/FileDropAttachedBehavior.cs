// ============================================================================
// 
// ファイルやフォルダーのドラッグ＆ドロップ時にコマンドを発行する添付ビヘイビア
// Copyright (C) 2019 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// PreviewDragOver はトンネルイベントで、親→子の順に呼ばれる
// DragOver はバブルイベントで、子→親の順に呼ばれる
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/06/24 (Mon) | オリジナルバージョン。
// (1.01) | 2019/11/04 (Mon) |   TextBox もハンドルできるように DragOver から PreviewDragOver に変更。
//  1.10  | 2019/11/07 (Thu) | null 許容参照型を有効化した。
// (1.11) | 2019/11/09 (Sat) |   .NET Framework でのコードを改善。
// (1.12) | 2019/11/10 (Sun) |   NotNullWhen アノテーション属性を使用しないコードに改善。
//  1.20  | 2019/11/10 (Sun) | ドラッグコマンドを設定できるようにした。
//  1.30  | 2019/11/11 (Mon) | プレビュードラッグコマンドを設定できるようにした。
// (1.31) | 2019/11/16 (Sat) |   ドロップ時のハンドル制御の不具合を修正。
// ============================================================================

using System;
using System.Windows;
using System.Windows.Input;

#nullable enable

namespace Shinta.Behaviors
{
	public class FileDropAttachedBehavior
	{
		// ====================================================================
		// public メンバー変数
		// ====================================================================

		// プレビュードラッグコマンド添付プロパティー
		public static readonly DependencyProperty PreviewDragCommandProperty =
				DependencyProperty.RegisterAttached("PreviewDragCommand", typeof(ICommand), typeof(FileDropAttachedBehavior),
				new PropertyMetadata(null, SourceCommandChanged));

		// ドラッグコマンド添付プロパティー
		public static readonly DependencyProperty DragCommandProperty =
				DependencyProperty.RegisterAttached("DragCommand", typeof(ICommand), typeof(FileDropAttachedBehavior),
				new PropertyMetadata(null, SourceCommandChanged));

		// ドロップコマンド添付プロパティー
		public static readonly DependencyProperty DropCommandProperty =
				DependencyProperty.RegisterAttached("DropCommand", typeof(ICommand), typeof(FileDropAttachedBehavior),
				new PropertyMetadata(null, SourceCommandChanged));

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// プレビュードラッグコマンド添付プロパティー GET
		// --------------------------------------------------------------------
		public static ICommand? GetPreviewDragCommand(DependencyObject oObject)
		{
			return (ICommand)oObject.GetValue(PreviewDragCommandProperty);
		}

		// --------------------------------------------------------------------
		// プレビュードラッグコマンド添付プロパティー SET
		// --------------------------------------------------------------------
		public static void SetPreviewDragCommand(DependencyObject oObject, ICommand? oValue)
		{
			oObject.SetValue(PreviewDragCommandProperty, oValue);
		}

		// --------------------------------------------------------------------
		// ドラッグコマンド添付プロパティー GET
		// --------------------------------------------------------------------
		public static ICommand? GetDragCommand(DependencyObject oObject)
		{
			return (ICommand)oObject.GetValue(DragCommandProperty);
		}

		// --------------------------------------------------------------------
		// ドラッグコマンド添付プロパティー SET
		// --------------------------------------------------------------------
		public static void SetDragCommand(DependencyObject oObject, ICommand? oValue)
		{
			oObject.SetValue(DragCommandProperty, oValue);
		}

		// --------------------------------------------------------------------
		// ドロップコマンド添付プロパティー GET
		// --------------------------------------------------------------------
		public static ICommand? GetDropCommand(DependencyObject oObject)
		{
			return (ICommand)oObject.GetValue(DropCommandProperty);
		}

		// --------------------------------------------------------------------
		// ドロップコマンド添付プロパティー SET
		// --------------------------------------------------------------------
		public static void SetDropCommand(DependencyObject oObject, ICommand? oValue)
		{
			oObject.SetValue(DropCommandProperty, oValue);
		}

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 設定されたプレビュードラッグコマンドが実行可能な場合にそのコマンドを返す
		// --------------------------------------------------------------------
		private static ICommand? ExecutablePreviewDragCommand(Object? oSender)
		{
			if (oSender is UIElement aElement)
			{
				ICommand? aCommand = GetPreviewDragCommand(aElement);
				if (aCommand != null && aCommand.CanExecute(null))
				{
					return aCommand;
				}
			}

			return null;
		}

		// --------------------------------------------------------------------
		// 設定されたドラッグコマンドが実行可能な場合にそのコマンドを返す
		// --------------------------------------------------------------------
		private static ICommand? ExecutableDragCommand(Object? oSender)
		{
			if (oSender is UIElement aElement)
			{
				ICommand? aCommand = GetDragCommand(aElement);
				if (aCommand != null && aCommand.CanExecute(null))
				{
					return aCommand;
				}
			}

			return null;
		}

		// --------------------------------------------------------------------
		// 設定されたドロップコマンドが実行可能な場合にそのコマンドを返す
		// --------------------------------------------------------------------
		private static ICommand? ExecutableDropCommand(Object? oSender)
		{
			if (oSender is UIElement aElement)
			{
				ICommand? aCommand = GetDropCommand(aElement);
				if (aCommand != null && aCommand.CanExecute(null))
				{
					return aCommand;
				}
			}

			return null;
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// プレビュードラッグコマンドが設定されている場合のみハンドルする
		// 伝播の方向が異なるため、ドロップコマンドが設定されていてもハンドルしない
		// --------------------------------------------------------------------
		private static void OnPreviewDragOver(Object oSender, DragEventArgs oDragEventArgs)
		{
			ICommand? aPreviewDragCommand = ExecutablePreviewDragCommand(oSender);
			if (aPreviewDragCommand == null)
			{
				return;
			}

			if (oDragEventArgs.Data.GetData(DataFormats.FileDrop, false) is String[] aDropFiles)
			{
				// ファイル類のときのみドラッグを受け付ける
				oDragEventArgs.Effects = DragDropEffects.Copy;
				oDragEventArgs.Handled = true;

				// プレビュードラッグコマンドを実行
				aPreviewDragCommand.Execute(aDropFiles);
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// ドロップコマンドのみが設定されている場合でも受付判定されるよう、ここでハンドルする
		// --------------------------------------------------------------------
		private static void OnDragOver(Object oSender, DragEventArgs oDragEventArgs)
		{
			ICommand? aDragCommand = ExecutableDragCommand(oSender);
			ICommand? aDropCommand = ExecutableDropCommand(oSender);
			if (aDragCommand == null && aDropCommand == null)
			{
				return;
			}

			if (oDragEventArgs.Data.GetData(DataFormats.FileDrop, false) is String[] aDropFiles)
			{
				// ファイル類のときのみドラッグを受け付ける
				oDragEventArgs.Effects = DragDropEffects.Copy;
				oDragEventArgs.Handled = true;

				// ドラッグコマンドが設定されている場合は実行
				if (aDragCommand != null)
				{
					aDragCommand.Execute(aDropFiles);
				}
			}
		}

		// --------------------------------------------------------------------
		// イベントハンドラー
		// --------------------------------------------------------------------
		private static void OnDrop(Object oSender, DragEventArgs oDragEventArgs)
		{
			ICommand? aCommand = ExecutableDropCommand(oSender);
			if (aCommand == null)
			{
				return;
			}

			if (oDragEventArgs.Data.GetData(DataFormats.FileDrop, false) is String[] aDropFiles)
			{
				oDragEventArgs.Handled = true;

				// ドロップされたファイル類のパスを引数としてコマンドを実行
				aCommand.Execute(aDropFiles);
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で Command が変更された
		// --------------------------------------------------------------------
		private static void SourceCommandChanged(DependencyObject oObject, DependencyPropertyChangedEventArgs oArgs)
		{
			if (oObject is UIElement aElement)
			{
				if (GetPreviewDragCommand(aElement) != null || GetDragCommand(aElement) != null || GetDropCommand(aElement) != null)
				{
					// コマンドが設定された場合はイベントハンドラーを有効にする
					aElement.AllowDrop = true;
					aElement.PreviewDragOver += OnPreviewDragOver;
					aElement.DragOver += OnDragOver;
					aElement.Drop += OnDrop;
				}
				else
				{
					// コマンドが解除された場合はイベントハンドラーを無効にする
					aElement.AllowDrop = false;
					aElement.PreviewDragOver -= OnPreviewDragOver;
					aElement.DragOver -= OnDragOver;
					aElement.Drop -= OnDrop;
				}
			}
		}
	}
	// public class FileDropAttachedBehavior ___END___
}
// namespace Shinta.Behaviors ___END___
