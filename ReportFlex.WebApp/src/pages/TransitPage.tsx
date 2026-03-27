import React, { useState } from 'react'
import { api } from '../api'

export function TransitPage(){
  const [card, setCard] = useState('')
  const [terminal, setTerminal] = useState('')
  const [userType, setUserType] = useState('')
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [rows, setRows] = useState<any[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reportOptions, setReportOptions] = useState<{ csv: boolean }>({ csv: true })
  React.useEffect(() => {
    let mounted = true
    ;(async () => {
      try{
        const opts = await api.getReportOptions()
        if (!mounted) return
        setReportOptions({
          csv: !!opts.csv
        })
      }catch{}
    })()
    return () => { mounted = false }
  }, [])
  return (
    <section>
      <h2>Trânsito</h2>
      <div className="row">
        <input className="form-control" value={card} onChange={e=>setCard(e.target.value)} placeholder="Crachá (opcional)" />
        <input className="form-control" value={terminal} onChange={e=>setTerminal(e.target.value)} placeholder="Terminal (opcional)" />
        <input className="form-control" value={userType} onChange={e=>setUserType(e.target.value)} placeholder="Tipo de usuário (opcional)" />
        <input className="form-control" type="date" value={start} onChange={e=>setStart(e.target.value)} />
        <input className="form-control" type="date" value={end} onChange={e=>setEnd(e.target.value)} />
        <input className="form-control" type="number" value={page} onChange={e=>setPage(parseInt(e.target.value || '1'))} style={{width:80}} />
        <input className="form-control" type="number" value={pageSize} onChange={e=>setPageSize(parseInt(e.target.value || '20'))} style={{width:80}} />
        <button className="btn btn-primary" onClick={async ()=>{
          try{
            setLoading(true); setError(null)
            if(!start || !end) return;
            const res = await api.transitByPeriod({ start, end, card, terminal, userType, page, pageSize })
            setRows(res.items ?? []); setTotal(res.total ?? 0)
          }catch{ setError('Falha ao buscar trânsitos') } finally{ setLoading(false) }
        }}>Consultar</button>
        {reportOptions.csv && (
          <button className="btn btn-outline-secondary" onClick={()=> exportCsv(rows, ['CardNumber','Name','Direction','UserType','Terminal','TerminalDescription','TransitDate'])}>Exportar CSV</button>
        )}
      </div>
      <div className="row" style={{marginTop:8}}>
        <button className="btn btn-sm btn-light" onClick={()=> {
          const now = new Date()
          const s = new Date(now.getFullYear(), now.getMonth(), now.getDate())
          const e = new Date(now.getFullYear(), now.getMonth(), now.getDate()+1)
          setStart(s.toISOString().slice(0,10)); setEnd(e.toISOString().slice(0,10))
        }}>Hoje</button>
        <button className="btn btn-sm btn-light" onClick={()=> {
          const now = new Date()
          const s = new Date(now.getTime() - 6*24*3600*1000)
          const e = new Date(now.getFullYear(), now.getMonth(), now.getDate()+1)
          setStart(s.toISOString().slice(0,10)); setEnd(e.toISOString().slice(0,10))
        }}>Últimos 7 dias</button>
        <button className="btn btn-sm btn-light" onClick={()=> {
          const now = new Date()
          const s = new Date(now.getFullYear(), now.getMonth(), 1)
          const e = new Date(now.getFullYear(), now.getMonth()+1, 1)
          setStart(s.toISOString().slice(0,10)); setEnd(e.toISOString().slice(0,10))
        }}>Mês atual</button>
      </div>
      {loading && <div>Carregando...</div>}
      {error && <div style={{color:'red'}}>{error}</div>}
      <table className="table table-sm">
        <thead><tr>{['CardNumber','Name','Direction','UserType','Terminal','TerminalDescription','TransitDate'].map(c=> <th key={c}>{c === 'CardNumber' ? 'Crachá' : c}</th>)}</tr></thead>
        <tbody>{rows.map((r,i)=> <tr key={i}>{['CardNumber','Name','Direction','UserType','Terminal','TerminalDescription','TransitDate'].map(c => <td key={c}>{String(r[c] ?? '')}</td>)}</tr>)}</tbody>
      </table>
      <div className="row" style={{marginTop:8}}>
        <span>Total: {total}</span>
        <button className="btn btn-sm btn-light" disabled={page<=1} onClick={async ()=>{ const p = 1; setPage(p); const res = await api.transitByPeriod({ start, end, card, terminal, userType, page: p, pageSize }); setRows(res.items ?? []); setTotal(res.total ?? 0) }}>Primeira</button>
        <button className="btn btn-sm btn-light" disabled={page<=1} onClick={async ()=>{ const p = page-1; setPage(p); const res = await api.transitByPeriod({ start, end, card, terminal, userType, page: p, pageSize }); setRows(res.items ?? []); setTotal(res.total ?? 0) }}>Anterior</button>
        <button className="btn btn-sm btn-light" disabled={(page*pageSize)>=total} onClick={async ()=>{ const p = page+1; setPage(p); const res = await api.transitByPeriod({ start, end, card, terminal, userType, page: p, pageSize }); setRows(res.items ?? []); setTotal(res.total ?? 0) }}>Próxima</button>
        <button className="btn btn-sm btn-light" disabled={(page*pageSize)>=total} onClick={async ()=>{ const p = Math.ceil(total/pageSize)||1; setPage(p); const res = await api.transitByPeriod({ start, end, card, terminal, userType, page: p, pageSize }); setRows(res.items ?? []); setTotal(res.total ?? 0) }}>Última</button>
      </div>
    </section>
  )
}

function exportCsv(rows: any[], cols: string[]){
  const header = cols.map(c => c === 'CardNumber' ? 'Cracha' : c).join(',')
  const data = rows.map(r=> cols.map(c=> JSON.stringify(String(r[c] ?? '')).replace(/^\"|\"$/g,'')).join(',')).join('\n')
  const blob = new Blob([header+'\n'+data], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = 'transitos.csv'; a.click()
  URL.revokeObjectURL(url)
}
