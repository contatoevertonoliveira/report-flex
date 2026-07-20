using System.Collections.Generic;
using System.Threading.Tasks;

file sealed class DoorQueryCacheEntry
{
    public object SyncRoot { get; } = new();
    public List<(long EventID, System.DateTime? TimeOrder, string? DataHora, string? TAG, string? Acesso, string? Evento, string? NomeCompleto, string? DocumentoMatricula, string? Cartao, string? Tipo, string? Empresa, string? StatusAcesso, string? DetalheStatusAcesso)> Items { get; } = new();
    public bool IsComplete { get; set; }
    public string? Error { get; set; }
    public Task? LoadTask { get; set; }
}
