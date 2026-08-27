namespace VanguardLib

open System
open System.Collections.Generic
open VanguardLib.Extensions

type VanguardProcessedData = {
    InvestmentsByCompanySymbol : SortedDictionary<string, Investment>
    InvestmentsByCompanyName   : SortedDictionary<string, Investment>
    TBills                     : SortedDictionary<string, Investment>
    Cash                       : SortedDictionary<string, Investment>
}

module ProcessInvestmentsPartOfVanguardDataFile =

    let private toSortedDictionary (keySelector: 'T -> 'Key) (valueSelector: 'T -> 'Value) (source: seq<'T>) : SortedDictionary<'Key, 'Value> =
        let dict = SortedDictionary<'Key, 'Value>()
        for item in source do
            dict.TryAdd(keySelector item, valueSelector item) |> ignore
        dict

    let private parseInvestment (currentLine: string) : Investment =
        let investmentLine = currentLine.Split(',')

        let getFallbackInvestment () =
            match Investment.TryCreate("0", "Unknown", "UKN", 0.0M, 0.0M, 0.0M) with
            | Success value -> value
            | Failure _     -> failwith "Fallback initialization failed"

        if investmentLine.Length < 6 then
            getFallbackInvestment()
        else
            // Using F# slice pattern matching (.. ) to handle 6 or more items safely
            match investmentLine with

            | [| accNum; name; sym; sharesStr; priceStr; totalValueStr; _ |] -> 
                
                let result = Investment.TryCreate(
                    accountNumber  = accNum,
                    investmentName = name,
                    symbol         = sym,
                    shares         = (sharesStr |> String.toCleanDecimal),
                    sharePrice     = (priceStr |> String.toCleanDecimal),
                    totalValue     = (totalValueStr |> String.toCleanDecimal)
                )

                match result with
                | Success value -> value
                | Failure _     -> getFallbackInvestment()

            | _ -> getFallbackInvestment()

    let ProcessData (fileLines: seq<string>) : VanguardProcessedData =
        
        let investments = 
            fileLines
            |> Seq.skip 1
            |> Seq.takeWhile (fun line -> not (String.IsNullOrWhiteSpace(line)))
            |> Seq.map parseInvestment
            |> Seq.filter (fun inv -> box inv <> null && box inv.Symbol <> null)
            |> Seq.toList

        let investmentsByCompanySymbol = 
            investments
            |> Seq.filter (fun inv -> inv.Symbol <> "NULL")
            |> toSortedDictionary (fun inv -> inv.Symbol.TrimEnd()) id

        let investmentsByCompanyName = 
            investmentsByCompanySymbol.Values
            |> Seq.filter (fun inv -> box inv.InvestmentName <> null)
            |> Seq.groupBy (fun inv -> inv.InvestmentName)
            |> toSortedDictionary fst (fun (_, group) -> Seq.head group)

        let tBills = 
            investments
            |> Seq.filter (fun inv -> 
                inv.Symbol = "NULL" && 
                box inv.InvestmentName <> null && 
                inv.InvestmentName.Contains("TREASURY", StringComparison.CurrentCulture))
            |> toSortedDictionary (fun inv -> inv.InvestmentName) id

        let cash = 
            investments
            |> Seq.filter (fun inv -> 
                inv.Symbol = "NULL" && 
                box inv.InvestmentName <> null && 
                inv.InvestmentName.Contains("CASH", StringComparison.CurrentCulture))
            |> toSortedDictionary (fun inv -> inv.InvestmentName) id

        {
            InvestmentsByCompanySymbol = investmentsByCompanySymbol
            InvestmentsByCompanyName   = investmentsByCompanyName
            TBills                     = tBills
            Cash                       = cash
        }
