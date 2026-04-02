import React from 'react'
import { api } from '../api'

export function LogsPage() {
  const [items, setItems] = React.useState<any[]>([])
  const [total, setTotal] = React.useState(0)
  const [page, setPage] = React.useState(1)
  const pageSize = 50
  const [loading, setLoading] = React.useState(false)
  const [error, setError] = React.useState<string | null>(null)

  React.useEffect(() => {
    let alive = true
    setLoading(true)
    setError(null)
    api.adminActivityLog({ page, pageSize }).then((r: any) => {
      if (!alive) return
      setItems(Array.isArray(r?.items) ? r.items : [])
      setTotal(typeof r?.total === 'number' ? r.total : 0)
    }).catch((e: any) => {
      if (!alive) return
      setError(e?.message || 'Falha ao carregar logs')
      setItems([])
      setTotal(0)
    }).finally(() => {
      if (!alive) return
      setLoading(false)
    })
    return () => { alive = false }
  }, [page])

  const pageCount = Math.max(1, Math.ceil(total / pageSize))

  return (
    <section className="page">
      <h2>Logs</h2>
      {error && <div className="alert alert-danger py-2">{error}</div>}
      <div className="d-flex align-items-center gap-2" style={{marginBottom:10}}>
        <button className="btn btn-outline-secondary" disabled={loading || page <= 1} onClick={() => setPage(p => Math.max(1, p - 1))}>Anterior</button>
        <div className="text-muted" style={{fontSize:13}}>Página {page} / {pageCount} • Total: {total}</div>
        <button className="btn btn-outline-secondary" disabled={loading || page >= pageCount} onClick={() => setPage(p => Math.min(pageCount, p + 1))}>Próxima</button>
      </div>
      <div style={{overflowX:'auto'}}>
        <table className="table table-sm table-striped">
          <thead>
            <tr>
              <th>Data (UTC)</th>
              <th>Usuário</th>
              <th>Nível</th>
              <th>Ação</th>
              <th>Status</th>
              <th>ms</th>
              <th>IP</th>
            </tr>
          </thead>
          <tbody>
            {items.map((x, i) => (
              <tr key={x?.Id ?? i}>
                <td>{x?.TsUtc ? new Date(x.TsUtc).toLocaleString('pt-BR') : ''}</td>
                <td title={x?.Nome || ''}>{x?.Usuario || ''}</td>
                <td>{x?.Nivel || ''}</td>
                <td>
                  <div style={{maxWidth:520, whiteSpace:'nowrap', overflow:'hidden', textOverflow:'ellipsis'}} title={[x?.Action, x?.QueryString].filter(Boolean).join(' ')}>
                    {x?.Action || ''}{x?.QueryString ? ` ${x.QueryString}` : ''}
                  </div>
                </td>
                <td>{x?.StatusCode ?? ''}</td>
                <td>{x?.DurationMs ?? ''}</td>
                <td>{x?.Ip ?? ''}</td>
              </tr>
            ))}
            {items.length === 0 && !loading && (
              <tr><td colSpan={7} className="text-muted">Nenhum log encontrado</td></tr>
            )}
            {loading && (
              <tr><td colSpan={7} className="text-muted">Carregando...</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  )
}

