import React, { useState } from 'react'
import { api } from '../api'

export function AccessPage(){
  const [levelId, setLevelId] = useState<number | undefined>(undefined)
  const [levelName, setLevelName] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [rows, setRows] = useState<any[]>([])
  const [agg, setAgg] = useState<any[]>([])
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
  return (
    <section>
      <h2>Acessos por Nível</h2>
      <div className="row">
        <input className="form-control" type="number" placeholder="Nivel ID" value={levelId ?? ''} onChange={e=> setLevelId(parseInt(e.target.value || '0') || undefined)} style={{width:120}} />
        <input className="form-control" placeholder="Descrição do nível" value={levelName} onChange={e=> setLevelName(e.target.value)} />
        <input className="form-control" type="number" value={page} onChange={e=>setPage(parseInt(e.target.value || '1'))} style={{width:80}} />
        <input className="form-control" type="number" value={pageSize} onChange={e=>setPageSize(parseInt(e.target.value || '20'))} style={{width:80}} />
        <button className="btn btn-primary" onClick={async ()=>{
          const res = await api.accessByLevel({ levelId, levelName, page, pageSize })
          setRows(res.items ?? [])
        }}>Buscar</button>
        <button className="btn btn-outline-secondary" onClick={async ()=>{
          const data = await api.reportsAccessAggregated()
          setAgg(data ?? [])
        }}>Agregados</button>
        {reportOptions.csv && (
          <button className="btn btn-outline-secondary" onClick={()=> window.open('/api/reports/access/aggregated/export?format=csv','_blank')}>Exportar CSV</button>
        )}
        {reportOptions.xlsx && (
          <button className="btn btn-outline-secondary" onClick={()=> window.open('/api/reports/access/aggregated/export?format=xlsx','_blank')}>Exportar XLSX</button>
        )}
        {reportOptions.pdf && (
          <button className="btn btn-outline-secondary" onClick={()=> window.open('/api/reports/access/aggregated/export?format=pdf','_blank')}>Exportar PDF</button>
        )}
      </div>
      <table className="table table-sm">
        <thead><tr>{['CardNumber','Name','LevelId','Level'].map(c=> <th key={c}>{c === 'CardNumber' ? 'Crachá' : c}</th>)}</tr></thead>
        <tbody>{rows.map((r,i)=> <tr key={i}>{['CardNumber','Name','LevelId','Level'].map(c => <td key={c}>{r[c] ?? ''}</td>)}</tr>)}</tbody>
      </table>
      {agg.length>0 && (
        <>
          <h3>Agregado</h3>
          <table className="table table-sm">
            <thead><tr>{['LevelId','Level','Total'].map(c=> <th key={c}>{c}</th>)}</tr></thead>
            <tbody>{agg.map((r,i)=> <tr key={i}>{['LevelId','Level','Total'].map(c => <td key={c}>{r[c] ?? ''}</td>)}</tr>)}</tbody>
          </table>
        </>
      )}
    </section>
  )
}
