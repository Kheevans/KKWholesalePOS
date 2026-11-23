using KKWholesalePOS.Core.Services;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using QuestPDF.Infrastructure;

namespace KKWholesalePOS.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            QuestPDF.Settings.License = LicenseType.Community;

            using var dbContext = new AppDbContext();

            // Repositories
            var userRepository = new UserRepository(dbContext);
            var roleRepository = new RoleRepository(dbContext);
            var auditLogRepository = new AuditLogRepository(dbContext);
            var customerRepository = new CustomerRepository(dbContext);
            var reportingRepository = new ReportingRepository(dbContext);
            var regularSaleRepository = new RegularSaleRepository(dbContext);
            var dashboardRepository = new DashboardRepository(dbContext);
            var permissionRepository = new PermissionRepository(dbContext);
            var businessSettingsRepository = new BusinessSettingsRepository(dbContext);

            // Core services
            var passwordService = new PasswordService();
            var authorizationService = new AuthorizationService();
            var userSessionService = new UserSessionService();

            var businessSettingsService = new BusinessSettingsService(
                businessSettingsRepository,
                userSessionService,
                authorizationService);

            var permissionManagementService = new PermissionManagementService(
                permissionRepository,
                roleRepository,
                userSessionService,
                authorizationService);

            var userManagementService = new UserManagementService(
                userRepository,
                passwordService,
                userSessionService,
                authorizationService);

            var roleManagementService = new RoleManagementService(
                roleRepository,
                userRepository,
                userSessionService,
                authorizationService);

            var auditLogService = new AuditLogService(
                auditLogRepository,
                userSessionService,
                authorizationService);

            var reportingService = new ReportingService(
                reportingRepository,
                userSessionService,
                authorizationService);

            var initialSetupService = new InitialSetupService(
                userRepository,
                userManagementService,
                roleManagementService,
                permissionManagementService,
                auditLogService);

            var dashboardService = new DashboardService(
                dashboardRepository,
                userSessionService,
                authorizationService);

            var regularSaleService = new RegularSaleService(
                regularSaleRepository,
                userSessionService,
                authorizationService);

            // Initial administrator setup
            if (!userRepository.HasAnyUsers())
            {
                using var setupForm = new SetupForm(initialSetupService);

                if (setupForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            // Seed default permissions for both new and existing installations.
            initialSetupService.EnsureDefaultPermissions();

            // Authentication
            var authenticationService = new AuthenticationService(
                userRepository,
                passwordService);

            var loginService = new LoginService(
                authenticationService,
                userSessionService,
                auditLogService);

            using var loginForm = new LoginForm(loginService);

            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm(
                    userSessionService,
                    loginService,
                    customerRepository,
                    reportingService,
                    regularSaleService,
                    dashboardService,
                    userManagementService,
                    roleManagementService,
                    permissionManagementService,
                    auditLogService,
                    authorizationService,
                    businessSettingsService));
            }
        }
    }
}