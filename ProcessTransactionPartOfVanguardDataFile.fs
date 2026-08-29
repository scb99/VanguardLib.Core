namespace VanguardLib

open System
open System.Globalization
open System.Collections.Generic
open VanguardLib.Extensions

module ProcessTransactionsPartOfVanguardDataFile =

    let private retrieveTransactionFromLine (currentLine: string) : ValidationResult<Transaction> =
        if String.IsNullOrWhiteSpace(currentLine) then
            ValidationResult<Transaction>.Fail "Line is null or empty."
        else
            let transactionLine = currentLine.Split(',')

            // FIX: This helper now returns the correct ValidationResult wrapper 
            // and passes exactly 14 arguments matching the TryCreate signature
            let getFallbackTransaction () =
                Transaction.TryCreate(
                    "0", DateOnly.MinValue, DateOnly.MinValue, "Unknown", "Unknown", 
                    "Unknown", "UKN", 0.0M, 0.0M, 0.0M, 0.0M, 0.0M, 0.0M, "CASH"
                )

            if transactionLine.Length < 14 then
                ValidationResult<Transaction>.Fail "Malformed CSV row data column count."
            else
                match transactionLine with

                | [| accountNumber; tradeDate; settlementDate; transactionType; transactionDescription;
                     investmentName; symbol; shares; sharePrice; principalAmount;
                     commissionsAndFees; netAmount; accruedInterest; accountType; _ |] ->

                    Transaction.TryCreate(
                        accountNumber          = accountNumber,
                        tradeDate              = DateOnly.Parse(tradeDate, CultureInfo.InvariantCulture),
                        settlementDate         = DateOnly.Parse(settlementDate, CultureInfo.InvariantCulture),
                        transactionType        = transactionType,
                        transactionDescription = transactionDescription,
                        investmentName         = investmentName,
                        symbol                 = symbol,
                        shares                 = (shares |> String.toCleanDecimal),
                        sharePrice             = (sharePrice |> String.toCleanDecimal),
                        principalAmount        = (principalAmount |> String.toCleanDecimal),
                        commissionAndFees      = (commissionsAndFees |> String.toCleanDecimal),
                        netAmount              = (netAmount |> String.toCleanDecimal),     
                        accruedInterest        = (accruedInterest |> String.toCleanDecimal),
                        accountType            = accountType
                    )

                | _ -> getFallbackTransaction()

    let ProcessData (fileLines: seq<string>, existingTransactions: IReadOnlyDictionary<string, List<Transaction>>) : SortedDictionary<string, List<Transaction>> =
        
        let parsedTransactionsGroupedByType = 
            fileLines
            |> Seq.skip 1
            |> Seq.takeWhile (fun line -> not (String.IsNullOrEmpty(line)))
            |> Seq.map retrieveTransactionFromLine

            |> Seq.choose (function Success value -> Some value | Failure _ -> None)
            |> Seq.groupBy (fun t -> if isNull t.TransactionType then "" else t.TransactionType)

        // 2. Instantiate an empty SortedDictionary
        let outputDictionary = SortedDictionary<string, List<Transaction>>()

        // Clone existing items directly into the sorted dictionary
        for kvp in existingTransactions do
            outputDictionary.Add(kvp.Key, List<Transaction>(kvp.Value))

        // 3. Merge new elements into the dictionary
        for key, group in parsedTransactionsGroupedByType do
            match outputDictionary.TryGetValue(key) with
            | true, targetList -> 
                targetList.AddRange(group)
            | false, _ -> 
                outputDictionary.[key] <- List<Transaction>(group)

        outputDictionary
