import React, { useState } from 'react'
import { api } from '../api'
import { DataTable } from '../components/DataTable'

export function ReportsPage(){
  const [empresa, setEmpresa] = useState('')
  const [terminal, setTerminal] = useState('')
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [mode, setMode] = useState<'detalhado'|'agregado'>('detalhado')
  const [rows, setRows] = useState<any[]>([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reportOptions, setReportOptions] = useState<{ csv: boolean, xlsx: boolean, pdf: boolean }>({ csv: true, xlsx: true, pdf: true })
  React.useEffect(() => {
    let mounted = true
    ;(async () => {
      try{
        const opts = await api.getReportOptions()
        if (!mounted) return
        setReportOptions({
          csv: !!opts.csv,
          xlsx: !!opts.xlsx || !!opts.excel,
          pdf: !!opts.pdf
        })
      }catch{}
    })()
    return () => { mounted = false }
  }, [])
  async function load(){
    try{
      setLoading(true); setError(null)
      if(!start || !end) return;
      if(mode === 'detalhado'){
        const res = await api.reportsTransit({ empresa, terminal, start, end, page, pageSize })
        setRows(res.items ?? []); setTotal(res.total ?? 0)
      }else{
        const res = await api.reportsTransitAggregated({ empresa, start, end })
        setRows(res ?? []); setTotal(res.length ?? 0)
      }
    }catch{ setError('Falha ao buscar relatório') } finally{ setLoading(false) }
  }
  function exportFormat(fmt: 'csv'|'xlsx'|'pdf'){
    const qs = new URLSearchParams({ empresa, terminal, start, end, format: fmt }).toString()
    window.open('/api/reports/transit/export?' + qs, '_blank')
  }
  function setPreset(p: 'hoje'|'7dias'|'mes'){
    const now = new Date()
    if(p === 'hoje'){
      const s = new Date(now.getFullYear(), now.getMonth(), now.getDate())
      const e = new Date(now.getFullYear(), now.getMonth(), now.getDate()+1)
      setStart(s.toISOString().slice(0,10)); setEnd(e.toISOString().slice(0,10))
    }else if(p === '7dias'){
      const s = new Date(now.getTime() - 6*24*3600*1000)
      const e = new Date(now.getFullYear(), now.getMonth(), now.getDate()+1)
      setStart(s.toISOString().slice(0,10)); setEnd(e.toISOString().slice(0,10))
    }else{
      const s = new Date(now.getFullYear(), now.getMonth(), 1)
      const e = new Date(now.getFullYear(), now.getMonth()+1, 1)
      setStart(s.toISOString().slice(0,10)); setEnd(e.toISOString().slice(0,10))
    }
  }
  return (
    <section>
      <h2>Relatórios: Trânsitos</h2>
      <div className="row">
        <input value={empresa} onChange={e=>setEmpresa(e.target.value)} placeholder="Empresa" />
        <input value={terminal} onChange={e=>setTerminal(e.target.value)} placeholder="Terminal" />
        <input type="date" value={start} onChange={e=>setStart(e.target.value)} />
        <input type="date" value={end} onChange={e=>setEnd(e.target.value)} />
        <select className="form-select" value={mode} onChange={e=> setMode(e.target.value as any)}>
          <option value="detalhado">Detalhado</option>
          <option value="agregado">Agregado (Empresa + Terminal)</option>
        </select>
        <input type="number" value={page} onChange={e=>setPage(parseInt(e.target.value || '1'))} style={{width:80}} />
        <input type="number" value={pageSize} onChange={e=>setPageSize(parseInt(e.target.value || '20'))} style={{width:80}} />
        <button className="btn btn-primary" onClick={load}>Buscar</button>
        {reportOptions.csv && (
          <button className="btn btn-outline-secondary" onClick={()=> exportFormat('csv')}>Exportar CSV</button>
        )}
        {reportOptions.xlsx && (
          <button className="btn btn-outline-secondary" onClick={()=> exportFormat('xlsx')}>Exportar XLSX</button>
        )}
        {reportOptions.pdf && (
          <button className="btn btn-outline-secondary" onClick={()=> exportFormat('pdf')}>Exportar PDF</button>
        )}
      </div>
      <div className="row" style={{marginTop:8}}>
        <button className="btn btn-sm btn-light" onClick={()=> setPreset('hoje')}>Hoje</button>
        <button className="btn btn-sm btn-light" onClick={()=> setPreset('7dias')}>Últimos 7 dias</button>
        <button className="btn btn-sm btn-light" onClick={()=> setPreset('mes')}>Mês atual</button>
      </div>
      {loading && <div>Carregando...</div>}
      {error && <div style={{color:'red'}}>{error}</div>}
      {mode==='detalhado' ? (
        <DataTable
          columns={[
            { key:'CardNumber', label:'Crachá' },
            { key:'Name', label:'Name' },
            { key:'Empresa', label:'Empresa' },
            { key:'Terminal', label:'Terminal' },
            { key:'TerminalDescription', label:'TerminalDescription' },
            { key:'TransitDate', label:'TransitDate' }
          ]}
          rows={rows}
        />
      ) : (
        <DataTable
          columns={[
            { key:'Empresa', label:'Empresa' },
            { key:'Terminal', label:'Terminal' },
            { key:'Total', label:'Total' }
          ]}
          rows={rows}
        />
      )}
      <div className="row" style={{marginTop:8}}>
        <span>Total: {total}</span>
        <button className="btn btn-sm btn-light" disabled={page<=1} onClick={()=>{ setPage(1); load() }}>Primeira</button>
        <button className="btn btn-sm btn-light" disabled={page<=1} onClick={()=>{ const p=page-1; setPage(p); load() }}>Anterior</button>
        <button className="btn btn-sm btn-light" disabled={(page*pageSize)>=total} onClick={()=>{ const p=page+1; setPage(p); load() }}>Próxima</button>
        <button className="btn btn-sm btn-light" disabled={(page*pageSize)>=total} onClick={()=>{ const p=Math.ceil(total/pageSize)||1; setPage(p); load() }}>Última</button>
      </div>
    </section>
  )
}
