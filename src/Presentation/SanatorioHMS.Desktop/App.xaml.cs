using System.Net.Http.Headers;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SanatorioHMS.Api.Client;
using SanatorioHMS.Desktop.ViewModels;

namespace SanatorioHMS.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<HmsApiClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5000/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddSingleton<NavigationService>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<PatientSearchViewModel>();
        services.AddTransient<PatientRegistrationViewModel>();
        services.AddTransient<PatientDetailViewModel>();
        services.AddTransient<CoverageEditorViewModel>();
        services.AddTransient<GuardianEditorViewModel>();
        services.AddTransient<AgendaGridViewModel>();
        services.AddTransient<BookingDialogViewModel>();
        services.AddTransient<TurnStatusActionsViewModel>();
        Services = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var login = Services.GetRequiredService<LoginViewModel>();
        var window = new MainWindow { DataContext = login };
        login.LoggedIn += permissions => window.DataContext = Services.GetRequiredService<ShellViewModel>();
        login.LoggedIn += permissions => ((ShellViewModel)window.DataContext).Initialize(permissions);
        window.Show();
    }
}

