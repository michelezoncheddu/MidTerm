#load "LWC.fsx"

open System.Drawing
open System.Windows.Forms

open LWC

// Container
type LWCContainer() as this =
  inherit UserControl()

  let controls = System.Collections.ObjectModel.ObservableCollection<LWCControl>()
  let pressedKeys = ResizeArray<Keys>()

  let mutable drag = None
  let mutable op = "none"

  let planetSize = SizeF(300.f, 300.f)

  let ship = Ship(Position=PointF(600.f - 45.f, 300.f - 90.f), Size=SizeF(90.f, 180.f))
  
  let timer = new Timer(Interval=15)
  let mutable acceleration = PointF(0.f, 0.f)
  let maxAcceleration = 7.f
  let mutable shipAngle = 90
  let mutable viewAngle = 0
  let mutable viewZoom = 1.f
  let mutable ticks = 0
  let maxTicks = 200

  do
    this.SetStyle(ControlStyles.AllPaintingInWmPaint ||| ControlStyles.OptimizedDoubleBuffer, true)
    controls.CollectionChanged.Add(fun e ->
      for i in e.NewItems do
        (i :?> LWCControl).Parent <- Some(this :> UserControl)
      let i = controls.IndexOf(ship)
      if (i <> controls.Count - 1) then
        controls.Move(i, controls.Count - 1)
    )
    controls.Add(ship)

    timer.Tick.Add(fun _ ->
      let rad = System.Math.PI * (float shipAngle - float viewAngle) / float 180

      ship.State <- if (pressedKeys.Contains(Keys.W)) then "moving" else "base"

      acceleration.X <-
        if (ship.State = "moving") then
          acceleration.X + (0.2f * cos(single(rad)))
        elif (acceleration.X > 0.f) then
          acceleration.X - (maxAcceleration / single maxTicks)
        else
          acceleration.X + (maxAcceleration / single maxTicks)

      acceleration.Y <-
        if (ship.State = "moving") then
          acceleration.Y + (0.2f * sin(single(rad)))
        elif (acceleration.Y > 0.f) then
          acceleration.Y - (maxAcceleration / single maxTicks)
        else
          acceleration.Y + (maxAcceleration / single maxTicks)

      if (acceleration.X > maxAcceleration) then
        acceleration.X <- maxAcceleration
      elif (acceleration.X < -maxAcceleration) then
        acceleration.X <- -maxAcceleration
      elif (abs acceleration.X) <= maxAcceleration / single maxTicks then
        acceleration.X <- 0.f

      if (acceleration.Y > maxAcceleration) then
        acceleration.Y <- maxAcceleration
      elif (acceleration.Y < -maxAcceleration) then
        acceleration.Y <- -maxAcceleration
      elif (abs acceleration.Y) <= maxAcceleration / single maxTicks then
        acceleration.Y <- 0.f

      ship.Position <- PointF(ship.Position.X + acceleration.X * viewZoom, ship.Position.Y - acceleration.Y * viewZoom)

      let cx, cy = ship.Width / 2.f, ship.Height / 2.f
      pressedKeys |> Seq.iter(fun c ->
        match c with
        | Keys.D ->
          ship.WV.TranslateW(cx, cy)
          ship.WV.RotateW(6.f)
          shipAngle <- shipAngle - 6
          ship.WV.TranslateW(-cx, -cy)
        | Keys.A ->
          ship.WV.TranslateW(cx, cy)
          ship.WV.RotateW(-6.f)
          shipAngle <- shipAngle + 6
          ship.WV.TranslateW(-cx, -cy)
        | _ -> ()
      )
      if (pressedKeys.Count > 0) then
        ticks <- 0
      else
        ticks <- ticks + 1
        if (ticks = maxTicks) then
          timer.Stop()
          acceleration.X <- 0.f
          acceleration.Y <- 0.f
      ship.Invalidate()
    )
  
  member this.Controls with get() = controls

  member this.Op
    with get() = op
    and set(v) = op <- v

  member this.MoveView(direction) =
    controls |> Seq.iter(fun c ->
      match c with
      | :? Button -> ()
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
      | :? Button -> ()
      | _ ->
        c.WV.TranslateV(this.Width / 2 |> single, this.Height / 2 |> single)
        if (direction = "clockwise") then
          c.WV.RotateV(-5.f)
        else
          c.WV.RotateV(5.f)
        c.WV.TranslateV(-this.Width / 2 |> single, -this.Height / 2 |> single)
    )
    viewAngle <- if (direction = "clockwise") then viewAngle + 5 else viewAngle - 5
  
  member this.ZoomView(sign) =
    controls |> Seq.iter(fun c ->
      match c with
      | :? Button -> ()
      | _ ->
        let cx, cy = this.Width / 2 |> single, this.Height / 2 |> single
        // po is the difference between the control center and the current control vertex 
        let po = PointF(cx, cy) |> c.WV.TransformPointV
        if (sign = "+") then
          c.WV.ScaleW(1.1f, 1.1f)
        else
          c.WV.ScaleW(1.f / 1.1f, 1.f / 1.1f)
        // pn is the difference between the control center and the new control vertex (scaled)
        let pn = PointF(cx, cy) |> c.WV.TransformPointV
        c.WV.TranslateW(pn.X - po.X, pn.Y - po.Y)
    )
    viewZoom <- if (sign = "+") then viewZoom * 1.1f else viewZoom / 1.1f

  member this.CreatePlanet() =
    let dialog = new OpenFileDialog()
    let random = System.Random()
    dialog.Filter <- "|*.jpg;*.jpeg;*.gif;*.png"
    if dialog.ShowDialog() = DialogResult.OK then
      let image = new Bitmap(dialog.FileName)
      controls.Add(Planet(
                    Position=PointF(
                              planetSize.Width / 2.f + single (random.Next(this.Width - int planetSize.Width)),
                              planetSize.Height / 2.f + single (random.Next(this.Height - int planetSize.Height))),
                    Size=SizeF(planetSize.Width * viewZoom, planetSize.Height * viewZoom),
                    Shape="circle",
                    Image=image)
                  )

  override this.OnMouseDown(e) =
    let c = controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location))
    match c with
    | Some c ->
      match c with
      | :? Button -> () // button not draggable
      | _ ->
        if (op = "zoom + object") then
          c.WV.ScaleW(1.1f, 1.1f)
        elif (op = "zoom - object") then
          c.WV.ScaleW(1.f / 1.1f, 1.f / 1.1f)
        else
          let dx, dy = e.X - int c.Left, e.Y - int c.Top
          drag <- Some(c, dx, dy)
      let p = c.WV.TransformPointV(PointF(single e.X, single e.Y))
      let evt = MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
      c.OnMouseDown(evt)
      this.Invalidate()
    | None -> ()

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
      e.Graphics.SmoothingMode <- Drawing2D.SmoothingMode.AntiAlias
      c.OnPaint(evt)
      e.Graphics.Restore(bkg)
    )

  member this.HitTestPlanet(c : LWCControl, point : PointF) =
    match c with
    | :? Planet -> c.HitTest(Point(int point.X, int point.Y))
    | _ -> false

  override this.OnKeyDown(e) =
    let keyCode = e.KeyCode
    if (keyCode = Keys.Space) then
      let shipCenter = PointF(ship.Width / 2.f, ship.Height / 2.f) |> ship.WV.TransformPointW
      let c = controls |> Seq.tryFindBack(fun c -> this.HitTestPlanet(c, shipCenter))
      match c with
      | Some _ ->
        //let planetCenter = PointF(planet.Position.X + planet.Width / 2.f, planet.Position.Y + planet.Height / 2.f)
        //ship.Position <- PointF(single planetCenter.X - ship.Width / 2.f, single planetCenter.Y - ship.Height / 2.f)
        ship.State <- "landed"
        acceleration <- PointF(0.f, 0.f)
        timer.Stop()
        ship.Invalidate()
      | None -> ()
    elif not (pressedKeys.Contains(keyCode)) then
      pressedKeys.Add(keyCode)
      timer.Start()

  override this.OnKeyUp(e) =
    pressedKeys.Remove(e.KeyCode) |> ignore

// Button
and Button() =
  inherit LWCControl()

  let mutable op = "none"
  let font = new Font("Consolas", 9.f)
  let buttonColor = new SolidBrush(Color.DarkGreen)
  let triangleColor = new SolidBrush(Color.FromArgb(140, 140, 140))

  member this.Op
    with get() = op
    and set(v) = op <- v

  override this.OnPaint(e) =
    let g = e.Graphics
    
    match op with
    | "up" ->
      let triangle = [| Point(0, int this.Height); Point(int this.Width / 2, 0); Point(int this.Width, int this.Height) |]
      g.FillPolygon(triangleColor, triangle)
    | "down" ->
      let triangle = [| Point(0, 0); Point(int this.Width / 2, int this.Height); Point(int this.Width, 0) |]
      g.FillPolygon(triangleColor, triangle)
    | "left" ->
      let triangle = [| Point(0, int this.Height / 2); Point(int this.Width, int this.Height); Point(int this.Width, 0) |]
      g.FillPolygon(triangleColor, triangle)
    | "right" ->
      let triangle = [| Point(0, 0); Point(0, int this.Height); Point(int this.Width, int this.Height / 2) |]
      g.FillPolygon(triangleColor, triangle)
    | _ ->
      g.FillRectangle(buttonColor, 0.f, 0.f, this.Width, this.Height)
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
      | "create planet" -> parent.CreatePlanet()
      | "rotate cw" -> parent.RotateView("clockwise")
      | "rotate ccw" -> parent.RotateView("counterclockwise")
      | "zoom +" -> parent.ZoomView("+")
      | "zoom -" -> parent.ZoomView("-")
      | "zoom + object" -> parent.Op <- if (parent.Op = "zoom + object") then "none" else "zoom + object"
      | "zoom - object" -> parent.Op <- if (parent.Op = "zoom - object") then "none" else "zoom - object"
      | _ -> printfn "%A not recognized" op
    | None -> ()

// Space ship
and Ship() =
  inherit LWCControl()

  let baseImage = Image.FromFile("MidTerm/img/millennium_falcon.png")
  let movingImage = Image.FromFile("MidTerm/img/millennium_falcon_moving.png")
  let landedImage = Image.FromFile("MidTerm/img/millennium_falcon_landed.png")

  let mutable state = "base"

  member this.State
    with get() = state
    and set(v) = state <- v

  override this.OnPaint(e) =
    let g = e.Graphics
    match state with
    | "base" ->  g.DrawImage(baseImage, 0.f, 0.f, this.Width, this.Height)
    | "moving" -> g.DrawImage(movingImage, 0.f, 0.f, this.Width, this.Height)
    | "landed" -> g.DrawImage(landedImage, 0.f, 0.f, this.Width, this.Height)
    | _ -> ()
    
// Planet
and Planet() =
  inherit LWCControl()

  let mutable image = null

  member this.Image
    with set(v) = image <- v

  override this.OnPaint(e) =
    let g = e.Graphics
    g.DrawImage(image, 0.f, 0.f, this.Width, this.Height)
