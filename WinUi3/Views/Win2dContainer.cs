// ============================================================================
// 
// CanvasControl をコンテナし Win2D で描画するカスタムコントロールの基底クラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// Draw 時に Win2D 直接描画するコントロールはこのクラスから派生させる
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// フォーカス、マウス・キーボード入力は this で行う（_canvasControl で行わない）
// ----------------------------------------------------------------------------

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.Foundation;

namespace Shinta.WinUi3.Views;

internal partial class Win2dContainer : Grid
{
	// ====================================================================
	// コンストラクター
	// ====================================================================

	/// <summary>
	/// メインコンストラクター
	/// </summary>
	public Win2dContainer()
	{
		// フォーカス
		IsTabStop = true;

		// イベント
		Loaded += Win2dContainerLoaded;
		PointerMoved += Win2dContainerPointerMoved;
		PointerPressed += Win2dContainerPointerPressed;
		PointerReleased += Win2dContainerPointerReleased;
		Unloaded += Win2dContainerUnloaded;
	}

	// ====================================================================
	// public プロパティー
	// ====================================================================

	/// <summary>
	/// 有効
	/// </summary>
	private Boolean _isEnabled;
	public Boolean IsEnabled
	{
		get => _isEnabled;
		set
		{
			if (value == _isEnabled)
			{
				return;
			}
			_isEnabled = value;
			Invalidate();
		}
	}

	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// 再描画を引き起こす
	/// </summary>
	public virtual void Invalidate()
	{
		_canvasControl?.Invalidate();
	}

	// ====================================================================
	// protected 変数
	// ====================================================================

	/// <summary>
	/// Win2D 描画用
	/// </summary>
	protected CanvasControl? _canvasControl;

	// ====================================================================
	// protected 関数
	// ====================================================================

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="canvasControl"></param>
	/// <param name="drawingSession"></param>
	protected virtual void Draw(CanvasControl canvasControl, CanvasDrawingSession drawingSession)
	{
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="point"></param>
	/// <param name="args"></param>
	protected virtual void MouseDownLeft(Point point, PointerRoutedEventArgs args)
	{
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="point"></param>
	/// <param name="args"></param>
	protected virtual void MouseDownRight(Point point, PointerRoutedEventArgs args)
	{
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="point"></param>
	/// <param name="args"></param>
	protected virtual void MouseMove(Point point, PointerRoutedEventArgs args)
	{
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="point"></param>
	/// <param name="args"></param>
	protected virtual void MouseUp(Point point, PointerRoutedEventArgs args)
	{
	}

	// ====================================================================
	// private 定数
	// ====================================================================

	/// <summary>
	/// マウス押下位置引き継ぎをする時間 [ms]
	/// </summary>
	private const Int32 SUCCESSION_TIME = 200;

	// ====================================================================
	// private 変数
	// ====================================================================

	/// <summary>
	/// 最後に押されたマウス（DataGrid で使用されたとき用）
	/// 本来は static ではなく、1 つの DataGridXxxColumn に紐付くこのコントロール同士で引き継ぐのが良いが、
	/// DataGrid の同じセルでも同じ DataGridXxxColumn からこのコントロールが生成されるとは限らないような挙動をしており
	/// うまく引き継げなかったので、やむを得ず static にしている
	/// </summary>
	private static PointerPoint? _lastPointerPoint;
	private static PointerRoutedEventArgs? _lastPointerArgs;

	/// <summary>
	/// 最後にマウスが押されたクラス
	/// _lastPointerPoint が static なので派生クラスで共用してしまう
	/// どの派生クラスでマウスが押されたのかを見分ける
	/// </summary>
	private static Type? _lastPointerType;

	// ====================================================================
	// private 関数
	// ====================================================================

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void CanvasControlDraw(CanvasControl canvasControl, CanvasDrawEventArgs args)
	{
		try
		{
			Draw(canvasControl, args.DrawingSession);
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException(GetType().Name + " 描画時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void Win2dContainerLoaded(Object sender, RoutedEventArgs args)
	{
		try
		{
			// 描画設定
			// AddVeil() / RemoveVeil() により Loaded() / Unloaded() がうまく対応しない模様なので、_canvasControl が既に作成されている可能性を考慮する
			if (_canvasControl == null)
			{
				_canvasControl = new()
				{
					IsTabStop = false
				};
				_canvasControl.Draw += CanvasControlDraw;
				Children.Add(_canvasControl);
			}

			// マウス押下位置引き継ぎ
			if (_lastPointerPoint != null && _lastPointerArgs != null && _lastPointerType == GetType())
			{
				Int32 delta = Environment.TickCount - (Int32)(_lastPointerPoint.Timestamp / 1000);
				if (delta < SUCCESSION_TIME)
				{
					// _lastPointerPoint は左ボタンの時のみ保存しているので実質左クリックのみ
					MouseDownLeft(_lastPointerPoint.Position, _lastPointerArgs);
				}
			}
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException(GetType().Name + " ロード時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void Win2dContainerPointerMoved(Object sender, PointerRoutedEventArgs args)
	{
		try
		{
			if (sender is not Win2dContainer win2DContainer)
			{
				return;
			}

			PointerPoint pointerPoint = args.GetCurrentPoint(win2DContainer);
			MouseMove(pointerPoint.Position, args);
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException(GetType().Name + " マウス移動時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void Win2dContainerPointerPressed(Object sender, PointerRoutedEventArgs args)
	{
		try
		{
			if (sender is not Win2dContainer win2DContainer)
			{
				return;
			}

			PointerPoint pointerPoint = args.GetCurrentPoint(win2DContainer);
			if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
			{
				if (pointerPoint.Properties.IsRightButtonPressed)
				{
					// マウスの場合、右ボタン押下も判定
					MouseDownRight(pointerPoint.Position, args);
				}
				if (!pointerPoint.Properties.IsLeftButtonPressed)
				{
					// マウスの場合、中ボタン押下は処理しない
					return;
				}
			}
			MouseDownLeft(pointerPoint.Position, args);
			_lastPointerPoint = pointerPoint;
			_lastPointerArgs = args;
			_lastPointerType = GetType();
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException(GetType().Name + " マウス押下時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void Win2dContainerPointerReleased(Object sender, PointerRoutedEventArgs args)
	{
		try
		{
			if (sender is not Win2dContainer win2DContainer)
			{
				return;
			}

			PointerPoint pointerPoint = args.GetCurrentPoint(win2DContainer);

			// マウスの場合でも IsLeftButtonPressed では左ボタン押下だったかどうか見分けられない
			MouseUp(pointerPoint.Position, args);
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException(GetType().Name + " マウス解放時エラー", ex);
		}
	}

	/// <summary>
	/// イベントハンドラー
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	private void Win2dContainerUnloaded(Object sender, RoutedEventArgs args)
	{
		try
		{
			// AddVeil() / RemoveVeil() により Loaded() / Unloaded() がうまく対応しない模様なので、IsLoaded で本当にアンロードされたかを確認する
			if (!IsLoaded)
			{
				_canvasControl?.RemoveFromVisualTree();
				_canvasControl = null;
			}
		}
		catch (Exception ex)
		{
			SerilogUtils.LogException(GetType().Name + " アンロード時エラー", ex);
		}
	}
}
