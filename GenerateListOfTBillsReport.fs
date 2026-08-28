namespace VanguardLib

open System.Collections.Generic

module GenerateListOfTBillsReport =

    // Module-level private constants matching your original design rules
    let private reportTitle = "List of TBills"
    let private headers = [| "T-Bill Key / Description"; "Amount"; "Paid" |]

    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTBills: SortedDictionary<string, Investment>) : string =
        // Pass arguments strictly by position to eliminate the FS0001 tuple error
        GenerateGenericInvestmentReport.Generate(
            sortedDictionaryOfTBills,
            headers,
            reportTitle,
            "No T-Bill records found."
        )
