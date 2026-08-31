namespace VanguardLib

open System
open System.Globalization
open System.Collections.Generic
open VanguardLib.Extensions

type VanguardProcessedData = {
    InvestmentsByCompanySymbol : SortedDictionary<string, Investment>
    InvestmentsByCompanyName   : SortedDictionary<string, Investment>
    TBills                     : SortedDictionary<string, Investment>
    Cash                       : SortedDictionary<string, Investment>
}

module ProcessInvestmentsPartOfVanguardDataFile =

    /// Imperative dictionary engine mimicking C# duplicate key suppression (.TryAdd)
    let private toSortedDictionary (keySelector: 'T -> 'Key) (valueSelector: 'T -> 'Value) (source: seq<'T>) : SortedDictionary<'Key, 'Value> =
        let dict = SortedDictionary<'Key, 'Value>()
        for item in source do
            dict.TryAdd(keySelector item, valueSelector item) |> ignore
        dict

    /// Safely parses an individual line. Returns None if data is corrupt or sized wrong.
    let private parseInvestment (currentLine: string) : Investment option =
        let parts = currentLine.Split(',')
        if parts.Length < 6 then 
            None
        else
            // Safely extracts indexes to fully handle rows with 6, 7, or more columns
            let accNum     = parts.[0]
            let name       = parts.[1]
            let sym        = parts.[2]
            let sharesStr  = parts.[3]
            let priceStr   = parts.[4]
            let totalStr   = parts.[5]

            let result = 
                Investment.TryCreate(
                    accountNumber  = accNum,
                    investmentName = name,
                    symbol         = sym,
                    shares         = String.toCleanDecimal sharesStr,
                    sharePrice     = String.toCleanDecimal priceStr,
                    totalValue     = String.toCleanDecimal totalStr
                )

            match result with
            | Success value -> Some value
            | Failure _     -> None // Drop corrupt rows immediately to prevent bad states

    let ProcessData (fileLines: seq<string>) : VanguardProcessedData =
        
        // Seq.choose skips header, drops empty lines, filters bad rows, and unpacks the options
        let investments = 
            fileLines
            |> Seq.skip 1
            |> Seq.choose parseInvestment
            |> Seq.toList

        let investmentsByCompanySymbol = 
            investments
            |> Seq.filter (fun inv -> inv.Symbol <> "NULL")
            |> toSortedDictionary (fun inv -> inv.Symbol.TrimEnd()) id

        let investmentsByCompanyName = 
            investmentsByCompanySymbol.Values
            |> Seq.groupBy (fun inv -> inv.InvestmentName)
            |> toSortedDictionary fst (fun (_, group) -> Seq.head group)

        let tBills = 
            investments
            |> Seq.filter (fun inv -> 
                inv.Symbol = "NULL" && 
                inv.InvestmentName.Contains("TREASURY", StringComparison.OrdinalIgnoreCase))
            |> toSortedDictionary (fun inv -> inv.InvestmentName) id

        let cash = 
            investments
            |> Seq.filter (fun inv -> 
                inv.Symbol = "NULL" && 
                inv.InvestmentName.Contains("CASH", StringComparison.OrdinalIgnoreCase))
            |> toSortedDictionary (fun inv -> inv.InvestmentName) id

        {
            InvestmentsByCompanySymbol = investmentsByCompanySymbol
            InvestmentsByCompanyName   = investmentsByCompanyName
            TBills                     = tBills
            Cash                       = cash
        }

module ProcessTransactionsPartOfVanguardDataFile =

    /// Safely parses an individual line. Returns None if data is corrupt or sized wrong.
    let private retrieveTransactionFromLine (currentLine: string) : Transaction option =
        if String.IsNullOrWhiteSpace(currentLine) then
            None
        else
            let parts = currentLine.Split(',')
            if parts.Length < 14 then
                None
            else
                // 1. Defend against Date format crashes using TryParse
                let mutable tradeDate = DateOnly.MinValue
                let mutable settlementDate = DateOnly.MinValue
                
                let isTradeDateValid = DateOnly.TryParse(parts.[1], CultureInfo.InvariantCulture, &tradeDate)
                let isSettlementValid = DateOnly.TryParse(parts.[2], CultureInfo.InvariantCulture, &settlementDate)

                if not isTradeDateValid || not isSettlementValid then
                    None
                else
                    // 2. Map columns safely via direct indexes (Handles 14 or more trailing fields)
                    let result = 
                        Transaction.TryCreate(
                            accountNumber          = parts.[0],
                            tradeDate              = tradeDate,
                            settlementDate         = settlementDate,
                            transactionType        = parts.[3],
                            transactionDescription = parts.[4],
                            investmentName         = parts.[5],
                            symbol                 = parts.[6],
                            shares                 = String.toCleanDecimal parts.[7],
                            sharePrice             = String.toCleanDecimal parts.[8],
                            principalAmount        = String.toCleanDecimal parts.[9],
                            commissionAndFees      = String.toCleanDecimal parts.[10],
                            netAmount              = String.toCleanDecimal parts.[11],     
                            accruedInterest        = String.toCleanDecimal parts.[12],
                            accountType            = parts.[13]
                        )

                    match result with
                    | Success value -> Some value
                    | Failure _     -> None

    let ProcessData (fileLines: seq<string>, existingTransactions: IReadOnlyDictionary<string, List<Transaction>>) : SortedDictionary<string, List<Transaction>> =
        
        // 1. Process and unwrap valid transactions from stream
        let parsedTransactionsGroupedByType = 
            fileLines
            |> Seq.skip 1
            |> Seq.takeWhile (String.IsNullOrWhiteSpace >> not)
            |> Seq.choose retrieveTransactionFromLine
            // Record properties are non-nullable; fallback check is simplified to a blank string check
            |> Seq.groupBy (fun t -> if String.IsNullOrWhiteSpace t.TransactionType then "" else t.TransactionType)

        // 2. Instantiate and seed the C# compatible SortedDictionary
        let outputDictionary = SortedDictionary<string, List<Transaction>>()

        for kvp in existingTransactions do
            outputDictionary.Add(kvp.Key, List<Transaction>(kvp.Value))

        // 3. Imperatively merge elements into mutable List containers for C# compatibility
        for key, group in parsedTransactionsGroupedByType do
            match outputDictionary.TryGetValue(key) with
            | true, targetList -> 
                targetList.AddRange(group)
            | false, _ -> 
                outputDictionary.[key] <- List<Transaction>(group)

        outputDictionary
