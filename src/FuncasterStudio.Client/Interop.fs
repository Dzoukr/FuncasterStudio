module FuncasterStudio.Client.Interop

type Props = Map<string,obj>

module Props =
    let inline ofList (p:List<'a>) : Props =
        p
        |> List.map unbox<string * obj>
        |> Map.ofList

    let inline singleton (p:'a) : Props =
        p
        |> List.singleton
        |> ofList

    let inline get<'a> (name:string) (p:Props) = name |> p.TryFind |> Option.map (fun x -> x :?> 'a)
    let inline getDefault<'a> (name:string) (v:'a) (p:Props) = p |> get name |> Option.defaultValue v