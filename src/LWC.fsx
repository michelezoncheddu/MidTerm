open System.Windows.Forms
open System.Drawing

// Library
type WVMatrix() =
  let wv = new Drawing2D.Matrix()
  let vw = new Drawing2D.Matrix()

  member this.TranslateW(tx, ty) =
    wv.Translate(tx, ty)
    vw.Translate(-tx, -ty, Drawing2D.MatrixOrder.Append)

  member this.ScaleW(sx, sy) =
    wv.Scale(sx, sy)
    vw.Scale(1.f / sx, 1.f / sy, Drawing2D.MatrixOrder.Append)

  member this.RotateW(a) =
    wv.Rotate(a)
    vw.Rotate(-a, Drawing2D.MatrixOrder.Append)

  member this.RotateV(a) =
    vw.Rotate(a)
    wv.Rotate(-a, Drawing2D.MatrixOrder.Append)

  member this.TranslateV(tx, ty) =
    vw.Translate(tx, ty)
    wv.Translate(-tx, -ty, Drawing2D.MatrixOrder.Append)

  member this.ScaleV(sx, sy) =
    vw.Scale(sx, sy)
    wv.Scale(1.f / sx, 1.f / sy, Drawing2D.MatrixOrder.Append)
  
  member this.TransformPointV(p:PointF) =
    let a = [| p |]
    vw.TransformPoints(a)
    a.[0]

  member this.TransformPointW(p:PointF) =
    let a = [| p |]
    wv.TransformPoints(a)
    a.[0]
  
  member this.WV with get() = wv

// Control
type LWCControl() =
  let wv = WVMatrix()
  let mutable pos = PointF()
  let mutable size = SizeF(0.f, 0.f)

  let mutable parent : UserControl option = None
  
  member this.WV with get() = wv

  member this.Parent
    with get() = parent
    and set(v) = parent <- v

  abstract OnPaint : PaintEventArgs -> unit
  default this.OnPaint(e) = ()

  abstract OnMouseDown : MouseEventArgs -> unit
  default this.OnMouseDown(e) = ()

  abstract OnMouseUp : MouseEventArgs -> unit
  default this.OnMouseUp(e) = ()

  abstract OnMouseMove : MouseEventArgs -> unit
  default this.OnMouseMove(e) = ()

  member this.Invalidate() =
    match parent with
    | Some p -> p.Invalidate()
    | None -> ()

  member this.HitTest(p:Point) =
    let pt = wv.TransformPointV(PointF(single p.X, single p.Y))
    let boundingbox = RectangleF(0.f, 0.f, size.Width, size.Height)
    boundingbox.Contains(pt)

  member this.Size
    with get() = size
    and set(v) =
      size <- v
      this.Invalidate()

  member this.Position
    with get() = pos
    and set(v) =
      wv.TranslateV(pos.X, pos.Y)
      pos <- v
      wv.TranslateV(-pos.X, -pos.Y)
      this.Invalidate()
  
  member this.PositionInt with get() = Point(int pos.X, int pos.Y)
  member this.SizeInt with get() = Size(int size.Width, int size.Height)
  member this.Top = pos.Y
  member this.Left = pos.X
  member this.Width = size.Width
  member this.Height = size.Height
