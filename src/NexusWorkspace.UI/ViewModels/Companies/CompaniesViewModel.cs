using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.People;

namespace NexusWorkspace.UI.ViewModels.Companies;

public partial class CompaniesViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool _showArchived;

    [ObservableProperty]
    private bool _isCreatePanelOpen;

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private CompanyKind _newKind = CompanyKind.Client;

    [ObservableProperty]
    private string _newWebsite = string.Empty;

    [ObservableProperty]
    private TagFilterOption? _selectedTag;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<CompanyListItem> Companies { get; } = [];

    public ObservableCollection<TagFilterOption> TagFilters { get; } = [];

    public IReadOnlyList<CompanyKind> Kinds { get; } = Enum.GetValues<CompanyKind>();

    public override Task OnActivatedAsync() => RefreshAsync();

    partial void OnShowArchivedChanged(bool value) => _ = RefreshAsync();

    partial void OnSearchTextChanged(string? value) => _ = RefreshAsync();

    partial void OnSelectedTagChanged(TagFilterOption? value) => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var scope = ShowArchived ? CompanyScope.Archived : CompanyScope.Active;
            var tagId = SelectedTag?.Id;

            var data = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var companies = await sp.GetRequiredService<CompanyReadService>().GetListAsync(scope, SearchText, tagId, ct);
                var tags = await sp.GetRequiredService<TagReadService>().GetAllAsync(false, ct);
                return (companies, tags);
            });

            Companies.Reset(data.companies);
            IsEmpty = Companies.Count == 0;

            var current = SelectedTag?.Id;
            var options = new List<TagFilterOption> { TagFilterOption.All };
            options.AddRange(data.tags.Select(t => new TagFilterOption(t.Id, t.Name)));
            TagFilters.Reset(options);
            SelectedTag = options.FirstOrDefault(o => o.Id == current) ?? TagFilterOption.All;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar las empresas: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleCreatePanel()
    {
        IsCreatePanelOpen = !IsCreatePanelOpen;
        if (!IsCreatePanelOpen)
        {
            NewName = string.Empty;
            NewKind = CompanyKind.Client;
            NewWebsite = string.Empty;
        }
    }

    [RelayCommand]
    private void OpenCompany(CompanyListItem? company)
    {
        if (company is not null)
        {
            navigation.NavigateTo<CompanyDetailViewModel>(vm => vm.Load(company.Id));
        }
    }

    [RelayCommand]
    private async Task CreateCompanyAsync()
    {
        var name = NewName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Escribe un nombre.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<CompanyService>().CreateAsync(new CreateCompanyRequest
                {
                    Name = name,
                    Kind = NewKind,
                    Website = NewWebsite,
                }, ct));

            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            IsCreatePanelOpen = false;
            NewName = string.Empty;
            NewWebsite = string.Empty;
            await RefreshAsync();
            navigation.NavigateTo<CompanyDetailViewModel>(vm => vm.Load(result.Value));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo crear la empresa: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(CompanyListItem? company)
    {
        if (company is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<CompanyService>().SetFavoriteAsync(company.Id, !company.IsFavorite, ct));
        await RefreshAsync();
    }
}
