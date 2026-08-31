namespace VanguardLib

open System
open Validation // Opens the operators (<!>), (<*>), and check function

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
        
        // 1. Define the success target factory constructor with structural transformations
        let createInvestment acc name sym sh pr tv =
            { AccountNumber = acc
              InvestmentName = name
              Symbol = if String.IsNullOrWhiteSpace(sym) then "" else sym.ToUpperInvariant()
              Shares = sh
              SharePrice = pr
              TotalValue = tv }

        // 2. Run all individual parameter checks in isolated evaluation
        let accountCheck = Validation.check (String.IsNullOrWhiteSpace >> not) "Account number cannot be empty." accountNumber
        let nameCheck    = Validation.check (String.IsNullOrWhiteSpace >> not) "Investment name cannot be empty." investmentName
        let symbolCheck  = Validation.check (String.IsNullOrWhiteSpace >> not) "Ticker symbol cannot be empty." symbol
        let sharesCheck  = Validation.check (fun x -> x >= 0M) "Shares cannot be negative." shares
        let priceCheck   = Validation.check (fun x -> x >= 0M) "Share price cannot be negative." sharePrice
        let totalCheck   = Validation.check (fun x -> x >= 0M) "Total value cannot be negative." totalValue

        // 3. Aggregate everything applicatively. If all pass, the record instance is created.
        createInvestment 
        <!> accountCheck
        <*> nameCheck
        <*> symbolCheck
        <*> sharesCheck
        <*> priceCheck
        <*> totalCheck

/// Represents an immutable Vanguard transaction row record.
type Transaction =
    { AccountNumber: string
      TradeDate: DateOnly
      SettlementDate: DateOnly
      TransactionType: string
      TransactionDescription: string
      InvestmentName: string
      Symbol: string
      Shares: decimal
      SharePrice: decimal
      PrincipalAmount: decimal
      CommissionAndFees: decimal
      NetAmount: decimal
      AccruedInterest: decimal
      AccountType: string }

    static member TryCreate(
        accountNumber: string, 
        tradeDate: DateOnly, 
        settlementDate: DateOnly, 
        transactionType: string,
        transactionDescription: string, 
        investmentName: string, 
        symbol: string, 
        shares: decimal, 
        sharePrice: decimal,
        principalAmount: decimal, 
        commissionAndFees: decimal, 
        netAmount: decimal, 
        accruedInterest: decimal, 
        accountType: string
    ) : ValidationResult<Transaction> =
        
        // 1. Define the success target factory constructor with map transformations
        let createTransaction acc tDate sDate tType tDesc iName (sym : string) sh pr princ comm net int' accType =
            { AccountNumber = acc
              TradeDate = tDate
              SettlementDate = sDate
              TransactionType = tType
              TransactionDescription = tDesc
              InvestmentName = iName
              Symbol = sym.ToUpperInvariant()
              Shares = sh
              SharePrice = pr
              PrincipalAmount = princ
              CommissionAndFees = comm
              NetAmount = net
              AccruedInterest = int'
              AccountType = accType }

        // 2. Prepare transformations and run individual rules in isolation
        let checkAccount = Validation.check (String.IsNullOrWhiteSpace >> not) "Account number cannot be empty." accountNumber
        let checkTradeDate = Success tradeDate // Structural validation handled at parsing boundary
        let checkSettlementDate = Validation.check (fun d -> d >= tradeDate) "Settlement date cannot precede trade date." settlementDate
        let checkTransactionType = Validation.check (String.IsNullOrWhiteSpace >> not) "Transaction type cannot be empty." transactionType
        let checkTransactionDescription = Validation.check (String.IsNullOrWhiteSpace >> not) "Transaction description cannot be empty." transactionDescription
        let checkInvestmentName = Validation.check (String.IsNullOrWhiteSpace >> not) "Investment name cannot be empty." investmentName
        
        // Handle Vanguard blank symbol default inline
        let parsedSymbol = if String.IsNullOrWhiteSpace symbol then "CASH" else symbol.Trim()
        let checkSymbol = Validation.check (String.IsNullOrWhiteSpace >> not) "Ticker symbol cannot be empty." parsedSymbol
        let checkAccountType = Validation.check (String.IsNullOrWhiteSpace >> not) "Account type cannot be empty." accountType   

        // Numerical range rules (incorporating Math.Abs for absolute value invariants)
        let checkShares = Validation.check (fun v -> v >= 0M) "Shares cannot be negative." (Math.Abs shares)
        let checkSharePrice = Validation.check (fun v -> v >= 0M) "Share price cannot be negative." sharePrice
        let checkPrincipalAmount = Validation.check (fun v -> v >= 0M) "Principal amount cannot be negative." (Math.Abs principalAmount)
        let checkCommissionsAndFees = Validation.check (fun v -> v >= 0M) "Commission cannot be negative." commissionAndFees
        let checkNetAmount = Validation.check (fun v -> v >= 0M) "Net amount cannot be negative." (Math.Abs netAmount)
        let checkAccruedInterest = Validation.check (fun v -> v >= 0M) "Accrued interest cannot be negative." accruedInterest

        // 3. Chain all 14 arguments applicatively with absolute type safety
        createTransaction
        <!> checkAccount
        <*> checkTradeDate
        <*> checkSettlementDate
        <*> checkTransactionType
        <*> checkTransactionDescription
        <*> checkInvestmentName
        <*> checkSymbol
        <*> checkShares
        <*> checkSharePrice
        <*> checkPrincipalAmount
        <*> checkCommissionsAndFees
        <*> checkNetAmount
        <*> checkAccruedInterest
        <*> checkAccountType
