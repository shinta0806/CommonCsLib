// ============================================================================
// 
// 複数の位置を指定できるコントロール
// Copyright (C) 2016 by SHINTA
// 
// ============================================================================

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2016/04/07 (Thu) | 作成開始。
//  1.00  | 2016/04/09 (Sat) | オリジナルバージョン。
// ============================================================================

// ============================================================================
// 【マーカー】
// 範囲は 0.0f ～ 1.0f
// 常に 0.0f および 1.0f の位置にはマーカーがあるものとする
// マーカーはソートしない（入れ替わった時に勝手にソートするとクライアントが困る場合があるため）
// 【動作】
// マウスダウン：近辺にマーカーがあればそれを選択、なければマウスダウン位置を示す
// ドラッグ：両端なら新たにマーカーを作成してそれを移動、そうでなければ選択中のを移動
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Shinta.Wpf
{
	public class MultiMarker : Control
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// マーカーの位置
		public List<Single> Positions
		{
			get
			{
				// 複製を返す（勝手に中身をいじられないようにするため）
				return new List<Single>(mPositions);
			}
			set
			{
				mPositions = value;
				if (mPositions == null)
				{
					mPositions = new List<Single>();
				}
				else
				{
					// 複製を取得（コピー元がいじられても影響を受けないようにするため）
					mPositions = new List<Single>(value);
				}

				// 両端がなければ追加
				if (!mPositions.Contains(0.0f))
				{
					mPositions.Add(0.0f);
				}
				if (!mPositions.Contains(1.0f))
				{
					mPositions.Add(1.0f);
				}

				// SelectedIndex の調整
				if (SelectedIndex >= mPositions.Count)
				{
					SelectedIndex = mPositions.Count - 1;
				}

				Invalidate();
			}
		}

		// 選択中のマーカーの番号（List 中の位置、未選択なら UNSELECTED_INDEX）
		public Int32 SelectedIndex
		{
			get
			{
				return mSelectedIndex;
			}
			set
			{
				if (value >= mPositions.Count)
				{
					throw new ArgumentOutOfRangeException();
				}
				if (value < 0)
				{
					mSelectedIndex = UNSELECTED_INDEX;
				}
				else
				{
					mSelectedIndex = value;
				}

				Invalidate();
			}
		}

		// 追加予定のマーカーの位置（無しなら NO_ADDITIONAL_POSITION）
		public Single AdditionalPosition
		{
			get
			{
				return mAdditionalPosition;
			}
			set
			{
				if (value > 1.0f)
				{
					throw new ArgumentOutOfRangeException();
				}
				if (value < 0.0f)
				{
					mAdditionalPosition = NO_ADDITIONAL_POSITION;
				}
				else
				{
					mAdditionalPosition = value;
				}

				Invalidate();
			}
		}

		// 通常マーカー色（上半分）
		public Color MarkerTopColor
		{
			get
			{
				return mMarkerTopColor;
			}
			set
			{
				mMarkerTopColor = value;
				Invalidate();
			}
		}

		// 通常マーカー色（下半分）
		public Color MarkerBottomColor
		{
			get
			{
				return mMarkerBottomColor;
			}
			set
			{
				mMarkerBottomColor = value;
				Invalidate();
			}
		}

		// 選択中のマーカー色（上半分）
		public Color SelectedMarkerTopColor
		{
			get
			{
				return mSelectedMarkerTopColor;
			}
			set
			{
				mSelectedMarkerTopColor = value;
				Invalidate();
			}
		}

		// 選択中のマーカー色（下半分）
		public Color SelectedMarkerBottomColor
		{
			get
			{
				return mSelectedMarkerBottomColor;
			}
			set
			{
				mSelectedMarkerBottomColor = value;
				Invalidate();
			}
		}

		// ====================================================================
		// public 定数
		// ====================================================================

		// 未選択
		public const Int32 UNSELECTED_INDEX = -1;

		// 追加位置無し
		public const Single NO_ADDITIONAL_POSITION = -1.0f;

		// ====================================================================
		// public イベントハンドラー
		// ====================================================================

		// マーカー位置が変更された
		public event EventHandler PositionsChanged;

		// マーカー選択が変更された
		public event EventHandler SelectedIndexChanged;

		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public MultiMarker()
		{
			// デフォルトマーカー位置作成
			Positions = null;
			SelectedIndex = UNSELECTED_INDEX;
			AdditionalPosition = NO_ADDITIONAL_POSITION;

			// 色設定
			//BackColor = Color.Black;
			MarkerTopColor = Color.Black;
			MarkerBottomColor = Color.Black;
			SelectedMarkerTopColor = Color.FromArgb(65, 208, 244);
			SelectedMarkerBottomColor = Color.FromArgb(65, 208, 244);
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マウスダウン
		// --------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs oMouseEventArgs)
		{
			base.OnMouseDown(oMouseEventArgs);

			if (oMouseEventArgs.Button != MouseButtons.Left)
			{
				return;
			}

			Int32 aIndex;
			Int32 aDistance;
			GetNearestMarker(oMouseEventArgs.Y, out aIndex, out aDistance);

			if (aDistance <= MARKER_HEIGHT)
			{
				// 既存のマーカーを選択
				if (SelectedIndex != aIndex)
				{
					SelectedIndex = aIndex;
					AdditionalPosition = NO_ADDITIONAL_POSITION;

					// イベント発生
					if (SelectedIndexChanged != null)
					{
						SelectedIndexChanged(this, new EventArgs());
					}
				}
			}
			else
			{
				// 追加予定のマーカーの位置を設定
				SelectedIndex = UNSELECTED_INDEX;
				AdditionalPosition = (Single)oMouseEventArgs.Y / Height;

				// イベント発生
				if (SelectedIndexChanged != null)
				{
					SelectedIndexChanged(this, new EventArgs());
				}
			}
			Invalidate();

			// マウスキャプチャー
			Capture = true;
			mMouseDownInThis = true;
		}

		// --------------------------------------------------------------------
		// マウスムーブ
		// --------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs oMouseEventArgs)
		{
			base.OnMouseMove(oMouseEventArgs);

			if (oMouseEventArgs.Button != MouseButtons.Left || !mMouseDownInThis)
			{
				return;
			}

			//Debug.WriteLine("OnMouseMove() oMouseEventArgs.Y: " + oMouseEventArgs.Y.ToString());

			// マウス位置調整
			Int32 aY = oMouseEventArgs.Y;
			aY = Math.Max(aY, 0);
			aY = Math.Min(aY, Height);

			// マーカー
			if (SelectedIndex >= 0)
			{
				if (mPositions[SelectedIndex] == 0.0f || mPositions[SelectedIndex] == 1.0f)
				{
					// 両端が選択されている場合は、両端はずらさず、新規にマーカーを生成する
					mPositions.Add((Single)aY / Height);
					SelectedIndex = mPositions.Count - 1;
				}
				else
				{
					mPositions[SelectedIndex] = (Single)aY / Height;
				}

				// イベント発生
				if (PositionsChanged != null)
				{
					PositionsChanged(this, new EventArgs());
				}
			}

			// 追加予定位置
			if (AdditionalPosition >= 0.0f)
			{
				AdditionalPosition = (Single)aY / Height;
			}

			Invalidate();
		}

		// --------------------------------------------------------------------
		// マウスアップ
		// --------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs oMouseEventArgs)
		{
			base.OnMouseUp(oMouseEventArgs);

			Capture = false;
			mMouseDownInThis = false;
		}

		// --------------------------------------------------------------------
		// 再描画
		// --------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs oPaintEventArgs)
		{
			base.OnPaint(oPaintEventArgs);

			using (Bitmap aBitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb))
			{
				// バッファリング
				using (Graphics aGraphics = Graphics.FromImage(aBitmap))
				{
					// 背景
					using (Brush aBackBrush = new SolidBrush(BackColor))
					{
						aGraphics.FillRectangle(aBackBrush, 0, 0, Width, Height);
					}

					// マーカー
					for (Int32 i = 0; i < mPositions.Count; i++)
					{
						DrawMarker(aGraphics, i, MarkerTopColor, MarkerBottomColor);
					}

					// 選択中のマーカー
					if (SelectedIndex >= 0)
					{
						DrawMarker(aGraphics, SelectedIndex, SelectedMarkerTopColor, SelectedMarkerBottomColor);
					}

					// 追加予定のマーカーの位置
					if (AdditionalPosition >= 0.0f)
					{
						aGraphics.DrawLine(Pens.Blue, 0, Height * AdditionalPosition, Width, Height * AdditionalPosition);
					}

					// 枠
					aGraphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
				}

				// 描画
				oPaintEventArgs.Graphics.DrawImage(aBitmap, 0, 0);
			}
		}

		// ====================================================================
		// private 定数
		// ====================================================================

		// マーカーのサイズ（半分）
		private const Int32 MARKER_HEIGHT = 6;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// マーカーの位置
		private List<Single> mPositions;

		// 選択中のマーカーの番号
		private Int32 mSelectedIndex;

		// 追加予定のマーカーの位置
		private Single mAdditionalPosition;

		// 通常マーカー色（上半分）
		private Color mMarkerTopColor;

		// 通常マーカー色（下半分）
		private Color mMarkerBottomColor;

		// 選択中のマーカー色（上半分）
		private Color mSelectedMarkerTopColor;

		// 選択中のマーカー色（下半分）
		private Color mSelectedMarkerBottomColor;

		// コントロール内でマウスダウンされたかどうか
		private Boolean mMouseDownInThis = false;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// マーカー描画
		// --------------------------------------------------------------------
		private void DrawMarker(Graphics oGraphics, Int32 oIndex, Color oTopColor, Color oBottomColor)
		{
			Int32 aY = Y(oIndex);

			// 上半分
			Point[] aTopMarkerPoints = { new Point(0, aY), new Point(Width, aY), new Point(Width, aY - MARKER_HEIGHT) };
			using (Brush aTopBrush = new SolidBrush(oTopColor))
			{
				oGraphics.FillPolygon(aTopBrush, aTopMarkerPoints);
			}

			// 下半分
			Point[] aBottomMarkerPoints = { new Point(0, aY), new Point(Width, aY), new Point(Width, aY + MARKER_HEIGHT) };
			using (Brush aBottomBrush = new SolidBrush(oBottomColor))
			{
				oGraphics.FillPolygon(aBottomBrush, aBottomMarkerPoints);
			}
		}

		// --------------------------------------------------------------------
		// 指定された Y 座標に最も近いマーカーのインデックスを返す
		// --------------------------------------------------------------------
		private void GetNearestMarker(Int32 oY, out Int32 oIndex, out Int32 oDistance)
		{
			oIndex = 0;
			oDistance = oY;

			for (Int32 i = 1; i < mPositions.Count; i++)
			{
				Int32 aTmpDistance = Math.Abs(oY - Y(i));
				if (aTmpDistance < oDistance)
				{
					oIndex = i;
					oDistance = aTmpDistance;
				}
			}
		}

		// --------------------------------------------------------------------
		// 指定されたマーカーを描画する際の Y 座標
		// --------------------------------------------------------------------
		private Int32 Y(Int32 oIndex)
		{
			return (Int32)(mPositions[oIndex] * Height);
		}

	} // public class MultiMarker : Control
} // namespace Shinta

