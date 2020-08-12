module MathLibrary.Functions

open System.Net.Http.Headers



type Element =
    | Constant of float
    | Variable
    with
        member x.Derivative = match x with
                              | Constant _ -> 0.0
                              | Variable -> 1.0
                              |> Constant

type Expression =
    | Elementar of Element
    | Product of Expression * Expression
    | Division of Expression * Expression
    | Difference of Expression * Expression
    | Sum of Expression * Expression
    | Sine of Expression
    | Cosine of Expression
    | Tangent of Expression
    | Exponential of Expression
    | Logarithm of Expression
    with
        member x.Derivative = function
                              | Elementar e -> Elementar e.Derivative
                              | Product (e1, e2) -> Sum (
                                                        Product(e1, e2.Derivative),
                                                        Product(e1.Derivative, e2)
                                                    )
                              | Sine e -> Cosine e
                             <| x

type Function =
    {
        Expression : Expression
    }
    with
        member x.Derivative =
            {
                Expression = x.Expression.Derivative
            }


do
    ()