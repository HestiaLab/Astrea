using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeVirtualizationPolicyTests
{
    [TestMethod]
    public void Disabled_WithInvalidConfig_DoesNotThrow()
    {
        TreeVirtualizationPolicy.Validate(
            enableVirtualization: false,
            itemSize: 0f,
            hasNodeTemplate: true,
            hasEditTemplate: true);
    }

    [TestMethod]
    public void Enabled_ItemSizeZero_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            TreeVirtualizationPolicy.Validate(
                enableVirtualization: true,
                itemSize: 0f,
                hasNodeTemplate: false,
                hasEditTemplate: false));
    }

    [TestMethod]
    public void Enabled_ItemSizeNegative_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            TreeVirtualizationPolicy.Validate(
                enableVirtualization: true,
                itemSize: -1f,
                hasNodeTemplate: false,
                hasEditTemplate: false));
    }

    [TestMethod]
    public void Enabled_NodeTemplateSet_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            TreeVirtualizationPolicy.Validate(
                enableVirtualization: true,
                itemSize: 32f,
                hasNodeTemplate: true,
                hasEditTemplate: false));
    }

    [TestMethod]
    public void Enabled_EditTemplateSet_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            TreeVirtualizationPolicy.Validate(
                enableVirtualization: true,
                itemSize: 32f,
                hasNodeTemplate: false,
                hasEditTemplate: true));
    }

    [TestMethod]
    public void Enabled_ValidFixedHeightConfig_DoesNotThrow()
    {
        TreeVirtualizationPolicy.Validate(
            enableVirtualization: true,
            itemSize: 32f,
            hasNodeTemplate: false,
            hasEditTemplate: false);
    }
}
