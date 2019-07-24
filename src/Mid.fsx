#load "Components.fsx"

open System.Drawing
open System.Windows.Forms

open Components

let form = new Form(Text="MidTerm - Michele Zoncheddu", Size=Size(1200, 600), StartPosition=FormStartPosition.CenterScreen)
let container = new LWCContainer(Dock=DockStyle.Fill)
let background = Image.FromFile("MidTerm/img/background.jpg")
container.BackgroundImage <- background
let controls = container.Controls

let arrowSize = SizeF(20.f, 20.f)
let buttonSize = SizeF(110.f, 30.f)

let up = Button(Position=PointF(30.f, 10.f), Size=arrowSize, Op="up")
let down = Button(Position=PointF(30.f, 50.f), Size=arrowSize, Op="down")
let left = Button(Position=PointF(10.f, 30.f), Size=arrowSize, Op="left")
let right = Button(Position=PointF(50.f, 30.f), Size=arrowSize, Op="right")

let create = Button(Position=PointF(10.f, 80.f), Size=buttonSize, Op="create planet")
let rotateCW = Button(Position=PointF(10.f, 120.f), Size=buttonSize, Op="rotate cw")
let rotateCCW = Button(Position=PointF(10.f, 160.f), Size=buttonSize, Op="rotate ccw")
let zoomUp = Button(Position=PointF(10.f, 200.f), Size=buttonSize, Op="zoom +")
let zoomDown = Button(Position=PointF(10.f, 240.f), Size=buttonSize, Op="zoom -")
let zoomUpObject = Button(Position=PointF(10.f, 280.f), Size=buttonSize, Op="zoom + object")
let zoomDownObject = Button(Position=PointF(10.f, 320.f), Size=buttonSize, Op="zoom - object")

controls.Add(up)
controls.Add(down)
controls.Add(left)
controls.Add(right)
controls.Add(create)
controls.Add(rotateCW)
controls.Add(rotateCCW)
controls.Add(zoomUp)
controls.Add(zoomDown)
controls.Add(zoomUpObject)
controls.Add(zoomDownObject)

form.Controls.Add(container)
form.Show()
