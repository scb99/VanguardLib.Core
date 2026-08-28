namespace VanguardLib

open System
open System.Globalization
open System.Net
open System.Collections.Generic

module GenerateGenericInvestmentReport =

    // Private static culture field matching your original definition
    let private reportCulture = CultureInfo("en-US")

    // Public method using tuple arguments for seamless C# library interop
    let Generate (
        sortedDictionary: SortedDictionary<string, Investment>,
        headers: string[],
        reportTitle: string,
        emptyMessage: string) : string =
        
        if box sortedDictionary = null || sortedDictionary.Count = 0 then
            HtmlReportLayout.WrapWithTemplate(reportTitle, $"<p>{WebUtility.HtmlEncode(emptyMessage)}</p>")
        else
            // Explicitly wrapping the F# lambda inside a standard .NET Func delegate
            // to satisfy the signature expected by your HtmlTableBuilder
            let renderer = Func<KeyValuePair<string, Investment>, string>(fun pair ->
                let investment = pair.Value
                
                // Pure conditional matching logic for T-Bill calculations
                let pricePaidForTBillOrTotalValue = 
                    if investment.InvestmentName.Contains("TREASURY BILL") then
                        let tBillValue = (investment.SharePrice * investment.Shares) / 100.0M
                        tBillValue.ToString("C", reportCulture)
                    else
                        investment.TotalValue.ToString("C", reportCulture)

                $"""
                    <tr>
                      <td>{WebUtility.HtmlEncode(pair.Key)}</td>
                      <td class="text-right">{investment.Shares.ToString("N4", reportCulture)}</td>
                      <td class="text-right">{pricePaidForTBillOrTotalValue}</td>
                    </tr>
                """
            )

            // Invoke the tuple-style method using parentheses and commas
            let tableContent = 
                HtmlTableBuilder.BuildTable<KeyValuePair<string, Investment>>(
                    sortedDictionary,
                    headers,
                    renderer,
                    emptyMessage,
                    null
                )

            HtmlReportLayout.WrapWithTemplate(reportTitle, tableContent)
