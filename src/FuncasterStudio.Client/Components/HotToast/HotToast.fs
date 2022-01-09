namespace FuncasterStudio.Client.Components.HotToast

type ToastPosition =
    | TopLeft
    | TopCenter
    | TopRight
    | BottomLeft
    | BottomCenter
    | BottomRight

module ToastPosition =
    let value = function
        | TopLeft -> "top-left"
        | TopCenter -> "top-center"
        | TopRight -> "top-right"
        | BottomLeft -> "bottom-left"
        | BottomCenter -> "bottom-center"
        | BottomRight -> "bottom-right"