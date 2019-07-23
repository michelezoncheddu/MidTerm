#load "LWC.fsx"

open LWC

open System.Windows.Forms
open System.Drawing

// Container
type LWCContainer() as this =
  inherit UserControl()
  
  let arrowSize = SizeF(20.f, 20.f)
  let buttonSize = SizeF(100.f, 30.f)
  let planetSize = SizeF(120.f, 120.f)

  let up = LWButton(Position=PointF(30.f, 10.f), Size=arrowSize, Op="up")
  let down = LWButton(Position=PointF(30.f, 50.f), Size=arrowSize, Op="down")
  let left = LWButton(Position=PointF(10.f, 30.f), Size=arrowSize, Op="left")
  let right = LWButton(Position=PointF(50.f, 30.f), Size=arrowSize, Op="right")

  let create = LWButton(Position=PointF(10.f, 80.f), Size=buttonSize, Op="create planet")
  let rotateCW = LWButton(Position=PointF(10.f, 120.f), Size=buttonSize, Op="rotate cw")
  let rotateCCW = LWButton(Position=PointF(10.f, 160.f), Size=buttonSize, Op="rotate ccw")
  let zoomUp = LWButton(Position=PointF(10.f, 200.f), Size=buttonSize, Op="zoom +")
  let zoomDown = LWButton(Position=PointF(10.f, 240.f), Size=buttonSize, Op="zoom -")

  let ship = LWShip(Position=PointF(100.f, 100.f), Size=SizeF(60.f, 83.f))

  let mutable drag = None
  let controls = System.Collections.ObjectModel.ObservableCollection<LWCControl>()
  let keys = ResizeArray<Keys>()

  do
    controls.CollectionChanged.Add(fun e ->
      for i in e.NewItems do
        (i :?> LWCControl).Parent <- Some(this :> UserControl)
    )
    this.SetStyle(ControlStyles.AllPaintingInWmPaint ||| ControlStyles.OptimizedDoubleBuffer, true)
    controls.Add(up)
    controls.Add(down)
    controls.Add(left)
    controls.Add(right)
    controls.Add(rotateCW)
    controls.Add(rotateCCW)
    controls.Add(zoomUp)
    controls.Add(zoomDown)
    controls.Add(create)
    controls.Add(ship)

  member this.MoveView(direction) =
    controls |> Seq.iter(fun c ->
      match c with
      | :? LWButton -> ()
      | _ ->
        match direction with
        | "up" -> c.WV.TranslateV(0.f, -10.f)
        | "down" -> c.WV.TranslateV(0.f, 10.f)
        | "left" -> c.WV.TranslateV(-10.f, 0.f)
        | "right" -> c.WV.TranslateV(10.f, 0.f)
        | _ -> ()
    )

  member this.RotateView(direction) =
    controls |> Seq.iter(fun c ->
      match c with
      | :? LWButton -> ()
      | _ ->
        let client = this.ClientSize
        c.WV.TranslateV(client.Width / 2 |> single, client.Height / 2 |> single)
        if (direction = "clockwise") then
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
        // po is the difference between the control center and the current control vertex 
        let po = PointF(cx, cy) |> c.WV.TransformPointV
        if (sign = "up") then
          c.WV.ScaleW(1.1f, 1.1f)
        else
          c.WV.ScaleW(1.f / 1.1f, 1.f / 1.1f)
        // pn is the difference between the control center and the new control vertex (scaled)
        let pn = PointF(cx, cy) |> c.WV.TransformPointV
        c.WV.TranslateW(pn.X - po.X, pn.Y - po.Y)
    )

  member this.CreatePlanet() =
    let dialog = new OpenFileDialog()
    dialog.Filter <- "|*.jpg;*.jpeg;*.gif;*.png"
    if dialog.ShowDialog() = DialogResult.OK then
      let image : Bitmap = new Bitmap(dialog.FileName)
      controls.Add(LWPlanet(
                    Position=PointF(
                              single this.Width / 2.f - planetSize.Width / 2.f,
                              single this.Height / 2.f - planetSize.Height / 2.f),
                    Size=planetSize,
                    Image=image)
                  )

  override this.OnMouseDown(e) =
    let c = controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location))
    match c with
    | Some c ->
      match c with
      | :? LWButton -> () // button not draggable
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
      c.OnPaint(evt)
      e.Graphics.Restore(bkg)
    )

  override this.OnKeyDown(e) =
    let keyCode = e.KeyCode
    if (not (keys.Contains(keyCode))) then
      keys.Add(keyCode)
    let cx, cy = ship.Width / 2.f, ship.Height / 2.f
    keys |> Seq.iter(fun c ->
      match c with
      | Keys.W ->
        ship.WV.TranslateW(0.f, -10.f)
      | Keys.D ->
        ship.WV.TranslateW(cx, cy)
        ship.WV.RotateW(10.f)
        ship.WV.TranslateW(-cx, -cy)
      | Keys.A ->
        ship.WV.TranslateW(cx, cy)
        ship.WV.RotateW(-10.f)
        ship.WV.TranslateW(-cx, -cy)
      | _ -> ()
    )
    this.Invalidate()

  override this.OnKeyUp(e) =
    let keyCode = e.KeyCode
    if (keys.Contains(keyCode)) then
      keys.Remove(keyCode) |> ignore

// Buttons
and LWButton() =
  inherit LWCControl()

  let bgcolor = Color.DarkGreen
  let mutable op = "none"
  let font = new Font("Consolas", 9.f)

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
      g.DrawString(op.ToUpper(), font, Brushes.White, 5.f, 7.f)

  override this.OnMouseDown(e) =
    match this.Parent with
    | Some p ->
      let parent = p :?> LWCContainer
      match op with
      | "up" -> parent.MoveView("up")
      | "down" -> parent.MoveView("down")
      | "left" -> parent.MoveView("left")
      | "right" -> parent.MoveView("right")
      | "rotate cw" -> parent.RotateView("clockwise")
      | "rotate ccw" -> parent.RotateView("counterclockwise")
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
