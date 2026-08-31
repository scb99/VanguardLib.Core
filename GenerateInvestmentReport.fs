namespace VanguardLib

//open System
//open System.Globalization
//open System.Net
//open System.Collections.Generic
//open System.Linq
//open VanguardLib.Extensions // Imports String.cleanWhitespace

///// Expose pre-configured, immutable instances for options
//type InvestmentsReportConfiguration private (title: string) =
//    member this.Title = title

//    static member ByName = InvestmentsReportConfiguration("Investments Report by Investment Company Name")
//    static member BySymbol = InvestmentsReportConfiguration("Investments Report by Investment Company Symbol")

//module GenerateInvestmentsReport =

//    let private reportCulture = CultureInfo("en-US")
//    let private headers = [| "Investment Name"; "Symbol"; "Shares"; "Total Value" |]

//    /// Public API exposed via standard .NET parameters for seamless C# library interop
//    let GenerateReport (
//        sortedInvestments: SortedDictionary<string, Investment>,
//        config: InvestmentsReportConfiguration) : string =
        
//        let title = config.Title

//        // 1. High-performance typesafe null/empty check (No boxing overhead)
//        if isNull sortedInvestments || sortedInvestments.Count = 0 then
//            HtmlReportLayout.WrapWithTemplate(title, "<p>No investments available to generate the report.</p>")
//        else
//            let sumOfInvestments = sortedInvestments.Values.Sum(fun inv -> inv.TotalValue)
            
//            let formattedSum = sumOfInvestments.ToString("C", reportCulture)
//            let formattedCount = sortedInvestments.Count.ToString(reportCulture)

//            // 2. FIXED: Multi-row footer alignment matrix
//            // Row 1: Spans 3 columns, aligns dollar amount under 'Total Value'
//            // Row 2: Spans 2 columns, aligns quantity under 'Shares', leaves final cell blank
//            let footerHtml = $"""
//                    <tr class="total-row">
//                      <td colspan="3">Total value of investments</td>
//                      <td class="text-right">{formattedSum}</td>
//                    </tr>
//                    <tr class="total-row">
//                      <td colspan="2">Total number of investments</td>
//                      <td class="text-right">{formattedCount}</td>
//                      <td></td>
//                    </tr>
//                """

//            // 3. Robust Explicit Row Renderer
//            let renderer = Func<Investment, string>(fun investment ->
//                // Guard text references and normalize whitespace anomalies (e.g., double space errors)
//                let rawName = if isNull investment.InvestmentName then "N/A" else investment.InvestmentName
//                let displayName = String.cleanWhitespace rawName
                
//                let displaySymbol = if isNull investment.Symbol then "N/A" else investment.Symbol.Trim().ToUpperInvariant()
//                let currentValue = investment.TotalValue

//                // Safe mathematical range evaluations tracking fractional zero/dust bounds
//                let isZeroShares = Math.Abs(investment.Shares) < 0.00001M
//                let isZeroValue = Math.Abs(currentValue) < 0.01M

//                let rowStyle = 
//                    if isZeroShares && isZeroValue then 
//                        "style=\"background-color: #f8d7da; color: #721c24;\"" 
//                    else 
//                        ""

//                let formattedShares = investment.Shares.ToString("N4", reportCulture)
//                let formattedValue = currentValue.ToString("C", reportCulture)

//                $"""
//                     <tr {rowStyle}>
//                       <td>{WebUtility.HtmlEncode(displayName)}</td>
//                       <td>{WebUtility.HtmlEncode(displaySymbol)}</td>
//                       <td class="text-right">{formattedShares}</td>
//                       <td class="text-right">{formattedValue}</td>
//                     </tr>
//                 """
//            )

//            // 4. FIX: Cast the ValueCollection to a standardized sequence layout safely 
//            let itemsSource = sortedInvestments.Values |> Seq.cast<Investment>

//            // Invoke the structural table builder
//            let tableContent = 
//                HtmlTableBuilder.BuildTable<Investment>(
//                    itemsSource,
//                    headers,
//                    renderer,
//                    "No investments found.",
//                    footerHtml
//                )

//            HtmlReportLayout.WrapWithTemplate(title, tableContent)

