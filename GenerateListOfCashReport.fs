namespace VanguardLib

//open System.Collections.Generic

//module GenerateListOfCashReport =

//    let private reportTitle = "List of Cash"
    
//    // FIX: Standardize headers so GenerateGenericInvestmentReport 
//    // maps them to actual Investment object properties cleanly!
//    let private headers = [| "Account / Investment Key"; "Total Value" |]

//    /// Public API exposed via standard .NET parameters for seamless C# library interop
//    let GenerateReport (sortedDictionaryOfCash: SortedDictionary<string, Investment>) : string =
//        GenerateGenericInvestmentReport.Generate(
//            sortedDictionaryOfCash,
//            headers,
//            reportTitle,
//            "No cash records found."
//        )
