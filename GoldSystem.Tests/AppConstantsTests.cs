using GoldSystem.Core.Constants;

namespace GoldSystem.Tests;

/// <summary>
/// Smoke tests to verify core constants and infrastructure are wired up correctly.
/// Full tests will be added in Phase 2.
/// </summary>
public class AppConstantsTests
{
    [Fact]
    public void Purities_All_ContainsExpectedValues()
    {
        Assert.Contains(AppConstants.Purities.Gold22K, AppConstants.Purities.All);
        Assert.Contains(AppConstants.Purities.Gold18K, AppConstants.Purities.All);
    }

    [Fact]
    public void Roles_All_ContainsExpectedValues()
    {
        Assert.Contains(AppConstants.Roles.Admin, AppConstants.Roles.All);
        Assert.Contains(AppConstants.Roles.Salesperson, AppConstants.Roles.All);
    }

    [Fact]
    public void Purities_Fineness_Gold22K_IsCorrect()
    {
        Assert.Equal(0.916m, AppConstants.Purities.Fineness[AppConstants.Purities.Gold22K]);
    }

    [Fact]
    public void Purities_Fineness_Gold18K_IsCorrect()
    {
        Assert.Equal(0.750m, AppConstants.Purities.Fineness[AppConstants.Purities.Gold18K]);
    }
}
