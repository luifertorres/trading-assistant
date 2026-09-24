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
    public void ShowCorrelation_DefaultIsFalseWhenSumPlotted()
    {
        var selection = new ChartLegendSelection();

        selection.ShowCorrelation.Should().BeFalse();
    }

    [Fact]
    public void ShowCorrelation_TrueWhenBothWalksPlotted()
    {
        var selection = new ChartLegendSelection();
        selection.TryToggle(ChartLegendId.Sum);

        selection.ShowCorrelation.Should().BeTrue();
    }

    [Fact]
    public void ShowCorrelation_FalseWhenOnlyOneWalkPlotted()
    {
        var selection = new ChartLegendSelection();
        selection.TryToggle(ChartLegendId.Sum);
        selection.TryToggle(ChartLegendId.Seed42);

        selection.ShowCorrelation.Should().BeFalse();
    }
}
