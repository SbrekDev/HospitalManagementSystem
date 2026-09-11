using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;

namespace SanatorioHMS.Infrastructure.Data;

public static class SeedData
{
    private const string UnassignedSpecialty = "Sin Especialidad Asignada";

    public static async Task SeedAsync(AuthDbContext db, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == "Admin", cancellationToken);
        if (role is null)
        {
            role = new Role(Guid.NewGuid(), "Admin");
            db.Roles.Add(role);
        }

        var permission = await db.Permissions.SingleOrDefaultAsync(x => x.Code == "Admin.Access", cancellationToken);
        if (permission is null)
        {
            permission = new Permission(Guid.NewGuid(), "Admin.Access");
            db.Permissions.Add(permission);
        }

        var rolePermissionExists = await db.Set<RolePermission>()
            .AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id, cancellationToken);
        if (!rolePermissionExists)
        {
            db.Set<RolePermission>().Add(new RolePermission(role.Id, permission.Id));
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == "admin", cancellationToken);
        if (user is null)
        {
            user = new User(Guid.NewGuid(), "admin");
            user.SetPasswordHash(new PasswordHasher<User>().HashPassword(user, "Admin123!"));
            db.Users.Add(user);
        }

        var userRoleExists = await db.Set<UserRole>()
            .AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, cancellationToken);
        if (!userRoleExists)
        {
            db.Set<UserRole>().Add(new UserRole(user.Id, role.Id));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static async Task SeedCatalogsAsync(SchedulingDbContext db, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var specialtyNames = SpecialtyNames;
        var specialties = await db.Set<Specialty>().ToDictionaryAsync(x => x.Nombre, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var (name, index) in specialtyNames.Select((name, index) => (name, index)))
        {
            if (!specialties.ContainsKey(name))
            {
                var specialty = new Specialty { Id = Guid.NewGuid(), Codigo = $"ESP-{index + 1:000}", Nombre = name, Activo = true };
                db.Set<Specialty>().Add(specialty);
                specialties[name] = specialty;
            }
        }

        if (!specialties.TryGetValue(UnassignedSpecialty, out var unassigned))
        {
            unassigned = new Specialty { Id = Guid.NewGuid(), Codigo = "ESP-000", Nombre = UnassignedSpecialty, Descripcion = "Registro temporal para profesionales sin sector informado.", Activo = true };
            db.Set<Specialty>().Add(unassigned);
            specialties[unassigned.Nombre] = unassigned;
        }

        var existingProfessionals = await db.Set<Professional>().ToListAsync(cancellationToken);
        var byName = existingProfessionals.ToDictionary(x => $"{x.Apellido}|{x.Nombre}", StringComparer.OrdinalIgnoreCase);
        var links = new List<ProfessionalSpecialty>();
        var imported = Professionals;
        for (var i = 0; i < imported.Count; i++)
        {
            var source = imported[i];
            var key = $"{source.LastName}|{source.FirstName}";
            if (!byName.TryGetValue(key, out var professional))
            {
                professional = new Professional
                {
                    Id = Guid.NewGuid(),
                    NroDocumento = $"{40000000 + i:00000000}",
                    TipoDocumento = "DNI",
                    Apellido = source.LastName,
                    Nombre = source.FirstName,
                    MatriculaProvincial = $"MP-{10000 + i:00000}",
                    MatriculaNacional = $"MN-{20000 + i:00000}",
                    FechaIngreso = new DateTime(2024, 1, 1),
                    Activo = true
                };
                db.Set<Professional>().Add(professional);
                byName[key] = professional;
            }

            foreach (var specialtyName in source.Specialties.DefaultIfEmpty(UnassignedSpecialty).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!specialties.TryGetValue(specialtyName, out var specialty)) specialty = unassigned;
                if (!await db.Set<ProfessionalSpecialty>().AnyAsync(x => x.ProfessionalId == professional.Id && x.SpecialtyId == specialty.Id, cancellationToken))
                {
                    links.Add(new ProfessionalSpecialty { ProfessionalId = professional.Id, SpecialtyId = specialty.Id, FechaAsignacion = new DateTime(2024, 1, 1), EsPrincipal = source.Principal == specialtyName, Activo = true });
                }
            }
        }

        if (links.Count > 0) db.Set<ProfessionalSpecialty>().AddRange(links);
        var roomNames = new[] { "Sala 1", "Sala 2", "Sala 3", "Sala 4", "Consultorio 1", "Consultorio 2", "Consultorio 3", "Consultorio 4", "Diagnóstico por Imágenes", "Sala de Procedimientos" };
        var existingRooms = await db.Set<Room>().Select(x => x.Nombre).ToListAsync(cancellationToken);
        db.Set<Room>().AddRange(roomNames.Where(x => !existingRooms.Contains(x, StringComparer.OrdinalIgnoreCase)).Select(x => new Room { Id = Guid.NewGuid(), Nombre = x, Activo = true }));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static async Task SeedStudiesAsync(DiagnosticsDbContext db, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var existing = (await db.Set<Study>().Select(x => x.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, index) in StudyNames.Select((name, index) => (name, index)))
        {
            var code = $"LOINC-{index + 1:000}";
            if (existing.Contains(code)) continue;
            db.Set<Study>().Add(Study.Create(code, name, IsImaging(name) ? StudyModality.Imaging : StudyModality.Laboratory, preparationInstructions: PreparationFor(name)));
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static bool IsImaging(string name) => name.Contains("ECO", StringComparison.OrdinalIgnoreCase) || name.Contains("Ecograf", StringComparison.OrdinalIgnoreCase) || name.Contains("Tomograf", StringComparison.OrdinalIgnoreCase) || name.Contains("Resonancia", StringComparison.OrdinalIgnoreCase) || name.Contains("Radiolog", StringComparison.OrdinalIgnoreCase) || name.Contains("Mamograf", StringComparison.OrdinalIgnoreCase) || name.Contains("Fluoroscopia", StringComparison.OrdinalIgnoreCase) || name.Contains("OCT", StringComparison.OrdinalIgnoreCase) || name.Contains("Ortopantomograf", StringComparison.OrdinalIgnoreCase) || name.Contains("Paquimetr", StringComparison.OrdinalIgnoreCase) || name.Contains("Tonometr", StringComparison.OrdinalIgnoreCase) || name.Contains("Topograf", StringComparison.OrdinalIgnoreCase) || name.Contains("Campo Visual", StringComparison.OrdinalIgnoreCase) || name.Contains("Autorrefractometr", StringComparison.OrdinalIgnoreCase) || name.Contains("Biometría", StringComparison.OrdinalIgnoreCase);

    private static string? PreparationFor(string name) => name.Contains("Aire Espirado", StringComparison.OrdinalIgnoreCase) ? "Ayuno de 8 horas y seguir las indicaciones del servicio." : name.Contains("Papanicolaou", StringComparison.OrdinalIgnoreCase) ? "Evitar relaciones sexuales y productos vaginales durante 48 horas." : null;

    private sealed record ProfessionalSeed(string LastName, string FirstName, string[] Specialties, string? Principal = null);

    // The public directory does not publish DNI or matrícula. Values are deterministic synthetic identifiers for development data.
    private static List<ProfessionalSeed> Professionals =>
    [
        new("Abasto", "Angel Ernesto", ["Psiquiatría"]), new("Acosta", "Beatriz", ["Emergencias"]), new("Acosta", "Marianela Yael", ["Dermatología"]),
        new("Albornoz", "Maria de las Mercedes", ["Endocrinología"]), new("Ale", "Said Omar", ["Anestesiología"]), new("Alfaro Calderon", "Mariana Elizabeth", ["Medicina General y Familiar", "Tabaquismo"]),
        new("Angeloro Acosta", "Pablo Andres", ["Terapia Intensiva"]), new("Arbelo", "Mayra", ["Genética"]), new("Arce", "Jonathan Sebastian", ["Clínica Médica"]), new("Arce Morari", "Martín Demetrio", ["Gastroenterología y Enfermedades Digestivas"]),
        new("Arenillas", "Fernando Cesar", ["Oncología"]), new("Arnolds", "Werner Alfredo", [UnassignedSpecialty]), new("Ayala Cushicondor", "Ramiro Alejandro", ["Cardiología", "Terapia Intensiva", "Emergencias"], "Emergencias"),
        new("Baroli", "Estefania Belén", ["Diagnóstico y Tratamiento del Dolor"]), new("Baron", "María Gabriela", ["Clínica Médica", "Hepatología"]), new("Barraud Dobanton", "Carlos René", ["Flebología"]), new("Bartoli", "Agustín", ["Cardiología", "Terapia Intensiva"]),
        new("Bender Schulz", "Paola Natali", ["Ginecología y Obstetricia"]), new("Bernahrdt", "Evelin", [UnassignedSpecialty]), new("Bernhardt Leal", "Roy Alfredo", ["Cirugía General", "Endoscopía Digestiva", "Gastroenterología y Enfermedades Digestivas"]),
        new("Bernhardt Tardivo", "Roy Ariel", ["Ginecología y Obstetricia"]), new("Bernhardt Vivas", "Leylen Yanina", ["Pediatría", "Neonatología"], "Neonatología"), new("Bishop Maier", "Sergio Dario", ["Anestesiología"]),
        new("Block", "Glyn Milton", ["Oftalmología"]), new("Boffelli Dupertuis", "Sergio Damian", ["Cirugía General", "Endoscopía Digestiva", "Gastroenterología y Enfermedades Digestivas"]), new("Bonzi", "Leonardo Rafael", ["Urología"]),
        new("Borrino Morel", "Diego Gaston", ["Cirugía Plástica y Reparadora"]), new("Borrino Morel", "Erika Magali", ["Medicina General y Familiar"]), new("Bottaro Lascano", "Fernanda", ["Fisiatría y Rehabilitación"]),
        new("Britez Bartumeus", "Lucas Matías", ["Urología"], "Urología"), new("Broder Killer", "Elsa Valeria", ["Cardiología", "Medicina Vascular", "Medicina del Estilo de Vida"]), new("Brunetti Esquivel", "Alicia Bibiana", ["Pediatría"]),
        new("Caisso", "Maria Veronica", ["Psiquiatría"]), new("Cali", "Nicolás Alejandro", ["Alergia e Inmunología"]), new("Calvo", "Fanny Gretel", ["Cardiología"]), new("Cantero", "Noemí", ["Odontología"]),
        new("Capellino", "Valentina Gabriela", ["Clínica Médica"]), new("Carmona", "Melisa", ["Odontología"]), new("Cartagena", "Marianela Betsabé", ["Pediatría", "Neonatología"]), new("Castaldo", "Orlando Jose", ["Ortopedia y Traumatología"]),
        new("Castillo Salazar", "Juan Carlos", ["Diagnóstico por Imágenes"]), new("Cedeño", "Melanie", [UnassignedSpecialty]), new("Colazo", "Anabella Belén", ["Pediatría", "Nefrología Infantil"]), new("Cortés", "Pamela", ["Emergencias"]),
        new("Cousillas Gomez", "Javier Emilio", ["Neurología"]), new("Cruz", "Giovanni", ["Guardia y Urgencias"]), new("D'Alessandro Vicari", "María Milagros", ["Sexología Clínica"]), new("Daneri Blanco", "Gustavo Humberto", ["Cirugía General", "Cirugía Digestiva"], "Cirugía General"),
        new("Daneri", "María Florencia", ["Diagnóstico por Imágenes"]), new("Daniele Fernandez", "Alida Noemi", ["Psicología"]), new("Davila Peralta", "Antonio Eduardo", ["Alergia e Inmunología"]), new("Davila Peralta", "Esteban Javier", ["Psiquiatría"], "Psiquiatría"),
        new("Decco", "Marianela Patricia", ["Diagnóstico por Imágenes"]), new("De Dios", "Denisse Gisele", ["Emergencias"]), new("Della Giustina Auzqui", "Maria Virginia", ["Anatomía Patológica"], "Anatomía Patológica"), new("Di Dionisio Kalbermatter", "Leroy Gustavo", ["Oftalmología"]),
        new("Dubs", "María Alejandra", ["Pediatría", "Neonatología"]), new("Duluc", "Beltran", ["Otorrinolaringología"]), new("Dutto", "María Mónica", ["Dermatología"]), new("Escandriolo", "Jorge", ["Odontología"]),
        new("Escudero Grelmann", "Cristina Pamela", ["Oncología"]), new("Espíndola", "Germán Darío", ["Medicina General y Familiar"]), new("Espinosa", "María Belen", ["Clínica Médica"]), new("Femopase", "Giselle", ["Medicina del Estilo de Vida"]),
        new("Fernández", "María Evangelina", ["Nutrición"]), new("Forni Rodriguez", "Ruben Alejandro", ["Medicina General y Familiar"]), new("Franco", "Glenda Lucia", ["Cirugía Infantil", "Pediatría", "Urología Pediátrica"]), new("Freitas Mones", "Mariana", ["Clínica Médica"]),
        new("Frick", "Edith", ["Psicodiagnóstico"]), new("Fritzler", "Guillermo Iván", ["Ortopedia y Traumatología"]), new("Fumero Priano", "Gabriel Nelson", ["Oftalmología"], "Oftalmología"), new("Galante", "Ronald Daniel", ["Medicina del Deporte y Nutrición"]),
        new("Garcia Santos", "Victor Adriel", ["Ginecología y Obstetricia"]), new("Garcia Vera", "Alejandro Antonio", ["Ortopedia y Traumatología"]), new("Garrigós", "Gustavo", ["Odontología"], "Odontología"), new("Gay", "Mariana", ["Diagnóstico por Imágenes"]),
        new("Gervasini", "Santiago Ignacio", ["Ginecología y Obstetricia", "Medicina Reproductiva", "Fertilidad"]), new("Gianinni", "Silvina", ["Odontología"]), new("Godoy Lafarga", "Maria Soledad", ["Diagnóstico por Imágenes"]), new("Goette", "Melina Belen", ["Clínica Médica"]),
        new("Goltz Frickel", "Carla", ["Nutrición", "Diabetología", "Cirugía Bariátrica"]), new("Gomez", "Martin Eugenio", ["Ortopedia y Traumatología"], "Ortopedia y Traumatología"), new("Gomez Tchilinguirian", "Ariel Sergio", ["Clínica Médica"]), new("Gonzalez", "Daniela Soledad", ["Pediatría"]),
        new("González", "Marcos", ["Anestesiología"]), new("González Nena", "Gabriela", [UnassignedSpecialty]), new("Gonzalez", "Pamela Karen", ["Psicología"]), new("Grancelli", "Sofía", ["Anestesiología"]), new("Grubert", "Diego", ["Odontología"]),
        new("Guarnaschelli Lemus", "Marlen Elisabet", ["Neurología"]), new("Guevara La Malfa", "Rosa Raquel", ["Clínica Médica"]), new("Guillen", "Raul Horacio", ["Electrofisiología Cardíaca", "Cardiología"]), new("Gutierrez Saez", "Marianela Elsa", ["Anestesiología"]), new("Gutierrez Saez", "Marisel", ["Neuropsicología", "Evaluación Neurocognitiva"]),
        new("Haenggi Bovier", "Federico", ["Ortopedia y Traumatología"]), new("Hardy", "Alexis", ["Terapia Intensiva"]), new("Hardy", "Carina", ["Fonoaudiología"]), new("Heinze", "Walter Gustavo", ["Psicología"]), new("Heissenberg Bognar", "Daniel Marcelo", ["Gerontología"]),
        new("Hernandez Nuñez", "Leslie", ["Ginecología y Obstetricia"]), new("Hilt", "Brenda", ["Diagnóstico por Imágenes"]), new("Hirle", "Walane", ["Clínica Médica"]), new("Iglesias Cabanay", "Mauricio Javier", ["Diagnóstico por Imágenes"]), new("Incahuanaco Paricahua", "Liberth", ["Clínica Médica", "Gastroenterología y Enfermedades Digestivas"]),
        new("Ingui", "Jose Ignacio", ["Diagnóstico por Imágenes"]), new("Iurno Alsanoglou", "Christian Eduardo", ["Cardiología"]), new("Jacob Zapata", "Matias Francisco", ["Ortopedia y Traumatología"]), new("Jensen", "María Virginia", ["Gastroenterología y Enfermedades Digestivas"]), new("Jurczuk Toranzo", "Rafael Ivan", ["Psiquiatría"]),
        new("Kalbermatter", "Guillermo", ["Terapia Intensiva"]), new("Kalbermatter Lottersberger", "Javier Reinaldo", ["Clínica Médica"], "Clínica Médica"), new("Kalbermatter Wöhr", "Arnoldo Miguel", ["Cardiología"]), new("Klos", "Yesica", ["Nutrición"]), new("Koloszwa Skorubsky", "Benjamin Teodoro", ["Cardiología"]),
        new("Korniejczuk", "Edgar Ariel", ["Neumonología"]), new("Kreitzer", "Lucia", ["Ortopedia y Traumatología"]), new("Lell", "Flavia Vanesa", ["Pediatría", "Neonatología"]), new("Liebich Frasqueri", "Carlos Andres", ["Diagnóstico por Imágenes"]), new("Lobos", "María de los Angeles", ["Diagnóstico por Imágenes"]),
        new("Lopez Chiappesoni", "Andrea María del Lujan", ["Terapia Intensiva"]), new("Lopez", "Jorge Luis", ["Cardiología Nuclear"]), new("Loson", "Maria Victoria", ["Flebología"]), new("Manrique", "Gerardo Oscar", ["Diagnóstico por Imágenes"], "Diagnóstico por Imágenes"), new("Mantilla Simon", "Luis Emilio", ["Cardiología"], "Cardiología"),
        new("Manucci", "Yanina", ["Psiquiatría"]), new("Marker", "Elizabeth", ["Kinesiología y Fisioterapia"]), new("Marquez Chada", "Juan Cruz", ["Otorrinolaringología"]), new("Martinez García", "Evelyn Esther", ["Ginecología y Obstetricia"]), new("Martinez Milutinovic", "Sabrina", ["Clínica Médica", "Hematología adultos", "Hemoterapia e Inmunohematología"]),
        new("Martiniano Da Silva Junior", "Jurandir", ["Ortopedia y Traumatología"]), new("Melgar Taglang", "Carolina", ["Cardiología"]), new("Melgar Taglang", "Evangelina", ["Psiquiatría"]), new("Mentasti", "José", ["Pediatría", "Neumonología Infantil"]), new("Miranda Saez", "Angel Esteban", ["Clínica Médica"]),
        new("Molina Albornoz", "Victor Hugo", ["Anatomía Patológica"]), new("Montecinos Salazar", "Jorge Andres Martin", ["Cardiología", "Hemodinamia y Cardioangiología Intervencionista"]), new("Morales", "Gastón", ["Cirugía General", "Cirugía Digestiva"]), new("Morales", "María Celeste", ["Diagnóstico por Imágenes"]), new("Moreno", "Ailin", ["Clínica Médica", "Medicina del Estilo de Vida"]),
        new("Morero", "Laura", ["Odontología"]), new("Morra", "Brian", ["Ortopedia y Traumatología"]), new("Morra Herdt", "Daniel Ricardo", ["Ortopedia y Traumatología"]), new("Morra", "Jonathan", ["Anestesiología"]), new("Mota", "Manuel Andrés", ["Anestesiología"]),
        new("Muller", "Ingrid", ["Nutrición"], "Nutrición"), new("Muller Svemer", "Walter Gerardo", ["Ginecología y Obstetricia"], "Ginecología y Obstetricia"), new("Natella", "Delfina", ["Terapia Ocupacional"]), new("Navarro Silva", "Enrique Gustavo", ["Ortopedia y Traumatología"]), new("Neiff", "Gonzalo", ["Terapia Intensiva"]),
        new("Nestares", "Sebastian Nicolas", ["Ortopedia y Traumatología"]), new("Nicolini Lai", "Julian Alejandro", ["Electrofisiología Cardíaca", "Cardiología"]), new("Nikolaus Heidenreich", "Juan Pablo", ["Cirugía de Cabeza y Cuello", "Cirugía General"]), new("Nuñez", "Angie", [UnassignedSpecialty]), new("Nuñez", "Erwin Oscar", ["Ortopedia y Traumatología"]),
        new("Orellana", "Kenneth José", ["Anestesiología"]), new("Orona", "Diego", ["Kinesiología y Fisioterapia"], "Kinesiología y Fisioterapia"), new("Páez", "Carla Ailen", ["Clínica Médica"]), new("Pais", "Alejandro Bernardo", ["Gastroenterología Infantil", "Pediatría"]), new("Patiño Gumucio", "Alvaro David", ["Cirugía General"]),
        new("Perales Callejas", "Iver", [UnassignedSpecialty]), new("Perales Mamani", "Humberto Rodney", ["Urología"]), new("Peretti", "Mariana Paola", ["Ginecología y Obstetricia"]), new("Pereyra Cousiño", "Magaly Ines", ["Ginecología y Obstetricia"]), new("Perez Baroni", "Maria Silvana", [UnassignedSpecialty]),
        new("Perez", "Rosendo Augusto", ["Anestesiología"]), new("Perrota", "Sofia", ["Otorrinolaringología"]), new("Picart", "Magali", ["Kinesiología y Fisioterapia"]), new("Pitana", "Milton", ["Kinesiología y Fisioterapia"]), new("Ponce", "Ruben", ["Odontología"]),
        new("Previale Boero", "Carlos Alberto", ["Oncología", "Clínica Médica"], "Oncología"), new("Previale", "Cecilia Andrea", ["Anatomía Patológica"]), new("Pujato Temporetti", "Sebastían", ["Anestesiología"]), new("Quinteros", "Facundo Manuel", ["Anestesiología"]), new("Quiroga", "Gabriel Andrés", ["Infectología", "Clínica Médica"]),
        new("Ramal Calderón", "Juan Carlos", ["Diagnóstico por Imágenes"]), new("Reinhard", "Sheila Janet", ["Otorrinolaringología"]), new("Rettore", "Martin Oscar", ["Medicina Nuclear"], "Medicina Nuclear"), new("Reyes Silva", "Marta Elba", ["Anestesiología"]), new("Rivera", "Lissette", ["Ortopedia y Traumatología"]),
        new("Rodriguez", "Gisela", ["Neurodesarrollo", "Orientación parental prenatal y posnatal"]), new("Rodriguez Molina", "Flavio", ["Clínica Médica"]), new("Rojas Bonventre", "Haroldo Moises", ["Infectología", "Clínica Médica"]), new("Romero Godoy", "Cristian David", ["Diagnóstico por Imágenes"]), new("Rossetto", "Luis Enrique", ["Cirugía Cardiovascular"]),
        new("Sakamoto", "Juan Francisco", ["Hematología adultos", "Hemoterapia e Inmunohematología"]), new("Salazar Saenz", "Nohemi Gabriela", ["Neonatología"]), new("Salcerini", "Marcia", ["Anatomía Patológica"]), new("Santa María", "José Ignacio", ["Neurocirugía"]), new("Saralegui", "Eliseo Martin", ["Cirugía de Cabeza y Cuello", "Cirugía General"]),
        new("Sato Pacheco", "Clissian", ["Nefrología"]), new("Schmidt", "Kenneth christian", ["Cardiología"]), new("Schmidt Weiss", "Carlos Daniel", ["Cardiología"]), new("Schreiber", "Ivana Beatriz", ["Clínica Médica"]), new("Schulz", "Doris Lilian", ["Medicina General y Familiar"]),
        new("Schwab", "Carina", ["Neuropsicología"]), new("Seidel", "Irina", ["Kinesiología y Fisioterapia"]), new("Simón", "Eduardo", ["Anatomía Patológica"]), new("Slame", "Amira Fiorella", ["Ginecología y Obstetricia"]), new("Sorace", "Lilia Yanina", ["Oftalmología"]),
        new("Sotelo", "Berta Alicia", ["Pediatría"]), new("Sotelo Calierno", "Andrea Roxana", ["Neurología"]), new("Spinetto", "María Andrea", ["Reumatología"]), new("Steger Gregorio", "Haroldo Raul", ["Cirugía General", "Cirugía Bariátrica"]), new("Stoletniy Barlocco", "Enrique Gabriel", ["Cirugía General", "Cirugía Digestiva"]),
        new("Stoletniy", "Christian Alexander", ["Cirugía General", "Cirugía Digestiva"]), new("Strong", "Sheila", ["Kinesiología y Fisioterapia"]), new("Sucasaca Añamuro", "Heber Jacinto", ["Cirugía General"]), new("Suschevich", "Emilce", ["Nutrición"]), new("Tamay", "Karine Alessandra", ["Fonoaudiología"]),
        new("Tanaka", "Valeria", ["Odontología"]), new("Tiscornia Gross", "Alicia Mabel", ["Anatomía Patológica"]), new("Treiyer", "Walter", [UnassignedSpecialty]), new("Trevisan", "Andrea Pamela", ["Terapia Intensiva", "Cardiología"], "Terapia Intensiva"), new("Troncoso", "Vasti", ["Psiquiatría"]),
        new("Tymkow", "María Eladia", ["Clínica Médica", "Diabetología"]), new("Utz Graf", "Norberto Ricardo", ["Otorrinolaringología"]), new("Utz", "Leylen", ["Odontología"]), new("Vales", "Daniela", ["Fonoaudiología"]), new("Varela Frias", "Eva Beatriz", ["Hepatología", "Clínica Médica"]),
        new("Vasquez Vergara", "Silvana Andrea", ["Endocrinología", "Diabetología"]), new("Velozo Lazcano", "Edson Javier", ["Reumatología"]), new("Vera", "Paola", ["Nutrición"]), new("Vitor", "Dario Oscar", ["Neurocirugía"]), new("Wengrovsky Rostan", "Marcos", ["Psicología"]),
        new("Yáñez", "Marina", ["Psiquiatría"]), new("Yanzon", "Carolina Evelin Judit", ["Cardiología"]), new("Yauri Quinto", "Jorge Amilcar", ["Fisiatría y Rehabilitación"]), new("Zapata", "María Paola", ["Reumatología"]), new("Zawadzki Desia", "Nestor Ivan", ["Pediatría"]),
        new("Zela Rojas", "Carlos Daniel", ["Cirugía General", "Cirugía Percutánea", "Cirugía Hepatobiliopancreática"]), new("Zincunegui", "Josefina", ["Ortopedia y Traumatología"])
    ];

    private static readonly string[] SpecialtyNames = [
        "Alergia e Inmunología", "Anatomía Patológica", "Anestesiología", "Cámara Gamma y Medicina Nuclear", "Cardiología", "Cardiología Nuclear", "Cirugía Bariátrica", "Cirugía Cardiovascular", "Cirugía de Cabeza y Cuello", "Cirugía del Hígado y Vías Biliares y Páncreas", "Cirugía Digestiva", "Cirugía General", "Cirugía Hepatobiliopancreática", "Cirugía Infantil", "Cirugía Oftalmológico", "Cirugía Percutánea", "Cirugía Plástica y Reparadora", "Cirugía Torácica", "Clínica Médica", "Coloproctología", "Cuidado de Heridas Crónicas", "Cuidados Paliativos", "Dermatología", "Diabetología", "Diagnóstico por Imágenes", "Diagnóstico y Tratamiento del Dolor", "Electrofisiología Cardíaca", "Embarazo de Alto Riesgo", "Emergencias", "Endocrinología", "Endoscopía Digestiva", "Evaluación Neurocognitiva", "Fertilidad", "Fisiatría y Rehabilitación", "Flebología", "Fonoaudiología", "Gastroenterología Infantil", "Gastroenterología y Enfermedades Digestivas", "Genética", "Gerontología", "Ginecología y Obstetricia", "Guardia y Urgencias", "Hematología adultos", "Hemodinamia y Cardioangiología Intervencionista", "Hemoterapia e Inmunohematología", "Hepatología", "Infectología", "Kinesiología y Fisioterapia", "Mastología y Cirugía Mamaria", "Medicina del Deporte y Nutrición", "Medicina del Estilo de Vida", "Medicina General y Familiar", "Medicina Nuclear", "Medicina Reproductiva", "Medicina Vascular", "Nefrología", "Nefrología Infantil", "Neonatología", "Neumonología", "Neumonología Infantil", "Neurocirugía", "Neurodesarrollo", "Neurofisiología", "Neurología", "Neuropsicología", "Nutrición", "Obesidad", "Odontología", "Oftalmología", "Oncología", "Orientación parental prenatal y posnatal", "Ortopedia y Traumatología", "Ortopedia y Traumatología Infantil", "Otorrinolaringología", "Pediatría", "Pie diabético", "Psicodiagnóstico", "Psicología", "Psicopedagogía", "Psiquiatría", "Reumatología", "Reumatología Infantil", "Sexología Clínica", "Tabaquismo", "Terapia Intensiva", "Terapia Ocupacional", "Urología", "Urología Pediátrica", UnassignedSpecialty
    ];

    private static readonly string[] StudyNames = [
        "Administración controlada de medicación", "Autorrefractometría", "Biometría láser", "Campo Visual Computarizado", "Cardioversión química y eléctrica", "Control desfibrilador", "CPRE con Litotricia Mecánica", "CPRE con Papilotomía y extracción de litios", "CPRE y prótesis biliares o drenaje nasobiliares", "Densitometría", "Dilatación colónica", "Dilataciones esofágicas", "Dilatación esofágica", "ECO Doppler abdominal", "ECO Doppler cardíaco", "ECO Doppler cavo ilíaco", "ECO Doppler hepático", "ECO Doppler miembro inferior venoso y arterial", "ECO Doppler renal", "ECO Doppler stress físico", "ECO Doppler stress químico", "ECO Doppler tisular", "ECO Doppler transesofágico", "ECO Doppler transesofágico intraoperatorio", "ECO Doppler vasos de cuello", "Ecografías de la mujer", "Ecografías generales", "Ecografías Gineco-obstétricas", "Ecografías obstetricas de alto riesgo", "Electrocardiograma", "Electromiografía y potenciales evocados", "Endoscopia Digestiva", "Ergometría computada", "Estudio de función endotelial", "Extracción de cuerpo extraño", "Fluoroscopia", "Gastrostomía Endoscópica", "Histeroscopia", "Holter de 24 Hs", "Intervencionismo guiado por imágenes", "Ligadura várices esofágicas", "Mamografías digitales", "MAPA (presurometría)", "Marcaje de lesiones con tinta china", "Monitoreo intraoperatorio", "Mucosectomía colónica", "Mucosectomía gástrica", "Oftalmoscopio Binocular Indirecto", "Ortopantomografía", "Papanicolaou", "Paquimetría", "Radiología digital", "Recambio de prótesis Biliar", "Reprogramación marcapaso", "Resonancia magnética de alta resolución con resonador de alto campo (1.5 Tesla)", "Test a medios físicos", "Test del Aire Espirado (SIBO)", "Test de parche a contactantes cosméticos y químicos", "Test de parche parcial", "Test de parches para alimentos", "Testificación alérgica parcial", "Testificación alérgica total", "Testificación insectos", "Testificación medios de contraste", "Testificación para evaluación de alergia a drogas", "TILT TEST", "Tomografía cardíaca y vascular avanzada", "Tomografía computada de alta resolución de todo el cuerpo", "Tomografía de coherencia óptica (OCT)", "Tomografías multicorte con reconstrucciones 3D/4D en tomógrafo de 64/128 detectores", "Tonometría", "Topografía Cornea", "Tratamiento end. hemorragia dig. Alta", "Tratamiento end. hemorragia dig. Baja", "Vacunas para aeroalérgenos Ácaros", "Vacunas para aeroalérgenos Hongos ambientales", "Vacunas para aeroalérgenos Pólenes", "Vacunas para aeroalérgenos Venenos e insectos", "Video colonoscopía (VCC)", "Video Colposcopía", "Video endocápsula", "Video endoscopía diagnóstica alta (veda)", "Video polipectomía colónica", "Video polipectomía gástrica", "Video rectosigmoidoscopía", "V.O.P (Velocidad de Onda de Pulso)"
    ];
}
