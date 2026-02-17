import React, { useMemo, useState } from 'react'
import { api } from '../api'

type QuickKind = 'access-agg' | 'transit-period' | 'employees' | 'external'
type Mode = 'prontas' | 'personalizadas'
type Dataset = 'access-agg' | 'transit' | 'employees' | 'external'

const DATASET_COLUMNS: Record<Dataset, { key: string, label: string }[]> = {
  'access-agg': [
    { key: 'LevelId', label: 'LevelId' },
    { key: 'Level', label: 'Level' },
    { key: 'Total', label: 'Total' }
  ],
  'transit': [
    { key: 'SbiID', label: 'SbiID' },
    { key: 'Name', label: 'Nome' },
    { key: 'Empresa', label: 'Empresa' },
    { key: 'Terminal', label: 'Terminal' },
    { key: 'TerminalDescription', label: 'Terminal Desc.' },
    { key: 'TransitDate', label: 'Data/Hora' }
  ],
  'employees': [
    { key: 'SbiID', label: 'SbiID' },
    { key: 'Name', label: 'Nome' },
    { key: 'Empresa', label: 'Empresa' }
  ],
  'external': [
    { key: 'SbiID', label: 'SbiID' },
    { key: 'Name', label: 'Nome' },
    { key: 'Empresa', label: 'Empresa' }
  ]
}

export function QueriesPage(){
  const [mode, setMode] = useState<Mode>('prontas')
  const [quickKind, setQuickKind] = useState<QuickKind>('access-agg')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [data, setData] = useState<any[]>([])
  const [filters, setFilters] = useState<{[k:string]: any}>({})
  const [pdfUrl, setPdfUrl] = useState<string | null>(null)

  // Personalizadas
  const [dataset, setDataset] = useState<Dataset>('transit')
  const [selectedCols, setSelectedCols] = useState<string[]>(DATASET_COLUMNS['transit'].map(c=>c.key))
  const [searchTerm, setSearchTerm] = useState('')
  const [searchColumn, setSearchColumn] = useState<string>('*')
  const [currentPage, setCurrentPage] = useState(1)
  const pageSize = 50
  const maxPreview = 1000

  const canExport = useMemo(() => {
    if (mode === 'prontas' && (quickKind === 'access-agg' || quickKind === 'transit-period')) {
      return data && data.length > 0
    }
    if (mode === 'personalizadas' && (dataset === 'access-agg' || dataset === 'transit')) {
      return data && data.length > 0
    }
    return false
  }, [mode, quickKind, dataset, data])

  function resetData(){ setData([]); setError(null) }

  function mapQuickToDataset(k: QuickKind): Dataset{
    if (k === 'transit-period') return 'transit'
    if (k === 'employees') return 'employees'
    if (k === 'external') return 'external'
    return 'access-agg'
  }

  async function collectUpTo(maxItems: number, fetchPage: (page:number, pageSize:number)=> Promise<any[]>){
    const batch: any[] = []
    let page = 1
    const ps = 200
    while (batch.length < maxItems){
      const items = await fetchPage(page, ps)
      if (!items || items.length === 0) break
      for (const it of items){
        batch.push(it)
        if (batch.length >= maxItems) break
      }
      if (items.length < ps) break
      page += 1
    }
    return batch
  }

  async function runQuick(){
    setError(null); setLoading(true)
    try{
      if (quickKind === 'access-agg'){
        const res = await api.reportsAccessAggregated()
        setData(Array.isArray(res) ? res : [])
      }else if (quickKind === 'transit-period'){
        const { start, end, empresa, terminal } = filters as any
        if(!start || !end){ setError('Informe início e fim'); setLoading(false); return }
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.reportsTransit({ start, end, empresa, terminal, page, pageSize: ps })
          return r?.items ?? []
        })
        setData(collected)
      }else if (quickKind === 'employees'){
        const { matricula, empresa } = filters as any
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.employeesSearch({ matricula, empresa, page, pageSize: ps, sort: 'SbiID', dir: 'asc' })
          return r?.items ?? []
        })
        setData(collected)
      }else if (quickKind === 'external'){
        const { matricula, empresa } = filters as any
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.externalSearch({ matricula, empresa, page, pageSize: ps, sort: 'SbiID', dir: 'asc' })
          const items = (r as any)?.items ?? r ?? []
          return Array.isArray(items) ? items : []
        })
        setData(collected)
      }
    }catch{
      setError('Falha na consulta')
      setData([])
    }finally{
      setLoading(false)
      setCurrentPage(1)
    }
  }

  async function runPersonalizada(){
    setError(null); setLoading(true)
    try{
      if (dataset === 'access-agg'){
        const res = await api.reportsAccessAggregated()
        setData(Array.isArray(res) ? res : [])
      }else if (dataset === 'transit'){
        const { start, end, empresa, terminal } = filters as any
        if(!start || !end){ setError('Informe início e fim'); setLoading(false); return }
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.reportsTransit({ start, end, empresa, terminal, page, pageSize: ps })
          return r?.items ?? []
        })
        setData(collected)
      }else if (dataset === 'employees'){
        const { matricula, empresa } = filters as any
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.employeesSearch({ matricula, empresa, page, pageSize: ps, sort: 'SbiID', dir: 'asc' })
          return r?.items ?? []
        })
        setData(collected)
      }else if (dataset === 'external'){
        const { matricula, empresa } = filters as any
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.externalSearch({ matricula, empresa, page, pageSize: ps, sort: 'SbiID', dir: 'asc' })
          const items = (r as any)?.items ?? r ?? []
          return Array.isArray(items) ? items : []
        })
        setData(collected)
      }
    }catch{
      setError('Falha na consulta')
      setData([])
    }finally{
      setLoading(false)
      setCurrentPage(1)
    }
  }

  async function exportData(format: 'csv'|'xlsx'|'pdf'){
    try{
      const h: Record<string,string> = {}
      const t = localStorage.getItem('rf_token')
      if (t) h['Authorization'] = `Bearer ${t}`
      const cid = localStorage.getItem('rf_client_id')
      if (cid) h['X-Client-Id'] = cid
      if ((mode === 'prontas' && quickKind === 'access-agg') || (mode === 'personalizadas' && dataset === 'access-agg')){
        const url = `/api/reports/access/aggregated/export?format=${format}`
        const res = await fetch(url, { headers: h })
        if(!res.ok) return
        const blob = await res.blob()
        const name = `access-aggregated.${format}`
        const a = document.createElement('a')
        a.href = URL.createObjectURL(blob)
        a.download = name
        a.click()
        URL.revokeObjectURL(a.href)
      }else if ((mode === 'prontas' && quickKind === 'transit-period') || (mode === 'personalizadas' && dataset === 'transit')){
        const { start, end, empresa, terminal } = filters as any
        if(!start || !end) return
        const qs = new URLSearchParams(Object.entries({ start, end, empresa: empresa||'', terminal: terminal||'', format })).toString()
        const res = await fetch(`/api/reports/transit/export?${qs}`, { headers: h })
        if(!res.ok) return
        const blob = await res.blob()
        const name = `transit.${format}`
        const a = document.createElement('a')
        a.href = URL.createObjectURL(blob)
        a.download = name
        a.click()
        URL.revokeObjectURL(a.href)
      }
    }catch{}
  }

  async function previewPdf(){
    try{
      setError(null)
      let url: string | null = null
      if ((mode === 'prontas' && quickKind === 'access-agg') || (mode === 'personalizadas' && dataset === 'access-agg')){
        url = `/api/reports/access/aggregated/export?format=pdf`
      }else if ((mode === 'prontas' && quickKind === 'transit-period') || (mode === 'personalizadas' && dataset === 'transit')){
        const { start, end, empresa, terminal } = filters as any
        if(!start || !end){ setError('Informe início e fim'); return }
        const qs = new URLSearchParams(Object.entries({ start, end, empresa: empresa||'', terminal: terminal||'', format:'pdf' })).toString()
        url = `/api/reports/transit/export?${qs}`
      }
      if (!url) { setError('Pré-visualização disponível apenas para Acessos e Trânsito'); return }
      const u = await api.fetchReportPdf(url)
      setPdfUrl(u)
    }catch{
      setError('Falha ao gerar PDF')
    }
  }

  function toggleSelected(colKey: string){
    setSelectedCols(prev => prev.includes(colKey) ? prev.filter(k=>k!==colKey) : [...prev, colKey])
  }
  function moveSelected(colKey: string, dir: -1|1){
    setSelectedCols(prev => {
      const idx = prev.indexOf(colKey)
      if (idx < 0) return prev
      const to = idx + dir
      if (to < 0 || to >= prev.length) return prev
      const arr = prev.slice()
      const tmp = arr[idx]
      arr[idx] = arr[to]; arr[to] = tmp
      return arr
    })
  }

  const visibleColumns = useMemo(()=>{
    if (mode === 'personalizadas'){
      const defs = DATASET_COLUMNS[dataset]
      return defs.filter(d => selectedCols.includes(d.key))
    }
    // modo prontas: usa todas colunas retornadas
    if (Array.isArray(data) && data[0]) return Object.keys(data[0]).map(k=>({ key:k, label:k }))
    return []
  }, [mode, dataset, selectedCols, data])
  const quickColumns = useMemo(()=>{
    const d = mapQuickToDataset(quickKind)
    return DATASET_COLUMNS[d]
  }, [quickKind])


  const searchColumnsList = useMemo(()=>{
    if (mode === 'prontas') return quickColumns
    return DATASET_COLUMNS[dataset]
  }, [mode, quickColumns, dataset])

  const filteredData = useMemo(()=>{
    const term = (searchTerm || '').toLowerCase().trim()
    if (!term) return data
    const cols = searchColumn === '*' ? (mode === 'personalizadas' ? DATASET_COLUMNS[dataset].map(c=>c.key) : quickColumns.map(c=>c.key)) : [searchColumn]
    return data.filter(row => {
      for (const c of cols){
        const v = row?.[c]
        if (v !== undefined && v !== null){
          const s = String(v).toLowerCase()
          if (s.includes(term)) return true
        }
      }
      return false
    })
  }, [data, searchTerm, searchColumn, mode, dataset, quickColumns])

  const previewData = useMemo(()=>{
    if (filteredData.length <= maxPreview) return filteredData
    return filteredData.slice(0, maxPreview)
  }, [filteredData])

  const pageCount = useMemo(()=>{
    return Math.max(1, Math.ceil(previewData.length / pageSize))
  }, [previewData.length, pageSize])

  const pageRows = useMemo(()=>{
    const start = (currentPage - 1) * pageSize
    return previewData.slice(start, start + pageSize)
  }, [previewData, currentPage, pageSize])

  return (
    <section className="queries">
      <h2>Consultas</h2>
      <div className="card queries-card">
        <div className="card-header">
          <div className="d-flex align-items-center" style={{gap:8}}>
            <i className="bi bi-search" />
            <strong>Consultas</strong>
          </div>
          <div className="queries-toolbar">
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" role="switch"
                id="switchProntas"
                checked={mode==='prontas'}
                onChange={()=>{ setMode('prontas'); resetData() }}
              />
              <label className="form-check-label" htmlFor="switchProntas">Consultas Prontas</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" role="switch"
                id="switchPersonalizadas"
                checked={mode==='personalizadas'}
                onChange={()=>{ setMode('personalizadas'); resetData() }}
              />
              <label className="form-check-label" htmlFor="switchPersonalizadas">Consultas Personalizadas</label>
            </div>
          </div>
        </div>
        <div className="card-body">

      {mode === 'prontas' && (
        <>
          <div className="queries-ready-options" style={{marginBottom:12}}>
            {([
              { key:'access-agg', label:'Acessos Agregados' },
              { key:'transit-period', label:'Trânsito por Período' },
              { key:'employees', label:'Funcionários' },
              { key:'external', label:'Externos' }
            ] as {key:QuickKind,label:string}[]).map(opt => (
              <button
                key={opt.key}
                type="button"
                className={'queries-ready-option' + (quickKind===opt.key ? ' active' : '')}
                onClick={()=> { setQuickKind(opt.key); setData([]); setError(null) }}
              >
                <span>{opt.label}</span>
                {quickKind===opt.key && <i className="bi bi-check-circle" />}
              </button>
            ))}
          </div>

          <div className="queries-row" style={{marginBottom:8}}>
            {quickKind === 'transit-period' && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <input className="form-control" type="date" value={filters.start || ''} onChange={e=> setFilters({...filters, start: e.target.value})} placeholder="Início" />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <input className="form-control" type="date" value={filters.end || ''} onChange={e=> setFilters({...filters, end: e.target.value})} placeholder="Fim" />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-building" /></span>
                  <input className="form-control" placeholder="Empresa (opcional)" value={filters.empresa || ''} onChange={e=> setFilters({...filters, empresa: e.target.value})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-upc-scan" /></span>
                  <input className="form-control" placeholder="Terminal (opcional)" value={filters.terminal || ''} onChange={e=> setFilters({...filters, terminal: e.target.value})} />
                </div>
              </>
            )}
            {(quickKind === 'employees' || quickKind === 'external') && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-credit-card-2-front" /></span>
                  <input className="form-control" placeholder="Matrícula (opcional)" value={filters.matricula || ''} onChange={e=> setFilters({...filters, matricula: e.target.value})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-building" /></span>
                  <input className="form-control" placeholder="Empresa (opcional)" value={filters.empresa || ''} onChange={e=> setFilters({...filters, empresa: e.target.value})} />
                </div>
              </>
            )}
          </div>
          <div className="queries-row" style={{marginBottom:12}}>
            <select className="form-select" style={{width:220}} value={searchColumn} onChange={e=> setSearchColumn(e.target.value)}>
              <option value="*">Todas as colunas</option>
              {searchColumnsList.map(c => <option key={c.key} value={c.key}>{c.label}</option>)}
            </select>
            <div className="input-group">
              <span className="input-group-text"><i className="bi bi-search" /></span>
              <input className="form-control" placeholder="Pesquisar" value={searchTerm} onChange={e=> { setSearchTerm(e.target.value); setCurrentPage(1) }} />
            </div>
            <button className="btn btn-primary d-flex align-items-center" onClick={runQuick} disabled={loading}>
              {loading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Consultando...</> : <><i className="bi bi-play-fill me-1" /> Consultar</>}
            </button>
            {canExport && (
              <div className="export-group">
                <span>Exportar:</span>
                <button className="btn btn-light btn-icon" title="CSV" onClick={()=> exportData('csv')}>
                  <i className="bi bi-filetype-csv" />
                </button>
                <button className="btn btn-light btn-icon" title="XLSX" onClick={()=> exportData('xlsx')}>
                  <i className="bi bi-file-earmark-excel" />
                </button>
                <button className="btn btn-light btn-icon" title="PDF" onClick={()=> exportData('pdf')}>
                  <i className="bi bi-file-earmark-pdf" />
                </button>
                <button className="btn btn-outline-secondary ms-2" onClick={previewPdf}>
                  <i className="bi bi-eye me-1" /> Visualizar PDF
                </button>
              </div>
            )}
          </div>
        </>
      )}

      {mode === 'personalizadas' && (
        <>
          <div className="queries-row" style={{marginBottom:8}}>
            <select className="form-select" style={{width:260}} value={dataset} onChange={e=>{
              const d = e.target.value as Dataset
              setDataset(d)
              setSelectedCols(DATASET_COLUMNS[d].map(c=>c.key))
              setData([]); setError(null)
            }}>
              <option value="transit">Trânsito</option>
              <option value="employees">Funcionários</option>
              <option value="external">Externos</option>
              <option value="access-agg">Acessos Agregados</option>
            </select>
            {(dataset === 'transit') && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <input className="form-control" type="date" value={filters.start || ''} onChange={e=> setFilters({...filters, start: e.target.value})} placeholder="Início" />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <input className="form-control" type="date" value={filters.end || ''} onChange={e=> setFilters({...filters, end: e.target.value})} placeholder="Fim" />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-building" /></span>
                  <input className="form-control" placeholder="Empresa (opcional)" value={filters.empresa || ''} onChange={e=> setFilters({...filters, empresa: e.target.value})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-upc-scan" /></span>
                  <input className="form-control" placeholder="Terminal (opcional)" value={filters.terminal || ''} onChange={e=> setFilters({...filters, terminal: e.target.value})} />
                </div>
              </>
            )}
            {(dataset === 'employees' || dataset === 'external') && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-credit-card-2-front" /></span>
                  <input className="form-control" placeholder="Matrícula (opcional)" value={filters.matricula || ''} onChange={e=> setFilters({...filters, matricula: e.target.value})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-building" /></span>
                  <input className="form-control" placeholder="Empresa (opcional)" value={filters.empresa || ''} onChange={e=> setFilters({...filters, empresa: e.target.value})} />
                </div>
              </>
            )}
          </div>
          <div className="queries-row" style={{marginBottom:8}}>
            <select className="form-select" style={{width:220}} value={searchColumn} onChange={e=> setSearchColumn(e.target.value)}>
              <option value="*">Todas as colunas</option>
              {DATASET_COLUMNS[dataset].map(c => <option key={c.key} value={c.key}>{c.label}</option>)}
            </select>
            <div className="input-group">
              <span className="input-group-text"><i className="bi bi-search" /></span>
              <input className="form-control" placeholder="Pesquisar" value={searchTerm} onChange={e=> { setSearchTerm(e.target.value); setCurrentPage(1) }} />
            </div>
            <button className="btn btn-primary d-flex align-items-center" onClick={runPersonalizada} disabled={loading}>
              {loading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Consultando...</> : <><i className="bi bi-play-fill me-1" /> Consultar</>}
            </button>
            {canExport && (
              <div className="export-group">
                <span>Exportar:</span>
                <button className="btn btn-light btn-icon" title="CSV" onClick={()=> exportData('csv')}>
                  <i className="bi bi-filetype-csv" />
                </button>
                <button className="btn btn-light btn-icon" title="XLSX" onClick={()=> exportData('xlsx')}>
                  <i className="bi bi-file-earmark-excel" />
                </button>
                <button className="btn btn-light btn-icon" title="PDF" onClick={()=> exportData('pdf')}>
                  <i className="bi bi-file-earmark-pdf" />
                </button>
                <button className="btn btn-outline-secondary ms-2" onClick={previewPdf}>
                  <i className="bi bi-eye me-1" /> Visualizar PDF
                </button>
              </div>
            )}
          </div>
          <div className="queries-cols-row" style={{marginBottom:12}}>
            <div className="queries-cols-list">
              {DATASET_COLUMNS[dataset].map(col => {
                const active = selectedCols.includes(col.key)
                return (
                  <button
                    key={col.key}
                    type="button"
                    className="queries-col-pill"
                    onClick={()=> toggleSelected(col.key)}
                  >
                    <span>{col.label}</span>
                    <input
                      type="checkbox"
                      checked={active}
                      readOnly
                    />
                    <span>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary"
                        onClick={e=> { e.stopPropagation(); moveSelected(col.key, -1) }}
                        title="Subir"
                      >
                        <i className="bi bi-arrow-up" />
                      </button>{' '}
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary"
                        onClick={e=> { e.stopPropagation(); moveSelected(col.key, 1) }}
                        title="Descer"
                      >
                        <i className="bi bi-arrow-down" />
                      </button>
                    </span>
                  </button>
                )
              })}
            </div>
          </div>
        </>
      )}

      {error && <div className="alert alert-danger"><i className="bi bi-exclamation-triangle me-2" />{error}</div>}

      {previewData.length > 0 && (
        <div className="d-flex align-items-center justify-content-between" style={{marginBottom:8}}>
          <div className="text-muted" style={{fontSize:12}}>
            Mostrando {Math.min(previewData.length, maxPreview)} registros (máx. {maxPreview}) • Página {currentPage} de {pageCount}
          </div>
          <div className="btn-group" role="group">
            <button className="btn btn-outline-secondary btn-sm" onClick={()=> setCurrentPage(p=> Math.max(1, p-1))} disabled={currentPage<=1}>
              <i className="bi bi-chevron-left" />
            </button>
            <button className="btn btn-outline-secondary btn-sm" onClick={()=> setCurrentPage(p=> Math.min(pageCount, p+1))} disabled={currentPage>=pageCount}>
              <i className="bi bi-chevron-right" />
            </button>
          </div>
        </div>
      )}

      <div className="table-responsive pro-table">
        <table className="table table-sm table-hover table-striped align-middle">
          <thead>
            <tr>
              {(mode==='personalizadas' ? visibleColumns : (Array.isArray(data)&&data[0]? Object.keys(data[0]).map(k=>({key:k,label:k})) : []))
                .map(c=> <th key={c.key}>{c.label}</th>)}
            </tr>
          </thead>
          <tbody>
            {Array.isArray(pageRows) && pageRows.map((row, idx)=> (
              <tr key={idx}>
                {(mode==='personalizadas' ? visibleColumns : (Object.keys(row).map(k=>({key:k,label:k}))))
                  .map(c => <td key={c.key}>{String((row as any)[c.key] ?? '')}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {pdfUrl && (
        <div className="mt-3">
          <div className="d-flex justify-content-between align-items-center mb-2">
            <strong>Pré-visualização do PDF</strong>
            <button className="btn btn-sm btn-outline-secondary" onClick={()=>{ if(pdfUrl) URL.revokeObjectURL(pdfUrl); setPdfUrl(null) }}>
              Fechar
            </button>
          </div>
          <iframe title="PDF Preview" src={pdfUrl} style={{width:'100%', height:'70vh', border:'1px solid #ddd'}} />
        </div>
      )}
        <div className="text-muted mt-1" style={{fontSize:12}}>
          Pré-visualização limitada no navegador para desempenho. Para volumes grandes (500k+ linhas), use a exportação.
        </div>
      </div>
      </div>
    </section>
  )
}
