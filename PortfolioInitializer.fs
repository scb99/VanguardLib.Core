namespace VanguardLib

open System
open System.Collections.Generic

module PortfolioInitializer =

    // Helper function to clone an existing IReadOnlyDictionary into a mutable SortedDictionary
    let private cloneToSortedDictionary (source: IReadOnlyDictionary<string, Investment>) : SortedDictionary<string, Investment> =
        let dict = SortedDictionary<string, Investment>()
        for kvp in source do
            dict.Add(kvp.Key, kvp.Value)
        dict

    // Internal engine using standard .NET tuple-style argument layout
    let private buildInternal (
        existingInvestments: IReadOnlyDictionary<string, Investment>,
        transactionHistory: IReadOnlyDictionary<string, List<Transaction>>,
        keySelector: Transaction -> string) : SortedDictionary<string, Investment> =

        // Functional Boundary Guard: Check for structural null states safely
        if box existingInvestments = null || box transactionHistory = null || box keySelector = null then
            if box existingInvestments <> null then 
                cloneToSortedDictionary existingInvestments
            else 
                SortedDictionary<string, Investment>()
        else
            // 1. Create a lookup of symbols that are ALREADY tracked (Corrected constructor syntax)
            let existingSymbols = 
                let symbolsSeq = 
                    existingInvestments.Values
                    |> Seq.filter (fun inv -> box inv <> null && not (String.IsNullOrEmpty(inv.Symbol)))
                    |> Seq.map (fun inv -> inv.Symbol)
                HashSet<string>(symbolsSeq, StringComparer.OrdinalIgnoreCase)

            // 2. Pure Pipeline: Stream, flatten, filter, group, map, and filter validation results
            let synthesizedInvestments = 
                transactionHistory.Values
                |> Seq.filter (fun list -> box list <> null)
                |> Seq.collect id // Flattens sequence safely
                |> Seq.filter (fun tx -> box tx <> null && not (String.IsNullOrEmpty(tx.Symbol)))
                |> Seq.filter (fun tx -> not (existingSymbols.Contains(tx.Symbol)))
                |> Seq.groupBy (fun tx -> tx.Symbol)
                |> Seq.map (fun (_, group) -> Seq.head group)
                |> Seq.map (fun tx -> 
                    let fallbackName = if isNull tx.InvestmentName then tx.Symbol.ToUpperInvariant() else tx.InvestmentName
                    let key = keySelector tx
                    let result = Investment.TryCreate("0", fallbackName, tx.Symbol, 0.0M, 0.0M, 0.0M)
                    (key, result)
                )
                // Seq.choose filters out Failures and unwraps Success values simultaneously
                |> Seq.choose (fun (key, result) -> 
                    match result with
                    | Success value -> Some(key, value)
                    | Failure _     -> None
                )

            // 3. Construct a fresh dictionary combining the seed items and new investments
            let outputDictionary = cloneToSortedDictionary existingInvestments

            for key, value in synthesizedInvestments do
                outputDictionary.TryAdd(key, value) |> ignore

            outputDictionary

    // Public APIs exposed predictably as static members to C# consumers
    let BuildInvestmentsBySymbol (
        existingInvestments: IReadOnlyDictionary<string, Investment>,
        transactionHistory: IReadOnlyDictionary<string, List<Transaction>>) : SortedDictionary<string, Investment> =
            buildInternal (existingInvestments, transactionHistory, (fun tx -> tx.Symbol.ToUpperInvariant()))

    let BuildInvestmentsByName (
        existingInvestments: IReadOnlyDictionary<string, Investment>,
        transactionHistory: IReadOnlyDictionary<string, List<Transaction>>) : SortedDictionary<string, Investment> =
            buildInternal (existingInvestments, transactionHistory, (fun tx -> 
                if isNull tx.InvestmentName then tx.Symbol.ToUpperInvariant() else tx.InvestmentName))
