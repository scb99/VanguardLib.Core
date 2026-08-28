namespace VanguardLib

//open System
//open System.IO
//open System.Text
//open System.Collections.Generic
//open System.Collections.ObjectModel

//type ParseVanguardDataFile(streamReader: StreamReader) =

//    // Guard Clause matching standard .NET runtime boundary expectations
//    do 
//        if box streamReader = null then 
//            raise (ArgumentNullException(nameof streamReader))

//    // 1. Core State Properties (Exposed via internal/private bindings)
//    let mutable cash = SortedDictionary<string, Investment>()
//    let mutable investmentsBySymbol = SortedDictionary<string, Investment>()
//    let mutable investmentsByName = SortedDictionary<string, Investment>()
//    let mutable tBills = SortedDictionary<string, Investment>()
//    let mutable transactions = SortedDictionary<string, List<Transaction>>()

//    // Static collections matching your exact initialization shapes
//    static let transactionTypes = 
//        ReadOnlyCollection<string>([|
//            "Buy"
//            "Corp Action (Redemption)"
//            "Distribution"
//            "Dividend"
//            "Fee"
//            "Interest"
//            "Sell"
//        |])

//    // 2. Report Registration Routing Configuration Engine
//    let reportGenerators = Dictionary<string, Func<string>>()

//    // Inner method to synthesize all reports into a unified StringBuilder buffer
//    let generateAllReports () =
//        let sb = StringBuilder()
//        for kvp in reportGenerators do
//            if kvp.Key <> "All Reports" then
//                sb.AppendLine(kvp.Value.Invoke()) |> ignore
//        sb.ToString()

//    // 3. Lazy Reader Helper Engine
//    let readAllLinesLazily (reader: StreamReader) =
//        seq {
//            while not reader.EndOfStream do
//                let line = reader.ReadLine()
//                if box line <> null then yield line
//        }

//    // Constructor Parsing Sequence Block (runs seamlessly at initialization time)
//    do
//        // Seed transactions dictionary with empty lists for each recognized type
//        for tType in transactionTypes do
//            transactions.TryAdd(tType, List<Transaction>()) |> ignore

//        // Consume stream lines lazily and process investments block
//        let fileLines = readAllLinesLazily streamReader
//        let result = ProcessInvestmentsPartOfVanguardDataFile.ProcessData(fileLines)
        
//        investmentsByName   <- result.InvestmentsByCompanyName
//        investmentsBySymbol <- result.InvestmentsByCompanySymbol
//        tBills              <- result.TBills
//        cash                <- result.Cash

//        // Stream remains open to continue reading transaction payload data downstream
//        transactions        <- ProcessTransactionsPartOfVanguardDataFile.ProcessData(fileLines, transactions)

//        // Synthesize and stabilize discovered investments missing structural seed data
//        investmentsByName   <- PortfolioInitializer.BuildInvestmentsByName(investmentsByName, transactions)
//        investmentsBySymbol <- PortfolioInitializer.BuildInvestmentsBySymbol(investmentsBySymbol, transactions)

//        // Map lookup actions directly to the state variables stabilized above
//        reportGenerators.["All Reports"] <- Func<string>(generateAllReports)
//        reportGenerators.["Investments Sorted By Company Name"] <- Func<string>(fun () -> GenerateInvestmentsReport.GenerateReport(investmentsByName, InvestmentsReportConfiguration.ByName))
//        reportGenerators.["Investments Sorted By Company Symbol"] <- Func<string>(fun () -> GenerateInvestmentsReport.GenerateReport(investmentsBySymbol, InvestmentsReportConfiguration.BySymbol))
//        reportGenerators.["Dividend Transactions Sorted By Company Name"] <- Func<string>(fun () -> GenerateDividendTransactionsReport.GenerateReport(transactions, investmentsByName, TransactionsReportConfiguration.ByName))
//        reportGenerators.["Dividend Transactions Sorted By Company Symbol"] <- Func<string>(fun () -> GenerateDividendTransactionsReport.GenerateReport(transactions, investmentsBySymbol, TransactionsReportConfiguration.BySymbol))
//        reportGenerators.["List of T Bills"] <- Func<string>(fun () -> GenerateListOfTBillsReport.GenerateReport(tBills))
//        reportGenerators.["List of Cash"] <- Func<string>(fun () -> GenerateListOfCashReport.GenerateReport(cash))
//        reportGenerators.["List of Dividends"] <- Func<string>(fun () -> GenerateListOfDividendsReport.GenerateReport(transactions))
//        reportGenerators.["List of Interest Payments"] <- Func<string>(fun () -> GenerateListOfInterestPayments.GenerateReport(transactions))
//        reportGenerators.["Distributions"] <- Func<string>(fun () -> GenerateDistributionsReport.GenerateReport(transactions))
//        reportGenerators.["List of Corp Actions"] <- Func<string>(fun () -> GenerateListOfCorpActions.GenerateReport(transactions))
//        reportGenerators.["List of Buy Transactions"] <- Func<string>(fun () -> GenerateListOfBuyTransactions.GenerateReport(transactions))
//        reportGenerators.["List of Sell Transactions"] <- Func<string>(fun () -> GenerateListOfSellTransactions.GenerateReport(transactions))
//        reportGenerators.["List of Fees"] <- Func<string>(fun () -> GenerateListOfFees.GenerateReport(transactions))

//    // 4. Public API Methods for C# dropdown UI interaction layers
//    static member StringsForReportDropDown () : string[] = [|
//        "All Reports"
//        "Investments Sorted By Company Name"
//        "Investments Sorted By Company Symbol"
//        "Dividend Transactions Sorted By Company Name"
//        "Dividend Transactions Sorted By Company Symbol"
//        "List of T Bills"
//        "List of Cash"
//        "List of Dividends"
//        "List of Interest Payments"
//        "Distributions"
//        "List of Corp Actions"
//        "List of Buy Transactions"
//        "List of Sell Transactions"
//        "List of Fees"
//    |]

//    member this.GenerateReports(reportName: string) : string =
//        match reportGenerators.TryGetValue(reportName) with
//        | true, generateReport -> generateReport.Invoke()
//        | false, _             -> String.Empty
