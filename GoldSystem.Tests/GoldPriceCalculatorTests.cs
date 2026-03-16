using GoldSystem.Core.Services;

namespace GoldSystem.Tests;

/// <summary>
/// Comprehensive unit tests for GoldPriceCalculator covering all gold pricing formulas,
/// making charge types, wastage calculations, and GST computations.
/// </summary>
public class GoldPriceCalculatorTests
{
    private readonly GoldPriceCalculator _calculator = new();

    // ─── Test Data Fixtures ─────────────────────────────────────────────────────

    // 22K Gold Chain: 10g gross, 0.5g stone, 12% making (percent), 2% wastage, Rs 75000/10g rate
    private static GoldPriceCalculator.BillLineInput Chain22K => new(
        GrossWeight: 10m,
        StoneWeight: 0.5m,
        Purity: "22K",
        MakingType: "PERCENT",
        MakingValue: 12m,
        WastagePercent: 2m,
        StoneCharge: 500m,
        Rate24KPer10g: 75000m);

    // 18K Gold Ring: 4.5g gross, 0.3g stone, Rs 250/gram making, 1.5% wastage
    private static GoldPriceCalculator.BillLineInput Ring18K => new(
        GrossWeight: 4.5m,
        StoneWeight: 0.3m,
        Purity: "18K",
        MakingType: "PER_GRAM",
        MakingValue: 250m,
        WastagePercent: 1.5m,
        StoneCharge: 1000m,
        Rate24KPer10g: 75000m);

    // 24K Gold Bangle: 20g gross, 0g stone, fixed Rs 2000 making, 2.5% wastage
    private static GoldPriceCalculator.BillLineInput Bangle24K => new(
        GrossWeight: 20m,
        StoneWeight: 0m,
        Purity: "24K",
        MakingType: "FIXED",
        MakingValue: 2000m,
        WastagePercent: 2.5m,
        StoneCharge: 0m,
        Rate24KPer10g: 75000m);

    // ─── 1. Basic Weight Calculations ───────────────────────────────────────────

    [Fact]
    public void Calculate_NetWeight_EqualsGrossMinusStone()
    {
        var result = _calculator.Calculate(Chain22K);
        Assert.Equal(9.5m, result.NetWeight);
    }

    [Fact]
    public void Calculate_NetWeight_WhenStoneWeightIsZero_EqualsGrossWeight()
    {
        var result = _calculator.Calculate(Bangle24K);
        Assert.Equal(20m, result.NetWeight);
    }

    [Fact]
    public void Calculate_NetWeight_WhenGrossEqualsStone_ReturnsZero()
    {
        var input = Chain22K with { GrossWeight = 2m, StoneWeight = 2m };
        var result = _calculator.Calculate(input);
        Assert.Equal(0m, result.NetWeight);
    }

    // ─── 2. Purity Factor Tests ──────────────────────────────────────────────────

    [Fact]
    public void Calculate_PurityFactor_24K_IsOne()
    {
        var result = _calculator.Calculate(Bangle24K);
        Assert.Equal(1.0000m, result.PurityFactor);
    }

    [Fact]
    public void Calculate_PurityFactor_22K_IsCorrect()
    {
        var result = _calculator.Calculate(Chain22K);
        Assert.Equal(22m / 24m, result.PurityFactor);
    }

    [Fact]
    public void Calculate_PurityFactor_18K_IsCorrect()
    {
        var result = _calculator.Calculate(Ring18K);
        Assert.Equal(18m / 24m, result.PurityFactor);
    }

    [Fact]
    public void Calculate_InvalidPurity_ThrowsArgumentException()
    {
        var input = Chain22K with { Purity = "16K" };
        var ex = Assert.Throws<ArgumentException>(() => _calculator.Calculate(input));
        Assert.Contains("16K", ex.Message);
    }

    // ─── 3. Pure Gold Weight Tests ───────────────────────────────────────────────

    [Fact]
    public void Calculate_PureGoldWeight_22K_IsNetWeightTimesPurityFactor()
    {
        // NetWeight = 9.5, PurityFactor = 22/24
        var result = _calculator.Calculate(Chain22K);
        var expected = 9.5m * (22m / 24m);
        Assert.Equal(expected, result.PureGoldWeight);
    }

    [Fact]
    public void Calculate_PureGoldWeight_18K_IsNetWeightTimesPurityFactor()
    {
        // NetWeight = 4.2, PurityFactor = 18/24
        var result = _calculator.Calculate(Ring18K);
        var expected = 4.2m * (18m / 24m);
        Assert.Equal(expected, result.PureGoldWeight);
    }

    [Fact]
    public void Calculate_PureGoldWeight_24K_EqualNetWeight()
    {
        var result = _calculator.Calculate(Bangle24K);
        Assert.Equal(result.NetWeight, result.PureGoldWeight);
    }

    // ─── 4. Rate Conversion Tests ─────────────────────────────────────────────────

    [Fact]
    public void Calculate_RatePerGram_IsDividedByTen()
    {
        var result = _calculator.Calculate(Chain22K);
        Assert.Equal(7500m, result.RatePerGram);
    }

    [Fact]
    public void Calculate_RatePerGram_MCXRate_75000Per10g_Is7500PerGram()
    {
        var input = Bangle24K with { Rate24KPer10g = 75000m };
        var result = _calculator.Calculate(input);
        Assert.Equal(7500m, result.RatePerGram);
    }

    // ─── 5. Gold Value Calculation Tests ──────────────────────────────────────────

    [Fact]
    public void Calculate_GoldValue_IsPureGoldWeightTimesRatePerGram()
    {
        var result = _calculator.Calculate(Chain22K);
        var expected = result.PureGoldWeight * result.RatePerGram;
        Assert.Equal(expected, result.GoldValue);
    }

    [Fact]
    public void Calculate_GoldValue_RealisticScenario_22K_9p7g_At75000()
    {
        // 9.7g net, 22K purity, Rs 75000/10g
        var input = new GoldPriceCalculator.BillLineInput(
            GrossWeight: 9.7m,
            StoneWeight: 0m,
            Purity: "22K",
            MakingType: "FIXED",
            MakingValue: 0m,
            WastagePercent: 0m,
            StoneCharge: 0m,
            Rate24KPer10g: 75000m);
        var result = _calculator.Calculate(input);
        // PureGoldWeight = 9.7 * (22/24), RatePerGram = 7500
        var expected = 9.7m * (22m / 24m) * 7500m;
        Assert.Equal(expected, result.GoldValue);
    }

    [Fact]
    public void Calculate_GoldValue_VerySmallWeight_IsCorrect()
    {
        var input = Bangle24K with { GrossWeight = 0.01m, StoneWeight = 0m };
        var result = _calculator.Calculate(input);
        var expected = 0.01m * 1m * 7500m;
        Assert.Equal(expected, result.GoldValue);
    }

    // ─── 6. Making Charge Type Tests ──────────────────────────────────────────────

    [Fact]
    public void Calculate_MakingAmount_PercentMode_IsGoldValueTimesPercentDivBy100()
    {
        var result = _calculator.Calculate(Chain22K);
        var expected = result.GoldValue * 12m / 100m;
        Assert.Equal(expected, result.MakingAmount);
    }

    [Fact]
    public void Calculate_MakingAmount_PerGramMode_IsNetWeightTimesRate()
    {
        var result = _calculator.Calculate(Ring18K);
        var expected = result.NetWeight * 250m;
        Assert.Equal(expected, result.MakingAmount);
    }

    [Fact]
    public void Calculate_MakingAmount_FixedMode_IsConstantRegardlessOfWeight()
    {
        var result = _calculator.Calculate(Bangle24K);
        Assert.Equal(2000m, result.MakingAmount);

        // Verify fixed is truly independent of weight
        var heavierInput = Bangle24K with { GrossWeight = 100m };
        var heavierResult = _calculator.Calculate(heavierInput);
        Assert.Equal(2000m, heavierResult.MakingAmount);
    }

    [Fact]
    public void Calculate_MakingAmount_RealisticPercentScenario()
    {
        // 22K chain, 9.5g net, 12% making on gold value at 75000/10g
        var result = _calculator.Calculate(Chain22K);
        var goldValue = 9.5m * (22m / 24m) * 7500m;
        var expected = goldValue * 12m / 100m;
        Assert.Equal(expected, result.MakingAmount);
    }

    [Fact]
    public void Calculate_InvalidMakingType_ThrowsArgumentException()
    {
        var input = Chain22K with { MakingType = "FLAT_FEE" };
        var ex = Assert.Throws<ArgumentException>(() => _calculator.Calculate(input));
        Assert.Contains("FLAT_FEE", ex.Message);
    }

    // ─── 7. Wastage Calculations ──────────────────────────────────────────────────

    [Fact]
    public void Calculate_WastageWeight_IsNetWeightTimesWastagePercentDivBy100()
    {
        var result = _calculator.Calculate(Chain22K);
        var expected = 9.5m * 2m / 100m;
        Assert.Equal(expected, result.WastageWeight);
    }

    [Fact]
    public void Calculate_WastageValue_IsWastageWeightTimesPurityTimesRate()
    {
        var result = _calculator.Calculate(Chain22K);
        var expected = result.WastageWeight * result.PurityFactor * result.RatePerGram;
        Assert.Equal(expected, result.WastageValue);
    }

    [Fact]
    public void Calculate_ZeroWastagePercent_WastageWeightAndValueAreZero()
    {
        var input = Chain22K with { WastagePercent = 0m };
        var result = _calculator.Calculate(input);
        Assert.Equal(0m, result.WastageWeight);
        Assert.Equal(0m, result.WastageValue);
    }

    // ─── 8. GST Calculations ──────────────────────────────────────────────────────

    [Fact]
    public void Calculate_GoldGST_Is3PercentOfGoldValuePlusWastageValue()
    {
        var result = _calculator.Calculate(Chain22K);
        var expected = (result.GoldValue + result.WastageValue) * 0.03m;
        Assert.Equal(expected, result.GoldGST);
    }

    [Fact]
    public void Calculate_MakingGST_Is5PercentOfMakingAmount()
    {
        var result = _calculator.Calculate(Chain22K);
        var expected = result.MakingAmount * 0.05m;
        Assert.Equal(expected, result.MakingGST);
    }

    [Fact]
    public void CalculateTotal_IntraState_CGSTandSGSTAreFiftyFiftySplit()
    {
        var lineResult = _calculator.Calculate(Chain22K);
        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            IsInterState: false);

        var total = _calculator.CalculateTotal(totalInput, 0m);

        Assert.Equal(total.CGST, total.SGST);
        Assert.Equal(0m, total.IGST);
        var totalGst = lineResult.GoldGST + lineResult.MakingGST + lineResult.StoneGST;
        Assert.Equal(totalGst / 2m, total.CGST);
    }

    [Fact]
    public void CalculateTotal_InterState_IGSTIsFullGSTAndCGSTSGSTAreZero()
    {
        var lineResult = _calculator.Calculate(Chain22K);
        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            IsInterState: true);

        var total = _calculator.CalculateTotal(totalInput, 0m);

        Assert.Equal(0m, total.CGST);
        Assert.Equal(0m, total.SGST);
        var expectedIGST = lineResult.GoldGST + lineResult.MakingGST + lineResult.StoneGST;
        Assert.Equal(expectedIGST, total.IGST);
    }

    [Fact]
    public void CalculateTotal_DiscountDoesNotReduceGSTBase_GSTComputedFromLineItems()
    {
        var lineResult = _calculator.Calculate(Chain22K);
        var withDiscount = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 500m,
            ExchangeValue: 0m,
            IsInterState: false);
        var withoutDiscount = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            IsInterState: false);

        var totalWithDiscount = _calculator.CalculateTotal(withDiscount, 0m);
        var totalWithoutDiscount = _calculator.CalculateTotal(withoutDiscount, 0m);

        // GST amounts are derived from line items, not affected by discount
        Assert.Equal(totalWithoutDiscount.CGST, totalWithDiscount.CGST);
        Assert.Equal(totalWithoutDiscount.SGST, totalWithDiscount.SGST);
    }

    [Fact]
    public void Calculate_StoneGST_IsZeroByDefault()
    {
        var input = Chain22K with { StoneCharge = 2000m };
        var result = _calculator.Calculate(input);
        Assert.Equal(0m, result.StoneGST);
    }

    // ─── 9. Taxable Amount Calculation ────────────────────────────────────────────

    [Fact]
    public void Calculate_TaxableAmount_IsGoldValuePlusMakingPlusWastagePlusStone()
    {
        var result = _calculator.Calculate(Chain22K);
        var expected = result.GoldValue + result.MakingAmount + result.WastageValue + 500m;
        Assert.Equal(expected, result.TaxableAmount);
    }

    [Fact]
    public void Calculate_TaxableAmount_WithAllComponents_IsCorrect()
    {
        var result = _calculator.Calculate(Ring18K);
        var expected = result.GoldValue + result.MakingAmount + result.WastageValue + 1000m;
        Assert.Equal(expected, result.TaxableAmount);
    }

    // ─── 10. Line Total Calculation ───────────────────────────────────────────────

    [Fact]
    public void Calculate_LineTotal_IsTaxableAmountPlusAllGST()
    {
        var result = _calculator.Calculate(Chain22K);
        var expected = result.TaxableAmount + result.GoldGST + result.MakingGST + result.StoneGST;
        Assert.Equal(expected, result.LineTotal);
    }

    [Fact]
    public void Calculate_LineTotal_RealisticEndToEnd_22KChain()
    {
        // 22K, 9.5g net, 75000/10g rate, 12% making, 2% wastage, 500 stone charge
        var result = _calculator.Calculate(Chain22K);

        var purityFactor = 22m / 24m;
        var ratePerGram = 7500m;
        var goldValue = 9.5m * purityFactor * ratePerGram;
        var makingAmount = goldValue * 0.12m;
        var wastageWeight = 9.5m * 0.02m;
        var wastageValue = wastageWeight * purityFactor * ratePerGram;
        var taxable = goldValue + makingAmount + wastageValue + 500m;
        var goldGST = (goldValue + wastageValue) * 0.03m;
        var makingGST = makingAmount * 0.05m;
        var expected = taxable + goldGST + makingGST;

        Assert.Equal(expected, result.LineTotal);
    }

    // ─── 11. Bill Total Calculation ───────────────────────────────────────────────

    [Fact]
    public void CalculateTotal_SubTotal_IsSumOfAllLineItemTaxableAmounts()
    {
        var line1 = _calculator.Calculate(Chain22K);
        var line2 = _calculator.Calculate(Ring18K);
        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { line1, line2 },
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            IsInterState: false);

        var total = _calculator.CalculateTotal(totalInput, 0m);

        Assert.Equal(line1.TaxableAmount + line2.TaxableAmount, total.SubTotal);
    }

    [Fact]
    public void CalculateTotal_DiscountAmount_ReducesTaxableAmount()
    {
        var lineResult = _calculator.Calculate(Chain22K);
        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 1000m,
            ExchangeValue: 0m,
            IsInterState: false);

        var total = _calculator.CalculateTotal(totalInput, 0m);

        Assert.Equal(lineResult.TaxableAmount - 1000m, total.TaxableAmount);
    }

    [Fact]
    public void CalculateTotal_RoundOff_IsWithinPlusMinusOne()
    {
        var lineResult = _calculator.Calculate(Chain22K);
        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            IsInterState: false);

        var total = _calculator.CalculateTotal(totalInput, 0m);

        Assert.True(Math.Abs(total.RoundOff) < 1m);
    }

    [Fact]
    public void CalculateTotal_BalanceDue_IsGrandTotalMinusPaymentMinusExchange()
    {
        var lineResult = _calculator.Calculate(Chain22K);
        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 0m,
            ExchangeValue: 5000m,
            IsInterState: false);

        var total = _calculator.CalculateTotal(totalInput, amountPaid: 10000m);

        Assert.Equal(total.GrandTotal - 10000m - 5000m, total.BalanceDue);
    }

    // ─── 12. Complex Scenarios ────────────────────────────────────────────────────

    [Fact]
    public void CalculateTotal_MultiItemBill_MixedPurities_IsCorrect()
    {
        var chain = _calculator.Calculate(Chain22K);
        var ring = _calculator.Calculate(Ring18K);
        var bangle = _calculator.Calculate(Bangle24K);

        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { chain, ring, bangle },
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            IsInterState: false);

        var total = _calculator.CalculateTotal(totalInput, 0m);

        Assert.Equal(chain.TaxableAmount + ring.TaxableAmount + bangle.TaxableAmount, total.SubTotal);
        // CGST and SGST should be equal for intra-state
        Assert.Equal(total.CGST, total.SGST);
    }

    [Fact]
    public void CalculateTotal_OldGoldExchange_ReducesBalanceDue()
    {
        var lineResult = _calculator.Calculate(Bangle24K);
        var exchangeValue = 15000m;
        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 0m,
            ExchangeValue: exchangeValue,
            IsInterState: false);

        var totalNoExchange = _calculator.CalculateTotal(
            totalInput with { ExchangeValue = 0m }, 0m);
        var totalWithExchange = _calculator.CalculateTotal(totalInput, 0m);

        Assert.Equal(totalNoExchange.BalanceDue - exchangeValue, totalWithExchange.BalanceDue);
    }

    [Fact]
    public void CalculateTotal_PartialPayment_CorrectBalanceDue()
    {
        var lineResult = _calculator.Calculate(Bangle24K);
        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            IsInterState: false);

        var total = _calculator.CalculateTotal(totalInput, amountPaid: 5000m);

        Assert.Equal(total.GrandTotal - 5000m, total.BalanceDue);
    }

    [Fact]
    public void CalculateTotal_InterStateVsIntraState_IGSTEqualsDoubledCGST()
    {
        var lineResult = _calculator.Calculate(Chain22K);

        var intraStateInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: new[] { lineResult },
            DiscountAmount: 0m,
            ExchangeValue: 0m,
            IsInterState: false);
        var interStateInput = intraStateInput with { IsInterState = true };

        var intraTotal = _calculator.CalculateTotal(intraStateInput, 0m);
        var interTotal = _calculator.CalculateTotal(interStateInput, 0m);

        Assert.Equal(intraTotal.CGST + intraTotal.SGST, interTotal.IGST);
        Assert.Equal(intraTotal.GrandTotal, interTotal.GrandTotal);
    }

    // ─── 13. Edge Cases & Validation ──────────────────────────────────────────────

    [Fact]
    public void Calculate_ZeroGrossWeight_AllValuesAreZero()
    {
        var input = new GoldPriceCalculator.BillLineInput(
            GrossWeight: 0m,
            StoneWeight: 0m,
            Purity: "22K",
            MakingType: "FIXED",
            MakingValue: 0m,
            WastagePercent: 0m,
            StoneCharge: 0m,
            Rate24KPer10g: 75000m);

        var result = _calculator.Calculate(input);

        Assert.Equal(0m, result.NetWeight);
        Assert.Equal(0m, result.PureGoldWeight);
        Assert.Equal(0m, result.GoldValue);
        Assert.Equal(0m, result.WastageValue);
        Assert.Equal(0m, result.GoldGST);
    }

    [Fact]
    public void Calculate_VeryLargeWeight_DoesNotOverflow()
    {
        // Use a large but safe decimal value (1000 kg of gold)
        var input = new GoldPriceCalculator.BillLineInput(
            GrossWeight: 1_000_000m,
            StoneWeight: 0m,
            Purity: "24K",
            MakingType: "FIXED",
            MakingValue: 0m,
            WastagePercent: 0m,
            StoneCharge: 0m,
            Rate24KPer10g: 75000m);

        // Should not throw
        var result = _calculator.Calculate(input);

        Assert.Equal(1_000_000m, result.NetWeight);
        Assert.Equal(1_000_000m * 7500m, result.GoldValue);
    }

    [Fact]
    public void Calculate_VeryHighGoldRate_ComputesCorrectly()
    {
        var input = Bangle24K with { Rate24KPer10g = 1_000_000m };
        var result = _calculator.Calculate(input);

        Assert.Equal(100_000m, result.RatePerGram);
        // GoldValue = 20g * 1.0 purity * 100000/g
        Assert.Equal(20m * 100_000m, result.GoldValue);
    }
}
