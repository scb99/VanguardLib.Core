namespace VanguardLib

open System
open System.Collections.Immutable

/// Enterprise validator implementation with multi-parameter applicative engines.
type Validator =

    /// Evaluates a single data check rule and converts it to a standard validation result
    static member Check<'T>(value: 'T, predicate: Func<'T, bool>, errorMessage: string) : ValidationResult<'T> =
        if predicate.Invoke(value) then 
            ValidationResult<'T>.Ok(value)
        else 
            ValidationResult<'T>.Fail<'T>(errorMessage)

    /// The Applicative engine: merges 6 individual check parameters into a final success constructor
    static member Combine<'T1, 'T2, 'T3, 'T4, 'T5, 'T6, 'TResult>
        (
            r1: ValidationResult<'T1>,
            r2: ValidationResult<'T2>,
            r3: ValidationResult<'T3>,
            r4: ValidationResult<'T4>,
            r5: ValidationResult<'T5>,
            r6: ValidationResult<'T6>,
            createSuccessInstance: Func<'T1, 'T2, 'T3, 'T4, 'T5, 'T6, 'TResult>
        ) : ValidationResult<'TResult> =

        match r1, r2, r3, r4, r5, r6 with
        | Success s1, Success s2, Success s3, Success s4, Success s5, Success s6 ->
            let instance = createSuccessInstance.Invoke(s1, s2, s3, s4, s5, s6)
            ValidationResult<'TResult>.Ok(instance)
        | _ ->
            let errorBuilder = ImmutableList.CreateBuilder<string>()
            
            // Local helper function to extract errors from any failure cases
            let collectErrors result =
                match result with
                | Failure errors -> errorBuilder.AddRange(errors)
                | _ -> ()

            collectErrors r1
            collectErrors r2
            collectErrors r3
            collectErrors r4
            collectErrors r5
            collectErrors r6

            ValidationResult<'TResult>.Fail(errorBuilder.ToImmutable())

    /// A clean 14-parameter applicative merger for complex data structures like Transaction
    static member Combine<'T1, 'T2, 'T3, 'T4, 'T5, 'T6, 'T7, 'T8, 'T9, 'T10, 'T11, 'T12, 'T13, 'T14, 'TResult>
        (
            r1: ValidationResult<'T1>, r2: ValidationResult<'T2>, r3: ValidationResult<'T3>, r4: ValidationResult<'T4>,
            r5: ValidationResult<'T5>, r6: ValidationResult<'T6>, r7: ValidationResult<'T7>, r8: ValidationResult<'T8>,
            r9: ValidationResult<'T9>, r10: ValidationResult<'T10>, r11: ValidationResult<'T11>, r12: ValidationResult<'T12>,
            r13: ValidationResult<'T13>, r14: ValidationResult<'T14>,
            createSuccessInstance: Func<'T1, 'T2, 'T3, 'T4, 'T5, 'T6, 'T7, 'T8, 'T9, 'T10, 'T11, 'T12, 'T13, 'T14, 'TResult>
        ) : ValidationResult<'TResult> =

        match r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14 with
        | Success s1,  Success s2,  Success s3,  Success s4,  Success s5,  Success s6,  Success s7, 
          Success s8,  Success s9,  Success s10, Success s11, Success s12, Success s13, Success s14 ->
            let instance = createSuccessInstance.Invoke(s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14)
            ValidationResult<'TResult>.Ok(instance)
        | _ ->
            let errorBuilder = ImmutableList.CreateBuilder<string>()
            
            let collectErrors result =
                match result with
                | Failure errors -> errorBuilder.AddRange(errors)
                | _ -> ()

            collectErrors r1;  collectErrors r2;  collectErrors r3;  collectErrors r4
            collectErrors r5;  collectErrors r6;  collectErrors r7;  collectErrors r8
            collectErrors r9;  collectErrors r10; collectErrors r11; collectErrors r12
            collectErrors r13; collectErrors r14

            ValidationResult<'TResult>.Fail(errorBuilder.ToImmutable())