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
  let wv = WVMatrix() // matrice mondo-vista
  let mutable pos = PointF() // angolo in alto a sinistra
  let mutable size = SizeF(0.f, 0.f)

  let mutable parent : LWCContainer option = None
  
  member this.WV with get() = wv

  member this.Parent
    with get() = parent
    and set(v) = parent <- v

  abstract OnPaint : PaintEventArgs -> unit // return unit perche' le callback non resituiscono nulla
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
      wv.TranslateV(pos.X, pos.Y) // traslo la vista
      pos <- v // applico le trasformazioni
      wv.TranslateV(-pos.X, -pos.Y) // ripristino la vista
      this.Invalidate()
  
  member this.PositionInt with get() = Point(int pos.X, int pos.Y)
  member this.SizeInt with get() = Size(int size.Width, int size.Height)
  member this.Top = pos.Y
  member this.Left = pos.X
  member this.Width = size.Width
  member this.Height = size.Height

// Container
and LWCContainer() as this =
  inherit UserControl()
  
  let arrowSize = SizeF(20.f, 20.f)
  let buttonSize = SizeF(70.f, 30.f)
  let planetSize = SizeF(120.f, 120.f)

  let up = LWButton(Position=PointF(30.f, 10.f), Size=arrowSize, Op="up")
  let down = LWButton(Position=PointF(30.f, 50.f), Size=arrowSize, Op="down")
  let left = LWButton(Position=PointF(10.f, 30.f), Size=arrowSize, Op="left")
  let right = LWButton(Position=PointF(50.f, 30.f), Size=arrowSize, Op="right")

  let create = LWButton(Position=PointF(20.f, 80.f), Size=buttonSize, Op="create planet")

  let rotate = LWButton(Position=PointF(20.f, 120.f), Size=buttonSize, Op="rotate")
  let zoomUp = LWButton(Position=PointF(20.f, 160.f), Size=buttonSize, Op="zoom +")
  let zoomDown = LWButton(Position=PointF(20.f, 200.f), Size=buttonSize, Op="zoom -")


  let ship = LWShip(Position=PointF(100.f, 100.f), Size=SizeF(60.f, 83.f))

  let mutable drag = None
  let controls = System.Collections.ObjectModel.ObservableCollection<LWCControl>()

  do
    controls.CollectionChanged.Add(fun e ->
      for i in e.NewItems do
        (i :?> LWCControl).Parent <- Some(this)
    )
    this.SetStyle(ControlStyles.AllPaintingInWmPaint ||| ControlStyles.OptimizedDoubleBuffer, true)
    controls.Add(ship)
    controls.Add(up)
    controls.Add(down)
    controls.Add(left)
    controls.Add(right)
    controls.Add(rotate)
    controls.Add(zoomUp)
    controls.Add(zoomDown)
    controls.Add(create)

  member this.LWControls with get() = controls

  member this.MoveView(direction) =
    controls |> Seq.iter(fun c ->
      match c with
      | :? LWButton -> ()
      | _ ->
        match direction with
        | "up" -> c.WV.TranslateV(0.f, 10.f)
        | "down" -> c.WV.TranslateV(0.f, -10.f)
        | "left" -> c.WV.TranslateV(10.f, 0.f)
        | "right" -> c.WV.TranslateV(-10.f, 0.f)
        | _ -> ()
    )

  member this.RotateView(sign) =
    controls |> Seq.iter(fun c ->
      match c with
      | :? LWButton -> ()
      | _ ->
        let client = this.ClientSize
        c.WV.TranslateV(client.Width / 2 |> single, client.Height / 2 |> single)
        if (sign = "clockwise") then
          c.WV.RotateV(-5.f)
        else
          c.WV.RotateV(5.f)
        c.WV.TranslateV(-client.Width / 2 |> single, -client.Height / 2 |> single)
    )
  
  member this.ZoomView(sign) =
    controls |> Seq.iter(fun c ->
      match c with
      | :? LWButton -> ()
      | _ ->
        let cx, cy = this.ClientSize.Width / 2 |> single, this.ClientSize.Height / 2 |> single
        // po è la differenza tra il centro e il vertice corrente del controllo
        let po = PointF(cx, cy) |> c.WV.TransformPointV
        if (sign = "up") then
          c.WV.ScaleW(1.1f, 1.1f)
        else
          c.WV.ScaleW(1.f / 1.1f, 1.f / 1.1f)
        // pn è la differenza tra il centro e il nuovo vertice del controllo (zoommato)
        let pn = PointF(cx, cy) |> c.WV.TransformPointV
        c.WV.TranslateW(pn.X - po.X, pn.Y - po.Y)
    )

  member this.CreatePlanet() =
    let dialog = new OpenFileDialog()
    dialog.Filter <- "|*.bmp;*.jpg;*.gif;*.png"
    if dialog.ShowDialog() = DialogResult.OK then
      let image : Bitmap = new Bitmap(dialog.FileName)
      controls.Add(LWPlanet(Position=PointF(20.f, 20.f), Size=planetSize, Image=image))

  override this.OnMouseDown(e) =
    let c = controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location))
    match c with
    | Some c ->
      match c with
      | :? LWButton ->
        let p = c.WV.TransformPointV(PointF(single e.X, single e.Y))
        let evt = MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
        c.OnMouseDown(evt)
        this.Invalidate()
      | _ ->
        let dx, dy = e.X - int c.Left, e.Y - int c.Top
        drag <- Some(c, dx, dy)
        let p = c.WV.TransformPointV(PointF(single e.X, single e.Y))
        let evt = MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
        c.OnMouseDown(evt)
        this.Invalidate()
    | None -> ()
    let i = controls.IndexOf(ship)
    controls.Move(i, controls.Count - 1)

  override this.OnMouseMove(e) =
    let c = controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location))
    match c with
    | Some c ->
      let p = c.WV.TransformPointV(PointF(single e.X, single e.Y))
      let evt = MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
      c.OnMouseMove(evt)
      this.Invalidate()      
    | None -> ()
    match drag with
    | Some(c, dx, dy) ->
      c.Position <- PointF(single (e.X - dx), single (e.Y - dy))
      this.Invalidate()
    | None -> ()
    
  override this.OnMouseUp(e) =
    drag <- None
    let c = controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location))
    match c with
    | Some c ->
      let p = c.WV.TransformPointV(PointF(single e.X, single e.Y))
      let evt = MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
      c.OnMouseUp(evt)
      this.Invalidate()
    | None -> ()

  override this.OnPaint(e) =
    controls |> Seq.iter(fun c ->
      let bkg = e.Graphics.Save()
      let evt = new PaintEventArgs(e.Graphics, Rectangle(c.PositionInt, c.SizeInt))
      e.Graphics.Transform <- c.WV.WV
      c.OnPaint(evt) // quello che faccio qua dentro e' trasparente al resto dei controlli
      e.Graphics.Restore(bkg)
    )

  override this.OnKeyDown(e) =
    match e.KeyCode with
    | Keys.W -> ship.WV.TranslateW(0.f, -10.f)
    | Keys.D ->
      let cx, cy = ship.Width / 2.f, ship.Height / 2.f
      ship.WV.TranslateW(cx, cy)
      ship.WV.RotateW(10.f)
      ship.WV.TranslateW(-cx, -cy)
    | Keys.A ->
      let cx, cy = ship.Width / 2.f, ship.Height / 2.f
      ship.WV.TranslateW(cx, cy)
      ship.WV.RotateW(-10.f)
      ship.WV.TranslateW(-cx, -cy)
    | _ -> ()
    this.Invalidate()

// Buttons
and LWButton() =
  inherit LWCControl()

  let bgcolor = Color.DarkGreen
  let mutable op = "none"
  let font = new Font("Calibri", 8.f)

  member this.Op
    with get() = op
    and set(v) = op <- v

  override this.OnPaint(e) =
    let g = e.Graphics
    g.SmoothingMode <- Drawing2D.SmoothingMode.AntiAlias
    let brush = new SolidBrush(bgcolor)
    match op with
    | "up" ->
      let triangle = [| Point(0, int this.Height); Point(int this.Width / 2, 0); Point(int this.Width, int this.Height) |]
      g.FillPolygon(Brushes.Black, triangle)
    | "down" ->
      let triangle = [| Point(0, 0); Point(int this.Width / 2, int this.Height); Point(int this.Width, 0) |]
      g.FillPolygon(Brushes.Black, triangle)
    | "left" ->
      let triangle = [| Point(0, int this.Height / 2); Point(int this.Width, int this.Height); Point(int this.Width, 0) |]
      g.FillPolygon(Brushes.Black, triangle)
    | "right" ->
      let triangle = [| Point(0, 0); Point(0, int this.Height); Point(int this.Width, int this.Height / 2) |]
      g.FillPolygon(Brushes.Black, triangle)
    | _ ->
      g.FillRectangle(brush, 0.f, 0.f, this.Width, this.Height)
      g.DrawString(op.ToUpper(), font, Brushes.White, 5.f, 5.f)

  override this.OnMouseDown(e) =
    match this.Parent with
    | Some parent ->
      match op with
      | "up" -> parent.MoveView("up")
      | "down" -> parent.MoveView("down")
      | "left" -> parent.MoveView("left")
      | "right" -> parent.MoveView("right")
      | "rotate" -> parent.RotateView("clockwise")
      | "zoom +" -> parent.ZoomView("up")
      | "zoom -" -> parent.ZoomView("down")
      | "create planet" -> parent.CreatePlanet()
      | _ -> printfn "%A not recognized" op
    | None -> ()

// Space ship
and LWShip() =
  inherit LWCControl()

  override this.OnPaint(e) =
    let g = e.Graphics
    g.SmoothingMode <- Drawing2D.SmoothingMode.AntiAlias
    let image = Image.FromFile("MidTerm/img/millennium-falcon.png")
    g.DrawImage(image, 0.f, 0.f, this.Width, this.Height)
    
// Planet
and LWPlanet() =
  inherit LWCControl()

  let mutable image = null

  member this.Image
    with set(v) = image <- v

  override this.OnPaint(e) =
    let g = e.Graphics
    g.SmoothingMode <- Drawing2D.SmoothingMode.AntiAlias
    g.DrawImage(image, 0.f, 0.f, this.Width, this.Height)
