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
