using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.UI.Services;

namespace NexusWorkspace.UI.ViewModels.People;

public partial class PeopleViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool _showArchived;

    [ObservableProperty]
    private bool _isCreatePanelOpen;

    [ObservableProperty]
    private string _newPersonName = string.Empty;

    [ObservableProperty]
    private string _newPersonRole = string.Empty;

    [ObservableProperty]
    private string _newPersonEmail = string.Empty;

    [ObservableProperty]
    private CompanyOption? _newPersonCompany;

    [ObservableProperty]
    private TagFilterOption? _selectedTag;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<PersonListItem> People { get; } = [];

    public ObservableCollection<CompanyOption> Companies { get; } = [];

    public ObservableCollection<TagFilterOption> TagFilters { get; } = [];

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
            var scope = ShowArchived ? PersonScope.Archived : PersonScope.Active;
            var tagId = SelectedTag?.Id;

            var data = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var people = await sp.GetRequiredService<PersonReadService>().GetListAsync(scope, SearchText, tagId, ct);
                var companies = await sp.GetRequiredService<CompanyReadService>().GetListAsync(CompanyScope.Active, null, null, ct);
                var tags = await sp.GetRequiredService<TagReadService>().GetAllAsync(false, ct);
                return (people, companies, tags);
            });

            People.Reset(data.people);
            IsEmpty = People.Count == 0;

            SyncCompanies(data.companies);
            SyncTagFilters(data.tags);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar las personas: {ex.Message}";
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
            ResetForm();
        }
    }

    [RelayCommand]
    private void OpenPerson(PersonListItem? person)
    {
        if (person is not null)
        {
            navigation.NavigateTo<PersonDetailViewModel>(vm => vm.Load(person.Id));
        }
    }

    [RelayCommand]
    private async Task CreatePersonAsync()
    {
        var name = NewPersonName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Escribe un nombre.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<PersonService>().CreateAsync(new CreatePersonRequest
                {
                    Name = name,
                    Role = NewPersonRole,
                    Email = NewPersonEmail,
                    CompanyId = NewPersonCompany?.Id,
                }, ct));

            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            IsCreatePanelOpen = false;
            ResetForm();
            await RefreshAsync();
            navigation.NavigateTo<PersonDetailViewModel>(vm => vm.Load(result.Value));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo crear la persona: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task ToggleFavoriteAsync(PersonListItem? person)
        => person is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) => sp.GetRequiredService<PersonService>().SetFavoriteAsync(person.Id, !person.IsFavorite, ct));

    private void SyncCompanies(IReadOnlyList<CompanyListItem> companies)
    {
        Companies.Reset(companies.Select(c => new CompanyOption(c.Id, c.Name)));
    }

    private void SyncTagFilters(IReadOnlyList<TagListItem> tags)
    {
        var current = SelectedTag?.Id;
        var options = new List<TagFilterOption> { TagFilterOption.All };
        options.AddRange(tags.Select(t => new TagFilterOption(t.Id, t.Name)));
        TagFilters.Reset(options);
        SelectedTag = options.FirstOrDefault(o => o.Id == current) ?? TagFilterOption.All;
    }

    private void ResetForm()
    {
        NewPersonName = string.Empty;
        NewPersonRole = string.Empty;
        NewPersonEmail = string.Empty;
        NewPersonCompany = null;
    }

    private async Task RunAndRefreshAsync(Func<IServiceProvider, CancellationToken, Task<Application.Common.Result>> operation)
    {
        try
        {
            var result = await unitOfWork.RunAsync(operation);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

public sealed record CompanyOption(Guid Id, string Name);

public sealed record TagFilterOption(Guid? Id, string Name)
{
    public static TagFilterOption All { get; } = new(null, "Todas las etiquetas");
}
