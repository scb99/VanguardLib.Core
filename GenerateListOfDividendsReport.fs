namespace VanguardLib

open System.Collections.Generic

module GenerateListOfDividendsReport =

    // Module-level private constants matching your original design rules
    let private reportTitle = "List of Dividends"
    let private headers = [| "Transaction Type"; "Settlement Date"; "Investment Name"; "Amount" |]

    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        // Pass arguments strictly by position to eliminate the FS0001 tuple error
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Dividend",
            reportTitle,
            "No dividend transactions found.",
            headers,
            "Total dividends received"
        )
