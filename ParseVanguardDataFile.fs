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
        let investmentsHeaderIdx = 
            rawLines |> Array.tryFindIndex (fun line -> line.StartsWith("Account Number,Investment Name"))
            
        let transactionsHeaderIdx = 
            rawLines |> Array.tryFindIndex (fun line -> line.StartsWith("Account Number,Trade Date"))

        let investmentLines =
            match investmentsHeaderIdx, transactionsHeaderIdx with
            | Some startIdx, Some endIdx when endIdx > startIdx ->
                let count = endIdx - startIdx
                rawLines |> Array.skip startIdx |> Array.take count
            | Some startIdx, _ ->
                rawLines |> Array.skip startIdx
            | _ -> 
                rawLines

        let transactionLines =
            match transactionsHeaderIdx with

            | Some startIdx -> rawLines |> Array.skip startIdx
            | None -> rawLines

        // Return the instantiated record
        { InvestmentLines = investmentLines; TransactionLines = transactionLines }

type ParseVanguardDataFile(streamReader: StreamReader) =
    // --- Private Instance Fields (State) ---
    let investmentsBySymbol = SortedDictionary<string, Investment>()
    let investmentsByName = SortedDictionary<string, Investment>()
    let transactions = SortedDictionary<string, List<Transaction>>()
    let reportGenerators = Dictionary<string, Func<string>>()

    // --- Private Helper Functions ---
    // Reads all non-empty lines out of the reader immediately into memory
    let readAllLines (reader: StreamReader) =
        Seq.initInfinite (fun _ -> reader.ReadLine())
        |> Seq.takeWhile (isNull >> not)
        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
        |> Seq.map (fun line -> line.Trim())
        |> Seq.toArray

    let generateAllReports () =
        let sb = StringBuilder()
        for kvp in reportGenerators do
            if kvp.Key <> "All Reports" then
                sb.AppendLine(kvp.Value.Invoke()) |> ignore
        sb.ToString()

    // --- Constructor Logic ---
    static do
        ()

    do
        if isNull streamReader then nullArg (nameof streamReader)

        // 1. Initialize transaction categories
        for tType in TransactionType.All do
            transactions.TryAdd(tType.DisplayText, List<Transaction>()) |> ignore

        // 2. Read every raw line into memory upfront safely
        let rawLines = readAllLines streamReader

        // 3. Slice the raw lines into structured blocks for independent parsing
        let fileData = SlicedFile.FromRawLines(rawLines)
        let investmentLines = fileData.InvestmentLines
        let transactionLines = fileData.TransactionLines

        // 4. Run parsers independently over their clean datasets
        let processedInvestments = ProcessInvestmentsPartOfVanguardDataFile.ProcessData(investmentLines)
        let processedTransactions = ProcessTransactionsPartOfVanguardDataFile.ProcessData(transactionLines, transactions)

        // 5. Hydrate standard cross-referenced portfolio states
        let hydratedByName = PortfolioInitializer.BuildInvestmentsByName(processedInvestments.InvestmentsByCompanyName, processedTransactions)
        let hydratedBySymbol = PortfolioInitializer.BuildInvestmentsBySymbol(processedInvestments.InvestmentsByCompanySymbol, processedTransactions)

        for kvp in hydratedByName do investmentsByName.Add(kvp.Key, kvp.Value)
        for kvp in hydratedBySymbol do investmentsBySymbol.Add(kvp.Key, kvp.Value)

        // 6. Register report rendering layout modules
        let register (reportName: string) (factoryFn: unit -> string) = 
            reportGenerators.Add(reportName, Func<string>(factoryFn))

        register "Investments Sorted By Company Name" (fun () -> 
            GenerateInvestmentsReport.GenerateReport(investmentsByName, InvestmentsReportConfiguration.ByName))
        register "Investments Sorted By Company Symbol" (fun () -> 
            GenerateInvestmentsReport.GenerateReport(investmentsBySymbol, InvestmentsReportConfiguration.BySymbol))
        register "Dividend Transactions Sorted By Company Name" (fun () -> 
            GenerateDividendTransactionsReport.GenerateReport(processedTransactions, investmentsByName, TransactionsReportConfiguration.ByName))
        register "Dividend Transactions Sorted By Company Symbol" (fun () -> 
            GenerateDividendTransactionsReport.GenerateReport(processedTransactions, investmentsBySymbol, TransactionsReportConfiguration.BySymbol))
        register "List of T BillsF" (fun () -> GenerateListOfTBillsReportF.GenerateReport(processedInvestments.TBillsF))
        register "List of CashF" (fun () -> GenerateListOfCashReportF.GenerateReport(processedInvestments.CashF))
        register "List of Dividends" (fun () -> GenerateListOfDividendsReport.GenerateReport(processedTransactions))
        register "List of Interest Payments" (fun () -> GenerateListOfInterestPayments.GenerateReport(processedTransactions))
        register "Distributions" (fun () -> GenerateDistributionsReport.GenerateReport(processedTransactions))
        register "List of Corp Actions" (fun () -> GenerateListOfCorpActions.GenerateReport(processedTransactions))
        register "List of Buy Transactions" (fun () -> GenerateListOfBuyTransactions.GenerateReport(processedTransactions))
        register "List of Sell Transactions" (fun () -> GenerateListOfSellTransactions.GenerateReport(processedTransactions))
        register "List of Fees" (fun () -> GenerateListOfFees.GenerateReport(processedTransactions))
        
        register "All Reports" generateAllReports

    // --- Public Properties (Exposed to C#) ---
    member _.InvestmentsBySymbol = investmentsBySymbol
    member _.InvestmentsByName = investmentsByName
    member _.Transactions = transactions

    // --- Public Methods (Exposed to C#) ---
    static member StringsForReportDropDown () = [|
        "All Reports"
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

    member _.GenerateReports(reportName: string) : string =
        match reportGenerators.TryGetValue reportName with
        | true, generateReport -> generateReport.Invoke()
        | false, _ -> String.Empty
