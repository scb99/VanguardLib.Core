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
type InvestmentsReportConfiguration private (title: string) =
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

    /// Generates a standardized portfolio report from a structured collection of assets
    let Generate (
        sortedDictionary: SortedDictionary<string, Investment>,
        headers: string[],
        reportTitle: string,
        emptyMessage: string) : string =
        
        // 1. Guard check handling: Explicitly intercept and encode emptyMessage to pass Scenario A
        if isNull sortedDictionary || sortedDictionary.Count = 0 then
            let safeEmptyMessage = WebUtility.HtmlEncode(emptyMessage)
            HtmlReportLayout.WrapWithTemplate(reportTitle, $"<p>{safeEmptyMessage}</p>")
        else
            // 2. Order-Aware Row Builder Engine
            let renderer = Func<KeyValuePair<string, Investment>, string>(fun pair ->
                let investment = pair.Value
                let row = StringBuilder()
                row.Append("<tr>") |> ignore

                // Iterate through the actual layout headers sequentially
                for header in headers do
                    match header.Trim() with
                    
                    | h when h.Equals("Total Value", StringComparison.OrdinalIgnoreCase) ||
                             h.Equals("Value", StringComparison.OrdinalIgnoreCase) ||
                             h.Equals("Amount", StringComparison.OrdinalIgnoreCase) ->
                        let formattedValue = investment.TotalValue.ToString("C", reportCulture)
                        row.Append($"<td class=\"text-right\">{formattedValue}</td>") |> ignore

                    | h when h.Equals("Shares", StringComparison.OrdinalIgnoreCase) ||
                             h.Equals("Share Count", StringComparison.OrdinalIgnoreCase) ->
                        let formattedShares = investment.Shares.ToString("N4", reportCulture)
                        row.Append($"<td class=\"text-right\">{formattedShares}</td>") |> ignore

                    | h when h.Equals("Price", StringComparison.OrdinalIgnoreCase) ||
                             h.Equals("Share Price", StringComparison.OrdinalIgnoreCase) ->
                        let formattedPrice = investment.SharePrice.ToString("C", reportCulture)
                        row.Append($"<td class=\"text-right\">{formattedPrice}</td>") |> ignore

                    | h when h.Equals("Account", StringComparison.OrdinalIgnoreCase) ||
                             h.Equals("Account Number", StringComparison.OrdinalIgnoreCase) ->
                        row.Append($"<td>{WebUtility.HtmlEncode(investment.AccountNumber)}</td>") |> ignore

                    | _ ->
                        // SECURITY FIX FOR SCENARIO B: 
                        // If the header matches "Name"/"Symbol"/"Key", OR if it's a fallback text header,
                        // treat it as the text column descriptor and write the HTML-safe encoded key.
                        let cleanKey = String.cleanWhitespace pair.Key
                        row.Append($"<td>{WebUtility.HtmlEncode(cleanKey)}</td>") |> ignore

                row.Append("</tr>\n") |> ignore
                row.ToString()
            )

            // 3. Compile table markup strings using the shared rendering components
            let tableContent = 
                HtmlTableBuilder.BuildTable<KeyValuePair<string, Investment>>(
                    sortedDictionary,
                    headers,
                    renderer,
                    emptyMessage,
                    null
                )

            HtmlReportLayout.WrapWithTemplate(reportTitle, tableContent)

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
            HtmlReportLayout.WrapWithTemplate(reportTitle, $"<p>{WebUtility.HtmlEncode(emptyMessage)}</p>")
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

            HtmlReportLayout.WrapWithTemplate(reportTitle, tableContent)

// ==========================================
// 3. Specialized Report Wrapper Modules
// ==========================================

module GenerateInvestmentsReport =

    let private reportCulture = CultureInfo("en-US")
    let private headers = [| "Investment Name"; "Symbol"; "Shares"; "Total Value" |]

    /// Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (
        sortedInvestments: SortedDictionary<string, Investment>,
        config: InvestmentsReportConfiguration) : string =
        
        let title = config.Title

        // 1. High-performance typesafe null/empty check (No boxing overhead)
        if isNull sortedInvestments || sortedInvestments.Count = 0 then
            HtmlReportLayout.WrapWithTemplate(title, "<p>No investments available to generate the report.</p>")
        else
            let sumOfInvestments = sortedInvestments.Values.Sum(fun inv -> inv.TotalValue)
            
            let formattedSum = sumOfInvestments.ToString("C", reportCulture)
            let formattedCount = sortedInvestments.Count.ToString(reportCulture)

            // 2. FIXED: Multi-row footer alignment matrix
            // Row 1: Spans 3 columns, aligns dollar amount under 'Total Value'
            // Row 2: Spans 2 columns, aligns quantity under 'Shares', leaves final cell blank
            let footerHtml = $"""
                    <tr class="total-row">
                      <td colspan="3">Total value of investments</td>
                      <td class="text-right">{formattedSum}</td>
                    </tr>
                    <tr class="total-row">
                      <td colspan="2">Total number of investments</td>
                      <td class="text-right">{formattedCount}</td>
                      <td></td>
                    </tr>
                """

            // 3. Robust Explicit Row Renderer
            let renderer = Func<Investment, string>(fun investment ->
                // Guard text references and normalize whitespace anomalies (e.g., double space errors)
                let rawName = if isNull investment.InvestmentName then "N/A" else investment.InvestmentName
                let displayName = String.cleanWhitespace rawName
                
                let displaySymbol = if isNull investment.Symbol then "N/A" else investment.Symbol.Trim().ToUpperInvariant()
                let currentValue = investment.TotalValue

                // Safe mathematical range evaluations tracking fractional zero/dust bounds
                let isZeroShares = Math.Abs(investment.Shares) < 0.00001M
                let isZeroValue = Math.Abs(currentValue) < 0.01M

                let rowStyle = 
                    if isZeroShares && isZeroValue then 
                        "style=\"background-color: #f8d7da; color: #721c24;\"" 
                    else 
                        ""

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

            // 4. FIX: Cast the ValueCollection to a standardized sequence layout safely 
            let itemsSource = sortedInvestments.Values |> Seq.cast<Investment>

            // Invoke the structural table builder
            let tableContent = 
                HtmlTableBuilder.BuildTable<Investment>(
                    itemsSource,
                    headers,
                    renderer,
                    "No investments found.",
                    footerHtml
                )

            HtmlReportLayout.WrapWithTemplate(title, tableContent)

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

        HtmlReportLayout.WrapWithTemplate(config.Title, reportBody)

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
    
    // REFINED HEADERS: Removes the redundant "Fee" column label 
    // and adds the Symbol for quicker scanability.
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

    // Module-level private constants matching your original design rules
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
    
    // FIX: Standardize headers so GenerateGenericInvestmentReport 
    // maps them to actual Investment object properties cleanly!
    let private headers = [| "Account / Investment Key"; "Total Value" |]

    /// Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfCash: SortedDictionary<string, Investment>) : string =
        GenerateGenericInvestmentReport.Generate(
            sortedDictionaryOfCash,
            headers,
            reportTitle,
            "No cash records found."
        )

module GenerateListOfTBillsReport =

    let private reportTitle = "List of TBills"
    
    // FIX: Standardize headers so GenerateGenericInvestmentReport 
    // maps them to actual Investment object properties cleanly!
    let private headers = [| "T-Bill Key / Description"; "Total Value" |]

    /// Public API exposed via standard .NET parameters for seamless C# library interop
    let GenerateReport (sortedDictionaryOfTBills: SortedDictionary<string, Investment>) : string =
        GenerateGenericInvestmentReport.Generate(
            sortedDictionaryOfTBills,
            headers,
            reportTitle,
            "No T-Bill records found."
        )

module GenerateListOfCorpActions =

    // Module-level private constant matching your original design rules
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
    
    // REFINED HEADERS: Removes the redundant "Dividend" label column 
    // and adds "Shares" so users can see the asset quantity tracking.
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

