// ============================================================================
// 
// ログをファイル・メッセージボックス・テキストボックスに出力する
// Copyright (C) 2016-2019 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2016/09/22 (Thu) | 作成開始。
//  1.00  | 2016/09/22 (Thu) | オリジナルバージョン。
//  1.10  | 2016/09/24 (Sat) | LogMessage() を作成。
// (1.11) | 2017/11/20 (Mon) | LogMessage() がリリース時にデバッグ情報をテキストボックスに表示していたのを修正。
// (1.12) | 2017/12/24 (Sun) | ShowLogMessage() のフォームを前面に出す動作の改善。
// (1.13) | 2018/03/18 (Sun) | ShowLogMessage() のフォームを前面に出す動作をさらに改善。
// (1.14) | 2019/01/20 (Sun) | WPF アプリケーションでも使用可能にした。
// (1.15) | 2019/03/10 (Sun) | WPF アプリケーションでも TextBox 出力できるようにした。
// ============================================================================

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

#if USE_FORM
using System.Windows.Forms;
#elif USE_WPF
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
#else
#error Define USE_FORM or USE_WPF
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
#endif
#if USE_WPF
		// メッセージボックス表示時に前面に出すウィンドウ
		public Window FrontWindow { get; set; }
#endif

		// ログを表示するテキストボックス
		public TextBox TextBoxDisplay { get; set; }

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
		public MessageBoxResult ShowLogMessage(TraceEventType oEventType, String oMessage, Boolean oSuppressMessageBox = false)
		{
			// メッセージが空の場合は何もしない
			if (String.IsNullOrEmpty(oMessage))
			{
				return MessageBoxResult.None;
			}

			// ログファイルに記録
			mTraceSource.TraceEvent(oEventType, 0, oMessage);

			// アプリが終了シーケンスに入っている場合は、UI にアクセスすると、
			// ウィンドウハンドルが無い例外が発生したり、スレッドが止まったりするようなので、ここで返る
			if (ApplicationQuitToken != null && ApplicationQuitToken.IsCancellationRequested)
			{
				return MessageBoxResult.OK;
			}

			// テキストボックスへの表示
			if (TextBoxDisplay != null)
			{
				TextBoxDisplay.Dispatcher.Invoke(new Action(() =>
				{
					Boolean aIsScrollNeeded = TextBoxDisplay.CaretIndex == TextBoxDisplay.Text.Length;
#if DEBUG
					TextBoxDisplay.AppendText(oMessage + "\r\n");
#else
					if (oEventType != TraceEventType.Verbose)
					{
						TextBoxDisplay.AppendText(oMessage + "\r\n");
					}
#endif
					if (aIsScrollNeeded)
					{
						TextBoxDisplay.CaretIndex = TextBoxDisplay.Text.Length;
						TextBoxDisplay.ScrollToEnd();
					}
				}));
			}

			// メッセージボックスに表示しない場合
			if (oSuppressMessageBox)
			{
				return MessageBoxResult.None;
			}

			// メッセージボックス表示
			MessageBoxImage aIcon = MessageBoxImage.Information;
			switch (oEventType)
			{
				case TraceEventType.Critical:
				case TraceEventType.Error:
					aIcon = MessageBoxImage.Error;
					break;
				case TraceEventType.Warning:
					aIcon = MessageBoxImage.Warning;
					break;
				case TraceEventType.Information:
					aIcon = MessageBoxImage.Information;
					break;
				default:
					return MessageBoxResult.None;
			}

			// FrontWindow が指定されている場合はその子フォームにする
			MessageBoxResult aResult = MessageBoxResult.None;
			if (FrontWindow != null)
			{
				FrontWindow.Dispatcher.Invoke(new Action(() =>
				{
					FrontWindow.Activate();
					aResult = MessageBox.Show(FrontWindow, oMessage, TraceEventTypeToCaption(oEventType), MessageBoxButton.OK, aIcon);
				}));
				return aResult;
			}

			try
			{
				// ワーカースレッドから Application.Current.Windows にアクセスすると別スレッドからのアクセスによる例外が発生する
				Window aActiveWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);

				// 自アプリでアクティブなフォームがある場合はその子フォームにする
				if (aActiveWindow != null)
				{
					aActiveWindow.Dispatcher.Invoke(new Action(() =>
					{
						// IsActive でも実際には前面に来ていない場合もあるのでアクティブ化する
						aActiveWindow.Activate();
						aResult = MessageBox.Show(aActiveWindow, oMessage, TraceEventTypeToCaption(oEventType), MessageBoxButton.OK, aIcon);
					}));
					return aResult;
				}
			}
			catch (Exception)
			{
			}

			try
			{
				// ダミーフォームを前面に表示してその子フォームにする
				Window aFrontForm = new Window();
				aFrontForm.Left = -100;
				aFrontForm.Top = -100;
				aFrontForm.Width = 50;
				aFrontForm.Height = 50;
				aFrontForm.Topmost = true;
				aFrontForm.Show();
				aResult = MessageBox.Show(aFrontForm, oMessage, TraceEventTypeToCaption(oEventType), MessageBoxButton.OK, aIcon);
				aFrontForm.Topmost = false;
				aFrontForm.Close();
				return aResult;
			}
			catch (Exception)
			{
			}

			return MessageBox.Show(oMessage, TraceEventTypeToCaption(oEventType), MessageBoxButton.OK, aIcon);
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

