namespace Common.General;

/// <summary>
///     Static application options populated by the API host on startup
///     (same pattern as ApplicationConstant in the reference architecture).
/// </summary>
public static class ApplicationConstant
{
    public static AppOptions AppOptions { get; set; } = new();
}

public class AppOptions
{
    public string ConnectionString { get; set; } =
        "Server=localhost;Database=ValuationSuite;Trusted_Connection=True;TrustServerCertificate=True;";

    public string SwaggerPath { get; set; } = "api/swagger/v1/swagger.json";

    public string GetSwaggerPath()
        => SwaggerPath;
}

/// <summary>
///     Marker type used as a generic "nothing here" placeholder
///     (mirrors <c>Common.General.Empty</c>).
/// </summary>
public sealed class Empty;
