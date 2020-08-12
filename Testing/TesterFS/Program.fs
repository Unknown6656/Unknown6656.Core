open System
open System.Text



type ID = string
type LAMBDA =
    | ABS of ID * LAMBDA 
    | APP of LAMBDA * LAMBDA
    | VAR of ID

    member x.Rename a b =
        match x with
        | VAR i when i = a -> VAR b
        | ABS (i, λ) when i = a -> ABS (b, λ.Rename a b)
        | ABS (i, λ) -> ABS (i, λ.Rename a b)
        | APP (l, r) -> (l.Rename a b, r.Rename a b) |> APP
        | _ -> x
    member x.Substitute a e = 
        match x with
        | VAR i when i = a -> e
        | ABS (i, λ) when i = a -> λ.Substitute a e
        | ABS (i, λ) -> ABS (i, λ.Substitute a e)
        | APP (l, r) -> (l.Substitute a e, r.Substitute a e) |> APP
        | _ -> x

    member x.Reduce() =
        match x with
        | APP(ABS(i, e), t) -> e.Substitute i t
        | _ -> x

    override x.ToString() = function
                            | VAR v -> v
                            | ABS(f, t) -> sprintf "λ%s.%s" f <| t.ToString()
                            | APP(f, t) -> sprintf "(%s) (%s)" <| f.ToString() <| t.ToString()
                           <| x


[<EntryPoint>]
let main _ =
    Console.OutputEncoding <- Encoding.Unicode
                           
    let f1 = ABS("x", ABS("y", APP(VAR "y", VAR "x")))
    let f2 = f1.Reduce()
                           
    printf "%s\n\n%s\n\n" <| f1.ToString() <| f2.ToString()
    0
