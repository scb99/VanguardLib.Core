namespace VanguardLib

open System
open System.Text
open System.Collections.Generic

type DropDownItems(parsedPortfolio : ParsedPortfolio) =

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

    static let reportOrder = [|
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

    // Forward declaration of generateAllReports so it can capture the map safely
    // Note: We use a recursive/lazy approach or look up directly from the map keys
    let reportGenerators : Map<string, unit -> string> = 
        Map [
            reportOrder.[0],  (fun () -> GenerateInvestmentsReport.GenerateReport(investmentsByName, InvestmentsReportConfiguration.ByName))
            reportOrder.[1],  (fun () -> GenerateInvestmentsReport.GenerateReport(investmentsBySymbol, InvestmentsReportConfiguration.BySymbol))
            reportOrder.[2],  (fun () -> GenerateDividendTransactionsReport.GenerateReport(processedTransactions, investmentsByName, TransactionsReportConfiguration.ByName))
            reportOrder.[3],  (fun () -> GenerateDividendTransactionsReport.GenerateReport(processedTransactions, investmentsBySymbol, TransactionsReportConfiguration.BySymbol))
            reportOrder.[4],  (fun () -> GenerateListOfTBillsReport.GenerateReport(processedInvestments.TBills))
            reportOrder.[5],  (fun () -> GenerateListOfCashReport.GenerateReport(processedInvestments.Cash))
            reportOrder.[6],  (fun () -> GenerateListOfDividendsReport.GenerateReport(processedTransactions))
            reportOrder.[7],  (fun () -> GenerateListOfInterestPayments.GenerateReport(processedTransactions))
            reportOrder.[8],  (fun () -> GenerateDistributionsReport.GenerateReport(processedTransactions))
            reportOrder.[9],  (fun () -> GenerateListOfCorpActions.GenerateReport(processedTransactions))
            reportOrder.[10], (fun () -> GenerateListOfBuyTransactions.GenerateReport(processedTransactions))
            reportOrder.[11], (fun () -> GenerateListOfSellTransactions.GenerateReport(processedTransactions))
            reportOrder.[12], (fun () -> GenerateListOfFees.GenerateReport(processedTransactions))
        ]   
    
    let generateAllReports () =
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
        match reportName with
        | "All Reports" -> generateAllReports() 
        | _ -> 
            match reportGenerators |> Map.tryFind reportName with
            | Some generateReport -> generateReport()
            | None -> String.Empty
