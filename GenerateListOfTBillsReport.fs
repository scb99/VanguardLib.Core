namespace VanguardLib

//open System.Collections.Generic

//module GenerateListOfTBillsReport =

//    let private reportTitle = "List of TBills"
    
//    // FIX: Standardize headers so GenerateGenericInvestmentReport 
//    // maps them to actual Investment object properties cleanly!
//    let private headers = [| "T-Bill Key / Description"; "Total Value" |]

//    /// Public API exposed via standard .NET parameters for seamless C# library interop
//    let GenerateReport (sortedDictionaryOfTBills: SortedDictionary<string, Investment>) : string =
//        GenerateGenericInvestmentReport.Generate(
//            sortedDictionaryOfTBills,
//            headers,
//            reportTitle,
//            "No T-Bill records found."
//        )
