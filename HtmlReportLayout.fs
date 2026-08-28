namespace VanguardLib

open System.Text.Encodings.Web

module HtmlReportLayout =

    /// <summary>
    /// Wraps report body fragments into a cohesive, styled HTML sheet or card.
    /// </summary>
    let WrapWithTemplate (title: string, bodyContent: string) : string =
        
        // 1. Safe XSS handling for title
        let encodedTitle = HtmlEncoder.Default.Encode(title)

        // 2. Pure raw string literal for CSS (No structural $ prefix means no braces conflict)
        let cssStyles = """
            <div class="vanguard-report-container">
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

        // 3. Simple interpolation for the dynamic body values only
        $"""
            {cssStyles}
              <h2>{encodedTitle}</h2>
              {bodyContent}
            </div>
            """
