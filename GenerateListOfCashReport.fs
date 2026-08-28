namespace VanguardLib

open System.Collections.Generic

module GenerateListOfCashReport =

    // Module-level private constants matching your original design rules
    let private reportTitle = "List of Cash"
    let private headers = [| "Account / Investment Key"; "Amount"; "Paid" |]

    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfCash: SortedDictionary<string, Investment>) : string =
        // Pass arguments strictly by position to eliminate the FS0001 tuple error
        GenerateGenericInvestmentReport.Generate(
            sortedDictionaryOfCash,
            headers,
            reportTitle,
            "No cash records found."
        )
