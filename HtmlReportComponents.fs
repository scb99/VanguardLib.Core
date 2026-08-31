namespace VanguardLib

open System
open System.Net
open System.Runtime.InteropServices
open System.Text
open System.Text.Encodings.Web

module HtmlReportLayout =

    /// <summary>
    /// Wraps report body fragments into a cohesive, styled HTML sheet or card.
    /// </summary>
    let WrapWithTemplate (title: string, bodyContent: string) : string =
        
        // 1. Safe XSS handling for title
        let encodedTitle = HtmlEncoder.Default.Encode(title)

        // 2. Pure raw string literal for CSS styles (No early closing tag)
        let cssStyles = """
            <style>
              .vanguard-report-container { font-family: Arial, sans-serif; margin: 20px; color: #333333; }
              .report-table { border-collapse: collapse; min-width: 600px; margin-bottom: 25px; width: 100%; }
              .report-table th, .report-table td { border: 1px solid #dddddd; padding: 8px 12px; text-align: left; }
              .report-table th { background-color: #f2f2f2; font-weight: bold; color: #2c3e50; }
              .report-table tr:nth-child(even) { background-color: #f9f9f9; }
              .report-table .text-right { text-align: right; }
              .report-table .total-row { font-weight: bold; background-color: #eaeded; }
              .log-box { background-color: #fcf8e3; border: 1px solid #fbeed5; color: #c09853; padding: 10px; margin-bottom: 20px; font-family: monospace; border-radius: 4px; white-space: pre-wrap; }
              .section-header { color: #2c3e50; margin-top: 25px; margin-bottom: 8px; border-bottom: 2px solid #34495e; padding-bottom: 4px; }
            </style>
            """

        // 3. Structured assembly keeping all dynamic body elements inside the shell
        $"""
        <div class="vanguard-report-container">
            {cssStyles}
            <h2>{encodedTitle}</h2>
            {bodyContent}
        </div>
        """

module HtmlTableBuilder =

    /// A private static array containing all column substrings that mandate right-alignment
    let private numericColumnTriggers = 
        [| "amount"; "paid"; "shares"; "value"; "principal"; "price"; "interest"; "fees" |]

    /// Checks if a header string contains any numeric keywords (case-insensitive)
    let private isNumericHeader (header: string) : bool =
        if String.IsNullOrWhiteSpace header then false
        else
            let cleanHeader = header.Trim().ToLowerInvariant()
            Array.exists (fun (trigger: string) -> cleanHeader.Contains(trigger)) numericColumnTriggers

    /// Generates a standardized, high-performance HTML table configuration for C# consumption
    let BuildTable<'T> (
        items: seq<'T>,
        headers: string[],
        rowRenderer: Func<'T, string>,
        emptyMessage: string,
        [<Optional; DefaultParameterValue(null: string)>] footerHtml: string) : string =
        
        // 1. Defend against null inputs and unpack streams safely without double-allocation
        let hasElements, enumerator = 
            match items with
            | null -> false, null
            | _ -> 
                let e = items.GetEnumerator()
                e.MoveNext(), e

        if not hasElements then
            if enumerator <> null then enumerator.Dispose()
            $"<p>{WebUtility.HtmlEncode(emptyMessage)}</p>"
        else
            use _boundEnumerator = enumerator // Guarantees proper garbage collection disposal
            let html = StringBuilder()
            html.AppendLine("<table class=\"report-table\">") |> ignore

            // 2. Structural Header Rendering Layer
            if not (isNull headers) && headers.Length > 0 then
                html.AppendLine("  <thead>") |> ignore
                html.AppendLine("    <tr>") |> ignore

                for header in headers do
                    let cssClass = if isNumericHeader header then " class=\"text-right\"" else ""
                    html.AppendLine($"      <th{cssClass}>{WebUtility.HtmlEncode(header)}</th>") |> ignore

                html.AppendLine("    </tr>") |> ignore
                html.AppendLine("  </thead>") |> ignore

            // 3. High-Performance Stream-Through Body Rendering Loop
            html.AppendLine("  <tbody>") |> ignore
            
            // Render the first element retrieved during the initial hasElements sizing test
            html.Append(rowRenderer.Invoke(enumerator.Current)) |> ignore
            
            // Loop through all remaining stream nodes cleanly without allocating a separate List intermediate container
            while enumerator.MoveNext() do
                html.Append(rowRenderer.Invoke(enumerator.Current)) |> ignore
                
            html.AppendLine("  </tbody>") |> ignore

            // 4. Summary Row Integration Suffix
            if not (String.IsNullOrEmpty(footerHtml)) then
                html.AppendLine("  <tfoot>") |> ignore
                html.Append(footerHtml) |> ignore
                html.AppendLine("  </tfoot>") |> ignore

            html.AppendLine("</table>") |> ignore
            html.ToString()
