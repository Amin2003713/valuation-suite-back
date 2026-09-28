// One-shot smoke-test helper: marks the advice payment (by Zarinpal authority)
// as Paid — exactly what the verified gateway callback does via MarkPaid — so
// the advice-thread unlock can be verified end to end without the sandbox UI.
//
// Run:  dotnet run --file mark-paid.cs <authority>
#:package Microsoft.Data.SqlClient@5.2.2

using Microsoft.Data.SqlClient;

if (args.Length < 1)
{
    Console.WriteLine("usage: dotnet run --file mark-paid.cs <authority>");
    return 1;
}
var authority = args[0];

// Identical literal to appsettings.json — SqlClient parses it the same way the app does.
var cs = "Server=localhost,5012;Database=ValuationSuite;User Id=sa;Password=Iu%7D%3A-%2Bw6eP%5EOY*qhgFtpwRAF;TrustServerCertificate=True;";

await using var conn = new SqlConnection(cs);
await conn.OpenAsync();

const string sql = """
    UPDATE Payments
    SET Status = 3, RefId = @ref, PaidAt = SYSUTCDATETIME()
    WHERE Authority = @auth AND Status = 2;
    SELECT @@ROWCOUNT;
    """;
await using var cmd = new SqlCommand(sql, conn);
cmd.Parameters.AddWithValue("@auth", authority);
cmd.Parameters.AddWithValue("@ref", "SANDBOX-SIMULATED");

var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync());
Console.WriteLine($"updated rows: {rows}");
return rows > 0 ? 0 : 2;
