namespace VanguardLib

open System
open System.Globalization
open System.Net
open System.Text
open System.Collections.Generic

type TransactionsReportConfiguration (title: string) =
    member _.Title = title

    static member ByName = TransactionsReportConfiguration("Dividend Transactions Report by Company Name")
    static member BySymbol = TransactionsReportConfiguration("Dividend Transactions Report by Company Symbol")

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
