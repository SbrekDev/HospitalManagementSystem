using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanatorioHMS.Api.Client;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Domain.PatientRegistry.Entities;

namespace SanatorioHMS.Desktop.ViewModels;

public partial class PatientSearchViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    public ObservableCollection<PatientResponse> Results { get; } = new();
    [RelayCommand]
    private async Task SearchAsync() { try { Results.Clear(); foreach (var patient in await api.SearchPatientsAsync(SearchText) ?? []) Results.Add(patient); ErrorMessage = string.Empty; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class PatientRegistrationViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string surname = string.Empty;
    [ObservableProperty] private DateTime dateOfBirth = DateTime.Today;
    [ObservableProperty] private string documentNumber = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [RelayCommand]
    private async Task RegisterAsync() { try { await api.CreatePatientAsync(new CreatePatientRequest(Name, Surname, DateOnly.FromDateTime(DateOfBirth), DocumentType.DNI, DocumentNumber)); ErrorMessage = "Paciente registrado correctamente."; } catch (ApiProblemException ex) when (ex.StatusCode == 409) { ErrorMessage = ex.Message; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class PatientDetailViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private PatientResponse? patient;
    [ObservableProperty] private string errorMessage = string.Empty;
    public async Task LoadAsync(Guid patientId) { Patient = await api.GetAsync<PatientResponse>($"api/v1/patients/{patientId}"); }
    [RelayCommand] private async Task SaveAsync() { if (Patient is null) return; var response = await api.PutAsync($"api/v1/patients/{Patient.Id}", new UpdatePatientRequest(Patient.Id, Patient.Name, Patient.Surname, Patient.DateOfBirth)); ErrorMessage = response.IsSuccessStatusCode ? "Cambios guardados." : "No se pudieron guardar los cambios."; }
}

public partial class CoverageEditorViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid patientId;
    [ObservableProperty] private CoverageType type = CoverageType.Particular;
    [ObservableProperty] private string payer = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [RelayCommand] private async Task SaveAsync() { try { await api.AddCoverageAsync(new CoverageRequest(PatientId, Type, Payer)); ErrorMessage = "Cobertura guardada."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class GuardianEditorViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid patientId;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string relationship = string.Empty;
    [ObservableProperty] private string contactData = string.Empty;
    [ObservableProperty] private bool isGuardian;
    [ObservableProperty] private string errorMessage = string.Empty;
    [RelayCommand] private async Task SaveAsync() { try { await api.AddGuardianAsync(new GuardianRequest(PatientId, Name, Relationship, ContactData, IsGuardian)); ErrorMessage = "Tutor guardado."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}
