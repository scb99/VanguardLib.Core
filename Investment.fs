namespace VanguardLib

open System

/// Represents an immutable investment holding within a portfolio.
type Investment =
    { 
        AccountNumber: string
        InvestmentName: string
        Symbol: string
        Shares: decimal
        SharePrice: decimal
        TotalValue: decimal 
    }
        
    static member TryCreate
        (
            accountNumber: string,
            investmentName: string,
            symbol: string,
            shares: decimal,
            sharePrice: decimal,
            totalValue: decimal
        ) : ValidationResult<Investment> =
        
        // 1. Run all individual parameter checks in parallel isolation
        let accountCheck = Validator.Check(accountNumber, (fun s -> not (String.IsNullOrWhiteSpace s)), "Account number cannot be empty.")
        let nameCheck = Validator.Check(investmentName, (fun s -> not (String.IsNullOrWhiteSpace s)), "Investment name cannot be empty.")
        let symbolCheck = Validator.Check(symbol, (fun s -> not (String.IsNullOrWhiteSpace s)), "Ticker symbol cannot be empty.")
        let sharesCheck = Validator.Check(shares, (fun val' -> val' >= 0M), "Shares cannot be negative.")
        let priceCheck = Validator.Check(sharePrice, (fun val' -> val' >= 0M), "Share price cannot be negative.")
        let totalValueCheck = Validator.Check(totalValue, (fun val' -> val' >= 0M), "Total value cannot be negative.")

        // 2. Aggregate everything applicatively. If all pass, the factory record expression is invoked.
        Validator.Combine(
            accountCheck,
            nameCheck,
            symbolCheck,
            sharesCheck,
            priceCheck,
            totalValueCheck,
            (fun acc name sym sh pr tv -> 
                { AccountNumber = acc
                  InvestmentName = name
                  Symbol = sym.ToUpperInvariant() // Transformation applied during construction
                  Shares = sh
                  SharePrice = pr
                  TotalValue = tv })
        )