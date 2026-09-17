namespace VanguardLib

open System
open System.IO
open System.Collections.Generic

type TransactionType =
    | Buy
    | CorpActionRedemption
    | Distribution
    | Dividend
    | Fee
    | Interest
    | Sell

    /// Returns the human-readable string for the UI
    member this.DisplayText =
        match this with
        | Buy -> "Buy"
        | CorpActionRedemption -> "Corp Action (Redemption)"
        | Distribution -> "Distribution"
        | Dividend -> "Dividend"
        | Fee -> "Fee"
        | Interest -> "Interest"
        | Sell -> "Sell"

    static member All = 
        [ Buy; CorpActionRedemption; Distribution; Dividend; Fee; Interest; Sell ]

type SlicedFile = {
    InvestmentLines: string[]
    TransactionLines: string[]
} with
    /// Factory method to slice raw CSV lines into structured blocks
    static member FromRawLines(rawLines: string[]) =
        let invHeaderIdx = rawLines |> Array.tryFindIndex (fun l -> l.StartsWith("Account Number,Investment Name"))
        let txHeaderIdx = rawLines |> Array.tryFindIndex (fun l -> l.StartsWith("Account Number,Trade Date"))

        let investmentLines =
            match invHeaderIdx, txHeaderIdx with
            | Some start, Some nextStart when nextStart > start -> rawLines.[start .. nextStart - 1]
            | Some start, _                                      -> rawLines.[start ..]
            | _                                                 -> rawLines

        let transactionLines =
            match txHeaderIdx with
            | Some start -> rawLines.[start ..]
            | None       -> rawLines

        { InvestmentLines = investmentLines; TransactionLines = transactionLines }

/// Holds the completely parsed and cross-referenced domain data model.
type ParsedPortfolio = {
    InvestmentsByName: IReadOnlyDictionary<string, Investment>
    InvestmentsBySymbol: IReadOnlyDictionary<string, Investment>    
    RawInvestments: VanguardProcessedData 
    RawTransactions: IReadOnlyDictionary<string, IReadOnlyCollection<Transaction>>
}

/// Module responsible for module VanguardParser =
type ParseVanguardDataFile(streamReader: StreamReader) =

    /// Reads all non-empty lines out of the stream reader into memory
    let readAllLines (reader: StreamReader) =
        Seq.initInfinite (fun _ -> reader.ReadLine())
        |> Seq.takeWhile (isNull >> not)
        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
        |> Seq.map (fun line -> line.Trim())
        |> Seq.toArray

    /// Initializes a fresh transaction structure using standard F# immutable maps
    let createInitialTransactionMap () : Map<string, Transaction list> =
        TransactionType.All
        |> List.map (fun tType -> tType.DisplayText, [])
        |> Map.ofList

    /// Parses a Vanguard data stream and returns a fully populated portfolio data model.
    member this.Parse () : ParsedPortfolio =
        if isNull streamReader then nullArg (nameof streamReader)

        // 1. Core data processing pipeline
        let rawLines = readAllLines streamReader
        let fileData = SlicedFile.FromRawLines(rawLines)
        let initialTxMap = createInitialTransactionMap ()

        // 2. Run independent domain parsers
        let processedInvestments = ProcessInvestmentsPartOfVanguardDataFile.ProcessData(fileData.InvestmentLines)
        let processedTransactions = ProcessTransactionsPartOfVanguardDataFile.ProcessData(fileData.TransactionLines, initialTxMap)

        // 3. Hydrate standard cross-referenced portfolio states
        let hydratedByName = 
            PortfolioInitializer.BuildInvestmentsByName(processedInvestments.InvestmentsByCompanyName, processedTransactions)
            |> Seq.map (fun kvp -> kvp.Key, kvp.Value)
            |> Map.ofSeq

        let hydratedBySymbol = 
            PortfolioInitializer.BuildInvestmentsBySymbol(processedInvestments.InvestmentsByCompanySymbol, processedTransactions)
            |> Seq.map (fun kvp -> kvp.Key, kvp.Value)
            |> Map.ofSeq

        let keepAllTransactions (transactions: Map<string, Transaction list>) : IReadOnlyDictionary<string, IReadOnlyCollection<Transaction>> =
            transactions
            // 1. Filter out keys that have empty lists (optional, but keeps the data clean)
            |> Map.filter (fun key list -> not (List.isEmpty list))
            // 2. Cast the F# list to a C#-friendly collection interface
            |> Map.map (fun key list -> list :> IReadOnlyCollection<Transaction>)
            // 3. Cast the Map to the final dictionary interface
            :> IReadOnlyDictionary<string, IReadOnlyCollection<Transaction>>

        // 4. Return the completely structured data model
        {
            InvestmentsByName = hydratedByName 
            InvestmentsBySymbol = hydratedBySymbol
            RawInvestments = processedInvestments
            RawTransactions = keepAllTransactions processedTransactions
        }
