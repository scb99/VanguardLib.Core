//namespace VanguardLib

//open System
//open System.Collections.Generic
//open VanguardLib.Extensions

//module PortfolioInitializer =

//    /// Helper function to clone an existing IReadOnlyDictionary into a mutable SortedDictionary
//    let private cloneToSortedDictionary (source: IReadOnlyDictionary<string, Investment>) : SortedDictionary<string, Investment> =
//        let dict = SortedDictionary<string, Investment>()
//        if isNull source then 
//            dict 
//        else
//            for kvp in source do
//                dict.Add(kvp.Key, kvp.Value)
//            dict

//    /// Internal engine using standard .NET argument layouts
//    let private buildInternal (
//        existingInvestments: IReadOnlyDictionary<string, Investment>,
//        transactionHistory: IReadOnlyDictionary<string, List<Transaction>>,
//        keySelector: Transaction -> string) : SortedDictionary<string, Investment> =

//        if isNull transactionHistory then
//            // Force case-insensitive and clean structure onto fallback clone
//            let dict = SortedDictionary<string, Investment>(StringComparer.OrdinalIgnoreCase)
//            if not (isNull existingInvestments) then
//                for kvp in existingInvestments do
//                    dict.TryAdd(kvp.Key.Trim(), kvp.Value) |> ignore
//            dict
//        else
//            let existingKeys = 
//                let hs = HashSet<string>(StringComparer.OrdinalIgnoreCase)
//                if not (isNull existingInvestments) then
//                    for key in existingInvestments.Keys do
//                        if not (String.IsNullOrWhiteSpace key) then
//                            hs.Add(String.cleanWhitespace key) |> ignore // CALL EXTENSION HERE
//                hs

//            // Pipeline: Stream, flatten, filter, and choose validation results
//            let synthesizedInvestments = 
//                transactionHistory.Values
//                |> Seq.filter (isNull >> not)
//                |> Seq.collect id 
//                |> Seq.filter (fun tx -> not (String.IsNullOrWhiteSpace tx.Symbol))
//                |> Seq.map (fun tx -> 
//                    let rawKey = keySelector tx
//                    let key = String.cleanWhitespace rawKey
                    
//                    let fallbackName = 
//                        if String.IsNullOrWhiteSpace tx.InvestmentName then tx.Symbol.ToUpperInvariant() 
//                        else String.cleanWhitespace tx.InvestmentName 
//                    (key, tx.Symbol, fallbackName)
//                )
//                // Filter out records that match clean lookup keys
//                |> Seq.filter (fun (key, _, _) -> not (String.IsNullOrWhiteSpace key) && not (existingKeys.Contains(key)))
//                // Group using case-insensitive transformations
//                |> Seq.groupBy (fun (key, _, _) -> key.ToUpperInvariant()) 
//                |> Seq.map (fun (_, group) -> Seq.head group)
//                // TryCreate the new asset wrappers
//                |> Seq.map (fun (key, sym, name) -> 
//                    let result = Investment.TryCreate("0", name, sym, 0.0M, 0.0M, 0.0M)
//                    (key, result)
//                )
//                |> Seq.choose (fun (key, result) -> 
//                    match result with
//                    | Success value -> Some(key, value)
//                    | Failure _     -> None
//                )

//            // FIX: Initialize with OrdinalIgnoreCase so it's a safe container
//            let outputDictionary = SortedDictionary<string, Investment>(StringComparer.OrdinalIgnoreCase)

//            if not (isNull existingInvestments) then
//                for kvp in existingInvestments do
//                    // FORCE the existing keys to uppercase or match your target selector style
//                    // so that lowercase keys from your setup are corrected immediately!
//                    let normalizedKey = kvp.Key.Trim().ToUpperInvariant() 
//                    outputDictionary.TryAdd(normalizedKey, kvp.Value) |> ignore

//            for key, value in synthesizedInvestments do
//                // Force to uppercase again here just to guarantee normalization consistency
//                outputDictionary.TryAdd(key.ToUpperInvariant(), value) |> ignore

//            outputDictionary


//    // --- Public C# Consumer APIs ---

//    let BuildInvestmentsBySymbol (
//        existingInvestments: IReadOnlyDictionary<string, Investment>,
//        transactionHistory: IReadOnlyDictionary<string, List<Transaction>>) : SortedDictionary<string, Investment> =
//            buildInternal (existingInvestments, transactionHistory, (fun tx -> tx.Symbol.ToUpperInvariant()))

//    let BuildInvestmentsByName (
//        existingInvestments: IReadOnlyDictionary<string, Investment>,
//        transactionHistory: IReadOnlyDictionary<string, List<Transaction>>) : SortedDictionary<string, Investment> =
//            buildInternal (existingInvestments, transactionHistory, (fun tx -> 
//                if String.IsNullOrWhiteSpace tx.InvestmentName then tx.Symbol.ToUpperInvariant() else tx.InvestmentName))
namespace VanguardLib

open System
open System.Collections.Generic
open VanguardLib.Extensions

module PortfolioInitializer =

    /// Pure F# internal engine using immutable Maps and lists
    let private buildInternal (
        existingInvestments: IReadOnlyDictionary<string, Investment>,
        transactionHistory: Map<string, Transaction list>,
        keySelector: Transaction -> string) : Map<string, Investment> =

        // 1. Build an immutable lookup set for existing keys (case-insensitive via upper-casing)
        let existingKeys = 
            if isNull existingInvestments then 
                Set.empty
            else
                existingInvestments.Keys
                |> Seq.filter (String.IsNullOrWhiteSpace >> not)
                |> Seq.map (String.cleanWhitespace >> fun k -> k.ToUpperInvariant())
                |> Set.ofSeq

        // 2. Core functional data pipeline to generate synthesized investments
        let synthesizedInvestments = 
            transactionHistory
            |> Map.values
            |> Seq.collect id 
            |> Seq.filter (fun tx -> not (String.IsNullOrWhiteSpace tx.Symbol))
            |> Seq.map (fun tx -> 
                let rawKey = keySelector tx
                let key = String.cleanWhitespace rawKey
                
                let fallbackName = 
                    if String.IsNullOrWhiteSpace tx.InvestmentName then tx.Symbol.ToUpperInvariant() 
                    else String.cleanWhitespace tx.InvestmentName 
                (key, tx.Symbol, fallbackName)
            )
            // Filter out records that are empty or already match an existing key
            |> Seq.filter (fun (key, _, _) -> 
                not (String.IsNullOrWhiteSpace key) && not (existingKeys.Contains(key.ToUpperInvariant())))
            // Group by normalized key and take unique occurrences
            |> Seq.groupBy (fun (key, _, _) -> key.ToUpperInvariant()) 
            |> Seq.map (fun (_, group) -> Seq.head group)
            // Attempt to build domain models and extract successful creations
            |> Seq.map (fun (key, sym, name) -> 
                let result = Investment.TryCreate("0", name, sym, 0.0M, 0.0M, 0.0M)
                (key.ToUpperInvariant(), result)
            )
            |> Seq.choose (fun (key, result) -> 
                match result with
                | Success value -> Some(key, value)
                | Failure _     -> None
            )
            |> Map.ofSeq

        // 3. Hydrate existing assets into a starting F# map (with normalized keys)
        let baseMap = 
            if isNull existingInvestments then 
                Map.empty 
            else
                existingInvestments
                |> Seq.map (fun kvp -> kvp.Key.Trim().ToUpperInvariant(), kvp.Value)
                |> Map.ofSeq

        // 4. Safely merge synthesized investments into the base map
        // (F# Map.add automatically replaces or adds the key safely)
        (baseMap, synthesizedInvestments)
        ||> Map.fold (fun acc key value -> Map.add key value acc)


    // --- Public C# Compatible APIs ---
    // These return Map<string, Investment>, which automatically implements IReadOnlyDictionary for C#

    let BuildInvestmentsBySymbol (
        existingInvestments: IReadOnlyDictionary<string, Investment>,
        transactionHistory: Map<string, Transaction list>) : Map<string, Investment> =
            buildInternal (existingInvestments, transactionHistory, (fun tx -> tx.Symbol.ToUpperInvariant()))

    let BuildInvestmentsByName (
        existingInvestments: IReadOnlyDictionary<string, Investment>,
        transactionHistory: Map<string, Transaction list>) : Map<string, Investment> =
            buildInternal (existingInvestments, transactionHistory, (fun tx -> 
                if String.IsNullOrWhiteSpace tx.InvestmentName then tx.Symbol.ToUpperInvariant() else tx.InvestmentName))
