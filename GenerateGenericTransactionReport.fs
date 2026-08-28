namespace VanguardLib

open System
open System.Globalization
open System.Net
open System.Text
open System.Collections.Generic
open System.Linq
open System.Runtime.InteropServices

module GenerateGenericTransactionReport =

    // Private static configuration fields matching your original setup
    let private reportCulture = CultureInfo("en-US")
    let private defaultHeaders = [| "Type"; "Settlement Date"; "Investment Name"; "Shares"; "Amount" |]

    // Public API using standard .NET tuple arguments and optional attributes for C# interop
    let Generate (
        sortedDictionary: SortedDictionary<string, List<Transaction>>,
        dictionaryKey: string,
        reportTitle: string,
        emptyMessage: string,
        [<Optional; DefaultParameterValue(null: string[])>] headers: string[],
        [<Optional; DefaultParameterValue(null: string)>] totalRowLabel: string) : string =
        
        if box sortedDictionary = null then
            HtmlReportLayout.WrapWithTemplate(reportTitle, "<p>No transactions available to generate the report.</p>")
        else
            // Safe key lookup matching C# TryGetValue and null-coalescing layout
            let transactions = 
                match sortedDictionary.TryGetValue(dictionaryKey) with
                | true, list when box list <> null -> list
                | _ -> List<Transaction>()

            let activeHeaders = if box headers = null then defaultHeaders else headers

            // High-performance HashSet lookup configuration
            let headerLookup = HashSet<string>(activeHeaders, StringComparer.OrdinalIgnoreCase)

            // Calculate footer content conditionally based on presence of totalRowLabel and records
            let footerHtml = 
                if not (String.IsNullOrEmpty(totalRowLabel)) && transactions.Count > 0 then
                    let totalSum = transactions.Sum(fun t -> t.NetAmount)
                    let colspan = activeHeaders.Length - 1
                    $"""
                        <tr class="total-row">
                          <td colspan="{colspan}">{WebUtility.HtmlEncode(totalRowLabel)}</td>
                          <td class="text-right">{totalSum.ToString("C", reportCulture)}</td>
                        </tr>
                    """
                else
                    null

            // Explicitly wrapping the row builder engine in a standard .NET Func delegate
            let renderer = Func<Transaction, string>(fun tx ->
                let row = StringBuilder()
                row.Append("    <tr>") |> ignore

                if headerLookup.Contains("Transaction Type") || headerLookup.Contains("Type") then
                    row.Append($"<td>{WebUtility.HtmlEncode(tx.TransactionType)}</td>") |> ignore

                if headerLookup.Contains("Settlement Date") then
                    let dateStr = tx.SettlementDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    row.Append($"<td>{dateStr}</td>") |> ignore

                if headerLookup.Contains("Investment Name") then
                    let name = if isNull tx.InvestmentName then "N/A" else tx.InvestmentName
                    row.Append($"<td>{WebUtility.HtmlEncode(name)}</td>") |> ignore

                if headerLookup.Contains("Shares") then
                    let formattedShares = tx.Shares.ToString("N4", reportCulture)
                    row.Append($"<td class=\"text-right\">{formattedShares}</td>") |> ignore

                if headerLookup.Contains("Amount") then
                    let formattedAmount = tx.NetAmount.ToString("C", reportCulture)
                    row.Append($"<td class=\"text-right\">{formattedAmount}</td>") |> ignore

                row.Append("</tr>\n") |> ignore
                row.ToString()
            )

            // Invoke the table builder cleanly using the required explicit tuple structure
            let tableContent = 
                HtmlTableBuilder.BuildTable<Transaction>(
                    transactions,
                    activeHeaders,
                    renderer,
                    emptyMessage,
                    footerHtml
                )

            HtmlReportLayout.WrapWithTemplate(reportTitle, tableContent)
