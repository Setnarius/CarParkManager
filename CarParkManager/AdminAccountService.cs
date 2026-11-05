using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Entity.Core.EntityClient;

namespace CarParkManager
{
    public sealed class AdminAccount
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }

    internal static class AdminAccountService
    {
        public const string DefaultEmail = "Admin@manager.ru";
        public const string DefaultPassword = "admin99";

        private static string GetProviderConnectionString()
        {
            var entityConnection = ConfigurationManager.ConnectionStrings["FleetDBEntities"];
            if (entityConnection == null)
            {
                throw new InvalidOperationException("Не найдена строка подключения FleetDBEntities.");
            }

            return new EntityConnectionStringBuilder(entityConnection.ConnectionString).ProviderConnectionString;
        }

        public static void EnsureDefaultAdmin()
        {
            using (var connection = new SqlConnection(GetProviderConnectionString()))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID(N'dbo.Administrators', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Administrators
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Administrators PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        Password NVARCHAR(100) NOT NULL,
        CONSTRAINT UQ_Administrators_Email UNIQUE (Email)
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Administrators WHERE Email = @Email)
BEGIN
    INSERT INTO dbo.Administrators (FullName, Email, Password)
    VALUES (@FullName, @Email, @Password);
END;";
                command.Parameters.AddWithValue("@FullName", "Администратор");
                command.Parameters.AddWithValue("@Email", DefaultEmail);
                command.Parameters.AddWithValue("@Password", PasswordHelper.getHashPassword(DefaultPassword));
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public static AdminAccount FindByEmail(string email)
        {
            EnsureDefaultAdmin();

            using (var connection = new SqlConnection(GetProviderConnectionString()))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT TOP (1) Id, FullName, Email, Password
FROM dbo.Administrators WHERE Email = @Email;";
                command.Parameters.AddWithValue("@Email", (email ?? string.Empty).Trim());
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new AdminAccount
                    {
                        Id = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Email = reader.GetString(2),
                        PasswordHash = reader.GetString(3)
                    };
                }
            }
        }

        public static bool EmailExists(string email)
        {
            return FindByEmail(email) != null;
        }
    }
}
