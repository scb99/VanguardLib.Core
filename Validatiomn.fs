namespace VanguardLib

type ValidationResult<'T> =
    | Success of 'T
    | Failure of string list  // Standard F# immutable list for error accumulation

module Validation =

    /// Lift a raw predicate into a validation check using native F# functions
    let check (predicate: 'T -> bool) (errorMessage: string) (value: 'T) : ValidationResult<'T> =
        if predicate value then Success value else Failure [errorMessage]

    /// Map operator (<!>): Applies a constructor function to a validation result
    let map (f: 'T -> 'U) (result: ValidationResult<'T>) : ValidationResult<'U> =
        match result with
        | Success x -> Success (f x)
        | Failure errs -> Failure errs

    /// Apply operator (<*>): Merges functions and accumulates errors
    let apply (fResult: ValidationResult<'T -> 'U>) (xResult: ValidationResult<'T>) : ValidationResult<'U> =
        match fResult, xResult with
        | Success f, Success x -> Success (f x)
        | Failure errs1, Failure errs2 -> Failure (errs1 @ errs2) // Accumulate errors
        | Failure errs, _ -> Failure errs
        | _, Failure errs -> Failure errs

    // Define standard operator symbols for clean composition
    let (<!>) = map
    let (<*>) = apply
