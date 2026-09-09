using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.ViewModels.Projects;

namespace NexusWorkspace.UI.Views.Projects;

public partial class ProjectDetailView : UserControl
{
    private const string CardFormat = "nexus/kanban-card";

    private WorkTaskListItem? _draggedCard;
    private bool _cardDragging;

    public ProjectDetailView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnColumnDragOver);
        AddHandler(DragDrop.DropEvent, OnColumnDrop);
    }

    private void OnTaskClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control { Tag: WorkTaskListItem item }
            && DataContext is ProjectDetailViewModel viewModel
            && viewModel.OpenTaskCommand.CanExecute(item))
        {
            viewModel.OpenTaskCommand.Execute(item);
        }
    }

    private async void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || sender is not Control { Tag: WorkTaskListItem card })
        {
            return;
        }

        _draggedCard = card;
        _cardDragging = true;

        var data = new DataObject();
        data.Set(CardFormat, card.Id.ToString());

        try
        {
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }
        finally
        {
            _cardDragging = false;
            _draggedCard = null;
        }
    }

    private void OnCardClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (_cardDragging)
        {
            return;
        }

        if (sender is Control { Tag: WorkTaskListItem card }
            && DataContext is ProjectDetailViewModel viewModel
            && viewModel.OpenTaskCommand.CanExecute(card))
        {
            viewModel.OpenTaskCommand.Execute(card);
        }
    }

    private void OnColumnDragOver(object? sender, DragEventArgs e)
        => e.DragEffects = e.Data.Contains(CardFormat) && ColumnUnder(e) is not null
            ? DragDropEffects.Move
            : DragDropEffects.None;

    private void OnColumnDrop(object? sender, DragEventArgs e)
    {
        if (ColumnUnder(e) is { } column
            && _draggedCard is { } card
            && card.Status != column.Status
            && DataContext is ProjectDetailViewModel viewModel
            && viewModel.MoveCardCommand.CanExecute((card, column.Status)))
        {
            viewModel.MoveCardCommand.Execute((card, column.Status));
        }
    }

    private static KanbanColumn? ColumnUnder(DragEventArgs e)
        => (e.Source as Visual)?
            .GetSelfAndVisualAncestors()
            .OfType<Border>()
            .Select(b => b.DataContext)
            .OfType<KanbanColumn>()
            .FirstOrDefault();
}
