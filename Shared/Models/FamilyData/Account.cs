using System.Text.Json.Serialization;

public class Account 
{
    public Account() {
        Investments = new();
        AvailableFunds = new();
    }

    public string? Identifier { get; set; }
    public string AccountType { get; set; }
    public string? Custodian { get; set; }
    public string? Note { get; set; }
    public double Value { 
        get {
            double newValue = 0;
            foreach (var investment in Investments) 
            {
                newValue += investment.Value ?? 0;
            }

            return newValue;
        }
    }

    [JsonIgnore]
    public bool Edit { get; set; }

    [JsonIgnore]
    public double Percentage { get; set; }

    public string FullName 
    {
        get {
            return (Identifier != null ? Identifier+ " " : "") + AccountType + (Custodian != null ? " at " + Custodian : "");
        }
    }

    public List<Investment> Investments { get; set; }

    public List<Investment> AvailableFunds { get; set; }

    [JsonIgnore]
    public Account? ReplaceAccount { get; set; }

    [JsonIgnore]
    public bool Import { get; set; }

    public double? CalculateCostBasis() {
        double costBasis = 0.0;
        foreach (var investment in Investments)
        {
            if (investment.CostBasis != null)
            {
                costBasis += investment.CostBasis.Value;
            }
        }
        if (costBasis == 0.0) {
            return null;
        } else {
            return costBasis;
        }
    }

    public void UpdatePercentages(double totalValue, FamilyData familyData)
    {
        foreach (var investment in Investments)
        {
            investment.Percentage = (investment.Value ?? 0) / totalValue * 100;
            if (investment.AssetType == null)
            {
                familyData.OtherBalance += investment.Value;
            } 
            else
            {
                switch (investment.AssetType) {
                    case AssetType.Stock: 
                    case AssetType.USStock:
                    case AssetType.USStock_ETF:
                    case AssetType.USStock_Fund:
                        familyData.StockBalance += investment.Value;
                        break;
                    case AssetType.InternationalStock:
                    case AssetType.InternationalStock_ETF:
                    case AssetType.InternationalStock_Fund:
                        familyData.InternationalStockBalance += investment.Value;
                        break;
                    case AssetType.Bond:
                    case AssetType.Bond_ETF:
                    case AssetType.Bond_Fund:
                        familyData.BondBalance += investment.Value;
                        break;
                    case AssetType.Cash:
                    case AssetType.Cash_BankAccount:
                    case AssetType.Cash_MoneyMarket:
                        familyData.CashBalance += investment.Value;
                        break;
                    //TODO: provide way for people to give usstock/intlstock/bond/cash mix
                    case AssetType.StocksAndBonds_ETF:
                    case AssetType.StocksAndBonds_Fund:
                        familyData.OtherBalance += investment.Value;
                        break;
                    case AssetType.Unknown:
                        familyData.OtherBalance += investment.Value;
                        break;
                    default:
                        throw new InvalidDataException("unexpected case");
                }
            }

            if (investment.ExpenseRatio.HasValue && investment.Value.HasValue) {
                familyData.OverallER += investment.Percentage / 100.0 * investment.ExpenseRatio.Value;
                familyData.ExpensesTotal += investment.Value.Value * investment.ExpenseRatio.Value / 100.0;
            } else {
                familyData.InvestmentsMissingER++;
            }

        }
    }

    public void GuessAccountType() 
    {
        if (Note.Contains("401K") || Note.Contains("401k"))
        {
            AccountType = "401k";
        }
        else if (Note.Contains("HSA") || Note.Contains("Health Savings Account"))
        {
            AccountType = "HSA";
        }
    }

    public string TaxType {
        get { 
            switch (AccountType) {
                case "401k":
                case "403b":
                case "457b":
                case "Annuity (Qualified)":
                case "Inherited IRA":
                case "SIMPLE IRA":
                case "Traditional IRA":
                case "Rollover IRA":
                case "Solo 401k":
                case "SEP IRA":
                    return "Pre-Tax";
                case "Inherited Roth IRA":
                case "Roth 401k":
                case "Roth IRA":
                case "HSA":
                    return "Post-Tax";
                case "Annuity (Non-Qualified)":
                case "Brokerage":
                case "Taxable":
                    return "Taxable";
                case "Refundable Deposit":
                    return "Refundable Deposits";
                case "Life Insurance":
                    return "For Beneficiaries (POD)";
                case "529":
                    return "Education Savings";
                default:
                    return "Other";
            }
        }
    }
}