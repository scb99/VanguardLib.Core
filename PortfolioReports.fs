namespace VanguardLib

open System
open System.Globalization
open System.Net
open System.Text
open System.Collections.Generic
open System.Linq
open System.Runtime.InteropServices
open VanguardLib.Extensions


// ==========================================
// 1. Shared Configurations & Domain Objects
// ==========================================

/// Expose pre-configured, immutable instances for options
type InvestmentsReportConfiguration (title: string) =
    member this.Title = title

    static member ByName = InvestmentsReportConfiguration("Investments Report by Investment Company Name")
    static member BySymbol = InvestmentsReportConfiguration("Investments Report by Investment Company Symbol")

type TransactionsReportConfiguration (title: string) =
    member _.Title = title

    static member ByName = TransactionsReportConfiguration("Dividend Transactions Report by Company Name")
    static member BySymbol = TransactionsReportConfiguration("Dividend Transactions Report by Company Symbol")

// ==========================================
// 2. Core Generic Engines
// ==========================================

module GenerateGenericInvestmentReport =

    let private reportCulture = CultureInfo("en-US")

    /// Generates a standardized portfolio report from an idiomatic F# Map of assets
    let Generate (
        investmentsMap: Map<string, Investment>,
        headers: string[],
        reportTitle: string,
        emptyMessage: string) : string =
    
        // 1. Guard check handling using Map.isEmpty
        if Map.isEmpty investmentsMap then
            let safeEmptyMessage = WebUtility.HtmlEncode(emptyMessage)
            HtmlReportLayout.wrapWithTemplate reportTitle $"<p>{safeEmptyMessage}</p>"
        else
            // 2. Idiomatic Row Builder Engine using a clean pattern-matching function
            let renderRow (key: string, investment: Investment) =
                let row = StringBuilder()
                row.Append("<tr>") |> ignore

                // Iterate through the actual layout headers sequentially
                for header in headers do
                    match header.Trim() with
                
                    | h when String.Equals(h, "Total Value", StringComparison.OrdinalIgnoreCase) ||
                             String.Equals(h, "Value", StringComparison.OrdinalIgnoreCase) ||
                             String.Equals(h, "Amount", StringComparison.OrdinalIgnoreCase) ->
                        let formattedValue = investment.TotalValue.ToString("C", reportCulture)
                        row.Append($"<td class=\"text-right\">{formattedValue}</td>") |> ignore

                    | h when String.Equals(h, "Shares", StringComparison.OrdinalIgnoreCase) ||
                             String.Equals(h, "Share Count", StringComparison.OrdinalIgnoreCase) ->
                        let formattedShares = investment.Shares.ToString("N4", reportCulture)
                        row.Append($"<td class=\"text-right\">{formattedShares}</td>") |> ignore

                    | h when String.Equals(h, "Price", StringComparison.OrdinalIgnoreCase) ||
                             String.Equals(h, "Share Price", StringComparison.OrdinalIgnoreCase) ->
                        let formattedPrice = investment.SharePrice.ToString("C", reportCulture)
                        row.Append($"<td class=\"text-right\">{formattedPrice}</td>") |> ignore

                    | h when String.Equals(h, "Account", StringComparison.OrdinalIgnoreCase) ||
                             String.Equals(h, "Account Number", StringComparison.OrdinalIgnoreCase) ->
                        row.Append($"<td>{WebUtility.HtmlEncode(investment.AccountNumber)}</td>") |> ignore

                    | _ ->
                        // Treat as text column descriptor and write the HTML-safe encoded key
                        let cleanKey = String.cleanWhitespace key
                        row.Append($"<td>{WebUtility.HtmlEncode(cleanKey)}</td>") |> ignore

                row.Append("</tr>\n") |> ignore
                row.ToString()

            // 3. Adapt F# Map to your existing HtmlTableBuilder.
            // F# Maps implement seq<KeyValuePair<'K, 'V>> under the hood, but your renderRow expects a tuple.
            // We can pass a proxy delegate or convert the map elements.
            let csharpRenderer = Func<KeyValuePair<string, Investment>, string>(fun kvp -> 
                renderRow (kvp.Key, kvp.Value)
            )

            let tableContent = 
                HtmlTableBuilder.BuildTable<KeyValuePair<string, Investment>>(
                    investmentsMap, // Map naturally implements the required sequence interface
                    headers,
                    csharpRenderer,
                    emptyMessage,
                    null
                )

            HtmlReportLayout.wrapWithTemplate reportTitle tableContent

module GenerateGenericTransactionReport =

    let private reportCulture = CultureInfo("en-US")
    let private defaultHeaders = [| "Type"; "Settlement Date"; "Investment Name"; "Shares"; "Amount" |]

    /// Public API optimized for high-performance processing and flawless C# consumption
    let Generate (
        sortedDictionary: SortedDictionary<string, List<Transaction>>,
        dictionaryKey: string,
        reportTitle: string,
        emptyMessage: string,
        [<Optional; DefaultParameterValue(null: string[])>] headers: string[],
        [<Optional; DefaultParameterValue(null: string)>] totalRowLabel: string) : string =
        
        // 1. High performance typesafe null guard (No boxing)
        if isNull sortedDictionary then
            HtmlReportLayout.wrapWithTemplate reportTitle $"<p>{WebUtility.HtmlEncode(emptyMessage)}</p>"
        else
            // 2. Safe retrieval tracking
            let transactions = 
                match sortedDictionary.TryGetValue(dictionaryKey) with
                | true, list when not (isNull list) -> list
                | _ -> List<Transaction>()

            let activeHeaders = if isNull headers then defaultHeaders else headers

            // 3. Fixed Table Alignment Footer Math
            let footerHtml = 
                if not (String.IsNullOrEmpty(totalRowLabel)) && transactions.Count > 0 then
                    let totalSum = transactions.Sum(fun t -> t.NetAmount)
                    // Colspan stops exactly 1 column before the final currency amount column
                    let colspan = activeHeaders.Length - 1
                    $"""
                        <tr class="total-row">
                          <td colspan="{colspan}">{WebUtility.HtmlEncode(totalRowLabel)}</td>
                          <td class="text-right">{totalSum.ToString("C", reportCulture)}</td>
                        </tr>
                    """
                else
                    null

            // 4. FIX: Dynamic Order-Aware HTML Rows Engine
            // This loops over the actual header elements array sequentially, 
            // ensuring the body data columns always align 100% with the layout headers.
            let renderer = Func<Transaction, string>(fun tx ->
                let row = StringBuilder()
                row.Append("    <tr>") |> ignore

                for header in activeHeaders do
                    match header.Trim() with

                    | h when h.Equals("Type", StringComparison.OrdinalIgnoreCase) || 
                             h.Equals("Transaction Type", StringComparison.OrdinalIgnoreCase) ->
                        row.Append($"<td>{WebUtility.HtmlEncode(tx.TransactionType)}</td>") |> ignore
                        
                    | h when h.Equals("Settlement Date", StringComparison.OrdinalIgnoreCase) ->
                        let dateStr = tx.SettlementDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        row.Append($"<td>{dateStr}</td>") |> ignore
                        
                    | h when h.Equals("Investment Name", StringComparison.OrdinalIgnoreCase) ->
                        // Clean whitespace errors using your brand-new utility extension!
                        let rawName = if isNull tx.InvestmentName then "N/A" else tx.InvestmentName
                        let cleanName = String.cleanWhitespace rawName
                        row.Append($"<td>{WebUtility.HtmlEncode(cleanName)}</td>") |> ignore
                        
                    | h when h.Equals("Shares", StringComparison.OrdinalIgnoreCase) ->
                        let formattedShares = tx.Shares.ToString("N4", reportCulture)
                        row.Append($"<td class=\"text-right\">{formattedShares}</td>") |> ignore
                        
                    | h when h.Equals("Amount", StringComparison.OrdinalIgnoreCase) ->
                        let formattedAmount = tx.NetAmount.ToString("C", reportCulture)
                        row.Append($"<td class=\"text-right\">{formattedAmount}</td>") |> ignore
                        
                    | unknownHeader ->
                        // Defensive catch-all to prevent table row collapsing on unexpected column injections
                        row.Append($"<td><!-- Unknown Column: {WebUtility.HtmlEncode(unknownHeader)} --></td>") |> ignore

                row.Append("</tr>\n") |> ignore
                row.ToString()
            )

            // 5. Invoke structural layout output engine
            let tableContent = 
                HtmlTableBuilder.BuildTable<Transaction>(
                    transactions,
                    activeHeaders,
                    renderer,
                    emptyMessage,
                    footerHtml
                )

            HtmlReportLayout.wrapWithTemplate reportTitle tableContent

// ==========================================
// 3. Specialized Report Wrapper Modules
// ==========================================

module GenerateInvestmentsReport =

    let private reportCulture = CultureInfo("en-US")
    let private headers = [| "Investment Name"; "Symbol"; "Shares"; "Total Value" |]

    /// Inline formatting helpers to keep the renderer clean and atomic
    let private formatCurrency (v: decimal) = v.ToString("C", reportCulture)
    let private formatCount (v: int) = v.ToString(reportCulture)
    let private formatShares (v: decimal) = v.ToString("N4", reportCulture)

    /// Renders a single typesafe row layout for an investment item
    let private renderRow (inv: Investment) =
        let displayName = if isNull inv.InvestmentName then "N/A" else String.cleanWhitespace inv.InvestmentName
        let displaySymbol = if isNull inv.Symbol then "N/A" else inv.Symbol.Trim().ToUpperInvariant()

        // Pure boolean flags tracking close-to-zero decimal dust boundaries
        let isZeroShares = Math.Abs(inv.Shares) < 0.00001M
        let isZeroValue = Math.Abs(inv.TotalValue) < 0.01M
        let rowStyle = if isZeroShares && isZeroValue then "style=\"background-color: #f8d7da; color: #721c24;\"" else ""

        $"""
         <tr {rowStyle}>
           <td>{WebUtility.HtmlEncode(displayName)}</td>
           <td>{WebUtility.HtmlEncode(displaySymbol)}</td>
           <td class="text-right">{formatShares inv.Shares}</td>
           <td class="text-right">{formatCurrency inv.TotalValue}</td>
         </tr>
         """

    /// Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (sortedInvestments: SortedDictionary<string, Investment>, config: InvestmentsReportConfiguration) : string =
        let title = config.Title

        if isNull sortedInvestments || sortedInvestments.Count = 0 then
            HtmlReportLayout.wrapWithTemplate title "<p>No investments available to generate the report.</p>"
        else
            // 1. Structural aggregate mapping using modern native functional sequence metrics
            let totalValue = sortedInvestments.Values |> Seq.sumBy (fun inv -> inv.TotalValue)

            // 2. High-readability alignment matrices (Shifted out from complex scopes)
            let footerHtml = $"""
                    <tr class="total-row">
                      <td colspan="3">Total value of investments</td>
                      <td class="text-right">{formatCurrency totalValue}</td>
                    </tr>
                    <tr class="total-row">
                      <td colspan="2">Total number of investments</td>
                      <td class="text-right">{formatCount sortedInvestments.Count}</td>
                      <td></td>
                    </tr>
                """

            // 3. Construct the HTML core directly via native typesafe delegate evaluation
            let tableContent = 
                HtmlTableBuilder.BuildTable<Investment>(
                    sortedInvestments.Values, // No Seq.cast needed; Values already implements IEnumerable<Investment>
                    headers,
                    Func<Investment, string>(renderRow),
                    "No investments found.",
                    footerHtml
                )

            HtmlReportLayout.wrapWithTemplate title tableContent

module GenerateDividendTransactionsReport =

    let private reportCulture = CultureInfo("en-US")

    /// Build a robust bidirectional map using Symbol as the ultimate source of truth,
    /// then route each dividend transaction into the correct group.
    let private prepareSortedDictionaryOfDividendTransactions
        (sortedDictionaryOfInvestments: SortedDictionary<string, Investment>)
        (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>)
        (sortedDictionaryOfDividendTransactions: SortedDictionary<string, List<Transaction>>) =

        if isNull (box sortedDictionaryOfInvestments) || isNull (box sortedDictionaryOfTransactions) then
            ()
        else
            let symbolToMasterNameMap = Dictionary<string, string>()
            for inv in sortedDictionaryOfInvestments.Values do
                if not (String.IsNullOrEmpty inv.Symbol) && not (String.IsNullOrEmpty inv.InvestmentName) then
                    symbolToMasterNameMap.TryAdd(inv.Symbol, inv.InvestmentName) |> ignore

            // Pre-populate report groups using the Master Portfolio names
            for investment in sortedDictionaryOfInvestments.Values do
                if not (String.IsNullOrEmpty investment.InvestmentName) then
                    sortedDictionaryOfDividendTransactions.TryAdd(investment.InvestmentName, List<Transaction>()) |> ignore

            // Scan and route transactions
            for transactionList in sortedDictionaryOfTransactions do
                for transaction in transactionList.Value do
                    if transaction.TransactionType = "Dividend" && not (String.IsNullOrEmpty transaction.Symbol) then
                        let unifiedName =
                            match symbolToMasterNameMap.TryGetValue(transaction.Symbol) with
                            | true, name -> name
                            | false, _ ->
                                if isNull (box transaction.InvestmentName) then transaction.Symbol
                                else transaction.InvestmentName

                        // If this is a completely brand new asset not covered by seeding, initialize it silently
                        if sortedDictionaryOfDividendTransactions.TryAdd(unifiedName, List<Transaction>()) then
                            let firstKey = sortedDictionaryOfInvestments.Keys |> Seq.tryHead
                            let investmentKey =
                                match firstKey with
                                | None -> transaction.Symbol
                                | Some key when key.Length <= 5 -> transaction.Symbol
                                | Some _ -> unifiedName

                            let result = Investment.TryCreate("0", unifiedName, transaction.Symbol, 0.0m, 0.0m, 0.0m)
                            match result with
                            | Success value ->
                                sortedDictionaryOfInvestments.TryAdd(investmentKey, value) |> ignore
                                symbolToMasterNameMap.TryAdd(transaction.Symbol, unifiedName) |> ignore
                            | Failure _ -> ()

                        // Route transaction to the verified unified name bucket
                        sortedDictionaryOfDividendTransactions.[unifiedName].Add(transaction)

    let private buildHtmlReport
        (sortedDictionaryOfInvestments: SortedDictionary<string, Investment>)
        (sortedDictionaryOfDividendTransactions: SortedDictionary<string, List<Transaction>>)
        (config: TransactionsReportConfiguration) =

        let html = StringBuilder()

        // Filter out empty transaction lists
        let mutable activeGroups =
            sortedDictionaryOfDividendTransactions
            |> Seq.filter (fun t -> t.Value.Count > 0)
            |> Seq.toList

        // Helper function to resolve an investment object by searching both keys and internal values
        let findInvestment (groupKey: string) : Investment option =
            match sortedDictionaryOfInvestments.TryGetValue(groupKey) with
            | true, investment -> Some investment
            | false, _ ->
                sortedDictionaryOfInvestments.Values
                |> Seq.tryFind (fun inv -> inv.InvestmentName = groupKey || inv.Symbol = groupKey)

        // Force correct sorting order before generating HTML
        activeGroups <-
            if config.Title = TransactionsReportConfiguration.BySymbol.Title then
                activeGroups |> List.sortBy (fun g ->
                    match findInvestment g.Key with
                    | Some inv when not (isNull (box inv.Symbol)) -> inv.Symbol
                    | _ -> g.Key)
            else
                activeGroups |> List.sortBy (fun g -> g.Key)

        if activeGroups.Length > 0 then
            for transactionGroup in activeGroups do
                match findInvestment transactionGroup.Key with
                | None ->
                    html.AppendLine(
                        sprintf "<h3 class=\"section-header\" style=\"color: #c0392b;\">NAME NOT FOUND: %s</h3>"
                            (WebUtility.HtmlEncode(transactionGroup.Key))) |> ignore
                | Some investment ->
                    let displaySymbol = if isNull (box investment.Symbol) then "N/A" else investment.Symbol
                    let displayName = if isNull (box investment.InvestmentName) then transactionGroup.Key else investment.InvestmentName

                    let sectionTitle =
                        if config.Title = TransactionsReportConfiguration.BySymbol.Title then
                            sprintf "%s : %s" displaySymbol displayName
                        else
                            sprintf "%s : %s" displayName displaySymbol

                    let headers = [| "Trade Date"; "Transaction Type"; "Symbol"; "Principal Amount" |]

                    let rowRenderer (tx: Transaction) =
                        sprintf
                            "<tr>\n  <td>%s</td>\n  <td>%s</td>\n  <td>%s</td>\n  <td class=\"text-right\">%s</td>\n</tr>\n"
                            (tx.TradeDate.ToString("yyyy-MM-dd"))
                            tx.TransactionType
                            (if isNull (box tx.Symbol) then "N/A" else tx.Symbol)
                            ((decimal tx.PrincipalAmount).ToString("C", reportCulture))

                    let footerHtml =
                        sprintf
                            "<tr class=\"total-row\">\n  <td colspan=\"3\">Subtotal for %s</td>\n  <td class=\"text-right\">%s</td>\n</tr>\n"
                            (WebUtility.HtmlEncode(displayName))
                            ((transactionGroup.Value |> Seq.sumBy (fun tx -> decimal tx.PrincipalAmount)).ToString("C", reportCulture))

                    // Delegate the core table grid generation to the utility class
                    let tableHtml =
                        HtmlTableBuilder.BuildTable(
                            transactionGroup.Value,
                            headers,
                            Func<Transaction, string>(rowRenderer),
                            "No transactions.",
                            footerHtml)

                    // Append the formatted section title followed by the generated table code
                    html.AppendLine(sprintf "<h3 class=\"section-header\">%s</h3>" (WebUtility.HtmlEncode(sectionTitle))) |> ignore
                    html.AppendLine(tableHtml: string) |> ignore

            let sumOfDividends =
                sortedDictionaryOfDividendTransactions
                |> Seq.collect (fun kvp -> kvp.Value)
                |> Seq.sumBy (fun transaction -> decimal transaction.PrincipalAmount)

            // Render the final Grand Total table layout using the HtmlTableBuilder
            let grandTotalTableHtml =
                HtmlTableBuilder.BuildTable(
                    [ sumOfDividends ],
                    [||],
                    Func<decimal, string>(fun total ->
                        sprintf
                            "<tr class=\"total-row\">\n  <td style=\"min-width: 430px;\">Total dividends received:</td>\n  <td class=\"text-right\">%s</td>\n</tr>\n"
                            (total.ToString("C", reportCulture))),
                    "No data available.",
                    null)

            html.AppendLine(grandTotalTableHtml: string) |> ignore
        else
            html.AppendLine("<p>No active dividend transactions found matching company names.</p>") |> ignore

        html.ToString()

    let GenerateReport
        (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>,
         sortedDictionaryOfInvestments: SortedDictionary<string, Investment>,
         config: TransactionsReportConfiguration) : string =

        if isNull (box sortedDictionaryOfTransactions) then
            nullArg "sortedDictionaryOfTransactions"
        if isNull (box sortedDictionaryOfInvestments) then
            nullArg "sortedDictionaryOfInvestments"

        let sortedDictionaryOfDividendTransactions = SortedDictionary<string, List<Transaction>>()

        prepareSortedDictionaryOfDividendTransactions
            sortedDictionaryOfInvestments
            sortedDictionaryOfTransactions
            sortedDictionaryOfDividendTransactions

        let reportBody =
            buildHtmlReport
                sortedDictionaryOfInvestments
                sortedDictionaryOfDividendTransactions
                config

        HtmlReportLayout.wrapWithTemplate config.Title reportBody

module GenerateDistributionsReport =

    let private reportTitle = "Distributions"
    let private headers = [| "Settlement Date"; "Amount" |]

    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        // Pass arguments strictly by position to eliminate the FS0001 tuple error
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Distribution",
            reportTitle,
            "No distributions found.",
            headers,
            "Total amount of distributions"
        )

module GenerateListOfBuyTransactions =

    /// Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Buy",
            "List of Buy Transactions",
            "No buy transactions found.",
            null, // Reuses default layout: Type, Date, Name, Shares, Amount
            "Total Amount Deployed" // Provides explicit alignment layout for the footer sum
        )

module GenerateListOfSellTransactions =

    // Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Sell",
            "List of Sell Transactions",
            "No sell transactions found.",
            null,                       // Reuses default headers layout
            "Total Capital Realized"    // Computes and renders cumulative sell totals
        )

module GenerateListOfFees =

    let private reportTitle = "List of Fees"
    let private headers = [| "Settlement Date"; "Investment Name"; "Symbol"; "Amount" |]

    /// Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Fee",
            reportTitle,
            "No fee transactions found.",
            headers,
            "Total fees paid"
        )

module GenerateListOfInterestPayments =

    let private reportTitle = "List of Interest Payments"
    let private headers = [| "Settlement Date"; "Investment Name"; "Amount" |]

    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        // Pass arguments strictly by position to eliminate the FS0001 tuple error
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Interest",
            reportTitle,
            "No interest payment transactions found.",
            headers,
            "Total interest payments received"
        )

module GenerateListOfCashReport =

    let private reportTitle = "List of Cash"
    let private headers = [| "Account / Investment Key"; "Total Value" |]

    /// Public API accepting an idiomatic F# Map instead of a SortedDictionary
    let GenerateReport (cashMap: Map<string, Investment>) : string =
        GenerateGenericInvestmentReport.Generate(
            cashMap,
            headers,
            reportTitle,
            "No cash records found."
        )

module GenerateListOfTBillsReport =

    let private reportTitle = "List of TBills"
    let private headers = [| "T-Bill Key / Description"; "Total Value" |]

    /// Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (tBillsMap: Map<string, Investment>) : string =
        GenerateGenericInvestmentReport.Generate(
            tBillsMap,
            headers,
            reportTitle,
            "No T-Bill records found."
        )


module GenerateListOfCorpActions =

    let private reportTitle = "List of Corporate Actions"

    // Public API exposed via standard .NET tuple parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        // Pass arguments strictly by position and supply trailing nulls to clear the FS0001 error
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Corp Action (Redemption)",
            reportTitle,
            "No corporate action transactions found.",
            null,
            null
        )

module GenerateListOfDividendsReport =

    let private reportTitle = "List of Dividends"
    let private headers = [| "Settlement Date"; "Investment Name"; "Shares"; "Amount" |]

    /// Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTransactions: SortedDictionary<string, List<Transaction>>) : string =
        GenerateGenericTransactionReport.Generate(
            sortedDictionaryOfTransactions,
            "Dividend",
            reportTitle,
            "No dividend transactions found.",
            headers,
            "Total dividends received"
        )
