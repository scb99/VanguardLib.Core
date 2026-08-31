namespace VanguardLib

//open System.Collections.Generic

//module GenerateListOfDividendsReport =

//    let private reportTitle = "List of Dividends"
    
//    // REFINED HEADERS: Removes the redundant "Dividend" label column 
//    // and adds "Shares" so users can see the asset quantity tracking.
//    let private headers = [| "Settlement Date"; "Investment Name"; "Shares"; "Amount" |]

//    /// Public API exposed via standard .NET parameters for seamless C# library interop
//    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
//        GenerateGenericTransactionReport.Generate(
//            sortedDictionaryOfTransactions,
//            "Dividend",
//            reportTitle,
//            "No dividend transactions found.",
//            headers,
//            "Total dividends received"
//        )
