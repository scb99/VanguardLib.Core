namespace VanguardLib

open System.Collections.Generic

module GenerateListOfSellTransactions =

    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        // Pass arguments strictly by position and supply trailing nulls to clear the FS0001 error
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Sell",
            "List of Sell Transactions",
            "No sell transactions found.",
            null,
            null
        )
