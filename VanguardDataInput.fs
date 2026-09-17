namespace VanguardLib

open System
open System.Globalization
open VanguardLib.Extensions

/// Clean, unified domain data container using standard immutable F# Maps
type VanguardProcessedData = {
    InvestmentsByCompanySymbol : Map<string, Investment>
    InvestmentsByCompanyName   : Map<string, Investment>
    TBills                     : Map<string, Investment>
    Cash                       : Map<string, Investment>
}

module ProcessInvestmentsPartOfVanguardDataFile =

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

            match Investment.TryCreate(
                accountNumber  = accNum,
                investmentName = name,
                symbol         = sym,
                shares         = String.toCleanDecimal sharesStr,
                sharePrice     = String.toCleanDecimal priceStr,
                totalValue     = String.toCleanDecimal totalStr
            ) with
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
            // Handle duplicate suppression elegantly using Map.ofSeq (last-write-wins behavior)
            // Or explicitly map to distinct keys if needed
            |> Seq.map (fun inv -> inv.Symbol.TrimEnd(), inv)
            |> Map.ofSeq

        let investmentsByCompanyName = 
            investmentsByCompanySymbol
            |> Map.values
            |> Seq.groupBy (fun inv -> inv.InvestmentName)
            |> Seq.map (fun (name, group) -> name, Seq.head group)
            |> Map.ofSeq

        let tBills = 
            investments
            |> Seq.filter (fun inv -> 
                inv.Symbol = "NULL" && 
                inv.InvestmentName.Contains("TREASURY", StringComparison.OrdinalIgnoreCase))
            |> Seq.map (fun inv -> inv.InvestmentName, inv)
            |> Map.ofSeq

        let cash = 
            investments
            |> Seq.filter (fun inv -> 
                inv.Symbol = "NULL" && 
                inv.InvestmentName.Contains("CASH", StringComparison.OrdinalIgnoreCase))
            |> Seq.map (fun inv -> inv.InvestmentName, inv)
            |> Map.ofSeq

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
                // Defend against Date format crashes using explicit pattern matches over out-parameters
                match DateOnly.TryParse(parts.[1], CultureInfo.InvariantCulture), 
                      DateOnly.TryParse(parts.[2], CultureInfo.InvariantCulture) with
                | (true, tradeDate), (true, settlementDate) ->
                    match Transaction.TryCreate(
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
                    ) with
                    | Success value -> Some value
                    | Failure _     -> None
                | _ -> None

    let ProcessData (fileLines: seq<string>, initialTransactions: Map<string, Transaction list>) : Map<string, Transaction list> =
        
        // 1. Process and unwrap valid transactions from the stream
        let parsedTransactions = 
            fileLines
            |> Seq.skip 1
            |> Seq.takeWhile (String.IsNullOrWhiteSpace >> not)
            |> Seq.choose retrieveTransactionFromLine
            |> Seq.toList

        // 2. Functional fold to safely group and append into the immutable maps 
        (initialTransactions, parsedTransactions)
        ||> List.fold (fun acc tx ->
            let key = if String.IsNullOrWhiteSpace tx.TransactionType then "" else tx.TransactionType
            match Map.tryFind key acc with
            | Some existingList -> Map.add key (existingList @ [tx]) acc // Or prepending: tx :: existingList (faster)
            | None              -> Map.add key [tx] acc
        )
