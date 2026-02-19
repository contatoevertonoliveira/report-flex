import React, { useState } from 'react'
import { api } from '../api'

export function ExternalPage(){
  const [matricula, setMatricula] = useState('')
  const [empresa, setEmpresa] = useState('')
  const [sort, setSort] = useState<'SbiID'|'Name'|'Matricula'|'Empresa'>('SbiID')
  const [dir, setDir] = useState<'asc'|'desc'>('asc')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [rows, setRows] = useState<any[]>([])
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
  async function load(){
    try{
      setLoading(true); setError(null)
      const res = await api.externalSearch({ matricula, empresa, page, pageSize, sort, dir })
      setRows(res.items ?? []); setTotal(res.total ?? 0)
    }catch{ setError('Falha ao buscar externos') } finally{ setLoading(false) }
  }
  function headerClick(col: 'SbiID'|'Name'|'Matricula'|'Empresa'){
    if (sort === col) setDir(dir === 'asc' ? 'desc' : 'asc')
    else setSort(col)
    setPage(1)
    load()
  }
  return (
    <section>
      <h2>Externos</h2>
      <div className="row">
        <input className="form-control" value={matricula} onChange={e=>setMatricula(e.target.value)} placeholder="Matrícula" />
        <input className="form-control" value={empresa} onChange={e=>setEmpresa(e.target.value)} placeholder="Empresa" />
        <select className="form-select" value={sort} onChange={e=> setSort(e.target.value as any)}>
          <option value="SbiID">Código</option>
          <option value="Name">Nome</option>
          <option value="Matricula">Matrícula</option>
          <option value="Empresa">Empresa</option>
        </select>
        <select className="form-select" value={dir} onChange={e=> setDir(e.target.value as any)}>
          <option value="asc">Asc</option>
          <option value="desc">Desc</option>
        </select>
        <input className="form-control" type="number" value={page} onChange={e=>setPage(parseInt(e.target.value || '1'))} style={{width:80}} />
        <input className="form-control" type="number" value={pageSize} onChange={e=>setPageSize(parseInt(e.target.value || '20'))} style={{width:80}} />
        <button className="btn btn-primary" onClick={load}>Buscar</button>
        {reportOptions.csv && (
          <button className="btn btn-outline-secondary" onClick={()=> exportCsv(rows, ['SbiID','Name','Surname','PreferredName','Identifier','Empresa','CardNumber'])}>Exportar CSV</button>
        )}
      </div>
      {loading && <div>Carregando...</div>}
      {error && <div style={{color:'red'}}>{error}</div>}
      <table className="table table-sm">
        <thead><tr>{['SbiID','Name','Surname','PreferredName','Identifier','Empresa','CardNumber'].map(c=> 
          <th key={c} onClick={()=> (c==='SbiID'||c==='Name'||c==='Identifier'||c==='Empresa') && headerClick(c as any)} style={{cursor:'pointer'}}>
            {c}{sort===c ? (dir==='asc'?' ▲':' ▼') : ''}
          </th>)}</tr></thead>
        <tbody>{rows.map((r,i)=> <tr key={i}>{['SbiID','Name','Surname','PreferredName','Identifier','Empresa','CardNumber'].map(c => <td key={c}>{r[c] ?? ''}</td>)}</tr>)}</tbody>
      </table>
      <div className="row" style={{marginTop:8}}>
        <span>Total: {total}</span>
        <button className="btn btn-sm btn-light" disabled={page<=1} onClick={()=>{ setPage(1); load() }}>Primeira</button>
        <button className="btn btn-sm btn-light" disabled={page<=1} onClick={()=>{ setPage(page-1); load() }}>Anterior</button>
        <button className="btn btn-sm btn-light" disabled={(page*pageSize)>=total} onClick={()=>{ setPage(page+1); load() }}>Próxima</button>
        <button className="btn btn-sm btn-light" disabled={(page*pageSize)>=total} onClick={()=>{ const p=Math.ceil(total/pageSize)||1; setPage(p); load() }}>Última</button>
      </div>
    </section>
  )
}

function exportCsv(rows: any[], cols: string[]){
  const header = cols.join(',')
  const data = rows.map(r=> cols.map(c=> JSON.stringify(String(r[c] ?? '')).replace(/^\"|\"$/g,'')).join(',')).join('\n')
  const blob = new Blob([header+'\n'+data], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = 'externos.csv'; a.click()
  URL.revokeObjectURL(url)
}
