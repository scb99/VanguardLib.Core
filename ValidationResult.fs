namespace VanguardLib

open System.Collections.Immutable
open System.Collections.Generic

type ValidationResult<'T> =
    | Success of Value: 'T
    | Failure of Errors: ImmutableList<string>
    
    static member Ok(value: 'T) = Success value
    static member Fail(error: string) = Failure (ImmutableList.Create(error))
    static member Fail(errors: IEnumerable<string>) = Failure (ImmutableList.CreateRange(errors))