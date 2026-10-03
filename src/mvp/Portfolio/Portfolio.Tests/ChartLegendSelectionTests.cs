using FluentAssertions;

namespace Portfolio.Tests;

public sealed class ChartLegendSelectionTests
{
    [Fact]
    public void Default_PlottedIsSumOnly()
    {
        var selection = new ChartLegendSelection();

        selection.Plotted.Should().Equal(ChartLegendId.Sum);
        selection.IsEnabled(ChartLegendId.Seed42).Should().BeTrue();
        selection.IsEnabled(ChartLegendId.Seed7).Should().BeTrue();
        selection.IsEnabled(ChartLegendId.Sum).Should().BeTrue();
    }

    [Fact]
    public void ToggleSumOff_ShowsEnabledWalks()
    {
        var selection = new ChartLegendSelection();

        selection.TryToggle(ChartLegendId.Sum).Should().BeTrue();

        selection.Plotted.Should().Equal(ChartLegendId.Seed42, ChartLegendId.Seed7);
        selection.IsEnabled(ChartLegendId.Sum).Should().BeFalse();
    }

    [Fact]
    public void ToggleSumOff_WhenBothWalksDisabled_DoesNothing()
    {
        var selection = new ChartLegendSelection();
        selection.TryToggle(ChartLegendId.Seed42);
        selection.TryToggle(ChartLegendId.Seed7);

        selection.TryToggle(ChartLegendId.Sum).Should().BeFalse();

        selection.Plotted.Should().Equal(ChartLegendId.Sum);
        selection.IsEnabled(ChartLegendId.Sum).Should().BeTrue();
    }

    [Fact]
    public void ToggleWalkWhileSumPlotted_FlipsFlagWithoutChangingPlotted()
    {
        var selection = new ChartLegendSelection();

        selection.TryToggle(ChartLegendId.Seed42).Should().BeTrue();

        selection.Plotted.Should().Equal(ChartLegendId.Sum);
        selection.IsEnabled(ChartLegendId.Seed42).Should().BeFalse();
    }

    [Fact]
    public void ToggleWalkWhileIndividualsPlotted_ShowsOrHidesThatWalk()
    {
        var selection = new ChartLegendSelection();
        selection.TryToggle(ChartLegendId.Sum);

        selection.TryToggle(ChartLegendId.Seed42).Should().BeTrue();

        selection.Plotted.Should().Equal(ChartLegendId.Seed7);
        selection.IsEnabled(ChartLegendId.Seed42).Should().BeFalse();
    }

    [Fact]
    public void ToggleLastVisibleWalk_DoesNothing()
    {
        var selection = new ChartLegendSelection();
        selection.TryToggle(ChartLegendId.Sum);
        selection.TryToggle(ChartLegendId.Seed7);

        selection.TryToggle(ChartLegendId.Seed42).Should().BeFalse();

        selection.Plotted.Should().Equal(ChartLegendId.Seed42);
    }

    [Fact]
    public void ToggleSumOn_ReturnsToSumOnly()
    {
        var selection = new ChartLegendSelection();
        selection.TryToggle(ChartLegendId.Sum);
        selection.TryToggle(ChartLegendId.Seed7);

        selection.TryToggle(ChartLegendId.Sum).Should().BeTrue();

        selection.Plotted.Should().Equal(ChartLegendId.Sum);
        selection.IsEnabled(ChartLegendId.Sum).Should().BeTrue();
    }

    [Fact]
    public void ShowDrawdown_DefaultIsFalseWhenSumPlotted()
    {
        var selection = new ChartLegendSelection();

        selection.ShowDrawdown.Should().BeFalse();
    }

    [Fact]
    public void ShowDrawdown_TrueWhenBothWalksPlotted()
    {
        var selection = new ChartLegendSelection();
        selection.TryToggle(ChartLegendId.Sum);

        selection.ShowDrawdown.Should().BeTrue();
    }

    [Fact]
    public void ShowDrawdown_FalseWhenOnlyOneWalkPlotted()
    {
        var selection = new ChartLegendSelection();
        selection.TryToggle(ChartLegendId.Sum);
        selection.TryToggle(ChartLegendId.Seed42);

        selection.ShowDrawdown.Should().BeFalse();
    }

    [Fact]
    public void ThreeWalk_ToggleSumOff_ShowsAllThreeWalks()
    {
        var selection = ThreeWalkSelection();

        selection.TryToggle(ChartLegendId.Sum).Should().BeTrue();

        selection.Plotted.Should().Equal(ChartLegendId.Seed42, ChartLegendId.Seed7, ChartLegendId.Seed13);
    }

    [Fact]
    public void ThreeWalk_ShowDrawdown_TrueWhenAllThreeWalksPlotted()
    {
        var selection = ThreeWalkSelection();
        selection.TryToggle(ChartLegendId.Sum);

        selection.ShowDrawdown.Should().BeTrue();
    }

    [Fact]
    public void ThreeWalk_ShowDrawdown_TrueWhenExactlyTwoWalksEnabled()
    {
        var selection = ThreeWalkSelection();
        selection.TryToggle(ChartLegendId.Sum);
        selection.TryToggle(ChartLegendId.Seed13);

        selection.ShowDrawdown.Should().BeTrue();
        selection.Plotted.Should().Equal(ChartLegendId.Seed42, ChartLegendId.Seed7);
    }

    private static ChartLegendSelection ThreeWalkSelection() =>
        new([ChartLegendId.Seed42, ChartLegendId.Seed7, ChartLegendId.Seed13]);
}
