namespace VanguardLib

//open System.Collections.Generic

//module GenerateListOfInterestPayments =

//    // Module-level private constants matching your original design rules
//    let private reportTitle = "List of Interest Payments"
//    let private headers = [| "Settlement Date"; "Investment Name"; "Amount" |]

//    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
//    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
//        // Pass arguments strictly by position to eliminate the FS0001 tuple error
//        GenerateGenericTransactionReport.Generate(
//            sortedDictionaryOfTransactions,
//            "Interest",
//            reportTitle,
//            "No interest payment transactions found.",
//            headers,
//            "Total interest payments received"
//        )
