// ============================================================================
// 
// Window のバインド可能なプロパティーを増やすためのビヘイビア
// Copyright (C) 2019-2023 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// AssociatedObject は null ではないことになっているが、実際には null があり得る（ViewModel 側での変更時にあり得る？）
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// 以下のパッケージがインストールされている前提
//   Microsoft.Windows.CsWin32
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  1.00  | 2019/06/24 (Mon) | オリジナルバージョン。
//  1.10  | 2019/06/29 (Sat) | IsCascade を実装。
// (1.11) | 2019/12/07 (Sat) |   null 許容参照型を有効化した。
// (1.12) | 2020/03/29 (Sun) |   null 許容参照型の対応強化。
//  1.20  | 2021/07/11 (Sun) | OwnedWindows を実装。
// (1.21) | 2021/11/20 (Sat) |   null 許容参照型の対応強化。
// (1.22) | 2022/02/26 (Sat) |   MinimizeBox を nullable にした。
//  1.30  | 2022/02/26 (Sat) | HelpBox を実装。
// (1.31) | 2022/02/26 (Sat) |   ClosingCommand の実装を改善。
//  1.40  | 2022/02/26 (Sat) | HelpBoxClickedCommand を実装。
//  1.50  | 2022/02/26 (Sat) | HelpBoxClickedCommandParameter を実装。
// (1.51) | 2022/05/31 (Tue) |   HelpBoxClickedCommand の動作を改善。
// (1.52) | 2023/02/11 (Sat) |   PInvoke.User32 を導入。
// (1.53) | 2023/11/19 (Sun) |   Microsoft.Windows.CsWin32 パッケージを導入。
// ============================================================================

using Microsoft.Xaml.Behaviors;

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Shinta.Wpf.Behaviors
{
	public class WindowBindingSupportBehavior : Behavior<Window>
	{
		// ====================================================================
		// コンストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// メインコンストラクター
		// --------------------------------------------------------------------
		public WindowBindingSupportBehavior()
		{
			_wndProc = new HwndSourceHook(WndProc);
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// Window.Closing をコマンドで扱えるようにする
		public static readonly DependencyProperty ClosingCommandProperty
				= DependencyProperty.RegisterAttached(nameof(ClosingCommand), typeof(ICommand), typeof(WindowBindingSupportBehavior),
				new PropertyMetadata(null, SourceClosingCommandChanged));
		public ICommand? ClosingCommand
		{
			get => (ICommand?)GetValue(ClosingCommandProperty);
			set => SetValue(ClosingCommandProperty, value);
		}

		// キャプションバーのヘルプボタンクリックをコマンドで扱えるようにする
		public static readonly DependencyProperty HelpBoxClickedCommandProperty
				= DependencyProperty.RegisterAttached(nameof(HelpBoxClickedCommand), typeof(ICommand), typeof(WindowBindingSupportBehavior),
				new PropertyMetadata(null, SourceHelpBoxClickedCommandChanged));
		public ICommand? HelpBoxClickedCommand
		{
			get => (ICommand?)GetValue(HelpBoxClickedCommandProperty);
			set => SetValue(HelpBoxClickedCommandProperty, value);
		}

		// キャプションバーのヘルプボタンクリックのパラメーター
		public static readonly DependencyProperty HelpBoxClickedCommandParameterProperty
				= DependencyProperty.RegisterAttached(nameof(HelpBoxClickedCommandParameter), typeof(String), typeof(WindowBindingSupportBehavior),
				new PropertyMetadata(null, SourceHelpBoxClickedCommandParameterChanged));
		public String? HelpBoxClickedCommandParameter
		{
			get => (String?)GetValue(HelpBoxClickedCommandParameterProperty);
			set => SetValue(HelpBoxClickedCommandParameterProperty, value);
		}

		// Window.IsActive をバインド可能にする
		// Window.IsActive は元々読み取り専用だが変更可能とするためにコールバックを登録する
		public static readonly DependencyProperty IsActiveProperty =
				DependencyProperty.Register(nameof(IsActive), typeof(Boolean), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceIsActiveChanged));
		public Boolean IsActive
		{
			get => (Boolean)GetValue(IsActiveProperty);
			set => SetValue(IsActiveProperty, value);
		}

		// オーナーウィンドウが設定されている場合、オーナーウィンドウの位置に対してカスケードするかどうか
		public static readonly DependencyProperty IsCascadeProperty =
				DependencyProperty.Register(nameof(IsCascade), typeof(Boolean), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceIsCascadeChanged));
		public Boolean IsCascade
		{
			get => (Boolean)GetValue(IsCascadeProperty);
			set => SetValue(IsCascadeProperty, value);
		}

		// ウィンドウのキャプションバーに最小化ボタンを表示するかどうか
		// null の場合は変更せずに現状維持
		public static readonly DependencyProperty MinimizeBoxProperty =
				DependencyProperty.Register(nameof(MinimizeBox), typeof(Boolean?), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceMinimizeBoxChanged));
		public Boolean? MinimizeBox
		{
			get => (Boolean?)GetValue(MinimizeBoxProperty);
			set => SetValue(MinimizeBoxProperty, value);
		}

		// ウィンドウのキャプションバーにヘルプボタンを表示するかどうか
		// null の場合は変更せずに現状維持
		public static readonly DependencyProperty HelpBoxProperty =
				DependencyProperty.Register(nameof(HelpBox), typeof(Boolean?), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceHelpBoxChanged));
		public Boolean? HelpBox
		{
			get => (Boolean?)GetValue(HelpBoxProperty);
			set => SetValue(HelpBoxProperty, value);
		}

		// Window.OwnedWindows をバインド可能にする
		public static readonly DependencyProperty OwnedWindowsProperty =
				DependencyProperty.Register(nameof(OwnedWindows), typeof(WindowCollection), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(new WindowCollection(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public WindowCollection OwnedWindows
		{
			get => (WindowCollection)GetValue(OwnedWindowsProperty);
			set => SetValue(OwnedWindowsProperty, value);
		}

		// Window.OwnedWindows の更新要求
		// ViewModel 側に false が伝播されないので、ViewModel 側は RaisePropertyChangedIfSet() ではなく
		// RaisePropertyChanged() で強制発効する必要がある
		public static readonly DependencyProperty OwnedWindowsUpdateRequestProperty =
				DependencyProperty.Register(nameof(OwnedWindowsUpdateRequest), typeof(Boolean), typeof(WindowBindingSupportBehavior),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SourceOwnedWindowsUpdateRequestChanged));
		public Boolean OwnedWindowsUpdateRequest
		{
			get => (Boolean)GetValue(OwnedWindowsUpdateRequestProperty);
			set => SetValue(OwnedWindowsUpdateRequestProperty, value);
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

			AssociatedObject.Activated += ControlActivated;
			AssociatedObject.Deactivated += ControlDeactivated;
			AssociatedObject.Loaded += ControlLoaded;
			AssociatedObject.SourceInitialized += ControlSourceInitialized;
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// WndProc
		private readonly HwndSourceHook _wndProc;

		// 初期化済
		private Boolean _initialized;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ウィンドウの位置をオーナーウィンドウに対してカスケードする
		// --------------------------------------------------------------------
		private void CascadeWindowIfNeeded()
		{
			if (!IsCascade || AssociatedObject?.Owner == null)
			{
				return;
			}

			// 位置をずらす
			Double delta = SystemParameters.CaptionHeight + SystemParameters.WindowResizeBorderThickness.Top;
			Double newLeft = AssociatedObject.Owner.Left + delta;
			Double newTop = AssociatedObject.Owner.Top + delta;

			// ディスプレイからはみ出さないように調整
			if (newLeft + AssociatedObject.ActualWidth > SystemParameters.VirtualScreenWidth)
			{
				newLeft = AssociatedObject.Owner.Left;
			}
			if (newTop + AssociatedObject.ActualHeight > SystemParameters.VirtualScreenHeight)
			{
				newTop = AssociatedObject.Owner.Top;
			}

			AssociatedObject.Left = newLeft;
			AssociatedObject.Top = newTop;
		}

		// --------------------------------------------------------------------
		// View 側で IsActive が変更された
		// --------------------------------------------------------------------
		private void ControlActivated(Object? sender, EventArgs args)
		{
			IsActive = true;
		}

		// --------------------------------------------------------------------
		// View 側で Closing された
		// --------------------------------------------------------------------
		private void ControlClosing(Object? sender, CancelEventArgs cancelEventArgs)
		{
			if (ClosingCommand == null || !ClosingCommand.CanExecute(null))
			{
				return;
			}

			// イベント引数を引数としてコマンドを実行
			ClosingCommand.Execute(cancelEventArgs);
		}

		// --------------------------------------------------------------------
		// View 側で IsActive が変更された
		// --------------------------------------------------------------------
		private void ControlDeactivated(Object? sender, EventArgs args)
		{
			IsActive = false;
		}

		// --------------------------------------------------------------------
		// View 側で Loaded された
		// --------------------------------------------------------------------
		private void ControlLoaded(Object sender, RoutedEventArgs routedEventArgs)
		{
			// SizeToContent が WidthAndHeight 等の場合は SourceInitialized よりも Loaded が先に呼び出されるのでここで処理を行う
			Initialize();
		}

		// --------------------------------------------------------------------
		// View 側で SourceInitialized された
		// --------------------------------------------------------------------
		private void ControlSourceInitialized(Object? sender, EventArgs args)
		{
			// SizeToContent が Manual の場合は Loaded よりも SourceInitialized が先に呼び出されるのでここで処理を行う
			Initialize();
		}

		// --------------------------------------------------------------------
		// Loaded または SourceInitialized 時の処理
		// --------------------------------------------------------------------
		private void Initialize()
		{
			if (AssociatedObject == null || _initialized)
			{
				return;
			}

			CascadeWindowIfNeeded();
			UpdateMinimizeBox();
			UpdateHelpBox();

			// メインウィンドウで使用される際、フレームワークから呼ばれる XXXXChanged() では AssociatedObject が null になるため、ここで再度呼びだす
			// OnAttached() だと Handle が IntPtr.Zero のためここで呼びだす
			SourceClosingCommandChangedCore(this, ClosingCommand);
			SourceHelpBoxClickedCommandChangedCore(this, HelpBoxClickedCommand);

			_initialized = true;
		}

		// --------------------------------------------------------------------
		// ViewModel 側で ClosingCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceClosingCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			SourceClosingCommandChangedCore(obj, args.NewValue);
		}

		// --------------------------------------------------------------------
		// ViewModel 側で ClosingCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceClosingCommandChangedCore(DependencyObject obj, Object? newValue)
		{
			if ((obj is not WindowBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			// 二重登録されないように、いったんイベントハンドラーを無効にする
			thisObject.AssociatedObject.Closing -= thisObject.ControlClosing;

			if (newValue != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				thisObject.AssociatedObject.Closing += thisObject.ControlClosing;
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で HelpBox が変更された
		// --------------------------------------------------------------------
		private static void SourceHelpBoxChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (obj is WindowBindingSupportBehavior thisObject)
			{
				thisObject.UpdateHelpBox();
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で HelpBoxClickedCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceHelpBoxClickedCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			SourceHelpBoxClickedCommandChangedCore(obj, args.NewValue);
		}

		// --------------------------------------------------------------------
		// ViewModel 側で HelpBoxClickedCommand が変更された
		// --------------------------------------------------------------------
		private static void SourceHelpBoxClickedCommandChangedCore(DependencyObject obj, Object? newValue)
		{
			if ((obj is not WindowBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			WindowInteropHelper helper = new(thisObject.AssociatedObject);
			if (helper.Handle == IntPtr.Zero)
			{
				return;
			}

			HwndSource hwndSource = HwndSource.FromHwnd(helper.Handle);

			// 二重登録されないように、いったんイベントハンドラーを無効にする
			hwndSource.RemoveHook(thisObject._wndProc);

			if (newValue != null)
			{
				// コマンドが設定された場合はイベントハンドラーを有効にする
				hwndSource.AddHook(thisObject._wndProc);
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で HelpBoxClickedCommandParameter が変更された
		// --------------------------------------------------------------------
		private static void SourceHelpBoxClickedCommandParameterChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
		}

		// --------------------------------------------------------------------
		// ViewModel 側で IsActive が変更された
		// --------------------------------------------------------------------
		private static void SourceIsActiveChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if ((obj is not WindowBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			if ((Boolean)args.NewValue)
			{
				thisObject.AssociatedObject.Activate();
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で IsCascade が変更された
		// --------------------------------------------------------------------
		private static void SourceIsCascadeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (obj is WindowBindingSupportBehavior thisObject)
			{
				thisObject.CascadeWindowIfNeeded();
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で MinimizeBox が変更された
		// --------------------------------------------------------------------
		private static void SourceMinimizeBoxChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (obj is WindowBindingSupportBehavior thisObject)
			{
				thisObject.UpdateMinimizeBox();
			}
		}

		// --------------------------------------------------------------------
		// ViewModel 側で OwnedWindowsUpdateRequest が変更された
		// --------------------------------------------------------------------
		private static void SourceOwnedWindowsUpdateRequestChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if ((obj is not WindowBindingSupportBehavior thisObject) || thisObject.AssociatedObject == null)
			{
				return;
			}

			thisObject.OwnedWindows = thisObject.AssociatedObject.OwnedWindows;
			thisObject.OwnedWindowsUpdateRequest = false;
		}

		// --------------------------------------------------------------------
		// ヘルプボタンの状態を更新
		// --------------------------------------------------------------------
		private void UpdateHelpBox()
		{
			if (AssociatedObject == null || HelpBox == null)
			{
				return;
			}

			WindowInteropHelper helper = new(AssociatedObject);
			WINDOW_EX_STYLE exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong((HWND)helper.Handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
			if (HelpBox == true)
			{
				PInvoke.SetWindowLong((HWND)helper.Handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (Int32)(exStyle | WINDOW_EX_STYLE.WS_EX_CONTEXTHELP));
			}
			else
			{
				PInvoke.SetWindowLong((HWND)helper.Handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (Int32)(exStyle & ~WINDOW_EX_STYLE.WS_EX_CONTEXTHELP));
			}
		}

		// --------------------------------------------------------------------
		// 最小化ボタンの状態を更新
		// --------------------------------------------------------------------
		private void UpdateMinimizeBox()
		{
			if (AssociatedObject == null || MinimizeBox == null)
			{
				return;
			}

			WindowInteropHelper helper = new(AssociatedObject);
			WINDOW_STYLE style = (WINDOW_STYLE)PInvoke.GetWindowLong((HWND)helper.Handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);

			if (MinimizeBox == true)
			{
				PInvoke.SetWindowLong((HWND)helper.Handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (Int32)(style | WINDOW_STYLE.WS_MINIMIZEBOX));
			}
			else
			{
				PInvoke.SetWindowLong((HWND)helper.Handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (Int32)(style & ~WINDOW_STYLE.WS_MINIMIZEBOX));
			}
		}

		// --------------------------------------------------------------------
		// WM_SYSCOMMAND メッセージハンドラ
		// --------------------------------------------------------------------
		private void WmSysCommand(IntPtr _1, IntPtr wParam, IntPtr _2, ref Boolean handled)
		{
			switch ((UInt32)wParam)
			{
				case PInvoke.SC_CONTEXTHELP:
					if (HelpBoxClickedCommand != null)
					{
						if (HelpBoxClickedCommand.CanExecute(HelpBoxClickedCommandParameter))
						{
							HelpBoxClickedCommand.Execute(HelpBoxClickedCommandParameter);
							handled = true;
						}
					}
					break;
			}
		}

		// --------------------------------------------------------------------
		// メッセージハンドラ
		// --------------------------------------------------------------------
		private IntPtr WndProc(IntPtr hWnd, Int32 msg, IntPtr wParam, IntPtr lParam, ref Boolean handled)
		{
			switch ((UInt32)msg)
			{
				case PInvoke.WM_SYSCOMMAND:
					WmSysCommand(hWnd, wParam, lParam, ref handled);
					break;
			}

			return IntPtr.Zero;
		}
	}
}
