using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanatorioHMS.Api.Client;
using SanatorioHMS.Application.Scheduling;

namespace SanatorioHMS.Desktop.ViewModels;

public partial class AgendaGridViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private string professionalFilter = string.Empty;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private string errorMessage = string.Empty;
    public ObservableCollection<TurnResponse> Turns { get; } = new();
    [RelayCommand]
    private async Task RefreshAsync() { try { Turns.Clear(); foreach (var turn in await api.GetTurnsAsync() ?? []) Turns.Add(turn); ErrorMessage = string.Empty; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class BookingDialogViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private Guid agendaId;
    [ObservableProperty] private Guid patientId;
    [ObservableProperty] private DateTime start = DateTime.Now;
    [ObservableProperty] private string modality = "Presencial";
    [ObservableProperty] private Guid? roomId;
    [ObservableProperty] private string? teleconsultationDestination;
    [ObservableProperty] private string? reason;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool canReserve = true;
    [RelayCommand]
    private async Task ReserveAsync() { try { var result = await api.ReserveTurnAsync(new ReserveTurnRequest(AgendaId, PatientId, Start, Modality, RoomId, TeleconsultationDestination, Reason)); CanReserve = result is not null; ErrorMessage = "Turno reservado."; } catch (ApiProblemException) { CanReserve = false; ErrorMessage = "El horario está ocupado. Seleccione otro horario."; } catch (Exception ex) { ErrorMessage = ex.Message; } }
}

public partial class TurnStatusActionsViewModel(HmsApiClient api) : ObservableObject
{
    [ObservableProperty] private TurnResponse? selectedTurn;
    [ObservableProperty] private string errorMessage = string.Empty;
    [RelayCommand] private Task ConfirmAsync() => ChangeStatusAsync("Confirmado");
    [RelayCommand] private Task CancelAsync() => ChangeStatusAsync("Cancelado");
    [RelayCommand] private Task MarkNoShowAsync() => ChangeStatusAsync("NoAsistio");
    private async Task ChangeStatusAsync(string status) { if (SelectedTurn is null) return; var response = await api.ChangeTurnStatusAsync(SelectedTurn.Id, new ChangeTurnStatusRequest(SelectedTurn.Id, status)); ErrorMessage = response.IsSuccessStatusCode ? "Estado actualizado." : "No se pudo actualizar el estado."; }
}
