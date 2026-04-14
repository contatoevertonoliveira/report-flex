import React, { useState } from 'react'
import { api } from '../api'

function getRowValue(row: any, key: string): any {
  if (!row || !key) return undefined
  if (row[key] !== undefined) return row[key]
  const lowerFirst = key.length ? key[0].toLowerCase() + key.slice(1) : key
  if (row[lowerFirst] !== undefined) return row[lowerFirst]
  const upperFirst = key.length ? key[0].toUpperCase() + key.slice(1) : key
  if (row[upperFirst] !== undefined) return row[upperFirst]
  const target = key.toLowerCase()
  for (const k of Object.keys(row)) {
    if (k.toLowerCase() === target) return row[k]
  }
  return undefined
}

function formatBrDateTime(v: any): string {
  if (v == null) return ''
  if (typeof v === 'string') {
    const s = v.trim()
    const m1 = s.match(/^(\d{4})-(\d{2})-(\d{2})[ T](\d{2}):(\d{2}):(\d{2})/)
    if (m1) return `${m1[3]}/${m1[2]}/${m1[1]} ${m1[4]}:${m1[5]}:${m1[6]}`
    const d = new Date(s)
    if (!Number.isNaN(d.getTime())) return d.toLocaleString('pt-BR')
    return s
  }
  if (v instanceof Date) return v.toLocaleString('pt-BR')
  try{
    const d = new Date(v)
    if (!Number.isNaN(d.getTime())) return d.toLocaleString('pt-BR')
  }catch{}
  return String(v)
}

function csvValue(v: any): string {
  if (v == null) return ''
  const s = String(v)
  const escaped = s.replaceAll('"', '""')
  if (escaped.includes(';') || escaped.includes('\n') || escaped.includes('\r')) return `"${escaped}"`
  return escaped
}

export function ExternalPage(){
  const [matricula, setMatricula] = useState('')
  const [empresa, setEmpresa] = useState('')
  const [sort, setSort] = useState<'CardNumber'|'Name'|'Matricula'|'Empresa'|'Cadastro'|'Expira'|'UltimoAcesso'>('CardNumber')
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
  function headerClick(col: 'CardNumber'|'Name'|'Matricula'|'Empresa'|'Cadastro'|'Expira'|'UltimoAcesso'){
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
          <option value="CardNumber">Crachá</option>
          <option value="Name">Nome</option>
          <option value="Matricula">Matrícula</option>
          <option value="Empresa">Empresa</option>
          <option value="Cadastro">Cadastro</option>
          <option value="Expira">Expiração</option>
          <option value="UltimoAcesso">Último Acesso</option>
        </select>
        <select className="form-select" value={dir} onChange={e=> setDir(e.target.value as any)}>
          <option value="asc">Asc</option>
          <option value="desc">Desc</option>
        </select>
        <input className="form-control" type="number" value={page} onChange={e=>setPage(parseInt(e.target.value || '1'))} style={{width:80}} />
        <input className="form-control" type="number" value={pageSize} onChange={e=>setPageSize(parseInt(e.target.value || '20'))} style={{width:80}} />
        <button className="btn btn-primary" onClick={load}>Buscar</button>
        {reportOptions.csv && (
          <button className="btn btn-outline-secondary" onClick={()=> exportCsv(rows)}>Exportar CSV</button>
        )}
      </div>
      {loading && <div>Carregando...</div>}
      {error && <div style={{color:'red'}}>{error}</div>}
      <table className="table table-sm table-hover table-striped align-middle rf-table-light">
        <thead>
          <tr>
            <th onClick={()=> headerClick('CardNumber')} style={{cursor:'pointer'}}>CRACHÁ{sort==='CardNumber' ? (dir==='asc'?' ▲':' ▼') : ''}</th>
            <th onClick={()=> headerClick('Name')} style={{cursor:'pointer'}}>NOME{sort==='Name' ? (dir==='asc'?' ▲':' ▼') : ''}</th>
            <th onClick={()=> headerClick('Matricula')} style={{cursor:'pointer'}}>MATRÍCULA{sort==='Matricula' ? (dir==='asc'?' ▲':' ▼') : ''}</th>
            <th>STATUS</th>
            <th onClick={()=> headerClick('Cadastro')} style={{cursor:'pointer'}}>CADASTRO{sort==='Cadastro' ? (dir==='asc'?' ▲':' ▼') : ''}</th>
            <th onClick={()=> headerClick('Expira')} style={{cursor:'pointer'}}>EXPIRAÇÃO{sort==='Expira' ? (dir==='asc'?' ▲':' ▼') : ''}</th>
            <th onClick={()=> headerClick('UltimoAcesso')} style={{cursor:'pointer'}}>ÚLTIMO ACESSO{sort==='UltimoAcesso' ? (dir==='asc'?' ▲':' ▼') : ''}</th>
            <th onClick={()=> headerClick('Empresa')} style={{cursor:'pointer'}}>EMPRESA{sort==='Empresa' ? (dir==='asc'?' ▲':' ▼') : ''}</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r,i)=> (
            <tr key={i}>
              <td>{getRowValue(r, 'CardNumber') ?? ''}</td>
              <td>{getRowValue(r, 'Name') ?? ''}</td>
              <td>{getRowValue(r, 'Identifier') ?? getRowValue(r, 'Matricula') ?? ''}</td>
              <td>{getRowValue(r, 'StatusCadastro') ?? ''}</td>
              <td>{formatBrDateTime(getRowValue(r, 'Cadastro'))}</td>
              <td>{formatBrDateTime(getRowValue(r, 'Expira'))}</td>
              <td>{formatBrDateTime(getRowValue(r, 'UltimoAcesso'))}</td>
              <td>{getRowValue(r, 'Empresa') ?? ''}</td>
            </tr>
          ))}
        </tbody>
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

function exportCsv(rows: any[]){
  const cols = ['CardNumber','Name','Identifier','StatusCadastro','Cadastro','Expira','UltimoAcesso','Empresa']
  const header = ['CRACHÁ','NOME','MATRÍCULA','STATUS','CADASTRO','EXPIRAÇÃO','ÚLTIMO ACESSO','EMPRESA'].join(';')
  const data = rows.map(r=> cols.map(c=> {
    const v = c === 'Identifier' ? (getRowValue(r, 'Identifier') ?? getRowValue(r, 'Matricula')) : getRowValue(r, c)
    const out = (c === 'Cadastro' || c === 'Expira' || c === 'UltimoAcesso') ? formatBrDateTime(v) : v
    return csvValue(out)
  }).join(';')).join('\n')
  const blob = new Blob([header+'\n'+data], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = 'externos.csv'; a.click()
  URL.revokeObjectURL(url)
}
