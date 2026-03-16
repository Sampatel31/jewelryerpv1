namespace GoldSystem.Core.Constants;

public static class AppConstants
{
    public static class ApplicationInfo
    {
        public const string Name = "Gold Jewellery Management System";
        public const string Version = "1.0.0";
        public const string Company = "GoldSystem";
    }

    public static class Purities
    {
        public const string Gold24K = "24K (999)";
        public const string Gold22K = "22K (916)";
        public const string Gold18K = "18K (750)";
        public const string Gold14K = "14K (583)";
        public const string Gold10K = "10K (417)";
        public const string Gold9K  = "9K (375)";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Gold24K, Gold22K, Gold18K, Gold14K, Gold10K, Gold9K
        };

        public static readonly IReadOnlyDictionary<string, decimal> Fineness = new Dictionary<string, decimal>
        {
            [Gold24K] = 0.999m,
            [Gold22K] = 0.916m,
            [Gold18K] = 0.750m,
            [Gold14K] = 0.583m,
            [Gold10K] = 0.417m,
            [Gold9K]  = 0.375m,
        };
    }

    public static class MakingChargeTypes
    {
        public const string PerGram = "Per Gram";
        public const string Percentage = "Percentage";
        public const string PerPiece = "Per Piece";
        public const string PercentageOnGold = "Percentage on Gold Value";

        public static readonly IReadOnlyList<string> All = new[]
        {
            PerGram, Percentage, PerPiece, PercentageOnGold
        };
    }

    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Salesperson = "Salesperson";
        public const string Cashier = "Cashier";
        public const string Viewer = "Viewer";

        public static readonly IReadOnlyList<string> All = new[]
        {
            SuperAdmin, Admin, Manager, Salesperson, Cashier, Viewer
        };
    }

    public static class PaymentMethods
    {
        public const string Cash = "Cash";
        public const string Card = "Card";
        public const string UPI = "UPI";
        public const string BankTransfer = "Bank Transfer";
        public const string Cheque = "Cheque";
        public const string OldGoldExchange = "Old Gold Exchange";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Cash, Card, UPI, BankTransfer, Cheque, OldGoldExchange
        };
    }

    public static class ItemCategories
    {
        public const string Ring = "Ring";
        public const string Necklace = "Necklace";
        public const string Bracelet = "Bracelet";
        public const string Earrings = "Earrings";
        public const string Pendant = "Pendant";
        public const string Chain = "Chain";
        public const string Bangle = "Bangle";
        public const string Anklet = "Anklet";
        public const string Coin = "Coin";
        public const string Bar = "Bar";
        public const string Other = "Other";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Ring, Necklace, Bracelet, Earrings, Pendant,
            Chain, Bangle, Anklet, Coin, Bar, Other
        };
    }

    public static class Database
    {
        public const string DefaultSqliteFileName = "GoldSystem.db";
        public const string ConnectionStringName = "GoldSystemDb";
    }

    public static class Cache
    {
        public const int GoldRateCacheDurationMinutes = 15;
        public const int DefaultCacheDurationMinutes = 60;
    }
}
