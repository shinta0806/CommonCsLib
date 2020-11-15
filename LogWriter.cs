// ============================================================================
// 
// ログをファイル・メッセージボックス・テキストボックスに出力する
// Copyright (C) 2016-2020 by SHINTA
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
// ============================================================================

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

#if USE_FORM
using System.Windows.Forms;
#elif USE_WPF
using System.Windows;
#else
#error Define USE_FORM or USE_WPF
#endif

#if !NULLABLE_DISABLED
#nullable enable
#endif

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

#if USE_FORM
		// メッセージボックス表示時に前面に出すフォーム
		public Form FrontForm { get; set; }

		// ログを表示するテキストボックス
		public TextBox TextBoxDisplay { get; set; }
#endif

		// 表示されるログを追加する関数
		public delegate void AppendDisplayTextDelegate(String oText);
#if !NULLABLE_DISABLED
		public AppendDisplayTextDelegate? AppendDisplayText { get; set; }
#else
		public AppendDisplayTextDelegate AppendDisplayText { get; set; }
#endif

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
			mTraceSource = new TraceSource(oName, SourceLevels.Verbose);
#else
			mTraceSource = new TraceSource(oName, SourceLevels.Information);
#endif

			// トレースリスナー
			mTraceSource.Listeners.Remove(Common.TRACE_SOURCE_DEFAULT_LISTENER_NAME);
			SimpleTraceListener = new SimpleTraceListener();
			mTraceSource.Listeners.Add(SimpleTraceListener);
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ログ書き込みのみ（UI には表示しない）
		// --------------------------------------------------------------------
		public void LogMessage(TraceEventType oEventType, String oMessage)
		{
			// メッセージが空の場合は何もしない
			if (String.IsNullOrEmpty(oMessage))
			{
				return;
			}

			// ログファイルに記録
			mTraceSource.TraceEvent(oEventType, 0, oMessage);
		}

#if USE_FORM
		// --------------------------------------------------------------------
		// ログ書き込み＆表示
		// --------------------------------------------------------------------
		public DialogResult ShowLogMessage(TraceEventType oEventType, String oMessage, Boolean oSuppressMessageBox = false)
		{
			// メッセージが空の場合は何もしない
			if (String.IsNullOrEmpty(oMessage))
			{
				return DialogResult.None;
			}

			// ログファイルに記録
			mTraceSource.TraceEvent(oEventType, 0, oMessage);

			// アプリが終了シーケンスに入っている場合は、UI にアクセスすると、
			// ウィンドウハンドルが無い例外が発生したり、スレッドが止まったりするようなので、ここで返る
			if (ApplicationQuitToken != null && ApplicationQuitToken.IsCancellationRequested)
			{
				return DialogResult.OK;
			}

			// テキストボックスへの表示
			if (TextBoxDisplay != null)
			{
				TextBoxDisplay.Invoke(new Action(() =>
				{
#if DEBUG
					TextBoxDisplay.AppendText(oMessage + "\r\n");
#else
					if (oEventType != TraceEventType.Verbose)
					{
						TextBoxDisplay.AppendText(oMessage + "\r\n");
					}
#endif
				}));
			}

			// メッセージボックスに表示しない場合
			if (oSuppressMessageBox)
			{
				return DialogResult.None;
			}

			// メッセージボックス表示
			MessageBoxIcon aIcon = MessageBoxIcon.Information;
			switch (oEventType)
			{
				case TraceEventType.Critical:
				case TraceEventType.Error:
					aIcon = MessageBoxIcon.Error;
					break;
				case TraceEventType.Warning:
					aIcon = MessageBoxIcon.Warning;
					break;
				case TraceEventType.Information:
					aIcon = MessageBoxIcon.Information;
					break;
				default:
					return DialogResult.None;
			}

			// FrontForm が指定されている場合はその子フォームにする
			DialogResult aResult = DialogResult.None;
			if (FrontForm != null)
			{
				FrontForm.Invoke(new Action(() =>
				{
					FrontForm.Activate();
					aResult = MessageBox.Show(FrontForm, oMessage, TraceEventTypeToCaption(oEventType), MessageBoxButtons.OK, aIcon);
				}));
				return aResult;
			}

			// 自アプリでアクティブなフォームがある場合はその子フォームにする
			Form aActiveForm = Form.ActiveForm;
			if (aActiveForm != null)
			{
				aActiveForm.Invoke(new Action(() =>
				{
					aResult = MessageBox.Show(aActiveForm, oMessage, TraceEventTypeToCaption(oEventType), MessageBoxButtons.OK, aIcon);
				}));
				return aResult;
			}

			// ダミーフォームを前面に表示してその子フォームにする
			using (Form aFrontForm = new Form())
			{
				aFrontForm.TopMost = true;
				aResult = MessageBox.Show(aFrontForm, oMessage, TraceEventTypeToCaption(oEventType), MessageBoxButtons.OK, aIcon);
				aFrontForm.TopMost = false;
				return aResult;
			}
		}
#endif

#if USE_WPF
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
			mTraceSource.TraceEvent(eventType, 0, message);

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

				// 自アプリでアクティブなフォームがある場合はその子フォームにする
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
				Debug.WriteLine("ShowLogMessage() ワーカースレッドから Application.Current.Windows にアクセスできない");
			}

			return MessageBox.Show(message, TraceEventTypeToCaption(eventType), MessageBoxButton.OK, icon);
		}
#endif

		// --------------------------------------------------------------------
		// TraceEventType を表示用の文字列に変換
		// --------------------------------------------------------------------
		public String TraceEventTypeToCaption(TraceEventType oEventType)
		{
			switch (oEventType)
			{
				case TraceEventType.Critical:
				case TraceEventType.Error:
					return "エラー";
				case TraceEventType.Warning:
					return "警告";
				case TraceEventType.Information:
					return "情報";
			}
			return String.Empty;
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// ログ
		private TraceSource mTraceSource;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

	} // public class LogWriter

} // namespace Shinta

