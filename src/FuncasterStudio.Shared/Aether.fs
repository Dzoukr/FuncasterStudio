namespace Aether

type NamedLens<'a,'b> = {
    Name : string
    Lens : Lens<'a,'b>
}

module NamedLens =
    let create name getter setter = { Name = name; Lens = (getter,setter) }

