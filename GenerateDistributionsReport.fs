namespace VanguardLib

//open System.Collections.Generic

//module GenerateDistributionsReport =

//    let private reportTitle = "Distributions"
//    let private headers = [| "Settlement Date"; "Amount" |]

//    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
//        // Pass arguments strictly by position to eliminate the FS0001 tuple error
//        GenerateGenericTransactionReport.Generate(
//            sortedDictionaryOfTransactions,
//            "Distribution",
//            reportTitle,
//            "No distributions found.",
//            headers,
//            "Total amount of distributions"
//        )
