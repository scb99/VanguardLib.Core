namespace VanguardLib.Extensions

open System
open System.Globalization

module String =

    let toCleanDecimal (rawValue: string) : decimal =
        if String.IsNullOrWhiteSpace(rawValue) then 
            0.0M
        else
            let cleanValue = 
                rawValue
                    .Replace("$", "", StringComparison.Ordinal)
                    .Replace("(", "-", StringComparison.Ordinal)
                    .Replace(")", "", StringComparison.Ordinal)
                    .Replace(" ", "", StringComparison.Ordinal)
                    .Trim()

            // F# decimal.TryParse returns a tuple: (bool success, decimal result)
            match Decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | true, result -> result
            | false, _     -> 0.0M
