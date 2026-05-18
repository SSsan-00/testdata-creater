using TestDataCreater.Core;

namespace TestDataCreater.Tests;

[TestClass]
public sealed class CellValueKindRulesTests
{
    [TestMethod]
    public void LeadingZeroTextDoesNotAllowNumericKinds()
    {
        IReadOnlyList<CellValueKind> allowed = CellValueKindRules.GetAllowedKinds("01");

        Assert.IsFalse(allowed.Contains(CellValueKind.Int32));
        Assert.IsFalse(allowed.Contains(CellValueKind.Int64));
        Assert.IsFalse(allowed.Contains(CellValueKind.Decimal));
        Assert.IsFalse(allowed.Contains(CellValueKind.Double));
        Assert.IsTrue(allowed.Contains(CellValueKind.String));
    }

    [TestMethod]
    public void CanonicalNumericTextAllowsNumericKinds()
    {
        IReadOnlyList<CellValueKind> allowed = CellValueKindRules.GetAllowedKinds("1200.50");

        Assert.IsFalse(allowed.Contains(CellValueKind.Int32));
        Assert.IsFalse(allowed.Contains(CellValueKind.Int64));
        Assert.IsTrue(allowed.Contains(CellValueKind.Decimal));
        Assert.IsTrue(allowed.Contains(CellValueKind.Double));
    }

    [TestMethod]
    public void BooleanGuidAndDateTimeAreAllowedOnlyWhenTextMatches()
    {
        Assert.IsTrue(CellValueKindRules.GetAllowedKinds("true").Contains(CellValueKind.Boolean));
        Assert.IsTrue(CellValueKindRules.GetAllowedKinds("8f14e45f-ea6d-4ef7-bb2a-25b1b80efed6").Contains(CellValueKind.Guid));
        Assert.IsTrue(CellValueKindRules.GetAllowedKinds("2026-05-19 10:30:00").Contains(CellValueKind.DateTime));

        IReadOnlyList<CellValueKind> allowed = CellValueKindRules.GetAllowedKinds("not-a-value");
        Assert.IsFalse(allowed.Contains(CellValueKind.Boolean));
        Assert.IsFalse(allowed.Contains(CellValueKind.Guid));
        Assert.IsFalse(allowed.Contains(CellValueKind.DateTime));
    }

    [TestMethod]
    public void EmptyTextDoesNotAllowConcreteValueKinds()
    {
        IReadOnlyList<CellValueKind> allowed = CellValueKindRules.GetAllowedKinds("");

        CollectionAssert.AreEqual(
            new[] { CellValueKind.String, CellValueKind.Null, CellValueKind.DbNull, CellValueKind.CustomExpression },
            allowed.ToArray());
    }
}
