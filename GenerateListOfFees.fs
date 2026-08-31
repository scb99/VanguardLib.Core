namespace VanguardLib

//open System.Collections.Generic

//module GenerateListOfFees =

//    let private reportTitle = "List of Fees"
    
//    // REFINED HEADERS: Removes the redundant "Fee" column label 
//    // and adds the Symbol for quicker scanability.
//    let private headers = [| "Settlement Date"; "Investment Name"; "Symbol"; "Amount" |]

//    /// Public API exposed via standard .NET parameters for seamless C# library interop
//    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
//        GenerateGenericTransactionReport.Generate(
//            sortedDictionaryOfTransactions,
//            "Fee",
//            reportTitle,
//            "No fee transactions found.",
//            headers,
//            "Total fees paid"
//        )
