namespace VanguardLib

open System
open System.Net
open System.Text
open System.Collections.Generic
open System.Runtime.InteropServices // Mandatory for the interop attributes

module HtmlTableBuilder =

    let BuildTable<'T> (
        items: seq<'T>,
        headers: string[],
        rowRenderer: Func<'T, string>,
        emptyMessage: string,
        // Adding interop attributes makes this completely optional for your C# consumers
        [<Optional; DefaultParameterValue(null: string)>] footerHtml: string) : string =
        
        let itemList = 
            match items with
            | null -> List<'T>()
            | :? IReadOnlyCollection<'T> as col -> List<'T>(col)
            | _ -> List<'T>(items)

        if itemList.Count = 0 then
            $"<p>{WebUtility.HtmlEncode(emptyMessage)}</p>"
        else
            let html = StringBuilder()
            html.AppendLine("<table class=\"report-table\">") |> ignore

            if box headers <> null && headers.Length > 0 then
                html.AppendLine("  <thead>") |> ignore
                html.AppendLine("    <tr>") |> ignore

                for header in headers do
                    let cssClass = 
                        match header with

                        | "Amount" | "Paid" | "Shares" | "Total Value" | "Principal Amount" -> " class=\"text-right\""
                        | _ -> ""
                    html.AppendLine($"      <th{cssClass}>{WebUtility.HtmlEncode(header)}</th>") |> ignore

                html.AppendLine("    </tr>") |> ignore
                html.AppendLine("  </thead>") |> ignore

            html.AppendLine("  <tbody>") |> ignore
            for item in itemList do
                html.Append(rowRenderer.Invoke(item)) |> ignore
            html.AppendLine("  </tbody>") |> ignore

            if not (String.IsNullOrEmpty(footerHtml)) then
                html.AppendLine("  <tfoot>") |> ignore
                html.Append(footerHtml) |> ignore
                html.AppendLine("  </tfoot>") |> ignore

            html.AppendLine("</table>") |> ignore
            html.ToString()
