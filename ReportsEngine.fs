namespace VanguardLib

open System
open System.Text
open System.Collections.Generic

/// Generates report content for a given parsed portfolio, keyed by the labels defined in DropDownItems.
type ReportsEngine(parsedPortfolio : ParsedPortfolio) =

    do if isNull (box parsedPortfolio) then nullArg (nameof parsedPortfolio)

    let investmentsByName =
        parsedPortfolio.InvestmentsByName
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value) // Convert to sequence of F# tuples
        |> dict                                    // Converts to a standard .NET IDictionary
        |> (fun d -> SortedDictionary<string, Investment>(d)) // Explicit instantiation helper

    let investmentsBySymbol =
        parsedPortfolio.InvestmentsBySymbol
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value) // Convert to sequence of F# tuples
        |> dict                                    // Converts to a standard .NET IDictionary
        |> (fun d -> SortedDictionary<string, Investment>(d)) // Explicit instantiation helper

    let processedInvestments = parsedPortfolio.RawInvestments

    let processedTransactions =
        parsedPortfolio.RawTransactions
        |> Seq.map (fun kvp ->
            // 1. Convert the inner IReadOnlyCollection<Transaction> to a new List<Transaction>
            let mutableList = List<Transaction>(kvp.Value)
            kvp.Key, mutableList)
        |> dict // 2. Convert the sequence of tuples to a standard .NET IDictionary
        // 3. Construct the SortedDictionary using an inline lambda to avoid FS0039/FS0041 errors
        |> (fun d -> SortedDictionary<string, List<Transaction>>(d))

    // Forward declaration of generateAllReports so it can capture the map safely
    // Note: We use a recursive/lazy approach or look up directly from the map keys
    let reportGenerators : Map<string, unit -> string> =
        Map [
            DropDownItems.InvestmentsByNameLabel,            (fun () -> GenerateInvestmentsReport.GenerateReport(investmentsByName, InvestmentsReportConfiguration.ByName))
            DropDownItems.InvestmentsBySymbolLabel,          (fun () -> GenerateInvestmentsReport.GenerateReport(investmentsBySymbol, InvestmentsReportConfiguration.BySymbol))
            DropDownItems.DividendTransactionsByNameLabel,   (fun () -> GenerateDividendTransactionsReport.GenerateReport(processedTransactions, investmentsByName, TransactionsReportConfiguration.ByName))
            DropDownItems.DividendTransactionsBySymbolLabel, (fun () -> GenerateDividendTransactionsReport.GenerateReport(processedTransactions, investmentsBySymbol, TransactionsReportConfiguration.BySymbol))
            DropDownItems.ListOfTBillsLabel,                 (fun () -> GenerateListOfTBillsReport.GenerateReport(processedInvestments.TBills))
            DropDownItems.ListOfCashLabel,                   (fun () -> GenerateListOfCashReport.GenerateReport(processedInvestments.Cash))
            DropDownItems.ListOfDividendsLabel,              (fun () -> GenerateListOfDividendsReport.GenerateReport(processedTransactions))
            DropDownItems.ListOfInterestPaymentsLabel,       (fun () -> GenerateListOfInterestPayments.GenerateReport(processedTransactions))
            DropDownItems.DistributionsLabel,                (fun () -> GenerateDistributionsReport.GenerateReport(processedTransactions))
            DropDownItems.ListOfCorpActionsLabel,            (fun () -> GenerateListOfCorpActions.GenerateReport(processedTransactions))
            DropDownItems.ListOfBuyTransactionsLabel,        (fun () -> GenerateListOfBuyTransactions.GenerateReport(processedTransactions))
            DropDownItems.ListOfSellTransactionsLabel,       (fun () -> GenerateListOfSellTransactions.GenerateReport(processedTransactions))
            DropDownItems.ListOfFeesLabel,                   (fun () -> GenerateListOfFees.GenerateReport(processedTransactions))
        ]

    let generateAllReports () =
        let sb = StringBuilder()
        for reportName in DropDownItems.ReportOrder do
            match reportGenerators |> Map.tryFind reportName with
            | Some generate -> sb.AppendLine(generate()) |> ignore
            | None -> ()
        sb.ToString()

    member _.GenerateReports(reportName: string) : string =
        match reportName with
        | name when name = DropDownItems.AllReportsLabel -> generateAllReports()
        | _ ->
            match reportGenerators |> Map.tryFind reportName with
            | Some generateReport -> generateReport()
            | None -> String.Empty
