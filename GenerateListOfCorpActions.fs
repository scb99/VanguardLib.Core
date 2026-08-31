namespace VanguardLib

//open System.Collections.Generic

//module GenerateListOfCorpActions =

//    // Module-level private constant matching your original design rules
//    let private reportTitle = "List of Corporate Actions"

//    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
//    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
//        // Pass arguments strictly by position and supply trailing nulls to clear the FS0001 error
//        GenerateGenericTransactionReport.Generate(
//            sortedDictionaryOfTransactions,
//            "Corp Action (Redemption)",
//            reportTitle,
//            "No corporate action transactions found.",
//            null,
//            null
//        )
