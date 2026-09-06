namespace VanguardLib

open System
open System.IO
open System.Text
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

type ParseVanguardDataFile(streamReader: StreamReader) =
    // --- Private Instance Fields (State) ---
    let investmentsBySymbol = SortedDictionary<string, Investment>()
    let investmentsByName = SortedDictionary<string, Investment>()
    let transactions = SortedDictionary<string, List<Transaction>>()

    static let reportOrder = [|
        "Investments Sorted By Company Name"
        "Investments Sorted By Company Symbol"
        "Dividend Transactions Sorted By Company Name"
        "Dividend Transactions Sorted By Company Symbol"
        "List of T BillsF"
        "List of CashF"
        "List of Dividends"
        "List of Interest Payments"
        "Distributions"
        "List of Corp Actions"
        "List of Buy Transactions"
        "List of Sell Transactions"
        "List of Fees"
    |]

    // Reads all non-empty lines out of the reader immediately into memory
    let readAllLines (reader: StreamReader) =
        Seq.initInfinite (fun _ -> reader.ReadLine())
        |> Seq.takeWhile (isNull >> not)
        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
        |> Seq.map (fun line -> line.Trim())
        |> Seq.toArray

    // --- Core Parsing & Initialization Setup ---
    let processedData = 
        if isNull streamReader then nullArg (nameof streamReader)

        // 1. Initialize transaction categories
        for tType in TransactionType.All do
            transactions.TryAdd(tType.DisplayText, List<Transaction>()) |> ignore

        // 2. Read every raw line into memory upfront safely
        let rawLines = readAllLines streamReader

        // 3. Slice the raw lines into structured blocks
        let fileData = SlicedFile.FromRawLines(rawLines)
        
        // 4. Run parsers independently over their clean datasets
        let processedInvestments = ProcessInvestmentsPartOfVanguardDataFile.ProcessData(fileData.InvestmentLines)
        let processedTransactions = ProcessTransactionsPartOfVanguardDataFile.ProcessData(fileData.TransactionLines, transactions)

        // 5. Hydrate standard cross-referenced portfolio states
        let hydratedByName = PortfolioInitializer.BuildInvestmentsByName(processedInvestments.InvestmentsByCompanyName, processedTransactions)
        let hydratedBySymbol = PortfolioInitializer.BuildInvestmentsBySymbol(processedInvestments.InvestmentsByCompanySymbol, processedTransactions)

        for kvp in hydratedByName do investmentsByName.Add(kvp.Key, kvp.Value)
        for kvp in hydratedBySymbol do investmentsBySymbol.Add(kvp.Key, kvp.Value)
        
        // Return a tuple of variables needed by the report generators
        (processedInvestments, processedTransactions)

    let processedInvestments, processedTransactions = processedData

    // Forward declaration of generateAllReports so it can capture the map safely
    // Note: We use a recursive/lazy approach or look up directly from the map keys
#nowarn "40"
    let rec reportGenerators : Map<string, unit -> string> = 
        Map [
            reportOrder.[0],  (fun () -> GenerateInvestmentsReport.GenerateReport(investmentsByName, InvestmentsReportConfiguration.ByName))
            reportOrder.[1],  (fun () -> GenerateInvestmentsReport.GenerateReport(investmentsBySymbol, InvestmentsReportConfiguration.BySymbol))
            reportOrder.[2],  (fun () -> GenerateDividendTransactionsReport.GenerateReport(processedTransactions, investmentsByName, TransactionsReportConfiguration.ByName))
            reportOrder.[3],  (fun () -> GenerateDividendTransactionsReport.GenerateReport(processedTransactions, investmentsBySymbol, TransactionsReportConfiguration.BySymbol))
            reportOrder.[4],  (fun () -> GenerateListOfTBillsReportF.GenerateReport(processedInvestments.TBillsF))
            reportOrder.[5],  (fun () -> GenerateListOfCashReportF.GenerateReport(processedInvestments.CashF))
            reportOrder.[6],  (fun () -> GenerateListOfDividendsReport.GenerateReport(processedTransactions))
            reportOrder.[7],  (fun () -> GenerateListOfInterestPayments.GenerateReport(processedTransactions))
            reportOrder.[8],  (fun () -> GenerateDistributionsReport.GenerateReport(processedTransactions))
            reportOrder.[9],  (fun () -> GenerateListOfCorpActions.GenerateReport(processedTransactions))
            reportOrder.[10], (fun () -> GenerateListOfBuyTransactions.GenerateReport(processedTransactions))
            reportOrder.[11], (fun () -> GenerateListOfSellTransactions.GenerateReport(processedTransactions))
            reportOrder.[12], (fun () -> GenerateListOfFees.GenerateReport(processedTransactions))
    
            "All Reports", generateAllReports
        ]
    and generateAllReports () =
        let sb = StringBuilder()
        for reportName in reportOrder do
            match reportGenerators |> Map.tryFind reportName with
            | Some generate -> sb.AppendLine(generate()) |> ignore
            | None -> ()
        sb.ToString()
    
    // Explicitly declaring it as a public static member method
    static member StringsForReportDropDown () = 
        Array.append [| "All Reports" |] reportOrder

    member _.GenerateReports(reportName: string) : string =
        // Clean F# Map lookup instead of .NET TryGetValue out-parameters
        match reportGenerators |> Map.tryFind reportName with
        | Some generateReport -> generateReport()
        | None -> String.Empty
