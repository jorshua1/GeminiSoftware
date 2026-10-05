using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VetClinic.Domain.Interfaces;
using VetClinic.Domain.Interfaces.Repositories;
using VetClinic.Domain.Interfaces.Services;
using VetClinic.Infrastructure.Data;
using VetClinic.Infrastructure.Repositories;
using VetClinic.Infrastructure.Security;
using VetClinic.Infrastructure.Services;
using VetClinic.Presentation.Services;
using VetClinic.Presentation.ViewModels;
using VetClinic.Presentation.Views;

namespace VetClinic.Presentation;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Fallback to software rendering to avoid GPU context failures in emulated/virtual environments
        System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        // Inicializar base de datos SQLite local, triggers y datos semilla (STF-01..05)
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await DbInitializer.InitializeAsync(dbContext, passwordHasher);
        }

        var loginView = _serviceProvider.GetRequiredService<LoginView>();
        var shellView = _serviceProvider.GetRequiredService<ShellView>();
        var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
        var shellViewModel = _serviceProvider.GetRequiredService<ShellViewModel>();

        loginViewModel.LoginSucceeded += usuario =>
        {
            shellViewModel.UsuarioActivo = usuario.NombreCompleto;
            shellViewModel.EjecutarNavegacion("Propietarios");
            shellView.Show();
            loginView.Hide();
        };

        shellViewModel.CerrarSesionSolicitado += () =>
        {
            shellView.Hide();
            loginViewModel.Password = string.Empty;
            loginView.Show();
        };

        loginView.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infraestructura y Datos
        services.AddDbContext<VetClinicDbContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddScoped<IPropietarioRepository, PropietarioRepository>();
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IAtencionClinicaRepository, AtencionClinicaRepository>();
        services.AddScoped<IInmunizacionRepository, InmunizacionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClinicaService, ClinicaService>();
        services.AddScoped<IPdfExportService, QuestPdfExportService>();
        services.AddSingleton<IExternalLauncherService, ExternalLauncherService>();

        // Servicios de Presentación
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<PropietariosViewModel>();
        services.AddTransient<PropietarioModalViewModel>();
        services.AddTransient<PacientesViewModel>();
        services.AddTransient<HistoriaClinicaViewModel>();
        services.AddTransient<NuevaAtencionViewModel>();
        services.AddTransient<InmunizacionesViewModel>();
        services.AddTransient<RecordatoriosViewModel>();

        // Vistas
        services.AddSingleton<LoginView>();
        services.AddSingleton<ShellView>();
        services.AddTransient<PropietariosView>();
        services.AddTransient<PropietarioModalView>();
        services.AddTransient<PacientesView>();
        services.AddTransient<HistoriaClinicaView>();
        services.AddTransient<NuevaAtencionModalView>();
        services.AddTransient<InmunizacionesView>();
        services.AddTransient<RecordatoriosView>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
