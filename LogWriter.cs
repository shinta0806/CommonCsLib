// ============================================================================
// 
// ログをファイル・メッセージボックス・テキストボックスに出力する
// Copyright (C) 2016-2021 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2016/09/22 (Thu) | 作成開始。
//  1.00  | 2016/09/22 (Thu) | オリジナルバージョン。
//  1.10  | 2016/09/24 (Sat) | LogMessage() を作成。
// (1.11) | 2017/11/20 (Mon) |   LogMessage() がリリース時にデバッグ情報をテキストボックスに表示していたのを修正。
// (1.12) | 2017/12/24 (Sun) |   ShowLogMessage() のフォームを前面に出す動作の改善。
// (1.13) | 2018/03/18 (Sun) |   ShowLogMessage() のフォームを前面に出す動作をさらに改善。
// (1.14) | 2019/01/20 (Sun) |   WPF アプリケーションでも使用可能にした。
// (1.15) | 2019/03/10 (Sun) |   WPF アプリケーションでも TextBox 出力できるようにした。
// (1.16) | 2019/06/21 (Fri) |   AppendDisplayText を作成。
// (1.17) | 2019/06/27 (Thu) |   WPF 版のコードをシンプルにした。
//  1.20  | 2019/11/10 (Sun) | null 許容参照型を有効化した。
// (1.21) | 2019/12/22 (Sun) |   null 許容参照型を無効化できるようにした。
// (1.22) | 2020/11/15 (Sun) |   null 許容参照型の対応強化。
// (1.23) | 2021/03/06 (Sat) |   TraceEventTypeToCaption() 改善。
// (1.24) | 2021/08/26 (Thu) |   WPF アプリケーションのみ使用可能にした。
// (1.25) | 2021/08/26 (Thu) |   null 許容参照型を無効化できないようにした。
// ============================================================================

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Shinta
{
	public class LogWriter
	{
		// ====================================================================
		// public 定数
		// ====================================================================

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// トレースリスナー
		public SimpleTraceListener SimpleTraceListener { get; private set; }

		// 表示されるログを追加する関数
		public delegate void AppendDisplayTextDelegate(String text);
		public AppendDisplayTextDelegate? AppendDisplayText { get; set; }

		// アプリが終了シーケンスに入っているかどうかを表すトークン
		public CancellationToken ApplicationQuitToken { get; set; }

		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public LogWriter(String oName)
		{
			// トレースソース
#if DEBUG
			_traceSource = new TraceSource(oName, SourceLevels.Verbose);
#else
			_traceSource = new TraceSource(oName, SourceLevels.Information);
#endif

			// トレースリスナー
			_traceSource.Listeners.Remove(Common.TRACE_SOURCE_DEFAULT_LISTENER_NAME);
			SimpleTraceListener = new SimpleTraceListener();
			_traceSource.Listeners.Add(SimpleTraceListener);
		}

		// ====================================================================
		// public static メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// TraceEventType を表示用の文字列に変換
		// --------------------------------------------------------------------
		public static String TraceEventTypeToCaption(TraceEventType eventType)
		{
			return eventType switch
			{
				TraceEventType.Critical => "エラー",
				TraceEventType.Error => "エラー",
				TraceEventType.Warning => "警告",
				TraceEventType.Information => "情報",
				_ => String.Empty,
			};
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ログ書き込みのみ（UI には表示しない）
		// --------------------------------------------------------------------
		public void LogMessage(TraceEventType oEventType, String message)
		{
			// メッセージが空の場合は何もしない
			if (String.IsNullOrEmpty(message))
			{
				return;
			}

			// ログファイルに記録
			_traceSource.TraceEvent(oEventType, 0, message);
		}

		// --------------------------------------------------------------------
		// ログ書き込み＆表示
		// --------------------------------------------------------------------
		public MessageBoxResult ShowLogMessage(TraceEventType eventType, String message, Boolean suppressMessageBox = false)
		{
			// メッセージが空の場合は何もしない
			if (String.IsNullOrEmpty(message))
			{
				return MessageBoxResult.None;
			}

			// ログファイルに記録
			_traceSource.TraceEvent(eventType, 0, message);

			// アプリが終了シーケンスに入っている場合は、UI にアクセスすると、
			// ウィンドウハンドルが無い例外が発生したり、スレッドが止まったりするようなので、ここで返る
			if (ApplicationQuitToken.IsCancellationRequested)
			{
				return MessageBoxResult.OK;
			}

			// 表示の追加
			if (AppendDisplayText != null)
			{
				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
#if DEBUG
					AppendDisplayText(message);
#else
					if (eventType != TraceEventType.Verbose)
					{
						AppendDisplayText(message);
					}
#endif
				}));
			}

			// メッセージボックスに表示しない場合
			if (suppressMessageBox)
			{
				return MessageBoxResult.None;
			}

			// メッセージボックス表示
			MessageBoxImage icon = MessageBoxImage.Information;
			switch (eventType)
			{
				case TraceEventType.Critical:
				case TraceEventType.Error:
					icon = MessageBoxImage.Error;
					break;
				case TraceEventType.Warning:
					icon = MessageBoxImage.Warning;
					break;
				case TraceEventType.Information:
					icon = MessageBoxImage.Information;
					break;
				default:
					return MessageBoxResult.None;
			}

			try
			{
				// ワーカースレッドから Application.Current.Windows にアクセスすると別スレッドからのアクセスによる例外が発生する
				Window? activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);

				// 自アプリでアクティブなウィンドウがある場合はその子ウィンドウにする
				if (activeWindow != null)
				{
					MessageBoxResult result = MessageBoxResult.None;
					activeWindow.Dispatcher.Invoke(new Action(() =>
					{
						// IsActive でも実際には前面に来ていない場合もあるのでアクティブ化する
						activeWindow.Activate();
						result = MessageBox.Show(activeWindow, message, TraceEventTypeToCaption(eventType), MessageBoxButton.OK, icon);
					}));
					return result;
				}
			}
			catch (Exception)
			{
				// ワーカースレッドから Application.Current.Windows にアクセスできない
			}

			return MessageBox.Show(message, TraceEventTypeToCaption(eventType), MessageBoxButton.OK, icon);
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// ログ
		private readonly TraceSource _traceSource;
	}
}
