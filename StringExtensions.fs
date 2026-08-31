namespace VanguardLib.Extensions

open System
open System.Globalization
open System.Text.RegularExpressions

module String =

    /// Cleans and parses a string into a decimal. 
    /// Returns None if parsing fails.
    let tryToCleanDecimal (rawValue: string) : decimal option =
        if String.IsNullOrWhiteSpace(rawValue) then 
            None
        else
            let cleanValue = rawValue.Trim().Replace(" ", "", StringComparison.Ordinal)

            // NumberStyles.Any natively supports $, (, ), and commas
            match Decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | true, result -> Some result
            | false, _     -> None
            
    /// Cleans and parses a string into a decimal. 
    /// Returns 0.0M if parsing fails.
    let toCleanDecimal (rawValue: string) : decimal =
        tryToCleanDecimal rawValue 
        |> Option.defaultValue 0.0M

    /// Collapses multiple consecutive spaces into a single space and trims the edges
    let cleanWhitespace (text: string) =
        if String.IsNullOrWhiteSpace text then ""
        else Regex.Replace(text.Trim(), @"\s+", " ")
