import React from 'react'
import { api } from '../api'

export function LogsPage() {
  const [items, setItems] = React.useState<any[]>([])
  const [total, setTotal] = React.useState(0)
  const [page, setPage] = React.useState(1)
  const pageSize = 50
  const [loading, setLoading] = React.useState(false)
  const [error, setError] = React.useState<string | null>(null)

  const pick = (obj: any, ...keys: string[]) => {
    for (const k of keys) {
      const v = obj?.[k]
      if (v !== undefined && v !== null) return v
    }
    return null
  }

  const formatSaoPaulo = (value: any) => {
    if (!value) return ''
    try {
      let d: Date
      if (value instanceof Date) d = value
      else if (typeof value === 'number') d = new Date(value)
      else if (typeof value === 'string') {
        let s = value.trim()
        const looksIso = /^\d{4}-\d{2}-\d{2}T/.test(s)
        const hasTz = /Z$/i.test(s) || /[+-]\d{2}:\d{2}$/.test(s)
        if (looksIso && !hasTz) s = s + 'Z'
        d = new Date(s)
      } else {
        d = new Date(value)
      }
      if (Number.isNaN(d.getTime())) return String(value)
      return new Intl.DateTimeFormat('pt-BR', {
        timeZone: 'America/Sao_Paulo',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
      }).format(d)
    } catch {
      return String(value)
    }
  }

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
        <div className="text-light" style={{fontSize:13}}>Página {page} / {pageCount} • Total: {total}</div>
        <button className="btn btn-outline-secondary" disabled={loading || page >= pageCount} onClick={() => setPage(p => Math.min(pageCount, p + 1))}>Próxima</button>
      </div>
      <div className="table-responsive" style={{flex:1, overflow:'auto'}}>
        <table className="table table-hover table-striped align-middle logs-table">
          <thead>
            <tr>
              <th style={{width:160}}>Data (SP)</th>
              <th style={{width:340}}>Usuário</th>
              <th style={{width:120}}>Nível</th>
              <th style={{width:360}}>Ação</th>
              <th style={{width:80}}>Status</th>
              <th style={{width:70}}>ms</th>
              <th style={{width:120}}>IP</th>
            </tr>
          </thead>
          <tbody>
            {items.map((x, i) => (
              <tr key={x?.Id ?? i}>
                <td>{formatSaoPaulo(pick(x, 'tsUtc', 'TsUtc'))}</td>
                <td
                  className="logs-user"
                  title={String(pick(x, 'usuario', 'Usuario') || '')}
                >
                  {String(pick(x, 'usuario', 'Usuario') || '')}
                </td>
                <td title={String(pick(x, 'nivel', 'Nivel') || '')}>{String(pick(x, 'nivel', 'Nivel') || '')}</td>
                <td className="logs-action" title={String([pick(x, 'action', 'Action'), pick(x, 'queryString', 'QueryString')].filter(Boolean).join(' '))}>
                  {String(pick(x, 'action', 'Action') || '')}{pick(x, 'queryString', 'QueryString') ? ` ${String(pick(x, 'queryString', 'QueryString'))}` : ''}
                </td>
                <td>{String(pick(x, 'statusCode', 'StatusCode') ?? '')}</td>
                <td>{String(pick(x, 'durationMs', 'DurationMs') ?? '')}</td>
                <td>{String(pick(x, 'ip', 'Ip') ?? '')}</td>
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
