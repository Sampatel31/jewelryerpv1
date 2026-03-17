using GoldSystem.Core.Models;

namespace GoldSystem.Reports.Services;

/// <summary>
/// Contract for generating PDF bill documents.
/// </summary>
public interface IBillPdfService
{
    /// <summary>
    /// Generates a PDF for the given bill and returns the byte array.
    /// </summary>
    byte[] GenerateBillPdf(BillDto bill, string shopName, string shopAddress, string shopPhone);

    /// <summary>
    /// Generates a PDF and saves it to the specified file path.
    /// </summary>
    void SaveBillPdf(BillDto bill, string filePath, string shopName, string shopAddress, string shopPhone);
}
