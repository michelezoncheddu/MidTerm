#load "Components.fsx"

open Components

open System.Windows.Forms
open System.Drawing

let lwcc = new LWCContainer(Dock=DockStyle.Fill)
let f = new Form(Text="MidTerm", Size=Size(1200, 600), StartPosition=FormStartPosition.CenterScreen)

f.Controls.Add(lwcc)
f.Show()
