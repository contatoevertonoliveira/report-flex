/// <summary>
/// Estado de progresso de uma consulta em andamento, consultado pelo frontend
/// via GET /api/progress/{id} (o id vem do header X-Progress-Id).
/// </summary>
internal sealed class QueryProgressState
{
    public string Stage { get; set; } = "running";
    public int Loaded { get; set; }
    public int? Total { get; set; }
    public bool Done { get; set; }
    public string? Error { get; set; }
    public System.DateTime UpdatedAtUtc { get; set; } = System.DateTime.UtcNow;
}
