namespace VanguardLib

open System
open System.IO
open System.Text
open System.Collections.Generic
open System.Collections.ObjectModel

type ParseVanguardDataFile(streamReader: StreamReader) =

    // --- Private Static/Constant Fields ---
    static let transactionTypes = 
        ReadOnlyCollection<string>([|
            "Buy"
            "Corp Action (Redemption)"
            "Distribution"
            "Dividend"
            "Fee"
            "Interest"
            "Sell"
        |])

    // --- Private Instance Fields (State) ---
    let cash = SortedDictionary<string, Investment>()
    let investmentsBySymbol = SortedDictionary<string, Investment>()
    let investmentsByName = SortedDictionary<string, Investment>()
    let tBills = SortedDictionary<string, Investment>()
    let transactions = SortedDictionary<string, List<Transaction>>()
    let reportGenerators = Dictionary<string, Func<string>>()

    // --- Private Helper Functions ---
    // Reads all non-empty lines out of the reader immediately into memory
    let readAllLines (reader: StreamReader) =
        [|
            while not reader.EndOfStream do
                let line = reader.ReadLine()
                if not (String.IsNullOrWhiteSpace line) then 
                    yield line.Trim()
        |]

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

        // 1. Initialize structural transactions categories
        for tType in transactionTypes do
            transactions.TryAdd(tType, List<Transaction>()) |> ignore

        // 2. Read every raw line into memory upfront safely
        let rawLines = readAllLines streamReader

        // --- SEGMENT SLICING LOGIC ---
        
        // Find the absolute 0-based index locations of both header rows
        let investmentsHeaderIdx = 
            rawLines |> Array.tryFindIndex (fun line -> line.StartsWith("Account Number,Investment Name"))
            
        let transactionsHeaderIdx = 
            rawLines |> Array.tryFindIndex (fun line -> line.StartsWith("Account Number,Trade Date"))

        // Safely extract rows starting WITH the investment header down to the transaction block
        let investmentLines =
            match investmentsHeaderIdx, transactionsHeaderIdx with
            | Some startIdx, Some endIdx when endIdx > startIdx ->
                // Start right at the investment header row index
                // The total count of lines including the header is (endIdx - startIdx)
                let count = endIdx - startIdx
                rawLines |> Array.skip startIdx |> Array.take count
            | Some startIdx, _ ->
                // Fallback: If no transaction header exists, take everything from the investment header to the end
                rawLines |> Array.skip startIdx
            | _ -> 
                // Ultimate Fallback: Pass all lines if structural tracking fails
                rawLines

        // Extract transaction rows starting from the transaction header down to the end of the file
        let transactionLines =
            match transactionsHeaderIdx with
            | Some startIdx -> rawLines |> Array.skip startIdx
            | None -> rawLines

        // 4. Run your parsers independently over their clean datasets
        let result = ProcessInvestmentsPartOfVanguardDataFile.ProcessData(investmentLines)
        
        for kvp in result.Cash do cash.Add(kvp.Key, kvp.Value)
        for kvp in result.TBills do tBills.Add(kvp.Key, kvp.Value)

        let processedTransactions = ProcessTransactionsPartOfVanguardDataFile.ProcessData(transactionLines, transactions)

        // 5. Hydrate standard cross-referenced portfolio states
        let hydratedByName = PortfolioInitializer.BuildInvestmentsByName(result.InvestmentsByCompanyName, processedTransactions)
        let hydratedBySymbol = PortfolioInitializer.BuildInvestmentsBySymbol(result.InvestmentsByCompanySymbol, processedTransactions)

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
        register "List of T Bills" (fun () -> GenerateListOfTBillsReport.GenerateReport(tBills))
        register "List of Cash" (fun () -> GenerateListOfCashReport.GenerateReport(cash))
        register "List of Dividends" (fun () -> GenerateListOfDividendsReport.GenerateReport(processedTransactions))
        register "List of Interest Payments" (fun () -> GenerateListOfInterestPayments.GenerateReport(processedTransactions))
        register "Distributions" (fun () -> GenerateDistributionsReport.GenerateReport(processedTransactions))
        register "List of Corp Actions" (fun () -> GenerateListOfCorpActions.GenerateReport(processedTransactions))
        register "List of Buy Transactions" (fun () -> GenerateListOfBuyTransactions.GenerateReport(processedTransactions))
        register "List of Sell Transactions" (fun () -> GenerateListOfSellTransactions.GenerateReport(processedTransactions))
        register "List of Fees" (fun () -> GenerateListOfFees.GenerateReport(processedTransactions))
        
        register "All Reports" generateAllReports

    // --- Public Properties (Exposed to C#) ---
    member _.Cash = cash
    member _.InvestmentsBySymbol = investmentsBySymbol
    member _.InvestmentsByName = investmentsByName
    member _.TBills = tBills
    member _.Transactions = transactions

    // --- Public Methods (Exposed to C#) ---
    static member StringsForReportDropDown () = [|
        "All Reports"
        "Investments Sorted By Company Name"
        "Investments Sorted By Company Symbol"
        "Dividend Transactions Sorted By Company Name"
        "Dividend Transactions Sorted By Company Symbol"
        "List of T Bills"
        "List of Cash"
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
