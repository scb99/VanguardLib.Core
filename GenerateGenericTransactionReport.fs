namespace VanguardLib

//open System
//open System.Globalization
//open System.Net
//open System.Text
//open System.Collections.Generic
//open System.Linq
//open System.Runtime.InteropServices
//open VanguardLib.Extensions // Ensures String.cleanWhitespace is available if needed

//module GenerateGenericTransactionReport =

//    let private reportCulture = CultureInfo("en-US")
//    let private defaultHeaders = [| "Type"; "Settlement Date"; "Investment Name"; "Shares"; "Amount" |]

//    /// Public API optimized for high-performance processing and flawless C# consumption
//    let Generate (
//        sortedDictionary: SortedDictionary<string, List<Transaction>>,
//        dictionaryKey: string,
//        reportTitle: string,
//        emptyMessage: string,
//        [<Optional; DefaultParameterValue(null: string[])>] headers: string[],
//        [<Optional; DefaultParameterValue(null: string)>] totalRowLabel: string) : string =
        
//        // 1. High performance typesafe null guard (No boxing)
//        if isNull sortedDictionary then
//            HtmlReportLayout.WrapWithTemplate(reportTitle, $"<p>{WebUtility.HtmlEncode(emptyMessage)}</p>")
//        else
//            // 2. Safe retrieval tracking
//            let transactions = 
//                match sortedDictionary.TryGetValue(dictionaryKey) with
//                | true, list when not (isNull list) -> list
//                | _ -> List<Transaction>()

//            let activeHeaders = if isNull headers then defaultHeaders else headers

//            // 3. Fixed Table Alignment Footer Math
//            let footerHtml = 
//                if not (String.IsNullOrEmpty(totalRowLabel)) && transactions.Count > 0 then
//                    let totalSum = transactions.Sum(fun t -> t.NetAmount)
//                    // Colspan stops exactly 1 column before the final currency amount column
//                    let colspan = activeHeaders.Length - 1
//                    $"""
//                        <tr class="total-row">
//                          <td colspan="{colspan}">{WebUtility.HtmlEncode(totalRowLabel)}</td>
//                          <td class="text-right">{totalSum.ToString("C", reportCulture)}</td>
//                        </tr>
//                    """
//                else
//                    null

//            // 4. FIX: Dynamic Order-Aware HTML Rows Engine
//            // This loops over the actual header elements array sequentially, 
//            // ensuring the body data columns always align 100% with the layout headers.
//            let renderer = Func<Transaction, string>(fun tx ->
//                let row = StringBuilder()
//                row.Append("    <tr>") |> ignore

//                for header in activeHeaders do
//                    match header.Trim() with

//                    | h when h.Equals("Type", StringComparison.OrdinalIgnoreCase) || 
//                             h.Equals("Transaction Type", StringComparison.OrdinalIgnoreCase) ->
//                        row.Append($"<td>{WebUtility.HtmlEncode(tx.TransactionType)}</td>") |> ignore
                        
//                    | h when h.Equals("Settlement Date", StringComparison.OrdinalIgnoreCase) ->
//                        let dateStr = tx.SettlementDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
//                        row.Append($"<td>{dateStr}</td>") |> ignore
                        
//                    | h when h.Equals("Investment Name", StringComparison.OrdinalIgnoreCase) ->
//                        // Clean whitespace errors using your brand-new utility extension!
//                        let rawName = if isNull tx.InvestmentName then "N/A" else tx.InvestmentName
//                        let cleanName = String.cleanWhitespace rawName
//                        row.Append($"<td>{WebUtility.HtmlEncode(cleanName)}</td>") |> ignore
                        
//                    | h when h.Equals("Shares", StringComparison.OrdinalIgnoreCase) ->
//                        let formattedShares = tx.Shares.ToString("N4", reportCulture)
//                        row.Append($"<td class=\"text-right\">{formattedShares}</td>") |> ignore
                        
//                    | h when h.Equals("Amount", StringComparison.OrdinalIgnoreCase) ->
//                        let formattedAmount = tx.NetAmount.ToString("C", reportCulture)
//                        row.Append($"<td class=\"text-right\">{formattedAmount}</td>") |> ignore
                        
//                    | unknownHeader ->
//                        // Defensive catch-all to prevent table row collapsing on unexpected column injections
//                        row.Append($"<td><!-- Unknown Column: {WebUtility.HtmlEncode(unknownHeader)} --></td>") |> ignore

//                row.Append("</tr>\n") |> ignore
//                row.ToString()
//            )

//            // 5. Invoke structural layout output engine
//            let tableContent = 
//                HtmlTableBuilder.BuildTable<Transaction>(
//                    transactions,
//                    activeHeaders,
//                    renderer,
//                    emptyMessage,
//                    footerHtml
//                )

//            HtmlReportLayout.WrapWithTemplate(reportTitle, tableContent)
