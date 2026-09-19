namespace VanguardLib

open System
open System.Globalization
open VanguardLib.Extensions

module ProcessInvestmentsPartOfVanguardDataFile =

    /// Safely parses an individual line. Returns None if data is corrupt or sized wrong.
    let private parseInvestment (currentLine: string) : Investment option =
        match currentLine.Split(',') with
        | [| accNum; name; sym; sharesStr; priceStr; totalStr; _ |] ->
            match Investment.TryCreate(
                accountNumber  = accNum,
                investmentName = name,
                symbol         = sym,
                shares         = String.toCleanDecimal sharesStr,
                sharePrice     = String.toCleanDecimal priceStr,
                totalValue     = String.toCleanDecimal totalStr
            ) with
            | Success value -> Some value
            | Failure _     -> None
        | _ -> None

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
            match currentLine.Split(',') with
            | [| accNum; tradeDateStr; settleDateStr; txType; txDesc; 
                 name; sym; shares; price; principal; fees; net; interest; accType; _ |] ->
                
                match DateOnly.TryParse(tradeDateStr, CultureInfo.InvariantCulture), 
                      DateOnly.TryParse(settleDateStr, CultureInfo.InvariantCulture) with
                | (true, tradeDate), (true, settlementDate) ->
                    match Transaction.TryCreate(
                        accountNumber          = accNum,
                        tradeDate              = tradeDate,
                        settlementDate         = settlementDate,
                        transactionType        = txType,
                        transactionDescription = txDesc,
                        investmentName         = name,
                        symbol                 = sym,
                        shares                 = String.toCleanDecimal shares,
                        sharePrice             = String.toCleanDecimal price,
                        principalAmount        = String.toCleanDecimal principal,
                        commissionAndFees      = String.toCleanDecimal fees,
                        netAmount              = String.toCleanDecimal net,     
                        accruedInterest        = String.toCleanDecimal interest,
                        accountType            = accType
                    ) with
                    | Success value -> Some value
                    | Failure _     -> None
                | _ -> None
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
