namespace VanguardLib

open System
open System.Globalization
open System.Net
open System.Collections.Generic
open System.Linq

/// Expose pre-configured, immutable instances for options
type InvestmentsReportConfiguration private (title: string) =
    member this.Title = title

    static member ByName = InvestmentsReportConfiguration("Investments Report by Investment Company Name")
    static member BySymbol = InvestmentsReportConfiguration("Investments Report by Investment Company Symbol")

module GenerateInvestmentsReport =

    // Private static configuration fields matching your original layout
    let private reportCulture = CultureInfo("en-US")
    let private headers = [| "Investment Name"; "Symbol"; "Shares"; "Total Value" |]

    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
    let GenerateReport (
        sortedInvestments: SortedDictionary<string, Investment>,
        config: InvestmentsReportConfiguration) : string =
        
        let title = config.Title

        if box sortedInvestments = null || sortedInvestments.Count = 0 then
            HtmlReportLayout.WrapWithTemplate(title, "<p>No investments available to generate the report.</p>")
        else
            let sumOfInvestments = sortedInvestments.Values.Sum(fun inv -> inv.TotalValue)
            
            // Format calculated total aggregates into standalone string references to protect interpolation logic
            let formattedSum = sumOfInvestments.ToString("C", reportCulture)
            let formattedCount = sortedInvestments.Count.ToString(reportCulture)

            let footerHtml = $"""
                    <tr class="total-row">
                      <td colspan="3">Total value of investments</td>
                      <td class="text-right">{formattedSum}</td>
                    </tr>
                    <tr class="total-row">
                      <td colspan="3">Total number of investments</td>
                      <td class="text-right">{formattedCount}</td>
                    </tr>
                """

            // Explicitly wrap the row generator in a standard .NET Func delegate
            let renderer = Func<Investment, string>(fun investment ->
                let displayName = if isNull investment.InvestmentName then "N/A" else investment.InvestmentName
                let displaySymbol = if isNull investment.Symbol then "N/A" else investment.Symbol
                let currentValue = investment.TotalValue

                // Safe mathematical range evaluations tracking small fractional zero bounds
                let isZeroShares = Math.Abs(investment.Shares) < 0.00001M
                let isZeroValue = Math.Abs(currentValue) < 0.01M

                let rowStyle = 
                    if isZeroShares && isZeroValue then 
                        "style=\"background-color: #f8d7da; color: #721c24;\"" 
                    else 
                        ""

                // Pre-evaluate column formats to prevent FS3373 string hole exceptions
                let formattedShares = investment.Shares.ToString("N4", reportCulture)
                let formattedValue = currentValue.ToString("C", reportCulture)

                $"""
                     <tr {rowStyle}>
                       <td>{WebUtility.HtmlEncode(displayName)}</td>
                       <td>{WebUtility.HtmlEncode(displaySymbol)}</td>
                       <td class="text-right">{formattedShares}</td>
                       <td class="text-right">{formattedValue}</td>
                     </tr>
                 """
            )

            // Invoke the table builder using parentheses to fulfill the expected tuple signature
            let tableContent = 
                HtmlTableBuilder.BuildTable<Investment>(
                    sortedInvestments.Values,
                    headers,
                    renderer,
                    "No investments found.",
                    footerHtml
                )

            HtmlReportLayout.WrapWithTemplate(title, tableContent)
