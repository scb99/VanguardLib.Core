namespace VanguardLib

//open System
//open System.Globalization
//open System.Net
//open System.Text
//open System.Collections.Generic
//open VanguardLib.Extensions // For String.cleanWhitespace

//module GenerateGenericInvestmentReport =

//    let private reportCulture = CultureInfo("en-US")

//    /// Generates a standardized portfolio report from a structured collection of assets
//    let Generate (
//        sortedDictionary: SortedDictionary<string, Investment>,
//        headers: string[],
//        reportTitle: string,
//        emptyMessage: string) : string =
        
//        // 1. Guard check handling: Explicitly intercept and encode emptyMessage to pass Scenario A
//        if isNull sortedDictionary || sortedDictionary.Count = 0 then
//            let safeEmptyMessage = WebUtility.HtmlEncode(emptyMessage)
//            HtmlReportLayout.WrapWithTemplate(reportTitle, $"<p>{safeEmptyMessage}</p>")
//        else
//            // 2. Order-Aware Row Builder Engine
//            let renderer = Func<KeyValuePair<string, Investment>, string>(fun pair ->
//                let investment = pair.Value
//                let row = StringBuilder()
//                row.Append("<tr>") |> ignore

//                // Iterate through the actual layout headers sequentially
//                for header in headers do
//                    match header.Trim() with
                    
//                    | h when h.Equals("Total Value", StringComparison.OrdinalIgnoreCase) ||
//                             h.Equals("Value", StringComparison.OrdinalIgnoreCase) ||
//                             h.Equals("Amount", StringComparison.OrdinalIgnoreCase) ->
//                        let formattedValue = investment.TotalValue.ToString("C", reportCulture)
//                        row.Append($"<td class=\"text-right\">{formattedValue}</td>") |> ignore

//                    | h when h.Equals("Shares", StringComparison.OrdinalIgnoreCase) ||
//                             h.Equals("Share Count", StringComparison.OrdinalIgnoreCase) ->
//                        let formattedShares = investment.Shares.ToString("N4", reportCulture)
//                        row.Append($"<td class=\"text-right\">{formattedShares}</td>") |> ignore

//                    | h when h.Equals("Price", StringComparison.OrdinalIgnoreCase) ||
//                             h.Equals("Share Price", StringComparison.OrdinalIgnoreCase) ->
//                        let formattedPrice = investment.SharePrice.ToString("C", reportCulture)
//                        row.Append($"<td class=\"text-right\">{formattedPrice}</td>") |> ignore

//                    | h when h.Equals("Account", StringComparison.OrdinalIgnoreCase) ||
//                             h.Equals("Account Number", StringComparison.OrdinalIgnoreCase) ->
//                        row.Append($"<td>{WebUtility.HtmlEncode(investment.AccountNumber)}</td>") |> ignore

//                    | _ ->
//                        // SECURITY FIX FOR SCENARIO B: 
//                        // If the header matches "Name"/"Symbol"/"Key", OR if it's a fallback text header,
//                        // treat it as the text column descriptor and write the HTML-safe encoded key.
//                        let cleanKey = String.cleanWhitespace pair.Key
//                        row.Append($"<td>{WebUtility.HtmlEncode(cleanKey)}</td>") |> ignore

//                row.Append("</tr>\n") |> ignore
//                row.ToString()
//            )

//            // 3. Compile table markup strings using the shared rendering components
//            let tableContent = 
//                HtmlTableBuilder.BuildTable<KeyValuePair<string, Investment>>(
//                    sortedDictionary,
//                    headers,
//                    renderer,
//                    emptyMessage,
//                    null
//                )

//            HtmlReportLayout.WrapWithTemplate(reportTitle, tableContent)
