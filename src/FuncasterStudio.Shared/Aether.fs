namespace Aether

type NamedLens<'a,'b> = {
    Name : string
    Lens : Lens<'a,'b>
}

module NamedLens =
    let create name getter setter = { Name = name; Lens = (getter,setter) }

module Compose =
    let namedLens (nl:NamedLens<'a,'b>) (iso:Isomorphism<'b,'c>) = { Name = nl.Name; Lens = iso |> Compose.lens nl.Lens }