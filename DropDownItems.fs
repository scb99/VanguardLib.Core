namespace VanguardLib

/// Defines the canonical set of report labels and their display order for the report dropdown.
/// Report generation itself is handled by ReportsEngine, which reuses these same labels as lookup keys.
type DropDownItems private () =

    static member val InvestmentsByNameLabel = "Investments Sorted By Company Name"
    static member val InvestmentsBySymbolLabel = "Investments Sorted By Company Symbol"
    static member val DividendTransactionsByNameLabel = "Dividend Transactions Sorted By Company Name"
    static member val DividendTransactionsBySymbolLabel = "Dividend Transactions Sorted By Company Symbol"
    static member val ListOfTBillsLabel = "List of T Bills"
    static member val ListOfCashLabel = "List of Cash"
    static member val ListOfDividendsLabel = "List of Dividends"
    static member val ListOfInterestPaymentsLabel = "List of Interest Payments"
    static member val DistributionsLabel = "Distributions"
    static member val ListOfCorpActionsLabel = "List of Corporate Actions"
    static member val ListOfBuyTransactionsLabel = "List of Buy Transactions"
    static member val ListOfSellTransactionsLabel = "List of Sell Transactions"
    static member val ListOfFeesLabel = "List of Fees"
    static member val AllReportsLabel = "All Reports"

    static member ReportOrder = [|
        DropDownItems.InvestmentsByNameLabel
        DropDownItems.InvestmentsBySymbolLabel
        DropDownItems.DividendTransactionsByNameLabel
        DropDownItems.DividendTransactionsBySymbolLabel
        DropDownItems.ListOfTBillsLabel
        DropDownItems.ListOfCashLabel
        DropDownItems.ListOfDividendsLabel
        DropDownItems.ListOfInterestPaymentsLabel
        DropDownItems.DistributionsLabel
        DropDownItems.ListOfCorpActionsLabel
        DropDownItems.ListOfBuyTransactionsLabel
        DropDownItems.ListOfSellTransactionsLabel
        DropDownItems.ListOfFeesLabel
    |]

    /// Explicitly declared as a public static member method
    static member StringsForReportDropDown () =
        Array.append [| DropDownItems.AllReportsLabel |] DropDownItems.ReportOrder
