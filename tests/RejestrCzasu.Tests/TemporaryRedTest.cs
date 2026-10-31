namespace RejestrCzasu.Tests;

// TYMCZASOWY: sprawdzenie, że ochrona main blokuje PR z czerwonym testem (historia 4.1). Usunąć przed scaleniem.
public sealed class TemporaryRedTest
{
    [Fact]
    public void BranchProtection_BlocksMerge_WhenTestFails() => Assert.Fail("Celowo czerwony test — sprawdzenie ochrony main.");
}
