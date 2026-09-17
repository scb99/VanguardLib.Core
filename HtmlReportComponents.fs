namespace VanguardLib

open System
open System.Net
open System.Runtime.InteropServices
open System.Text
open System.Text.Encodings.Web

module HtmlReportLayout =

    /// Wraps report body fragments into a cohesive, styled HTML sheet or card.
    let wrapWithTemplate title bodyContent =
        let encodedTitle = HtmlEncoder.Default.Encode(title)
    
        $$"""
        <div class="vanguard-report-container"><h2>{{encodedTitle}}</h2>{{bodyContent}}</div>
        """

module HtmlTableBuilder =

    let private numericColumnTriggers = 
        [| "amount"; "paid"; "shares"; "value"; "principal"; "price"; "interest"; "fees" |]

    let private isNumericHeader (header: string) =
        if String.IsNullOrWhiteSpace header then false
        else
            let cleanHeader = header.Trim().ToLowerInvariant()
            numericColumnTriggers |> Array.exists cleanHeader.Contains

    /// Generates a standardized, high-performance HTML table configuration for C# consumption
    let BuildTable<'T> (
        items: seq<'T>,
        headers: string[],
        rowRenderer: Func<'T, string>,
        emptyMessage: string,
        [<Optional; DefaultParameterValue(null: string)>] footerHtml: string) : string =
    
        let safeItems = Option.ofObj items |> Option.defaultValue Seq.empty

        if Seq.isEmpty safeItems then
            $"<p>{WebUtility.HtmlEncode(emptyMessage)}</p>"
        else
            // 1. Declaratively build the table headers
            let headersHtml = 
                if isNull headers || headers.Length = 0 then ""
                else
                    let thElements = 
                        headers 
                        |> Seq.map (fun h -> 
                            let cssClass = if isNumericHeader h then " class=\"text-right\"" else ""
                            $"      <th{cssClass}>{WebUtility.HtmlEncode(h)}</th>")
                        |> String.concat "\n"
                    $"  <thead>\n    <tr>\n{thElements}\n    </tr>\n  </thead>"

            // 2. Stream through rows without manual string building variables
            let bodyRows = 
                safeItems 
                |> Seq.map rowRenderer.Invoke 
                |> String.concat ""

            // 3. Declaratively wrap the tfoot wrapper markup
            let tfootHtml = 
                if String.IsNullOrEmpty footerHtml then ""
                else $"  <tfoot>\n{footerHtml}\n  </tfoot>"

            // 4. Return everything using a single clean, high-readability $$""" template
            $$"""<table class="report-table">
{{headersHtml}}
  <tbody>{{bodyRows}}</tbody>
{{tfootHtml}}
</table>
"""
