using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanatorioHMS.Api.Client;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.PatientRegistry;

namespace SanatorioHMS.Desktop.ViewModels;

public sealed record NavigationItem(string Title, string Permission, Type ViewModelType);

public sealed class NavigationService
{
    public static IReadOnlyList<NavigationItem> BuildMenu(IEnumerable<string> permissions)
    {
        var granted = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new[] {
            new NavigationItem("Pacientes", "Patient.Read", typeof(PatientSearchViewModel)),
            new NavigationItem("Registrar paciente", "Patient.Create", typeof(PatientRegistrationViewModel)),
            new NavigationItem("Agenda", "Scheduling.Read", typeof(AgendaGridViewModel))
        }.Where(item => granted.Contains(item.Permission)).ToArray();
    }
}

public partial class LoginViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBusy;
    public event Action<IReadOnlyList<string>>? LoggedIn;

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsBusy = true; ErrorMessage = string.Empty;
        try
        {
            var response = await api.LoginAsync(new LoginRequest(Username, Password));
            if (response is null) { ErrorMessage = "Usuario o contraseña inválidos."; return; }
            var permissions = new[] { "Patient.Read", "Patient.Create", "Patient.Update", "Scheduling.Read", "Scheduling.Reserve", "Scheduling.ChangeStatus" };
            LoggedIn?.Invoke(permissions);
        }
        catch (HttpRequestException) { ErrorMessage = "No se pudo conectar con el servidor."; }
        finally { IsBusy = false; }
    }
}

public partial class ShellViewModel : ObservableObject
{
    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();
    [ObservableProperty] private object? currentView;
    public void Initialize(IEnumerable<string> permissions) { NavigationItems.Clear(); foreach (var item in NavigationService.BuildMenu(permissions)) NavigationItems.Add(item); if (NavigationItems.Count > 0) Navigate(NavigationItems[0]); }
    [RelayCommand] private void Navigate(NavigationItem item) => CurrentView = Activator.CreateInstance(item.ViewModelType);
}
