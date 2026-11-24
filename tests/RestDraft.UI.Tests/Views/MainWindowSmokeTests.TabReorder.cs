using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using RestDraft.Domain.Collections;
using RestDraft.UI.Behaviors;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;

namespace RestDraft.UI.Tests.Views;

// Dragging request tabs to a new place in the strip. The tabs are "Stored", "X" and "Y", in that order.
public partial class MainWindowSmokeTests
{
    private const double LeftHalf = 0.25;
    private const double RightHalf = 0.75;
    private const double BeyondTheLastTab = -1;

    [TestCase(0, 2, RightHalf, "X,Y,Stored")]
    [TestCase(0, 2, LeftHalf, "X,Stored,Y")]
    [TestCase(2, 0, LeftHalf, "Y,Stored,X")]
    [TestCase(2, 0, RightHalf, "Stored,Y,X")]
    [TestCase(1, 2, RightHalf, "Stored,Y,X")]
    [TestCase(0, 1, LeftHalf, "Stored,X,Y")]
    [TestCase(1, 0, RightHalf, "Stored,X,Y")]
    [TestCase(0, 2, BeyondTheLastTab, "X,Y,Stored")]
    [AvaloniaTest]
    public void DroppingATabOnAnotherTabMovesItThere(int source, int target, double fraction, string expected)
    {
        OpenWindow(new RequestDefinition { Id = Guid.NewGuid(), Name = "X" }, new RequestDefinition { Id = Guid.NewGuid(), Name = "Y" });
        var tabs = _viewModel.TabStrip.Tabs.ToList();
        var selected = _viewModel.TabStrip.SelectedTab;
        var indicator = TabDropIndicator();

        var point = fraction == BeyondTheLastTab ? PointBeyondTheLastTab() : PointInTab(tabs[target], fraction);
        DragTabTo(tabs[source], point);

        Assert.That(string.Join(",", _viewModel.TabStrip.Tabs.Select(tab => tab.Name)), Is.EqualTo(expected));
        Assert.That(string.Join(",", TabHeadersLeftToRight().Select(tab => tab.Name)), Is.EqualTo(expected));
        Assert.That(_viewModel.TabStrip.SelectedTab, Is.SameAs(selected));
        Assert.That(indicator.IsVisible, Is.False);
    }

    [AvaloniaTest]
    public void TheSelectionStaysOnTheSelectedTabWhenAnotherTabIsDragged()
    {
        OpenWindow(new RequestDefinition { Id = Guid.NewGuid(), Name = "X" }, new RequestDefinition { Id = Guid.NewGuid(), Name = "Y" });
        var tabs = _viewModel.TabStrip.Tabs.ToList();
        _viewModel.TabStrip.SelectedTab = tabs[1];
        Dispatcher.UIThread.RunJobs();

        DragTabTo(tabs[0], PointInTab(tabs[2], RightHalf));

        Assert.That(_viewModel.TabStrip.SelectedTab, Is.SameAs(tabs[1]));
    }

    // While a tab is over another one, a line shows at the edge where it would land.
    [TestCase(LeftHalf, false)]
    [TestCase(RightHalf, true)]
    [AvaloniaTest]
    public void ALineMarksTheEdgeOfTheTabUnderTheDraggedTab(double fraction, bool atRightEdge)
    {
        OpenWindow(new RequestDefinition { Id = Guid.NewGuid(), Name = "X" }, new RequestDefinition { Id = Guid.NewGuid(), Name = "Y" });
        var tabs = _viewModel.TabStrip.Tabs.ToList();
        var target = RequestTabHeader(tabs[2]);
        var indicator = TabDropIndicator();
        var point = PointInTab(tabs[2], fraction);
        var transfer = DragTransfer.Create(TabReorder.TabFormat, tabs[0]);

        _window.DragDrop(point, RawDragEventType.DragEnter, transfer, DragDropEffects.Move, RawInputModifiers.None);
        _window.DragDrop(point, RawDragEventType.DragOver, transfer, DragDropEffects.Move, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        var area = TabArea();
        var edge = target.TranslatePoint(new Point(atRightEdge ? target.Bounds.Width : 0, 0), area)!.Value.X;
        Assert.That(indicator.IsVisible, Is.True);
        Assert.That(indicator.Classes, Does.Contain("dropLine"));
        Assert.That(indicator.Margin.Left + (indicator.Width / 2), Is.EqualTo(edge).Within(0.5));
    }

    // A tree node or anything else that is not a tab is not accepted by the strip.
    [AvaloniaTest]
    public void ADraggedTreeNodeDoesNotShowTheTabLine()
    {
        OpenWindow(new RequestDefinition { Id = Guid.NewGuid(), Name = "X" });
        var tabs = _viewModel.TabStrip.Tabs.ToList();
        var node = _viewModel.Sidebar.Roots[0];
        var transfer = DragTransfer.Create(CollectionsTreeView.NodeFormat, node);
        var point = PointInTab(tabs[1], RightHalf);

        _window.DragDrop(point, RawDragEventType.DragEnter, transfer, DragDropEffects.Move, RawInputModifiers.None);
        _window.DragDrop(point, RawDragEventType.DragOver, transfer, DragDropEffects.Move, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.That(TabDropIndicator().IsVisible, Is.False);
    }

    private void DragTabTo(RequestTabViewModel tab, Point point)
    {
        var transfer = DragTransfer.Create(TabReorder.TabFormat, tab);
        _window.DragDrop(point, RawDragEventType.DragEnter, transfer, DragDropEffects.Move, RawInputModifiers.None);
        _window.DragDrop(point, RawDragEventType.DragOver, transfer, DragDropEffects.Move, RawInputModifiers.None);
        _window.DragDrop(point, RawDragEventType.Drop, transfer, DragDropEffects.Move, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private Border TabDropIndicator()
    {
        return _window.GetVisualDescendants().OfType<Border>().First(border => border.Name == "TabDropIndicatorBorder");
    }

    private Grid TabArea()
    {
        return _window.GetVisualDescendants().OfType<Grid>().First(grid => grid.Name == "RequestArea");
    }

    private TabControl TabStrip()
    {
        return _window.GetVisualDescendants().OfType<TabControl>().First(control => control.Name == "OpenRequestTabs");
    }

    // Only the strip: the panel below it has tabs of its own, with the selected request as their data context.
    private TabItem RequestTabHeader(RequestTabViewModel tab)
    {
        return TabStrip().GetVisualDescendants().OfType<TabItem>().First(item => ReferenceEquals(item.DataContext, tab));
    }

    private List<RequestTabViewModel> TabHeadersLeftToRight()
    {
        return TabStrip().GetVisualDescendants().OfType<TabItem>()
            .Where(item => item.DataContext is RequestTabViewModel)
            .OrderBy(item => item.TranslatePoint(new Point(0, 0), _window)!.Value.X)
            .Select(item => (RequestTabViewModel)item.DataContext!)
            .ToList();
    }

    private Point PointInTab(RequestTabViewModel tab, double fraction)
    {
        var item = RequestTabHeader(tab);
        return item.TranslatePoint(new Point(item.Bounds.Width * fraction, item.Bounds.Height / 2), _window)!.Value;
    }

    // The strip is wider than its tabs, so the right end of it is empty.
    private Point PointBeyondTheLastTab()
    {
        var strip = TabStrip();
        var last = TabHeadersLeftToRight().Last();
        var y = RequestTabHeader(last).TranslatePoint(new Point(0, RequestTabHeader(last).Bounds.Height / 2), _window)!.Value.Y;
        return new Point(strip.TranslatePoint(new Point(strip.Bounds.Width - 5, 0), _window)!.Value.X, y);
    }
}
