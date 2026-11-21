using KKWholesalePOS.Core.Services;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;

namespace KKWholesalePOS.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            using var dbContext = new AppDbContext();

            // Repositories
            var userRepository = new UserRepository(dbContext);
            var roleRepository = new RoleRepository(dbContext);
            var auditLogRepository = new AuditLogRepository(dbContext);
            var customerRepository = new CustomerRepository(dbContext);

            // Core services
            var passwordService = new PasswordService();

            var userManagementService = new UserManagementService(
                userRepository,
                passwordService);

            var roleManagementService = new RoleManagementService(
                roleRepository,
                userRepository);

            var auditLogService = new AuditLogService(
                auditLogRepository);

            var initialSetupService = new InitialSetupService(
                userRepository,
                userManagementService,
                roleManagementService,
                auditLogService);

            // Initial administrator setup
            if (!userRepository.HasAnyUsers())
            {
                using var setupForm = new SetupForm(initialSetupService);

                if (setupForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            // Authentication
            var authenticationService = new AuthenticationService(
                userRepository,
                passwordService);

            var userSession = new UserSessionService();

            var loginService = new LoginService(
                authenticationService,
                userSession,
                auditLogService);

            using var loginForm = new LoginForm(loginService);

            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm(
                    userSession,
                    loginService,
                    customerRepository));
            }
        }
    }
}