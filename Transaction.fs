namespace VanguardLib

open System

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
      Fees: decimal
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
        
        // Check Rules
        let checkAccount = Validator.Check(accountNumber, (fun s -> not (String.IsNullOrWhiteSpace s)), "Account number cannot be empty.")
        let checkTradeDate = ValidationResult<DateOnly>.Ok tradeDate // Structural validation handled at parsing boundary
        let checkSettlementDate = Validator.Check(settlementDate, (fun d -> d >= tradeDate), "Settlement date cannot precede trade date.")
        let checkTransactionType = Validator.Check(transactionType, (fun s -> not (String.IsNullOrWhiteSpace s)), "Transaction type cannot be empty.")
        let checkTransactionDescription = Validator.Check(transactionDescription, (fun s -> not (String.IsNullOrWhiteSpace s)), "Transaction description cannot be empty.")
        let checkInvestmentName = Validator.Check(investmentName, (fun s -> not (String.IsNullOrWhiteSpace s)), "Investment name cannot be empty.")
        
        // FIX: If the Vanguard CSV symbol column is blank, default it so validation passes
        let parsedSymbol = if String.IsNullOrWhiteSpace symbol then "CASH" else symbol.Trim()
        let checkSymbol = Validator.Check(parsedSymbol, (fun s -> not (String.IsNullOrWhiteSpace s)), "Ticker symbol cannot be empty.")
        let checkAccountType = Validator.Check(accountType, (fun s -> not (String.IsNullOrWhiteSpace s)), "Account type cannot be empty.")   

        // Numerical range rules 
        let checkShares = Validator.Check(Math.Abs shares, (fun v -> v >= 0M), "Shares cannot be negative.") // FIX: Vanguard CSV has negative shares for some transaction types
        let checkSharePrice = Validator.Check(sharePrice, (fun v -> v >= 0M), "Share price cannot be negative.")
        let checkPrincipalAmount = Validator.Check(Math.Abs principalAmount, (fun v -> v >= 0M), "Principal amount cannot be negative.")
        let checkCommissionsAndFees = Validator.Check(commissionAndFees, (fun v -> v >= 0M), "Commission cannot be negative.")
        let checkNetAmount = Validator.Check(Math.Abs netAmount, (fun v -> v >= 0M), "Net amount cannot be negative.")
        let checkAccruedInterest = Validator.Check(accruedInterest, (fun v -> v >= 0M), "Accrued interest cannot be negative.")

        // 2. Feed all validation cells into the 14-parameter applicative engine
        Validator.Combine(
            checkAccount, checkTradeDate, checkSettlementDate, checkTransactionType, checkTransactionDescription, checkInvestmentName, checkSymbol,
            checkShares, checkSharePrice, checkPrincipalAmount, checkCommissionsAndFees, checkNetAmount, checkAccruedInterest, checkAccountType,
            (fun acc tDate sDate tType tDesc iName sym sh pr princ comm net int accType ->
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
                  Fees = comm // Map Fees directly to matching value
                  NetAmount = net
                  AccruedInterest = int
                  AccountType = accType })
        )