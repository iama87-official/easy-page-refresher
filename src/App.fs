module App

open Fable.Core
open Browser.Dom
open Feliz
open Feliz.Bulma
open Feliz.Delay
open System



type Model = { counter: int }

[<Emit("($0 ? true : false)")>]
let valid x : bool = jsNative

let tryInsertFirst (x: Browser.Types.Node, y: Browser.Types.Node) : bool =
    if valid x then
        if valid x.firstChild then
            ignore (x.insertBefore (y, x.firstChild))
        else
            ignore (x.appendChild (y))
        true
    else
        false

let includeCSS cssFile =
    let link = document.createElement "link"
    link.setAttribute ("rel", "stylesheet")
    link.setAttribute ("type", "text/css")
    link.setAttribute ("href", cssFile)
    document.head.appendChild(link)


let download fileName fileContent =
    let anchor = document.createElement "a"

    let encodedContent =
        fileContent
        |> sprintf "data:text/plain;charset=utf-8,%s"
        |> Fable.Core.JS.encodeURI

    anchor.setAttribute ("href", encodedContent)
    anchor.setAttribute ("download", fileName)
    anchor.click ()


let getCurrentTime() = DateTimeOffset.Now.ToUnixTimeMilliseconds()
let globalRNG = Random()
let makeAWaitTime() =
    let period = globalRNG.NextDouble() * float 5 + 30.0
    (int64) (period * 1000.0)

type GState =
    { mutable lastTimeInMilliseconds : int64
      mutable currentTimeToWaitInMilliseconds : int64
      mutable currentCount : int
      mutable pageUrl: string
      mutable isActivated: bool
    }


let IsActivateRef = ref false
let intervalIds = ResizeArray<int>()

let initState =
    {
        lastTimeInMilliseconds = getCurrentTime();
        currentTimeToWaitInMilliseconds = makeAWaitTime();
        currentCount = 0;
        pageUrl = window.location.href;
        isActivated = false
    }


let GlobalStateRef = ref initState
[<ReactComponent>]
let Service() =
    let (_st, setGlobalState) = React.useState (initState)
    GlobalStateRef.Value <- _st
    let periodicalCheck() =
        let state = GlobalStateRef.Value
        let t = getCurrentTime()
        if document.location.href <> state.pageUrl then
            ()
        else
        if state.isActivated then
            console.log (sprintf "%A" (t, state.lastTimeInMilliseconds, state.currentTimeToWaitInMilliseconds))
            if (t - state.lastTimeInMilliseconds > state.currentTimeToWaitInMilliseconds) then
                let iframe: Browser.Types.HTMLIFrameElement  =  downcast (document.getElementById "frame-to-refresh")
                if valid iframe then
                    iframe.contentWindow.location.reload(true)
                setGlobalState { 
                    state with
                        currentCount = state.currentCount + 1
                        lastTimeInMilliseconds = t
                        currentTimeToWaitInMilliseconds = makeAWaitTime()
                }
            else
                ()
    Html.div [
        Bulma.button.button [
            yield Bulma.button.isRounded
            yield Bulma.button.isMedium
            yield prop.style [style.zIndex 99999]
            if _st.isActivated || _st.pageUrl <> document.location.href then
                yield prop.text (sprintf "自动刷新第%d次 (取消)" (_st.currentCount))
                yield prop.onClick (fun _ ->
                    for each in intervalIds do
                        window.clearInterval ((float) each)
                    intervalIds.Clear ()
                    setGlobalState {_st with isActivated = false }
                )    
            else
                yield prop.text (sprintf "启动自动刷新")
                yield prop.onClick (fun _ ->
                    setGlobalState {
                        _st with
                            currentCount = _st.currentCount + 1
                            isActivated = true
                            currentTimeToWaitInMilliseconds = makeAWaitTime()
                            lastTimeInMilliseconds = getCurrentTime()
                            pageUrl = document.location.href
                    }
                    window.setInterval(periodicalCheck, 300, [||]) |> int |> intervalIds.Add
                )
        ]
        
        Bulma.card [
            Bulma.cardContent [
                Html.iframe [ prop.src _st.pageUrl; prop.id "frame-to-refresh"; prop.style [style.zIndex 99999] ]  
            ]
        ]
    ]


let _ =
    let appDiv = document.createElement ("div")
    appDiv.setAttribute ("id", "elm-root")
    ignore (includeCSS "https://cdn.bootcdn.net/ajax/libs/bulma/0.9.3/css/bulma.css")
    if
        tryInsertFirst (document.body, appDiv)
        || tryInsertFirst (document.head, appDiv)
    then
        ReactDOM.render (Service(), document.getElementById "elm-root")
