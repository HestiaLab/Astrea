using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeStateTests
{
    [TestMethod]
    public void SelectedIds_MutableCastCannotChangeInternalState()
    {
        var state = new TreeState();
        state.Select("a");

        if (state.SelectedIds is ISet<string> mutable)
        {
            mutable.Add("injected");
        }

        Assert.IsFalse(state.IsSelected("injected"));
        CollectionAssert.AreEquivalent(new[] { "a" }, state.SelectedIds.ToArray());
    }

    [TestMethod]
    public void Select_SetsSingleIdSelected()
    {
        var state = new TreeState();

        Assert.IsTrue(state.Select("a"));
        Assert.IsTrue(state.IsSelected("a"));
        CollectionAssert.AreEquivalent(new[] { "a" }, state.SelectedIds.ToArray());
    }

    [TestMethod]
    public void Select_ReplacesPreviousSelection()
    {
        var state = new TreeState();
        state.Select("a");

        Assert.IsTrue(state.Select("b"));
        Assert.IsFalse(state.IsSelected("a"));
        Assert.IsTrue(state.IsSelected("b"));
    }

    [TestMethod]
    public void Select_SameId_ReturnsFalse()
    {
        var state = new TreeState();
        state.Select("a");

        Assert.IsFalse(state.Select("a"));
    }

    [TestMethod]
    public void AddToSelection_Appends()
    {
        var state = new TreeState();
        state.Select("a");

        Assert.IsTrue(state.AddToSelection("b"));
        Assert.IsTrue(state.IsSelected("a"));
        Assert.IsTrue(state.IsSelected("b"));
    }

    [TestMethod]
    public void AddToSelection_Duplicate_ReturnsFalse()
    {
        var state = new TreeState();
        state.AddToSelection("a");

        Assert.IsFalse(state.AddToSelection("a"));
    }

    [TestMethod]
    public void ToggleSelection_FlipsState()
    {
        var state = new TreeState();
        state.AddToSelection("a");

        Assert.IsTrue(state.ToggleSelection("a"));
        Assert.IsFalse(state.IsSelected("a"));

        Assert.IsTrue(state.ToggleSelection("a"));
        Assert.IsTrue(state.IsSelected("a"));
    }

    [TestMethod]
    public void ClearSelection_ResetsIds()
    {
        var state = new TreeState();
        state.AddToSelection("a");
        state.AddToSelection("b");

        Assert.IsTrue(state.ClearSelection());
        Assert.AreEqual(0, state.SelectedIds.Count);

        Assert.IsFalse(state.ClearSelection());
    }

    [TestMethod]
    public void ReplaceSelection_DetectsChange()
    {
        var state = new TreeState();
        state.AddToSelection("a");
        state.AddToSelection("b");

        Assert.IsTrue(state.ReplaceSelection(new[] { "b", "c" }));
        CollectionAssert.AreEquivalent(new[] { "b", "c" }, state.SelectedIds.ToArray());

        Assert.IsFalse(state.ReplaceSelection(new[] { "c", "b" }));
    }

    [TestMethod]
    public void SetFocus_MaintainsSingleFocusedInvariant()
    {
        var state = new TreeState();

        Assert.IsTrue(state.SetFocus("a"));
        Assert.IsTrue(state.IsFocused("a"));
        Assert.AreEqual("a", state.FocusedId);

        Assert.IsTrue(state.SetFocus("b"));
        Assert.IsFalse(state.IsFocused("a"));
        Assert.IsTrue(state.IsFocused("b"));
    }

    [TestMethod]
    public void SetFocus_SameId_ReturnsFalse()
    {
        var state = new TreeState();
        state.SetFocus("a");

        Assert.IsFalse(state.SetFocus("a"));
    }

    [TestMethod]
    public void ClearFocus_ResetsFocusedId()
    {
        var state = new TreeState();
        state.SetFocus("a");

        Assert.IsTrue(state.ClearFocus());
        Assert.IsNull(state.FocusedId);

        Assert.IsFalse(state.ClearFocus());
    }

    [TestMethod]
    public void RetainSelected_RemovesMissingIds()
    {
        var state = new TreeState();
        state.AddToSelection("a");
        state.AddToSelection("b");
        state.AddToSelection("c");

        state.RetainSelected(new[] { "a", "c" });

        CollectionAssert.AreEquivalent(new[] { "a", "c" }, state.SelectedIds.ToArray());
    }

    [TestMethod]
    public void RetainFocus_ClearsMissingId()
    {
        var state = new TreeState();
        state.SetFocus("a");

        state.RetainFocus(new[] { "b", "c" });

        Assert.IsNull(state.FocusedId);
    }

    [TestMethod]
    public void RetainFocus_KeepsPresentId()
    {
        var state = new TreeState();
        state.SetFocus("a");

        state.RetainFocus(new[] { "a", "b" });

        Assert.AreEqual("a", state.FocusedId);
    }

    [TestMethod]
    public void IncrementRevision_AdvancesRevision()
    {
        var state = new TreeState();
        long before = state.Revision;

        state.IncrementRevision();

        Assert.AreEqual(before + 1, state.Revision);
    }
}
