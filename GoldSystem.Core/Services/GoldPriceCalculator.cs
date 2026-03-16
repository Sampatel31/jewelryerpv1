namespace GoldSystem.Core.Services;

/// <summary>
/// Calculates all pricing components for jewelry bill lines including gold value,
/// making charges, wastage, GST, and bill totals.
/// </summary>
public class GoldPriceCalculator
{
    // Input model - all parameters needed for a single bill line calculation
    public record BillLineInput(
        decimal GrossWeight,
        decimal StoneWeight,
        string Purity,
        string MakingType,
        decimal MakingValue,
        decimal WastagePercent,
        decimal StoneCharge,
        decimal Rate24KPer10g);

    // Output model - all intermediate values exposed for transparency and audit trail
    public record BillLineResult(
        decimal NetWeight,
        decimal PurityFactor,
        decimal PureGoldWeight,
        decimal RatePerGram,
        decimal GoldValue,
        decimal MakingAmount,
        decimal WastageWeight,
        decimal WastageValue,
        decimal TaxableAmount,
        decimal GoldGST,
        decimal MakingGST,
        decimal StoneGST,
        decimal LineTotal);

    /// <summary>
    /// Calculate all pricing components for a single bill line item.
    /// All calculations use decimal type with no intermediate rounding.
    /// Only final display values are rounded to 2 decimal places.
    /// </summary>
    public BillLineResult Calculate(BillLineInput input)
    {
        // Step 1: Net Weight = Gross Weight - Stone Weight
        var netWeight = input.GrossWeight - input.StoneWeight;

        // Step 2: Purity Factor (karat / 24)
        var purityFactor = GetPurityFactor(input.Purity);

        // Step 3: Pure Gold Weight = Net Weight × Purity Factor
        var pureGoldWeight = netWeight * purityFactor;

        // Step 4: Rate Per Gram = Rate24KPer10g / 10
        var ratePerGram = input.Rate24KPer10g / 10m;

        // Step 5: Gold Value = Pure Gold Weight × Rate Per Gram
        var goldValue = pureGoldWeight * ratePerGram;

        // Step 6: Making Amount (depends on making type)
        var makingAmount = CalculateMaking(input.MakingType, input.MakingValue, goldValue, netWeight);

        // Step 7: Wastage Weight = Net Weight × Wastage Percent / 100
        var wastageWeight = netWeight * input.WastagePercent / 100m;

        // Step 8: Wastage Value = Wastage Weight × Purity Factor × Rate Per Gram
        var wastageValue = wastageWeight * purityFactor * ratePerGram;

        // Step 9: Taxable Amount (before GST) = Gold Value + Making + Wastage + Stone Charge
        var taxableAmount = goldValue + makingAmount + wastageValue + input.StoneCharge;

        // Step 10: Calculate GST components
        // Gold GST: (GoldValue + WastageValue) × 3% [HSN 7113 - 1.5% CGST + 1.5% SGST]
        var goldGST = (goldValue + wastageValue) * 0.03m;

        // Making GST: MakingAmount × 5% [SAC 9988 - 2.5% CGST + 2.5% SGST]
        var makingGST = makingAmount * 0.05m;

        // Stone GST: StoneCharge × 0% (natural uncut) or 3% (polished)
        // For now, default to 0% for all stones. Can be configurable per stone type.
        var stoneGST = input.StoneCharge * 0m;

        // Step 11: Line Total = Taxable Amount + All GST
        var lineTotal = taxableAmount + goldGST + makingGST + stoneGST;

        return new BillLineResult(
            NetWeight: netWeight,
            PurityFactor: purityFactor,
            PureGoldWeight: pureGoldWeight,
            RatePerGram: ratePerGram,
            GoldValue: goldValue,
            MakingAmount: makingAmount,
            WastageWeight: wastageWeight,
            WastageValue: wastageValue,
            TaxableAmount: taxableAmount,
            GoldGST: goldGST,
            MakingGST: makingGST,
            StoneGST: stoneGST,
            LineTotal: lineTotal);
    }

    /// <summary>
    /// Get purity factor from karat designation.
    /// 24K = 1.0, 22K = 22/24 = 0.91667, 18K = 18/24 = 0.75
    /// </summary>
    private static decimal GetPurityFactor(string purity) => purity switch
    {
        "24K" => 1.0000m,
        "22K" => 22m / 24m,
        "18K" => 18m / 24m,
        _ => throw new ArgumentException($"Unknown purity: {purity}")
    };

    /// <summary>
    /// Calculate making charge based on type:
    /// - PERCENT: GoldValue × MakingPercent / 100
    /// - PER_GRAM: NetWeight × MakingRatePerGram
    /// - FIXED: Fixed amount regardless of weight or gold value
    /// </summary>
    private static decimal CalculateMaking(string makingType, decimal makingValue,
        decimal goldValue, decimal netWeight)
        => makingType switch
        {
            "PERCENT" => goldValue * makingValue / 100m,
            "PER_GRAM" => netWeight * makingValue,
            "FIXED" => makingValue,
            _ => throw new ArgumentException($"Unknown making type: {makingType}")
        };

    /// <summary>
    /// Calculate bill total including all line items, discount, and GST split (CGST/SGST for intra-state).
    /// </summary>
    public record BillTotalInput(
        IEnumerable<BillLineResult> LineItems,
        decimal DiscountAmount,
        decimal ExchangeValue,
        bool IsInterState);

    public record BillTotalResult(
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxableAmount,
        decimal CGST,
        decimal SGST,
        decimal IGST,
        decimal RoundOff,
        decimal GrandTotal,
        decimal BalanceDue);

    public BillTotalResult CalculateTotal(BillTotalInput input, decimal amountPaid)
    {
        var subTotal = input.LineItems.Sum(l => l.TaxableAmount);
        var taxableAmount = subTotal - input.DiscountAmount;

        decimal cgst, sgst, igst;
        if (input.IsInterState)
        {
            // Inter-state: IGST = 3% + 5% = 8% split
            cgst = 0m;
            sgst = 0m;
            igst = input.LineItems.Sum(l => l.GoldGST + l.MakingGST + l.StoneGST);
        }
        else
        {
            // Intra-state: CGST/SGST split
            cgst = input.LineItems.Sum(l => (l.GoldGST + l.MakingGST + l.StoneGST) / 2m);
            sgst = cgst;
            igst = 0m;
        }

        var totalTax = cgst + sgst + igst;
        var grandTotalBeforeRoundOff = taxableAmount + totalTax;
        var roundOff = Math.Round(grandTotalBeforeRoundOff, 0) - grandTotalBeforeRoundOff;
        var grandTotal = grandTotalBeforeRoundOff + roundOff;
        var balanceDue = grandTotal - amountPaid - input.ExchangeValue;

        return new BillTotalResult(
            SubTotal: subTotal,
            DiscountAmount: input.DiscountAmount,
            TaxableAmount: taxableAmount,
            CGST: cgst,
            SGST: sgst,
            IGST: igst,
            RoundOff: roundOff,
            GrandTotal: grandTotal,
            BalanceDue: balanceDue);
    }
}
