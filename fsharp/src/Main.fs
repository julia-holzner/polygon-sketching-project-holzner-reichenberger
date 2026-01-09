module App

open Feliz
open Elmish
open Elmish.React
open Elmish.HMR // Elmish.HMR needs to be the last open instruction in order to be able to shadow any supported API
 
   
let init,update,render = 
    PolygonDrawing.init, PolygonDrawing.update, PolygonDrawing.render
 
Program.mkProgram init update render
|> Program.withReactSynchronous "feliz-app"
|> Program.run  