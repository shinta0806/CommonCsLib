// ============================================================================
// 
// ログをファイル・メッセージボックス・テキストボックスに出力する
// Copyright (C) 2016-2017 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2016/09/22 (Thu) | 作成開始。
//  1.00  | 2016/09/22 (Thu) | オリジナルバージョン。
//  1.10  | 2016/09/24 (Sat) | LogMessage() を作成。
// (1.11) | 2017/11/20 (Mon) | LogMessage() がリリース時にデバッグ情報をテキストボックスに表示していたのを修正。
// (1.12) | 2017/12/24 (Sun) | フォームを前面に出す動作の改善。
// ============================================================================

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

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

		// メッセージボックス表示時に前面に出すフォーム
		public Form FrontForm { get; set; }

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

			using (Form aFrontForm = new Form())
			{
				aFrontForm.TopMost = true;
				aResult = MessageBox.Show(aFrontForm, oMessage, TraceEventTypeToCaption(oEventType), MessageBoxButtons.OK, aIcon);
				aFrontForm.TopMost = false;
				return aResult;
			}
		}

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

