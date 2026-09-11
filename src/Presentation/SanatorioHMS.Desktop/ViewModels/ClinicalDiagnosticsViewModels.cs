using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanatorioHMS.Api.Client;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Domain.Diagnostics.Entities;

namespace SanatorioHMS.Desktop.ViewModels;

public partial class EpisodeWorkspaceViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private EpisodeResponse? episode;
    [ObservableProperty] private string errorMessage = string.Empty;
    public ObservableCollection<OrderResponse> Orders { get; } = new();
    public bool CanEdit => Episode is not null && !Episode.Status.Equals("Cerrado", StringComparison.OrdinalIgnoreCase);
    public async Task LoadAsync(Guid episodeId)
    {
        try { Episode = await api.GetEpisodeAsync(episodeId); Orders.Clear(); foreach (var order in await api.GetEpisodeOrdersAsync(episodeId) ?? []) Orders.Add(order); OnPropertyChanged(nameof(CanEdit)); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}

public partial class ClinicalNoteEditorViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid episodeId;
    [ObservableProperty] private Guid professionalId;
    [ObservableProperty] private string noteType = "Evolución";
    [ObservableProperty] private string content = string.Empty;
    [ObservableProperty] private bool canEdit = true;
    [ObservableProperty] private string errorMessage = string.Empty;
    [RelayCommand]
    private async Task SaveAsync() { if (!CanEdit) { ErrorMessage = "El episodio está cerrado; agregue una corrección como nueva entrada."; return; } try { await api.AppendClinicalNoteAsync(EpisodeId, new AppendClinicalNoteRequest(EpisodeId, ProfessionalId, NoteType, Content)); Content = string.Empty; ErrorMessage = "Nota agregada."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class OrderEntryViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid episodeId;
    [ObservableProperty] private Guid encounterId;
    [ObservableProperty] private Guid professionalId;
    [ObservableProperty] private string type = "Study";
    [ObservableProperty] private string item = string.Empty;
    [ObservableProperty] private bool canEdit = true;
    [ObservableProperty] private string errorMessage = string.Empty;
    [RelayCommand]
    private async Task SaveAsync() { if (!CanEdit) { ErrorMessage = "El episodio está cerrado."; return; } try { await api.IssueOrderAsync(EpisodeId, new IssueOrderRequest(EpisodeId, EncounterId, ProfessionalId, Type, [Item])); Item = string.Empty; ErrorMessage = "Orden emitida."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class EpisodeClosureViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid episodeId;
    [ObservableProperty] private bool requirementsSatisfied;
    [ObservableProperty] private string errorMessage = string.Empty;
    [RelayCommand]
    private async Task CloseAsync() { if (!RequirementsSatisfied) { ErrorMessage = "Complete los requisitos clínicos antes de cerrar el episodio."; return; } try { var response = await api.CloseEpisodeAsync(EpisodeId); ErrorMessage = response.IsSuccessStatusCode ? "Episodio cerrado." : "No se pudo cerrar el episodio."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class DiagnosticOrderCreationViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid patientId;
    [ObservableProperty] private Guid episodeId;
    [ObservableProperty] private Guid professionalId;
    [ObservableProperty] private Guid studyId;
    [ObservableProperty] private AuthorizationStatus authorization;
    [ObservableProperty] private string priority = "Routine";
    [ObservableProperty] private string preparationInstructions = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    public async Task LoadStudyAsync(Guid id) { StudyId = id; var studies = await api.GetStudiesAsync(); PreparationInstructions = studies?.FirstOrDefault(x => x.Id == id)?.PreparationInstructions ?? string.Empty; }
    [RelayCommand]
    private async Task CreateAsync() { try { await api.CreateDiagnosticOrderAsync(new CreateDiagnosticOrderRequest(PatientId, EpisodeId, ProfessionalId, StudyId, Authorization, Priority)); ErrorMessage = "Orden diagnóstica creada."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class LabTechResultRegistrationViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid orderId;
    [ObservableProperty] private Guid recordedBy;
    [ObservableProperty] private string value = string.Empty;
    [ObservableProperty] private string? unit;
    [ObservableProperty] private AuthorizationStatus authorization = AuthorizationStatus.NotRequired;
    [ObservableProperty] private string errorMessage = string.Empty;
    public bool CanFulfill => Authorization is AuthorizationStatus.NotRequired or AuthorizationStatus.Approved;
    public string FulfillmentReason => CanFulfill ? string.Empty : $"La autorización está {Authorization}; no se puede procesar la orden.";
    partial void OnAuthorizationChanged(AuthorizationStatus value) { OnPropertyChanged(nameof(CanFulfill)); OnPropertyChanged(nameof(FulfillmentReason)); }
    [RelayCommand]
    private async Task RegisterAsync() { if (!CanFulfill) { ErrorMessage = FulfillmentReason; return; } try { await api.RegisterDiagnosticResultAsync(new RegisterDiagnosticResultRequest(OrderId, Value, RecordedBy, Unit)); ErrorMessage = "Resultado registrado."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class DoctorValidationViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid resultId;
    [ObservableProperty] private Guid professionalId;
    [ObservableProperty] private string errorMessage = string.Empty;
    [RelayCommand]
    private async Task ValidateAsync() { try { var response = await api.ValidateDiagnosticResultAsync(ResultId, new ValidateDiagnosticResultRequest(ResultId, ProfessionalId)); ErrorMessage = response.IsSuccessStatusCode ? "Resultado validado." : "No se pudo validar el resultado."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}
