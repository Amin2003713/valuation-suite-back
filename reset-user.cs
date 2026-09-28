// One-shot dev helper: resets a local user's password to a known value and
// reactivates the account, for bootstrapping an admin login during smoke tests.
//
// Run:  dotnet run --file reset-user.cs <email> <newPassword>
#:package Microsoft.Data.SqlClient@5.2.2
#:package Microsoft.Extensions.Identity.Core@9.0.9
#:package Microsoft.Extensions.Identity.Stores@9.0.9

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

if (args.Length < 2)
{
    Console.WriteLine("usage: dotnet run --file reset-user.cs <email> <newPassword>");
    return 1;
}
var email = args[0];
var password = args[1];

var cs = "Server=localhost,5012;Database=ValuationSuite;User Id=sa;Password=Iu%7D%3A-%2Bw6eP%5EOY*qhgFtpwRAF;TrustServerCertificate=True;";

var hasher = new PasswordHasher<Microsoft.AspNetCore.Identity.IdentityUser>();
var hash = hasher.HashPassword(new Microsoft.AspNetCore.Identity.IdentityUser(), password);

await using var conn = new SqlConnection(cs);
await conn.OpenAsync();

const string sql = """
    UPDATE AspNetUsers
    SET PasswordHash = @hash,
        IsActive = 1,
        LockoutEnd = NULL,
        AccessFailedCount = 0
    WHERE NormalizedEmail = @email;
    SELECT @@ROWCOUNT;
    """;
await using var cmd = new SqlCommand(sql, conn);
cmd.Parameters.AddWithValue("@hash", hash);
cmd.Parameters.AddWithValue("@email", email.ToUpperInvariant());

var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync());
Console.WriteLine($"updated rows: {rows}");
return rows > 0 ? 0 : 2;
