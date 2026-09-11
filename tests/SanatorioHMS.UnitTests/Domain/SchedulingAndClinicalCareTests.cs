using SanatorioHMS.Domain.ClinicalCare.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;

namespace SanatorioHMS.UnitTests.Domain;

public sealed class SchedulingAndClinicalCareTests
{
    [Fact]
    public void ProfessionalCreateSetsRequiredFields()
    {
        var professional = new Professional
        {
            Id = Guid.NewGuid(),
            NroDocumento = "12345678",
            TipoDocumento = "DNI",
            Apellido = "Perez",
            Nombre = "Juan"
        };

        Assert.True(professional.Activo);
        Assert.NotNull(professional.ProfessionalSpecialties);
    }

    [Fact]
    public void TurnoEstadoIsValid()
    {
        var turno = new Turno { Estado = "Reservado" };

        Assert.True(turno.IsValidStatus);
        Assert.True(turno.CanTransitionTo("Confirmado"));
    }

    [Fact]
    public void TurnoRejectsInvalidTransition()
    {
        var turno = new Turno { Estado = "Atendido" };

        Assert.Throws<InvalidOperationException>(() => turno.TransitionTo("Cancelado"));
    }

    [Fact]
    public void TurnoRequiresRoomForInPerson()
    {
        var turno = new Turno { Tipo = "Presencial" };

        Assert.Throws<InvalidOperationException>(turno.ValidateAllocation);
    }

    [Fact]
    public void EpisodeDoesNotCloseWithoutRequirements()
    {
        var episode = new Episode { Estado = "Activo" };

        Assert.Throws<InvalidOperationException>(() => episode.Close(DateTime.UtcNow, false));
        Assert.Equal("Activo", episode.Estado);
    }

    [Fact]
    public void EpisodeBlocksClosedContent()
    {
        var episode = new Episode { Estado = "Activo" };
        episode.Close(DateTime.UtcNow, true);

        Assert.Throws<InvalidOperationException>(episode.EnsureOpen);
    }

    [Fact]
    public void EpisodeIgnoresDuplicateClinicalNoteSubmission()
    {
        var episode = new Episode { Estado = "Activo" };
        var note = new ClinicalNote { Id = Guid.NewGuid(), Contenido = "Evolution" };

        Assert.True(episode.AddClinicalNote(note));
        Assert.False(episode.AddClinicalNote(note));
        Assert.Single(episode.ClinicalNotes);
    }
}
