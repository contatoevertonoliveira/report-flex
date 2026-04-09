import React, { useMemo, useRef, useState } from 'react'
import { api } from '../api'
import { DOOR_LOCATIONS } from '../doorLocations'

type QuickKind = 'access-agg' | 'transit-period' | 'employees' | 'external' | 'card-by-cpf' | 'cpf' | 'matricula' | 'empresa' | 'cracha' | 'nivel' | 'visitantes' | 'door-critical'
type Mode = 'prontas' | 'personalizadas'
type Dataset =
  | 'access-agg'
  | 'transit'
  | 'employees'
  | 'external'
  | 'card-by-cpf'
  | 'cpf-info'
  | 'matricula-info'
  | 'empresa-info'
  | 'cracha-info'
  | 'document-access'
  | 'visitors'
  | 'door-critical'
  | 'db-table'

function pad2(n: number){ return String(n).padStart(2, '0') }
function pad4(n: number){ return String(n).padStart(4, '0') }

function normalizeBrDateInput(s: string): string {
  const digits = (s || '').replace(/\D/g, '').slice(0, 8)
  if (digits.length <= 2) return digits
  if (digits.length <= 4) return digits.slice(0, 2) + '/' + digits.slice(2)
  return digits.slice(0, 2) + '/' + digits.slice(2, 4) + '/' + digits.slice(4)
}

function parseDateParts(s: string | undefined | null): { y: number, m: number, d: number } | null {
  if (!s) return null
  const t = s.trim()
  if (/^\d{4}-\d{2}-\d{2}$/.test(t)) {
    const [y, m, d] = t.split('-').map(n => parseInt(n, 10))
    return { y, m, d }
  }
  const m = t.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})$/)
  if (!m) return null
  const d = parseInt(m[1], 10)
  const mon = parseInt(m[2], 10)
  const y = parseInt(m[3], 10)
  if (!y || mon < 1 || mon > 12 || d < 1 || d > 31) return null
  return { y, m: mon, d }
}

function toIsoDateOnlyValue(s: string | undefined | null): string {
  const p = parseDateParts(s)
  if (!p) return ''
  return `${pad4(p.y)}-${pad2(p.m)}-${pad2(p.d)}`
}

function isoDateToBrValue(s: string | undefined | null): string {
  const t = (s || '').trim()
  const m = t.match(/^(\d{4})-(\d{2})-(\d{2})$/)
  if (!m) return normalizeBrDateInput(t)
  return `${m[3]}/${m[2]}/${m[1]}`
}

type BrDateInputProps = {
  value: string | undefined | null
  placeholder: string
  onChange: (value: string) => void
  inputStyle?: React.CSSProperties
}

function BrDateInput({ value, placeholder, onChange, inputStyle }: BrDateInputProps) {
  const dateRef = useRef<HTMLInputElement>(null)

  const openPicker = () => {
    const el = dateRef.current
    if (!el) return
    try {
      const anyEl = el as any
      el.focus()
      if (typeof anyEl.showPicker === 'function') anyEl.showPicker()
      else el.click()
    } catch {}
  }

  return (
    <>
      <input
        className="form-control"
        style={inputStyle}
        placeholder={placeholder}
        value={isoDateToBrValue(value)}
        onChange={e => onChange(normalizeBrDateInput(e.target.value))}
        onKeyDown={e => {
          if ((e.altKey && e.key === 'ArrowDown') || e.key === 'F4') {
            e.preventDefault()
            openPicker()
          }
        }}
      />
      <span className="btn btn-outline-secondary" title="Calendário" style={{ position: 'relative' }}>
        <i className="bi bi-calendar3" />
        <input
          ref={dateRef}
          type="date"
          value={toIsoDateOnlyValue(value)}
          onChange={e => onChange(isoDateToBrValue(e.target.value))}
          onClick={() => {
            const el = dateRef.current as any
            if (!el) return
            try { if (typeof el.showPicker === 'function') el.showPicker() } catch {}
          }}
          tabIndex={-1}
          aria-label="Selecionar data"
          style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', opacity: 0, cursor: 'pointer' }}
        />
      </span>
    </>
  )
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

function normalizeDoorStatusDisplay(statusAcesso: any, detalheStatusAcesso: any): string {
  const s1 = statusAcesso == null ? '' : String(statusAcesso).trim()
  const s2 = detalheStatusAcesso == null ? '' : String(detalheStatusAcesso).trim()
  const all = `${s1} ${s2}`.toLowerCase()
  if (
    all.includes('denied') ||
    all.includes('negado') ||
    all.includes('não liberado') ||
    all.includes('nao liberado') ||
    all.includes('inactive card') ||
    (all.includes('inactive') && all.includes('card'))
  ) return 'Negado'
  if (all.includes('granted') || all.includes('liberado')) return 'Liberado'
  const parts = [s1, s2].filter(Boolean)
  return parts.length ? parts.join(' - ') : ''
}

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

function normalizeTimeInput(s: string | undefined | null, fallback: string): string {
  const t = (s || '').trim()
  if (!t) return fallback
  const m = t.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?$/)
  if (!m) return fallback
  const hh = parseInt(m[1], 10)
  const mm = parseInt(m[2], 10)
  const ss = m[3] ? parseInt(m[3], 10) : 0
  if (hh < 0 || hh > 23 || mm < 0 || mm > 59 || ss < 0 || ss > 59) return fallback
  return `${pad2(hh)}:${pad2(mm)}:${pad2(ss)}`
}

function toIsoLocalDateTime(dateStr: string | undefined | null, timeStr: string | undefined | null, fallbackTime: string): string | null {
  const p = parseDateParts(dateStr)
  if (!p) return null
  const t = normalizeTimeInput(timeStr, fallbackTime)
  const tm = t.match(/^(\d{2}):(\d{2}):(\d{2})$/)
  if (!tm) return null
  return `${pad4(p.y)}-${pad2(p.m)}-${pad2(p.d)}T${tm[1]}:${tm[2]}:${tm[3]}`
}

function addSecondsToIsoLocal(isoLocal: string, seconds: number): string {
  const m = isoLocal.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})$/)
  if (!m) return isoLocal
  const y = parseInt(m[1], 10)
  const mon = parseInt(m[2], 10)
  const d = parseInt(m[3], 10)
  const hh = parseInt(m[4], 10)
  const mm = parseInt(m[5], 10)
  const ss = parseInt(m[6], 10)
  const dt = new Date(y, mon - 1, d, hh, mm, ss)
  dt.setSeconds(dt.getSeconds() + seconds)
  return `${dt.getFullYear()}-${pad2(dt.getMonth() + 1)}-${pad2(dt.getDate())}T${pad2(dt.getHours())}:${pad2(dt.getMinutes())}:${pad2(dt.getSeconds())}`
}

function rangeIso(filters: {[k:string]: any}): { startIso: string, endIso: string } | null {
  const startIso = toIsoLocalDateTime(filters.start, filters.startTime, '00:00:00')
  const endInclusiveIso = toIsoLocalDateTime(filters.end, filters.endTime, '23:59:59')
  if (!startIso || !endInclusiveIso) return null
  const endIso = addSecondsToIsoLocal(endInclusiveIso, 1)
  return { startIso, endIso }
}

function todayBr(): string {
  const dt = new Date()
  return `${pad2(dt.getDate())}/${pad2(dt.getMonth() + 1)}/${dt.getFullYear()}`
}

const DATASET_COLUMNS: Record<Dataset, { key: string, label: string }[]> = {
  'access-agg': [
    { key: 'LevelId', label: 'LevelId' },
    { key: 'Level', label: 'Level' },
    { key: 'Total', label: 'Total' }
  ],
  'transit': [
    { key: 'CardNumber', label: 'Crachá' },
    { key: 'Name', label: 'Nome' },
    { key: 'Empresa', label: 'Empresa' },
    { key: 'Terminal', label: 'Terminal' },
    { key: 'TerminalDescription', label: 'Terminal Desc.' },
    { key: 'TransitDate', label: 'Data/Hora' }
  ],
  'employees': [
    { key: 'CardNumber', label: 'Crachá' },
    { key: 'Name', label: 'Nome' },
    { key: 'Identifier', label: 'Matrícula' },
    { key: 'StatusCadastro', label: 'Status' },
    { key: 'Cadastro', label: 'Cadastro' },
    { key: 'Expira', label: 'Expira' },
    { key: 'UltimoAcesso', label: 'Último Acesso' },
    { key: 'Empresa', label: 'Empresa' }
  ],
  'external': [
    { key: 'CardNumber', label: 'Crachá' },
    { key: 'Name', label: 'Nome' },
    { key: 'Identifier', label: 'Matrícula' },
    { key: 'StatusCadastro', label: 'Status' },
    { key: 'Cadastro', label: 'Cadastro' },
    { key: 'Expira', label: 'Expira' },
    { key: 'UltimoAcesso', label: 'Último Acesso' },
    { key: 'Empresa', label: 'Empresa' }
  ],
  'card-by-cpf': [
    { key: 'Name', label: 'Nome' },
    { key: 'CardNumber', label: 'Crachá' },
    { key: 'UserType', label: 'Tipo' }
  ],
  'cpf-info': [
    { key: 'Name', label: 'Nome' },
    { key: 'CPF', label: 'CPF' },
    { key: 'Matricula', label: 'Matrícula' },
    { key: 'Empresa', label: 'Empresa' },
    { key: 'Tipo', label: 'Tipo' },
    { key: 'CardNumber', label: 'Crachá' },
    { key: 'Cadastro', label: 'Cadastro' },
    { key: 'Expira', label: 'Expira' }
  ],
  'document-access': [
    { key: 'Name', label: 'Nome' },
    { key: 'CPF', label: 'CPF' },
    { key: 'Matricula', label: 'Matrícula' },
    { key: 'Empresa', label: 'Empresa' },
    { key: 'Cartao', label: 'Crachá' },
    { key: 'Direcao', label: 'Direção' },
    { key: 'Tipo', label: 'Tipo' },
    { key: 'Terminal', label: 'Terminal' },
    { key: 'TerminalDescription', label: 'Descrição' },
    { key: 'Transito', label: 'Trânsito' }
  ],
  'matricula-info': [
    { key: 'Name', label: 'Nome' },
    { key: 'CPF', label: 'CPF' },
    { key: 'Matricula', label: 'Matrícula' },
    { key: 'Empresa', label: 'Empresa' },
    { key: 'Tipo', label: 'Tipo' },
    { key: 'CardNumber', label: 'Crachá' },
    { key: 'Cadastro', label: 'Cadastro' },
    { key: 'Expira', label: 'Expira' }
  ],
  'empresa-info': [
    { key: 'Name', label: 'Nome' },
    { key: 'CPF', label: 'CPF' },
    { key: 'Matricula', label: 'Matrícula' },
    { key: 'Empresa', label: 'Empresa' },
    { key: 'Tipo', label: 'Tipo' },
    { key: 'CardNumber', label: 'Crachá' }
  ],
  'cracha-info': [
    { key: 'Name', label: 'Nome' },
    { key: 'CPF', label: 'CPF' },
    { key: 'Matricula', label: 'Matrícula' },
    { key: 'Empresa', label: 'Empresa' },
    { key: 'Tipo', label: 'Tipo' },
    { key: 'CardNumber', label: 'Crachá' },
    { key: 'Cadastro', label: 'Cadastro' },
    { key: 'Expira', label: 'Expira' }
  ],
  'visitors': [
    { key: 'Nome', label: 'Nome' },
    { key: 'Documento', label: 'Documento' },
    { key: 'Contato', label: 'Contato' },
    { key: 'Visitou', label: 'Visitou' },
    { key: 'Telefone', label: 'Telefone' },
    { key: 'Email', label: 'Email' },
    { key: 'Entrada', label: 'Entrada' },
    { key: 'Saida', label: 'Saída' }
  ],
  'door-critical': [
    { key: 'DataHora', label: 'Data/Hora' },
    { key: 'TAG', label: 'TAG' },
    { key: 'Acesso', label: 'Acesso' },
    { key: 'Evento', label: 'Evento' },
    { key: 'NomeCompleto', label: 'Nome Completo' },
    { key: 'DocumentoMatricula', label: 'Matrícula' },
    { key: 'Cartao', label: 'Cartão' },
    { key: 'Tipo', label: 'Tipo' },
    { key: 'Empresa', label: 'Empresa' },
    { key: 'StatusAcessoDisplay', label: 'Status' }
  ],
  'db-table': []
}

export function QueriesPage(){
  const [mode, setMode] = useState<Mode>('prontas')
  const [quickKind, setQuickKind] = useState<QuickKind>('access-agg')
  const [queriesCfg, setQueriesCfg] = useState<Record<string, boolean>>({})
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [exportModal, setExportModal] = useState(false)
  const [exportMinimized, setExportMinimized] = useState(false)
  const [exportMaximized, setExportMaximized] = useState(false)
  const [exportStage, setExportStage] = useState<'generating'|'ready'|'error'>('generating')
  const [exportFmt, setExportFmt] = useState<'csv'|'xlsx'|'pdf'>('pdf')
  const [exportFileName, setExportFileName] = useState<string>('')
  const [exportUrl, setExportUrl] = useState<string | null>(null)
  const [exportErr, setExportErr] = useState<string | null>(null)
  const [exportProgress, setExportProgress] = useState(0)
  const [showCloseConfirm, setShowCloseConfirm] = useState(false)
  const exportTimerRef = React.useRef<any>(null)
  const [exportJobId, setExportJobId] = useState<string | null>(null)
  const exportJobPollRef = React.useRef<any>(null)
  const [exportPos, setExportPos] = useState<{x:number, y:number}>({x:0, y:0})
  const exportDragRef = React.useRef<{startX:number, startY:number, origX:number, origY:number} | null>(null)
  const exportDragHandlersRef = React.useRef<{move:(e:MouseEvent)=>void, up:(e:MouseEvent)=>void} | null>(null)
  const [reportsModal, setReportsModal] = useState(false)
  const [exportHistory, setExportHistory] = useState<{ id: string, ts: number, label: string, fileName: string, format: 'csv'|'xlsx'|'pdf', requestUrl: string, savedPath?: string }[]>([])
  const [lastPdfRequestUrl, setLastPdfRequestUrl] = useState<string | null>(null)
  const [lastPdfSavedPath, setLastPdfSavedPath] = useState<string | null>(null)
  const [lastPdfFileName, setLastPdfFileName] = useState<string>('')
  const restoredPdfOnceRef = React.useRef(false)
  const [hasCachedPdf, setHasCachedPdf] = React.useState(false)
  const [restoredCache, setRestoredCache] = React.useState(false)
  const [sqlModal, setSqlModal] = useState(false)
  const [sqlUser, setSqlUser] = useState('')
  const [sqlPwd, setSqlPwd] = useState('')
  const [applyingSql, setApplyingSql] = useState(false)
  const [data, setData] = useState<any[]>([])
  const [filters, setFilters] = useState<{[k:string]: any}>({})
  const [pdfUrl, setPdfUrl] = useState<string | null>(null)
  const [pdfExportedRun, setPdfExportedRun] = useState<number | null>(null)
  const [lastSuccessfulRun, setLastSuccessfulRun] = useState(0)
  const [progressActive, setProgressActive] = useState(false)
  const [progress, setProgress] = useState(0)
  const progressTimerRef = React.useRef<any>(null)
  const [resultTotal, setResultTotal] = useState<number | null>(null)
  const [reportOptions, setReportOptions] = useState<{ csv: boolean, xlsx: boolean, excel: boolean, pdf: boolean, txt: boolean, word: boolean, customQueries: boolean }>({ csv: false, xlsx: true, excel: true, pdf: true, txt: false, word: false, customQueries: true })

  // Personalizadas
  const [dataset, setDataset] = useState<Dataset>('transit')
  const [selectedCols, setSelectedCols] = useState<string[]>(DATASET_COLUMNS['transit'].map(c=>c.key))
  const [searchTerm, setSearchTerm] = useState('')
  const [searchColumn, setSearchColumn] = useState<string>('*')
  const [currentPage, setCurrentPage] = useState(1)
  const pageSize = 50
  const maxPreview = 1000
  const [cpfObter, setCpfObter] = useState<'info'|'todos'|'catracas'|'faciais'>('info')
  const [matriculaObter, setMatriculaObter] = useState<'info'|'todos'|'catracas'>('info')
  const [empresaObter, setEmpresaObter] = useState<'info'|'todos'>('info')
  const [crachaObter, setCrachaObter] = useState<'info'|'todos'|'catracas'>('info')
  const [nivelObter, setNivelObter] = useState<'todos'|'acessos'>('todos')
  const [visitantesObter, setVisitantesObter] = useState<'documento'|'empresa'>('documento')
  const [cpfSemPeriodo, setCpfSemPeriodo] = useState<boolean>(false)
  const [dbInfo, setDbInfo] = useState<any>(null)
  const [dbInfoErr, setDbInfoErr] = useState<string | null>(null)
  const [dbTableDb, setDbTableDb] = useState<'CMS'|'Logins'|'EMS'>('CMS')
  const [dbTableName, setDbTableName] = useState('')
  const [doorMode, setDoorMode] = useState<'critical'|'general'|'general-by-name'|'general-by-site'>('critical')
  const [doorAllData, setDoorAllData] = useState(false)
  const [doorAllSources, setDoorAllSources] = useState(true)
  const [doorSources, setDoorSources] = useState<{ key: string, description?: string, group?: string, subGroup?: string }[]>([])
  const [doorCriticalSources, setDoorCriticalSources] = useState<{ key: string, description?: string, group?: string, subGroup?: string }[]>([])
  const [doorSourcesLoading, setDoorSourcesLoading] = useState(false)
  const [doorSourcesErr, setDoorSourcesErr] = useState<string | null>(null)
  const [doorSelectedSources, setDoorSelectedSources] = useState<string[]>([])
  const [doorPickSource, setDoorPickSource] = useState<string>('')
  const [doorSourceFilter, setDoorSourceFilter] = useState<string>('')
  const [doorName, setDoorName] = useState('')
  const [doorSite, setDoorSite] = useState('')

  const level = typeof window !== 'undefined' ? localStorage.getItem('rf_level') : null
  const canUseDbTables = level !== 'Cliente'

  const cacheKey = 'rf_queries_cache_v1'
  const exportHistoryKey = 'rf_export_history_v1'
  const doorSourcesCacheKey = 'rf_door_sources_cache_v1'

  const todayKey = (ts?: number) => {
    const d = new Date(ts ?? Date.now())
    const mm = String(d.getMonth() + 1).padStart(2, '0')
    const dd = String(d.getDate()).padStart(2, '0')
    return `${d.getFullYear()}-${mm}-${dd}`
  }
  const formatTime = (ts: number) => {
    const d = new Date(ts)
    const hh = String(d.getHours()).padStart(2, '0')
    const mm = String(d.getMinutes()).padStart(2, '0')
    const ss = String(d.getSeconds()).padStart(2, '0')
    return `${hh}:${mm}:${ss}`
  }
  const loadExportHistory = () => {
    try{
      const raw = localStorage.getItem(exportHistoryKey)
      const list = raw ? JSON.parse(raw) : []
      return Array.isArray(list) ? list : []
    }catch{
      return []
    }
  }
  const saveExportHistory = (items: any[]) => {
    try{
      localStorage.setItem(exportHistoryKey, JSON.stringify(items))
    }catch{}
  }

  React.useEffect(() => {
    let mounted = true
    ;(async () => {
      try{
        const opts = await api.getReportOptions()
        if (!mounted) return
        setReportOptions({
          csv: !!opts.csv,
          xlsx: !!opts.xlsx,
          excel: !!opts.excel,
          pdf: !!opts.pdf,
          txt: !!opts.txt,
          word: !!opts.word,
          customQueries: !!opts.customQueries
        })
        if (opts.customQueries === false) {
          setMode('prontas')
        }
      }catch{}
    })()
    return () => { mounted = false }
  }, [])
  React.useEffect(() => {
    api.getQueriesConfig()
      .then(c => setQueriesCfg(c || {}))
      .catch(()=> setQueriesCfg({}))
  }, [])

  React.useEffect(() => {
    setExportHistory(loadExportHistory())
  }, [])

  React.useEffect(() => {
    setDoorSourcesLoading(true)
    setDoorSourcesErr(null)
    const fetchCritical = async () => {
      const r: any = await api.reportsDoorCriticalSources({ daysBack: 3650 })
      const items = Array.isArray(r?.items) ? r.items : []
      setDoorCriticalSources(items)
    }
    const fetchGeneral = async () => {
      // Tenta carregar do cache primeiro para Portas Gerais
      try {
        const raw = localStorage.getItem(doorSourcesCacheKey)
        const st = raw ? JSON.parse(raw) : null
        if (st && Array.isArray(st.items) && st.items.length > 0) {
          setDoorSources(st.items)
          setDoorSourcesLoading(false)
          // Atualiza em background
          api.reportsDoorSources({ daysBack: 3650 }).then(r => {
            const items = Array.isArray(r?.items) ? r.items : []
            if (items.length > 0) {
              // Só atualiza se o que veio da API for maior ou a lista atual estiver vazia/pequena
              setDoorSources(prev => {
                if (items.length >= prev.length || prev.length <= 100) {
                  localStorage.setItem(doorSourcesCacheKey, JSON.stringify({ ts: Date.now(), items }))
                  return items
                }
                return prev
              })
            }
          }).catch(() => { })
          return
        }
      } catch { }

      const r: any = await api.reportsDoorSources({ daysBack: 3650 })
      const items = Array.isArray(r?.items) ? r.items : []
      setDoorSources(items)
      if (items.length > 0) {
        try { localStorage.setItem(doorSourcesCacheKey, JSON.stringify({ ts: Date.now(), items })) } catch { }
      }
    }
    ; (async () => {
      try {
        if (quickKind === 'door-critical' && doorMode === 'critical') {
          await fetchCritical()
        } else {
          await fetchGeneral()
        }
      } catch (e: any) {
        if (quickKind === 'door-critical' && doorMode === 'critical') setDoorCriticalSources([])
        else setDoorSources([])
        setDoorSourcesErr(e?.message || 'Falha ao carregar portas')
      } finally {
        setDoorSourcesLoading(false)
      }
    })()
  }, [quickKind, doorMode])

  React.useEffect(() => {
    try{
      if (!localStorage.getItem('rf_token')) {
        sessionStorage.removeItem(cacheKey)
        return
      }
      const raw = sessionStorage.getItem(cacheKey)
      if (!raw) return
      const st = JSON.parse(raw)
      if (!st || st.v !== 1) return
      if (typeof st.lastPdfSavedPath === 'string' || typeof st.lastPdfRequestUrl === 'string') setHasCachedPdf(true)
      if (st.mode === 'prontas' || st.mode === 'personalizadas') setMode(st.mode)
      if (typeof st.quickKind === 'string') setQuickKind(st.quickKind)
      if (typeof st.dataset === 'string') setDataset(st.dataset)
      if (Array.isArray(st.selectedCols)) setSelectedCols(st.selectedCols)
      if (typeof st.searchTerm === 'string') setSearchTerm(st.searchTerm)
      if (typeof st.searchColumn === 'string') setSearchColumn(st.searchColumn)
      if (typeof st.currentPage === 'number' && st.currentPage > 0) setCurrentPage(st.currentPage)
      if (st.filters && typeof st.filters === 'object') setFilters(st.filters)
      if (Array.isArray(st.data)) setData(st.data)
      if (typeof st.resultTotal === 'number') setResultTotal(st.resultTotal)
      if (typeof st.lastSuccessfulRun === 'number' && st.lastSuccessfulRun > 0) setLastSuccessfulRun(st.lastSuccessfulRun)
      if (typeof st.doorMode === 'string') setDoorMode(st.doorMode)
      if (Array.isArray(st.doorSources)) setDoorSources(st.doorSources)
      if (typeof st.doorAllData === 'boolean') setDoorAllData(st.doorAllData)
      if (typeof st.doorAllSources === 'boolean') setDoorAllSources(st.doorAllSources)
      if (Array.isArray(st.doorSelectedSources)) setDoorSelectedSources(st.doorSelectedSources)
      if (typeof st.doorPickSource === 'string') setDoorPickSource(st.doorPickSource)
      if (typeof st.doorSourceFilter === 'string') setDoorSourceFilter(st.doorSourceFilter)
      if (typeof st.doorName === 'string') setDoorName(st.doorName)
      if (typeof st.doorSite === 'string') setDoorSite(st.doorSite)
      if (typeof st.lastPdfRequestUrl === 'string') setLastPdfRequestUrl(st.lastPdfRequestUrl)
      if (typeof st.lastPdfSavedPath === 'string') setLastPdfSavedPath(st.lastPdfSavedPath)
      if (typeof st.lastPdfFileName === 'string') setLastPdfFileName(st.lastPdfFileName)
      if (st.exportModal === true) setExportModal(true)
      if (typeof st.exportMinimized === 'boolean') setExportMinimized(st.exportMinimized)
      if (typeof st.exportMaximized === 'boolean') setExportMaximized(st.exportMaximized)
      if (typeof st.exportStage === 'string') setExportStage(st.exportStage)
      if (typeof st.exportProgress === 'number') setExportProgress(st.exportProgress)
      if (typeof st.exportFileName === 'string') setExportFileName(st.exportFileName)
      if (typeof st.exportFmt === 'string') setExportFmt(st.exportFmt)
      if (typeof st.exportUrl === 'string') setExportUrl(st.exportUrl)
      if (typeof st.exportErr === 'string') setExportErr(st.exportErr)
      if (typeof st.exportJobId === 'string') setExportJobId(st.exportJobId)
      if (typeof st.cpfObter === 'string') setCpfObter(st.cpfObter)
      if (typeof st.matriculaObter === 'string') setMatriculaObter(st.matriculaObter)
      if (typeof st.empresaObter === 'string') setEmpresaObter(st.empresaObter)
      if (typeof st.crachaObter === 'string') setCrachaObter(st.crachaObter)
      if (typeof st.nivelObter === 'string') setNivelObter(st.nivelObter)
      if (typeof st.visitantesObter === 'string') setVisitantesObter(st.visitantesObter)
    }catch{}
    setTimeout(() => { setRestoredCache(true) }, 0)
  }, [])

  React.useEffect(() => {
    if (restoredPdfOnceRef.current) return
    if (!restoredCache) return
    if (!hasCachedPdf) return
    restoredPdfOnceRef.current = true
  }, [restoredCache, hasCachedPdf])

  React.useEffect(() => {
    try{
      if (!localStorage.getItem('rf_token')) {
        sessionStorage.removeItem(cacheKey)
        return
      }
      if (!lastSuccessfulRun) return
      const dataPreview = Array.isArray(data) ? data.slice(0, maxPreview) : []
      const total = typeof resultTotal === 'number' ? resultTotal : (Array.isArray(data) ? data.length : 0)
      const snapshot = {
        v: 1,
        ts: Date.now(),
        mode,
        quickKind,
        dataset,
        selectedCols,
        searchTerm,
        searchColumn,
        currentPage,
        filters,
        data: dataPreview,
        resultTotal: total,
        lastSuccessfulRun,
        doorMode,
        doorSources,
        doorAllData,
        doorAllSources,
        doorSelectedSources,
        doorPickSource,
        doorSourceFilter,
        doorName,
        doorSite,
        lastPdfRequestUrl,
        lastPdfSavedPath,
        lastPdfFileName,
        exportModal,
        exportMinimized,
        exportMaximized,
        exportStage,
        exportFmt,
        exportFileName,
        exportProgress,
        exportUrl,
        exportErr,
        exportJobId,
        cpfObter,
        matriculaObter,
        empresaObter,
        crachaObter,
        nivelObter,
        visitantesObter
      }
      sessionStorage.setItem(cacheKey, JSON.stringify(snapshot))
    }catch{}
  }, [lastSuccessfulRun, lastPdfRequestUrl, lastPdfSavedPath, exportStage, exportModal, exportMinimized, exportMaximized, exportUrl, exportProgress, exportJobId])

  React.useEffect(() => {
    if (!restoredCache) return
    if (exportJobId && exportStage === 'generating') {
      if (exportJobPollRef.current) clearInterval(exportJobPollRef.current)
      const h: Record<string, string> = {}
      const t = localStorage.getItem('rf_token')
      if (t) h['Authorization'] = `Bearer ${t}`
      const cid = localStorage.getItem('rf_client_id')
      if (cid) h['X-Client-Id'] = cid

      exportJobPollRef.current = setInterval(async () => {
        try {
          const st = await fetch(`/api/reports/export-jobs/${exportJobId}`, { headers: h })
          if (!st.ok) return
          const s: any = await st.json()
          const status = s?.status
          const prog = Number(s?.progress ?? 0)
          if (!Number.isNaN(prog)) {
            let mapped = 65
            if (prog >= 100) mapped = 100
            else if (prog >= 90) mapped = 90
            else if (prog >= 50) mapped = 75
            else mapped = 65
            setExportProgress(mapped)
          }
          if (status === 'done' && s?.downloadUrl) {
            if (exportJobPollRef.current) clearInterval(exportJobPollRef.current)
            exportJobPollRef.current = null
            setExportUrl(s.downloadUrl)
            setExportStage('ready')
            setExportProgress(100)
          } else if (status === 'error') {
            if (exportJobPollRef.current) clearInterval(exportJobPollRef.current)
            exportJobPollRef.current = null
            const msg = s?.error || 'Falha ao exportar'
            setError(msg)
            setExportErr(msg)
            setExportStage('error')
          }
        } catch { }
      }, 1200)
    }
    return () => {
      if (exportJobPollRef.current) {
        clearInterval(exportJobPollRef.current)
        exportJobPollRef.current = null
      }
    }
  }, [restoredCache, exportJobId, exportStage])

  const exportEnabledCsv = reportOptions.csv
  const exportEnabledXlsx = reportOptions.xlsx || reportOptions.excel
  const exportEnabledPdf = reportOptions.pdf

  const canExport = useMemo(() => {
    if (mode === 'prontas' && (quickKind === 'access-agg' || quickKind === 'transit-period' || quickKind === 'door-critical')) {
      return data && data.length > 0
    }
    if (mode === 'prontas' && quickKind === 'cpf' && cpfObter !== 'info') {
      return data && data.length > 0
    }
    if (mode === 'personalizadas' && (dataset === 'access-agg' || dataset === 'transit')) {
      return data && data.length > 0
    }
    return false
  }, [mode, quickKind, dataset, data, cpfObter])

  const exportAllowsPdf = useMemo(() => {
    if (!canExport) return false
    if (mode === 'prontas' && (quickKind === 'access-agg' || quickKind === 'transit-period' || quickKind === 'door-critical')) return true
    if (mode === 'prontas' && quickKind === 'cpf' && cpfObter !== 'info') return true
    if (mode === 'personalizadas' && (dataset === 'access-agg' || dataset === 'transit')) return true
    return false
  }, [canExport, mode, quickKind, dataset, cpfObter])

  const exportAllowsXlsx = useMemo(() => {
    if (!canExport) return false
    if (mode === 'prontas' && (quickKind === 'access-agg' || quickKind === 'transit-period' || quickKind === 'door-critical')) return true
    if (mode === 'prontas' && quickKind === 'cpf' && cpfObter !== 'info') return true
    if (mode === 'personalizadas' && (dataset === 'access-agg' || dataset === 'transit')) return true
    return false
  }, [canExport, mode, quickKind, dataset, cpfObter])

  const exportAllowsCsv = useMemo(() => {
    if (!canExport) return false
    if (mode === 'prontas' && quickKind === 'cpf' && cpfObter !== 'info') return true
    if (mode === 'prontas' && (quickKind === 'access-agg' || quickKind === 'transit-period' || quickKind === 'door-critical')) return true
    if (mode === 'personalizadas' && (dataset === 'access-agg' || dataset === 'transit')) return true
    return false
  }, [canExport, mode, quickKind, cpfObter, dataset])

  const showExportGroup = canExport && ((exportEnabledCsv && exportAllowsCsv) || (exportEnabledXlsx && exportAllowsXlsx) || (exportEnabledPdf && exportAllowsPdf))
  const readyQueryEnabled = mode !== 'prontas' ? true : !!queriesCfg[quickKind]
  const enabledReadyKeys = useMemo(() => {
    const keys: QuickKind[] = [
      'access-agg', 'transit-period', 'door-critical',
      'employees', 'external', 'card-by-cpf',
      'cpf', 'matricula', 'empresa', 'cracha', 'nivel', 'visitantes'
    ]
    return keys.filter(k => !!queriesCfg[k])
  }, [queriesCfg])

  React.useEffect(() => {
    if (!restoredCache) return
    if (mode !== 'prontas') return
    if (readyQueryEnabled) return
    if (enabledReadyKeys.length === 0) return
    setQuickKind(enabledReadyKeys[0])
    setData([])
    setError(null)
  }, [restoredCache, mode, readyQueryEnabled, enabledReadyKeys])

  const exportsToday = useMemo(() => {
    const k = todayKey()
    return exportHistory.filter(x => todayKey(x.ts) === k).sort((a,b)=> b.ts - a.ts)
  }, [exportHistory])

  const activeDoorSources = useMemo(() => {
    return doorMode === 'critical' ? doorCriticalSources : doorSources
  }, [doorMode, doorCriticalSources, doorSources])

  const doorSourcesGrouped = useMemo(() => {
    const map = new Map<string, { label: string, items: { key: string, label: string }[] }>()
    for (const s of activeDoorSources) {
      const g = (s.group || '').trim() || 'Outros'
      const sg = (s.subGroup || '').trim()
      const groupKey = `${g}__${sg}`
      const label = sg ? `${g} • ${sg}` : g
      const desc = DOOR_LOCATIONS[s.key] || s.description
      const optLabel = desc ? `${desc} (${s.key})` : s.key
      if (!map.has(groupKey)) map.set(groupKey, { label, items: [] })
      map.get(groupKey)!.items.push({ key: s.key, label: optLabel })
    }
    const groups = Array.from(map.values())
    groups.sort((a, b) => a.label.localeCompare(b.label, 'pt-BR'))
    for (const g of groups) {
      g.items.sort((a, b) => a.label.localeCompare(b.label, 'pt-BR'))
    }
    return groups
  }, [activeDoorSources])

  const doorSourceLabelByKey = useMemo(() => {
    const m = new Map<string, string>()
    for (const s of activeDoorSources) {
      const desc = DOOR_LOCATIONS[s.key] || s.description
      const label = desc ? `${desc} (${s.key})` : s.key
      m.set(s.key, label)
    }
    return m
  }, [activeDoorSources])

  React.useEffect(() => {
    if (doorSelectedSources.length > 0 && doorAllSources) {
      setDoorAllSources(false)
    }
  }, [doorSelectedSources, doorAllSources])

  const autoRunTimerRef = React.useRef<any>(null)
  React.useEffect(() => {
    if (!restoredCache) return
    if (mode !== 'prontas') return
    if (quickKind !== 'door-critical') return
    if (autoRunTimerRef.current) clearTimeout(autoRunTimerRef.current)
    autoRunTimerRef.current = setTimeout(() => {
      runQuick()
    }, 400)
    return () => {
      if (autoRunTimerRef.current) clearTimeout(autoRunTimerRef.current)
      autoRunTimerRef.current = null
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [restoredCache, mode, quickKind, doorMode, doorAllSources, doorSelectedSources.join('|')])

  const doorShortKey = (key: string) => {
    const parts = (key || '').split('_').filter(Boolean)
    if (parts.length >= 3) {
      const last = parts[2]
      const m = last.match(/^RDR(\d+)$/i)
      const lastShort = m ? `R${m[1]}` : last
      return `${parts[0]}-${parts[1]}-${lastShort}`
    }
    if (parts.length === 2) return `${parts[0]}-${parts[1]}`
    return (key || '').length > 18 ? (key || '').slice(0, 18) + '…' : (key || '')
  }

  const normalizeDoorSearch = (s: string) => {
    const t = (s || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase()
    return t.replace(/[^a-z0-9]+/g, ' ').trim()
  }

  const filteredDoorSourcesGrouped = useMemo(() => {
    const tokens = normalizeDoorSearch(doorSourceFilter || '').split(' ').filter(Boolean)
    if (tokens.length === 0) return doorSourcesGrouped
    const groups = []
    for (const g of doorSourcesGrouped) {
      const items = g.items.filter(it => {
        const hay = normalizeDoorSearch(`${it.label} ${it.key} ${doorShortKey(it.key)}`)
        return tokens.every(t => hay.includes(t))
      })
      if (items.length) groups.push({ label: g.label, items })
    }
    return groups
  }, [doorSourceFilter, doorSourcesGrouped])

  const filteredDoorKeys = useMemo(() => {
    const keys: string[] = []
    for (const g of filteredDoorSourcesGrouped) {
      for (const it of g.items) keys.push(it.key)
    }
    return Array.from(new Set(keys))
  }, [filteredDoorSourcesGrouped])

  function resetData(){
    setData([])
    setError(null)
    setPdfExportedRun(null)
    if (pdfUrl) URL.revokeObjectURL(pdfUrl)
    setPdfUrl(null)
    if (exportUrl && exportUrl !== pdfUrl) URL.revokeObjectURL(exportUrl)
    setExportUrl(null)
    setExportModal(false)
    setExportMinimized(false)
    setExportMaximized(false)
    setExportStage('generating')
    setExportErr(null)
    setExportProgress(0)
    setExportJobId(null)
    setExportPos({x:0, y:0})
    if (exportTimerRef.current) clearInterval(exportTimerRef.current)
    exportTimerRef.current = null
    if (exportJobPollRef.current) clearInterval(exportJobPollRef.current)
    exportJobPollRef.current = null
    if (exportDragHandlersRef.current){
      window.removeEventListener('mousemove', exportDragHandlersRef.current.move)
      window.removeEventListener('mouseup', exportDragHandlersRef.current.up)
      exportDragHandlersRef.current = null
    }
    exportDragRef.current = null
    if (exportFloatDragHandlersRef.current){
      window.removeEventListener('mousemove', exportFloatDragHandlersRef.current.move)
      window.removeEventListener('mouseup', exportFloatDragHandlersRef.current.up)
      exportFloatDragHandlersRef.current = null
    }
    exportFloatDragRef.current = null
  }

  function mapQuickToDataset(k: QuickKind): Dataset{
    if (k === 'transit-period') return 'transit'
    if (k === 'door-critical') return 'door-critical'
    if (k === 'employees') return 'employees'
    if (k === 'external') return 'external'
    if (k === 'card-by-cpf') return 'card-by-cpf'
    if (k === 'cpf') return cpfObter === 'info' ? 'cpf-info' : 'document-access'
    if (k === 'matricula') return matriculaObter === 'info' ? 'matricula-info' : 'transit'
    if (k === 'empresa') return empresaObter === 'info' ? 'empresa-info' : 'transit'
    if (k === 'cracha') return crachaObter === 'info' ? 'cracha-info' : 'transit'
    if (k === 'nivel') return nivelObter === 'todos' ? 'access-agg' : 'transit'
    if (k === 'visitantes') return 'visitors'
    return 'access-agg'
  }

  function startProgress(){
    setProgressActive(true)
    setProgress(0)
    setResultTotal(null)
    if (progressTimerRef.current) clearInterval(progressTimerRef.current)
    progressTimerRef.current = setInterval(() => {
      setProgress(p => {
        if (p >= 90) return p
        const inc = p < 30 ? 6 : p < 60 ? 3 : 1
        return Math.min(90, p + inc)
      })
    }, 500)
  }

  function stopProgress(ok: boolean){
    if (progressTimerRef.current) clearInterval(progressTimerRef.current)
    progressTimerRef.current = null
    if (ok){
      setProgress(100)
      setTimeout(() => {
        setProgressActive(false)
        setProgress(0)
      }, 600)
    }else{
      setProgressActive(false)
      setProgress(0)
    }
  }

  async function collectUpTo(maxItems: number, fetchPage: (page:number, pageSize:number)=> Promise<any>){
    const batch: any[] = []
    let page = 1
    const ps = 200
    let lastTotal: number | null = null
    while (batch.length < maxItems){
      const res = await fetchPage(page, ps)
      const items = Array.isArray(res) ? res : (res?.items ?? [])
      const total = Array.isArray(res) ? null : (typeof res?.total === 'number' ? res.total : null)
      if (total != null && total >= 0){
        lastTotal = total
        setResultTotal(total)
      }
      if (!items || items.length === 0) break
      for (const it of items){
        batch.push(it)
        if (batch.length >= maxItems) break
      }
      if (lastTotal != null && lastTotal > 0 && progressActive){
        const denom = Math.min(lastTotal, maxItems)
        const pct = denom <= 0 ? 0 : Math.min(99, Math.floor((Math.min(batch.length, denom) / denom) * 100))
        setProgress(p => Math.max(p, pct))
      }
      if (items.length < ps) break
      page += 1
    }
    return batch
  }

  React.useEffect(() => {
    if (mode === 'personalizadas' && dataset === 'db-table' && canUseDbTables && !dbInfo && !dbInfoErr){
      api.getDbInfo().then(info => {
        setDbInfo(info)
      }).catch(() => {
        setDbInfoErr('Não foi possível carregar informações das tabelas dos bancos.')
      })
    }
  }, [mode, dataset, canUseDbTables, dbInfo, dbInfoErr])

  async function runQuick(){
    setError(null); setLoading(true); startProgress()
    setPdfExportedRun(null)
    if (pdfUrl) URL.revokeObjectURL(pdfUrl)
    setPdfUrl(null)
    let ok = false
    try{
      if (quickKind === 'access-agg'){
        const res = await api.reportsAccessAggregated()
        setData(Array.isArray(res) ? res : [])
      }else if (quickKind === 'transit-period'){
        const { empresa, terminal } = filters as any
        const r0 = rangeIso(filters)
        if(!r0){ setError('Informe início e fim'); setLoading(false); return }
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.reportsTransit({ start: r0.startIso, end: r0.endIso, empresa, terminal, page, pageSize: ps })
          return r
        })
        setData(collected)
      }else if (quickKind === 'employees'){
        const { matricula, empresa } = filters as any
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.employeesSearch({ matricula, empresa, page, pageSize: ps, sort: 'CardNumber', dir: 'asc' })
          return r
        })
        setData(collected.map((x:any) => ({
          CardNumber: x?.CardNumber ?? x?.cardNumber ?? null,
          Name: x?.Name ?? x?.name ?? null,
          Identifier: x?.Identifier ?? x?.identifier ?? null,
          StatusCadastro: x?.StatusCadastro ?? x?.statusCadastro ?? null,
          Cadastro: formatBrDateTime(x?.Cadastro ?? x?.cadastro ?? null),
          Expira: formatBrDateTime(x?.Expira ?? x?.expira ?? null),
          UltimoAcesso: formatBrDateTime(x?.UltimoAcesso ?? x?.ultimoAcesso ?? null),
          Empresa: x?.Empresa ?? x?.empresa ?? null
        })))
      }else if (quickKind === 'external'){
        const { matricula, empresa } = filters as any
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.externalSearch({ matricula, empresa, page, pageSize: ps, sort: 'CardNumber', dir: 'asc' })
          const items = (r as any)?.items ?? r ?? []
          return { items: Array.isArray(items) ? items : [], total: (r as any)?.total }
        })
        setData(collected.map((x:any) => ({
          CardNumber: x?.CardNumber ?? x?.cardNumber ?? null,
          Name: x?.Name ?? x?.name ?? null,
          Identifier: x?.Identifier ?? x?.identifier ?? null,
          StatusCadastro: x?.StatusCadastro ?? x?.statusCadastro ?? null,
          Cadastro: formatBrDateTime(x?.Cadastro ?? x?.cadastro ?? null),
          Expira: formatBrDateTime(x?.Expira ?? x?.expira ?? null),
          UltimoAcesso: formatBrDateTime(x?.UltimoAcesso ?? x?.ultimoAcesso ?? null),
          Empresa: x?.Empresa ?? x?.empresa ?? null
        })))
      }else if (quickKind === 'card-by-cpf'){
        const { cpf } = filters as any
        if (!cpf){ setError('Informe o CPF'); setLoading(false); return }
        const res = await api.cardByCpf(cpf)
        const list = Array.isArray(res) ? res : ((res as any)?.items ?? [])
        setData(list)
      }else if (quickKind === 'cpf'){
        const { cpf } = filters as any
        if (!cpf){ setError('Informe o CPF'); setLoading(false); return }
        if (cpfObter === 'info'){
          const res = await api.accessInfoByDocument(cpf)
          const list = Array.isArray(res) ? res : ((res as any)?.items ?? [])
          setData(list)
        }else{
          const mode = cpfObter === 'todos' ? 'all' : cpfObter
          if (cpfSemPeriodo){
            const collected = await collectUpTo(maxPreview, async (page, ps) => {
              const r = await api.accessByDocumentAll({ documento: cpf, mode, page, pageSize: ps })
              return r
            })
            setData(collected)
          }else{
            const r0 = rangeIso(filters)
            if(!r0){ setError('Informe início e fim'); setLoading(false); return }
            const collected = await collectUpTo(maxPreview, async (page, ps) => {
              const r = await api.accessByDocument({ documento: cpf, start: r0.startIso, end: r0.endIso, mode, page, pageSize: ps })
              return r
            })
            setData(collected)
          }
        }
      }else if (quickKind === 'matricula'){
        const { matricula } = filters as any
        if (!matricula){ setError('Informe a matrícula'); setLoading(false); return }
        if (matriculaObter === 'info'){
          const res = await api.personByMatriculaInfo(matricula)
          const list = Array.isArray(res) ? res : ((res as any)?.items ?? [])
          setData(list)
        }else{
          const r0 = rangeIso(filters)
          if(!r0){ setError('Informe início e fim'); setLoading(false); return }
          const onlyTurnstiles = matriculaObter === 'catracas'
          const collected = await collectUpTo(maxPreview, async (page, ps) => {
            const r = await api.transitByMatricula({ matricula, start: r0.startIso, end: r0.endIso, onlyTurnstiles, page, pageSize: ps })
            return r
          })
          setData(collected)
        }
      }else if (quickKind === 'empresa'){
        const { empresa } = filters as any
        if (!empresa){ setError('Informe a empresa'); setLoading(false); return }
        if (empresaObter === 'info'){
          const res = await api.companyByNameInfo(empresa)
          const list = Array.isArray(res) ? res : ((res as any)?.items ?? [])
          setData(list)
        }else{
          const r0 = rangeIso(filters)
          if(!r0){ setError('Informe início e fim'); setLoading(false); return }
          const collected = await collectUpTo(maxPreview, async (page, ps) => {
            const r = await api.transitByEmpresa({ empresa, start: r0.startIso, end: r0.endIso, page, pageSize: ps })
            return r
          })
          setData(collected)
        }
      }else if (quickKind === 'cracha'){
        const { cracha } = filters as any
        if (!cracha){ setError('Informe o crachá'); setLoading(false); return }
        if (crachaObter === 'info'){
          const res = await api.personByCardInfo(cracha)
          const list = Array.isArray(res) ? res : ((res as any)?.items ?? [])
          setData(list)
        }else{
          const r0 = rangeIso(filters)
          if(!r0){ setError('Informe início e fim'); setLoading(false); return }
          const onlyTurnstiles = crachaObter === 'catracas'
          const collected = await collectUpTo(maxPreview, async (page, ps) => {
            const r = await api.transitByCardPeriod({ card: cracha, start: r0.startIso, end: r0.endIso, onlyTurnstiles, page, pageSize: ps })
            return r
          })
          setData(collected)
        }
      }else if (quickKind === 'nivel'){
        const { levelId, levelName } = filters as any
        if (nivelObter === 'todos'){
          const r0 = rangeIso(filters)
          if(!r0){ setError('Informe início e fim'); setLoading(false); return }
          const res = await api.reportsAccessByLevelPeriod({ start: r0.startIso, end: r0.endIso })
          setData(Array.isArray(res) ? res : [])
        }else{
          const r0 = rangeIso(filters)
          if ((!levelId && !levelName) || !r0){ setError('Informe o nível e o período'); setLoading(false); return }
          const collected = await collectUpTo(maxPreview, async (page, ps) => {
            const r = await api.transitByLevel({ levelId: levelId? Number(levelId):undefined, levelName, start: r0.startIso, end: r0.endIso, page, pageSize: ps })
            return r
          })
          setData(collected)
        }
      }else if (quickKind === 'visitantes'){
        const { documento, empresa } = filters as any
        const r0 = rangeIso(filters)
        if(!r0){ setError('Informe início e fim'); setLoading(false); return }
        if (visitantesObter === 'documento'){
          if (!documento){ setError('Informe o documento'); setLoading(false); return }
          const collected = await collectUpTo(maxPreview, async (page, ps) => {
            const r = await api.visitorsByDocument({ documento, start: r0.startIso, end: r0.endIso, page, pageSize: ps })
            const items = (r as any)?.items ?? r ?? []
            return { items: Array.isArray(items) ? items : [], total: (r as any)?.total }
          })
          setData(collected)
        }else{
          if (!empresa){ setError('Informe a empresa'); setLoading(false); return }
          const collected = await collectUpTo(maxPreview, async (page, ps) => {
            const r = await api.visitorsByCompany({ empresa, start: r0.startIso, end: r0.endIso, page, pageSize: ps })
            const items = (r as any)?.items ?? r ?? []
            return { items: Array.isArray(items) ? items : [], total: (r as any)?.total }
          })
          setData(collected)
        }
      }else if (quickKind === 'door-critical'){
        const r0 = doorAllData
          ? { startIso: '1900-01-01T00:00:00', endIso: '2100-01-01T00:00:00' }
          : (() => {
              const sRaw = (filters as any).start || todayBr()
              const eRaw = (filters as any).end || sRaw
              return rangeIso({ ...filters, start: sRaw, end: eRaw })
            })()
        if(!r0){ setError('Informe início e fim'); setLoading(false); return }
        const src = doorAllSources ? undefined : doorSelectedSources.join(';')
        if ((doorMode === 'general' || doorMode === 'general-by-name' || doorMode === 'critical') && !doorAllSources && !src){ setError('Selecione uma ou mais portas'); setLoading(false); return }
        if (doorMode === 'critical'){
          const res = await api.reportsDoorCritical({ start: r0.startIso, end: r0.endIso, sourceList: src })
          const list = (res as any)?.data ?? res
          const rows = Array.isArray(list) ? list : []
          const mapped = rows.map((x:any)=> ({ ...x, DataHora: formatBrDateTime(getRowValue(x, 'DataHora')), TimeOrder: formatBrDateTime(getRowValue(x, 'TimeOrder')), StatusAcessoDisplay: normalizeDoorStatusDisplay(getRowValue(x, 'StatusAcesso'), getRowValue(x, 'DetalheStatusAcesso')) }))
          setResultTotal(mapped.length)
          setData(mapped.slice(0, maxPreview))
        }else if (doorMode === 'general'){
          const res = await api.reportsDoorGeneral({ start: r0.startIso, end: r0.endIso, sourceList: src })
          const list = (res as any)?.data ?? res
          const rows = Array.isArray(list) ? list : []
          const mapped = rows.map((x:any)=> ({ ...x, DataHora: formatBrDateTime(getRowValue(x, 'DataHora')), TimeOrder: formatBrDateTime(getRowValue(x, 'TimeOrder')), StatusAcessoDisplay: normalizeDoorStatusDisplay(getRowValue(x, 'StatusAcesso'), getRowValue(x, 'DetalheStatusAcesso')) }))
          setResultTotal(mapped.length)
          setData(mapped.slice(0, maxPreview))
        }else if (doorMode === 'general-by-name'){
          if (!doorName){ setError('Informe o nome'); setLoading(false); return }
          const res = await api.reportsDoorGeneralByName({ start: r0.startIso, end: r0.endIso, name: doorName, sourceList: src })
          const list = (res as any)?.data ?? res
          const rows = Array.isArray(list) ? list : []
          const mapped = rows.map((x:any)=> ({ ...x, DataHora: formatBrDateTime(getRowValue(x, 'DataHora')), TimeOrder: formatBrDateTime(getRowValue(x, 'TimeOrder')), StatusAcessoDisplay: normalizeDoorStatusDisplay(getRowValue(x, 'StatusAcesso'), getRowValue(x, 'DetalheStatusAcesso')) }))
          setResultTotal(mapped.length)
          setData(mapped.slice(0, maxPreview))
        }else if (doorMode === 'general-by-site'){
          if (!doorSite){ setError('Informe o site'); setLoading(false); return }
          const res = await api.reportsDoorGeneralBySite({ start: r0.startIso, end: r0.endIso, site: doorSite })
          const list = (res as any)?.data ?? res
          const rows = Array.isArray(list) ? list : []
          const mapped = rows.map((x:any)=> ({ ...x, DataHora: formatBrDateTime(getRowValue(x, 'DataHora')), TimeOrder: formatBrDateTime(getRowValue(x, 'TimeOrder')), StatusAcessoDisplay: normalizeDoorStatusDisplay(getRowValue(x, 'StatusAcesso'), getRowValue(x, 'DetalheStatusAcesso')) }))
          setResultTotal(mapped.length)
          setData(mapped.slice(0, maxPreview))
        }
      }
      ok = true
    }catch(e:any){
      const msg = e?.message || 'Falha na consulta'
      setError(msg)
      if (/Login failed for user/i.test(msg)) setSqlModal(true)
      setData([])
    }finally{
      setLoading(false)
      setCurrentPage(1)
      if (ok) setLastSuccessfulRun(v => v + 1)
      stopProgress(ok)
    }
  }

  async function runPersonalizada(){
    setError(null); setLoading(true); startProgress()
    setPdfExportedRun(null)
    if (pdfUrl) URL.revokeObjectURL(pdfUrl)
    setPdfUrl(null)
    let ok = false
    try{
      if (dataset === 'access-agg'){
        const res = await api.reportsAccessAggregated()
        setData(Array.isArray(res) ? res : [])
      }else if (dataset === 'transit'){
        const { empresa, terminal } = filters as any
        const r0 = rangeIso(filters)
        if(!r0){ setError('Informe início e fim'); setLoading(false); return }
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.reportsTransit({ start: r0.startIso, end: r0.endIso, empresa, terminal, page, pageSize: ps })
          return r
        })
        setData(collected)
      }else if (dataset === 'employees'){
        const { matricula, empresa } = filters as any
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.employeesSearch({ matricula, empresa, page, pageSize: ps, sort: 'CardNumber', dir: 'asc' })
          return r
        })
        setData(collected.map((x:any) => ({
          CardNumber: x?.CardNumber ?? x?.cardNumber ?? null,
          Name: x?.Name ?? x?.name ?? null,
          Identifier: x?.Identifier ?? x?.identifier ?? null,
          StatusCadastro: x?.StatusCadastro ?? x?.statusCadastro ?? null,
          Cadastro: formatBrDateTime(x?.Cadastro ?? x?.cadastro ?? null),
          Expira: formatBrDateTime(x?.Expira ?? x?.expira ?? null),
          UltimoAcesso: formatBrDateTime(x?.UltimoAcesso ?? x?.ultimoAcesso ?? null),
          Empresa: x?.Empresa ?? x?.empresa ?? null
        })))
      }else if (dataset === 'external'){
        const { matricula, empresa } = filters as any
        const collected = await collectUpTo(maxPreview, async (page, ps) => {
          const r = await api.externalSearch({ matricula, empresa, page, pageSize: ps, sort: 'CardNumber', dir: 'asc' })
          const items = (r as any)?.items ?? r ?? []
          return { items: Array.isArray(items) ? items : [], total: (r as any)?.total }
        })
        setData(collected.map((x:any) => ({
          CardNumber: x?.CardNumber ?? x?.cardNumber ?? null,
          Name: x?.Name ?? x?.name ?? null,
          Identifier: x?.Identifier ?? x?.identifier ?? null,
          StatusCadastro: x?.StatusCadastro ?? x?.statusCadastro ?? null,
          Cadastro: formatBrDateTime(x?.Cadastro ?? x?.cadastro ?? null),
          Expira: formatBrDateTime(x?.Expira ?? x?.expira ?? null),
          UltimoAcesso: formatBrDateTime(x?.UltimoAcesso ?? x?.ultimoAcesso ?? null),
          Empresa: x?.Empresa ?? x?.empresa ?? null
        })))
      }else if (dataset === 'db-table'){
        if (!canUseDbTables){
          setError('Apenas usuários internos podem consultar tabelas completas.')
          setLoading(false)
          return
        }
        if (!dbTableName){
          setError('Selecione a base e a tabela para consultar.')
          setLoading(false)
          return
        }
        const res = await api.dbTableRows({ db: dbTableDb, table: dbTableName, page: 1, pageSize: maxPreview })
        const items = (res as any)?.items ?? res ?? []
        const list = Array.isArray(items) ? items : []
        setData(list)
        if (list[0]){
          const cols = Object.keys(list[0])
          setSelectedCols(cols)
        }
      }
      ok = true
    }catch(e:any){
      const msg = e?.message || 'Falha na consulta'
      setError(msg)
      if (/Login failed for user/i.test(msg)) setSqlModal(true)
      setData([])
    }finally{
      setLoading(false)
      setCurrentPage(1)
      if (ok) setLastSuccessfulRun(v => v + 1)
      stopProgress(ok)
    }
  }

  async function exportData(format: 'csv'|'xlsx'|'pdf'){
    try{
      const h: Record<string,string> = {}
      const t = localStorage.getItem('rf_token')
      if (t) h['Authorization'] = `Bearer ${t}`
      const cid = localStorage.getItem('rf_client_id')
      if (cid) h['X-Client-Id'] = cid
      let url = ''
      let name = ''
      const startDoorExportJob = async (p: { start: string, end: string, sourceList?: string, name?: string }, downloadName: string) => {
        setExportFmt('csv')
        setExportFileName(downloadName)
        setExportErr(null)
        setExportStage('generating')
        setExportModal(true)
        setExportMinimized(false)
        setExportMaximized(false)
        setExportProgress(25) // Checkpoint inicial: 25%

        if (exportTimerRef.current) clearInterval(exportTimerRef.current)
        let waitSeconds = 0
        exportTimerRef.current = setInterval(() => {
          waitSeconds++
          setExportProgress(p => {
            if (waitSeconds === 1) return 35 // Checkpoint: 35% após 1s
            if (waitSeconds === 2) return 50 // Checkpoint: 50% após 2s
            return p
          })
        }, 1000)

        const res = await fetch('/api/reports/door-general/export-jobs', {
          method: 'POST',
          headers: { ...h, 'Content-Type': 'application/json' },
          body: JSON.stringify({ start: p.start, end: p.end, sourceList: p.sourceList, name: p.name, format: 'csv' })
        })
        if (exportTimerRef.current) clearInterval(exportTimerRef.current)
        exportTimerRef.current = null

        if (!res.ok){ await showErr(res); return }
        const j: any = await res.json()
        const id = j?.id
        if (!id){ setExportErr('Falha ao iniciar exportação'); setExportStage('error'); return }
        setExportJobId(id)
        setExportProgress(65) // Já iniciou o job -> 65%

        exportJobPollRef.current = setInterval(async () => {
          try{
            const st = await fetch(`/api/reports/export-jobs/${id}`, { headers: h })
            if (!st.ok) return
            const s: any = await st.json()
            const status = s?.status
            const prog = Number(s?.progress ?? 0)
            if (!Number.isNaN(prog)) {
              // Mapeia o progresso do servidor (0-100) para os checkpoints 65, 75, 90
              let mapped = 65
              if (prog >= 100) mapped = 100
              else if (prog >= 90) mapped = 90
              else if (prog >= 50) mapped = 75
              else mapped = 65
              setExportProgress(mapped)
            }
            if (status === 'done' && s?.downloadUrl){
              if (exportJobPollRef.current) clearInterval(exportJobPollRef.current)
              exportJobPollRef.current = null
              setExportUrl(s.downloadUrl)
              setExportStage('ready')
              setExportProgress(100)
              const a = document.createElement('a')
              a.href = s.downloadUrl
              a.download = downloadName
              a.click()
            }else if (status === 'error'){
              if (exportJobPollRef.current) clearInterval(exportJobPollRef.current)
              exportJobPollRef.current = null
              const msg = s?.error || 'Falha ao exportar'
              setError(msg)
              setExportErr(msg)
              setExportStage('error')
            }
          }catch{}
        }, 1200)
      }
      const showErr = async (res: Response) => {
        try{
          const raw = await res.text()
          const j: any = raw ? JSON.parse(raw) : null
          const msg = j?.detail || j?.title || j?.error || j?.message || raw || 'Falha ao exportar'
          setError(msg)
          setExportErr(msg)
          setExportStage('error')
        }catch{
          setError('Falha ao exportar')
          setExportErr('Falha ao exportar')
          setExportStage('error')
        }
      }
      if ((mode === 'prontas' && quickKind === 'access-agg') || (mode === 'personalizadas' && dataset === 'access-agg')){
        url = `/api/reports/access/aggregated/export?format=${format}`
        name = `access-aggregated.${format}`
      }else if ((mode === 'prontas' && quickKind === 'transit-period') || (mode === 'personalizadas' && dataset === 'transit')){
        const { empresa, terminal } = filters as any
        const r0 = rangeIso(filters)
        if(!r0) return
        const p: Record<string,string> = { start: r0.startIso, end: r0.endIso, format }
        if (empresa) p.empresa = empresa
        if (terminal) p.terminal = terminal
        const qs = new URLSearchParams(p).toString()
        url = `/api/reports/transit/export?${qs}`
        name = `transit.${format}`
      }else if (mode === 'prontas' && quickKind === 'door-critical'){
        const r0 = doorAllData ? { startIso: '1900-01-01T00:00:00', endIso: '2100-01-01T00:00:00' } : rangeIso(filters)
        if(!r0) return
        const src = doorAllSources ? undefined : (doorSelectedSources.length ? doorSelectedSources.join(';') : undefined)
        if ((doorMode === 'general' || doorMode === 'general-by-name' || doorMode === 'critical') && !doorAllSources && !src) return
        const daysRange = Math.abs((new Date(r0.endIso).getTime() - new Date(r0.startIso).getTime()) / 86400000)
        if (doorMode === 'critical'){
          const qs = new URLSearchParams({ start: r0.startIso, end: r0.endIso, format, ...(src ? { sourceList: src } : {}) } as any).toString()
          url = `/api/reports/door-critical/export?${qs}`
          name = `portas-criticas.${format}`
        }else if (doorMode === 'general'){
          if (format === 'pdf' && (doorAllData || daysRange > 31)){ setError('Para períodos grandes, use XLSX.'); return }
          if (format === 'csv' && (doorAllData || daysRange > 31)){ await startDoorExportJob({ start: r0.startIso, end: r0.endIso, sourceList: src }, `portas-gerais.${format}`); return }
          const qs = new URLSearchParams({ start: r0.startIso, end: r0.endIso, format, ...(src ? { sourceList: src } : {}) } as any).toString()
          url = `/api/reports/door-general/export?${qs}`
          name = `portas-gerais.${format}`
        }else if (doorMode === 'general-by-name'){
          if (!doorName) return
          if (format === 'pdf' && (doorAllData || daysRange > 31)){ setError('Para períodos grandes, use XLSX.'); return }
          if (format === 'csv' && (doorAllData || daysRange > 31)){ await startDoorExportJob({ start: r0.startIso, end: r0.endIso, sourceList: src, name: doorName }, `portas-gerais-por-nome.${format}`); return }
          const qs = new URLSearchParams({ start: r0.startIso, end: r0.endIso, name: doorName, format, ...(src ? { sourceList: src } : {}) } as any).toString()
          url = `/api/reports/door-general/by-name/export?${qs}`
          name = `portas-gerais-por-nome.${format}`
        }else if (doorMode === 'general-by-site'){
          if (!doorSite) return
          const qs = new URLSearchParams({ start: r0.startIso, end: r0.endIso, site: doorSite, format } as any).toString()
          url = `/api/reports/door-general/by-site/export?${qs}`
          name = `portas-gerais-por-site.${format}`
        }
      }else if (mode === 'prontas' && quickKind === 'cpf' && cpfObter !== 'info'){
        const { cpf } = filters as any
        if (!cpf) return
        const modeQ = cpfObter === 'todos' ? 'all' : cpfObter
        if (cpfSemPeriodo){
          const qs = new URLSearchParams({ documento: cpf, mode: modeQ, format }).toString()
          url = `/api/access/by-document/all/export?${qs}`
        }else{
          const r0 = rangeIso(filters)
          if(!r0) return
          const qs = new URLSearchParams({ documento: cpf, start: r0.startIso, end: r0.endIso, mode: modeQ, format }).toString()
          url = `/api/access/by-document/export?${qs}`
        }
        name = `acessos-${cpf}.${format}`
      }

      if (!url || !name) return

      const readBlobUrlWithProgress = async (res: Response, initialPct: number) => {
        const ct = res.headers.get('Content-Type') || 'application/octet-stream'
        const lenRaw = res.headers.get('Content-Length')
        const total = lenRaw ? Number(lenRaw) : NaN
        const body = res.body
        if (!body || !('getReader' in body)) {
          const blob = await res.blob()
          return URL.createObjectURL(blob)
        }
        const reader = body.getReader()
        const chunks: BlobPart[] = []
        let received = 0

        // Checkpoints após início do download: 65, 75, 90
        setExportProgress(65)

        while (true){
          const { done, value } = await reader.read()
          if (done) break
          if (value){
            const copy = new Uint8Array(value.byteLength)
            copy.set(value)
            chunks.push(copy)
            received += value.byteLength

            if (Number.isFinite(total) && total > 0){
              const ratio = received / total
              // Mapeia ratio (0 a 1) para checkpoints (65 a 90)
              let pct = 65
              if (ratio >= 0.9) pct = 90
              else if (ratio >= 0.5) pct = 75
              else pct = 65
              setExportProgress(p => Math.max(p, pct))
            } else {
              // Sem Content-Length, progride baseado em bytes lidos (estimativa)
              const estimatedPct = 65 + Math.floor(received / 100000)
              let pct = 65
              if (estimatedPct >= 90) pct = 90
              else if (estimatedPct >= 75) pct = 75
              else pct = 65
              setExportProgress(p => Math.max(p, pct))
            }
          }
        }
        const blob = new Blob(chunks, { type: ct })
        return URL.createObjectURL(blob)
      }
      setExportFmt(format)
      setExportFileName(name)
      setExportErr(null)
      setExportStage('generating')
      setExportModal(true)
      setExportMinimized(false)
      setExportMaximized(false)
      setExportPos({x:0, y:0})
      if (exportUrl && exportUrl !== pdfUrl) URL.revokeObjectURL(exportUrl)
      setExportUrl(null)
      if (format !== 'pdf' && pdfUrl){ URL.revokeObjectURL(pdfUrl); setPdfUrl(null) }

      // Checkpoint inicial: 25%
      setExportProgress(25)

      if (exportTimerRef.current) clearInterval(exportTimerRef.current)
      let waitSeconds = 0
      exportTimerRef.current = setInterval(() => {
        waitSeconds++
        setExportProgress(p => {
          if (waitSeconds === 1) return 35 // Checkpoint: 35% após 1s
          if (waitSeconds === 2) return 50 // Checkpoint: 50% após 2s
          return p
        })
      }, 1000)

      const res = await fetch(url, { headers: h })
      if (exportTimerRef.current) clearInterval(exportTimerRef.current)
      exportTimerRef.current = null
      if(!res.ok){ await showErr(res); return }
      const savedPath = res.headers.get('X-Report-Path')
      const blobUrl = await readBlobUrlWithProgress(res, 50)
      setExportUrl(blobUrl)
      if (format === 'pdf'){
        if (pdfUrl && pdfUrl.startsWith('blob:')) URL.revokeObjectURL(pdfUrl)
        setPdfUrl(blobUrl)
        setPdfExportedRun(lastSuccessfulRun)
        setLastPdfSavedPath(savedPath || null)
        setLastPdfRequestUrl(url.length > 1600 ? null : url)
        setLastPdfFileName(name)
      }
      const a = document.createElement('a')
      a.href = blobUrl
      a.download = name
      a.click()
      setExportProgress(100)
      setExportStage('ready')
      const label = (() => {
        if ((mode === 'prontas' && quickKind === 'access-agg') || (mode === 'personalizadas' && dataset === 'access-agg')) return 'Acessos Agregados'
        if ((mode === 'prontas' && quickKind === 'transit-period') || (mode === 'personalizadas' && dataset === 'transit')) return 'Trânsito por Período'
        if (mode === 'prontas' && quickKind === 'door-critical'){
          if (doorMode === 'critical') return 'Eventos de Porta • Portas Críticas'
          if (doorMode === 'general') return 'Eventos de Porta • Portas Gerais'
          if (doorMode === 'general-by-name') return 'Eventos de Porta • Portas Gerais por Nome'
          if (doorMode === 'general-by-site') return 'Eventos de Porta • Portas Gerais por Site'
          return 'Eventos de Porta'
        }
        if (mode === 'prontas' && quickKind === 'cpf') return 'CPF (Cadastro/Acessos)'
        return 'Relatório'
      })()
      const ts = Date.now()
      const id = `${ts}-${Math.random().toString(16).slice(2)}`
      setExportHistory(prev => {
        const next = [{ id, ts, label, fileName: name, format, requestUrl: url, savedPath: savedPath || undefined }, ...prev].slice(0, 100)
        saveExportHistory(next)
        return next
      })
    }catch{}
  }

  const beginExportDrag = (e: React.MouseEvent) => {
    if (exportMaximized) return
    const t = e.target as HTMLElement
    if (t && t.closest('button')) return
    e.preventDefault()
    if (exportDragHandlersRef.current){
      window.removeEventListener('mousemove', exportDragHandlersRef.current.move)
      window.removeEventListener('mouseup', exportDragHandlersRef.current.up)
      exportDragHandlersRef.current = null
    }
    exportDragRef.current = { startX: e.clientX, startY: e.clientY, origX: exportPos.x, origY: exportPos.y }
    const move = (ev: MouseEvent) => {
      const st = exportDragRef.current
      if (!st) return
      setExportPos({ x: st.origX + (ev.clientX - st.startX), y: st.origY + (ev.clientY - st.startY) })
    }
    const up = () => {
      if (exportDragHandlersRef.current){
        window.removeEventListener('mousemove', exportDragHandlersRef.current.move)
        window.removeEventListener('mouseup', exportDragHandlersRef.current.up)
        exportDragHandlersRef.current = null
      }
      exportDragRef.current = null
    }
    exportDragHandlersRef.current = { move, up }
    window.addEventListener('mousemove', move)
    window.addEventListener('mouseup', up)
  }

  async function previewPdf(){
    try{
      setError(null)
      if (pdfUrl && pdfExportedRun === lastSuccessfulRun){
        const fallbackName = (() => {
          if ((mode === 'prontas' && quickKind === 'access-agg') || (mode === 'personalizadas' && dataset === 'access-agg')) return 'access-aggregated.pdf'
          if ((mode === 'prontas' && quickKind === 'transit-period') || (mode === 'personalizadas' && dataset === 'transit')) return 'transit.pdf'
          if (mode === 'prontas' && quickKind === 'door-critical'){
            if (doorMode === 'critical') return 'portas-criticas.pdf'
            if (doorMode === 'general') return 'portas-gerais.pdf'
            if (doorMode === 'general-by-name') return 'portas-gerais-por-nome.pdf'
            if (doorMode === 'general-by-site') return 'portas-gerais-por-site.pdf'
            return 'portas.pdf'
          }
          if (mode === 'prontas' && quickKind === 'cpf' && cpfObter !== 'info'){
            const cpf = (filters as any)?.cpf
            return cpf ? `acessos-${cpf}.pdf` : 'acessos.pdf'
          }
          return 'relatorio.pdf'
        })()
        setExportFmt('pdf')
        setExportFileName(exportFileName && exportFileName.toLowerCase().endsWith('.pdf') ? exportFileName : fallbackName)
        setExportErr(null)
        setExportStage('ready')
        setExportProgress(100)
        setExportUrl(pdfUrl)
        setExportModal(true)
        setExportMinimized(false)
        return
      }
      let url: string | null = null
      if ((mode === 'prontas' && quickKind === 'access-agg') || (mode === 'personalizadas' && dataset === 'access-agg')){
        url = `/api/reports/access/aggregated/export?format=pdf`
      }else if ((mode === 'prontas' && quickKind === 'transit-period') || (mode === 'personalizadas' && dataset === 'transit')){
        const { empresa, terminal } = filters as any
        const r0 = rangeIso(filters)
        if(!r0){ setError('Informe início e fim'); return }
        const p: Record<string,string> = { start: r0.startIso, end: r0.endIso, format:'pdf' }
        if (empresa) p.empresa = empresa
        if (terminal) p.terminal = terminal
        const qs = new URLSearchParams(p).toString()
        url = `/api/reports/transit/export?${qs}`
      }else if (mode === 'prontas' && quickKind === 'door-critical'){
        const r0 = doorAllData ? { startIso: '1900-01-01T00:00:00', endIso: '2100-01-01T00:00:00' } : rangeIso(filters)
        if(!r0){ setError('Informe início e fim'); return }
        const src = doorAllSources ? undefined : doorSelectedSources.join(';')
        if ((doorMode === 'general' || doorMode === 'general-by-name') && !doorAllSources && !src){ setError('Selecione uma ou mais portas'); return }
        if (doorMode === 'critical'){
          const qs = new URLSearchParams({ start: r0.startIso, end: r0.endIso, format:'pdf' }).toString()
          url = `/api/reports/door-critical/export?${qs}`
        }else if (doorMode === 'general'){
          const qs = new URLSearchParams({ start: r0.startIso, end: r0.endIso, format:'pdf', ...(src ? { sourceList: src } : {}) } as any).toString()
          url = `/api/reports/door-general/export?${qs}`
        }else if (doorMode === 'general-by-name'){
          if (!doorName){ setError('Informe o nome'); return }
          const qs = new URLSearchParams({ start: r0.startIso, end: r0.endIso, name: doorName, format:'pdf', ...(src ? { sourceList: src } : {}) } as any).toString()
          url = `/api/reports/door-general/by-name/export?${qs}`
        }else if (doorMode === 'general-by-site'){
          if (!doorSite){ setError('Informe o site'); return }
          const qs = new URLSearchParams({ start: r0.startIso, end: r0.endIso, site: doorSite, format:'pdf' } as any).toString()
          url = `/api/reports/door-general/by-site/export?${qs}`
        }
      }else if (mode === 'prontas' && quickKind === 'cpf' && cpfObter !== 'info'){
        const { cpf } = filters as any
        if (!cpf){ setError('Informe o CPF'); return }
        const modeQ = cpfObter === 'todos' ? 'all' : cpfObter
        if (cpfSemPeriodo){
          const qs = new URLSearchParams({ documento: cpf, mode: modeQ, format:'pdf' }).toString()
          url = `/api/access/by-document/all/export?${qs}`
        }else{
          const r0 = rangeIso(filters)
          if(!r0){ setError('Informe início e fim'); return }
          const qs = new URLSearchParams({ documento: cpf, start: r0.startIso, end: r0.endIso, mode: modeQ, format:'pdf' }).toString()
          url = `/api/access/by-document/export?${qs}`
        }
      }
      if (!url) { setError('Pré-visualização indisponível para esta consulta'); return }
      const u = await api.fetchReportPdf(url)
      if (pdfUrl) URL.revokeObjectURL(pdfUrl)
      setPdfUrl(u)
      setLastPdfRequestUrl(url)
      setLastPdfFileName(exportFileName && exportFileName.toLowerCase().endsWith('.pdf') ? exportFileName : 'relatorio.pdf')
    }catch(e:any){
      const msg = e?.message || 'Falha ao gerar PDF'
      setError(msg)
      if (/Login failed for user/i.test(msg)) setSqlModal(true)
    }
  }

  async function openLastPdf(){
    try{
      setError(null)
      if (!localStorage.getItem('rf_token')) return
      if (pdfUrl && exportFmt === 'pdf' && exportStage === 'ready'){
        setExportUrl(pdfUrl)
        setExportFmt('pdf')
        setExportFileName(lastPdfFileName || exportFileName || 'relatorio.pdf')
        setExportErr(null)
        setExportStage('ready')
        setExportProgress(100)
        setExportModal(true)
        setExportMinimized(false)
        setExportMaximized(false)
        return
      }
      if (!lastPdfSavedPath && !lastPdfRequestUrl){
        setError('Nenhum PDF em cache para visualizar')
        return
      }
      const u = lastPdfSavedPath
        ? await api.fetchSavedReport(lastPdfSavedPath)
        : await api.fetchReportPdf(lastPdfRequestUrl!)
      if (pdfUrl && pdfUrl.startsWith('blob:')) URL.revokeObjectURL(pdfUrl)
      if (exportUrl && exportUrl !== pdfUrl && exportUrl.startsWith('blob:')) URL.revokeObjectURL(exportUrl)
      setPdfUrl(u)
      setExportUrl(u)
      setExportFmt('pdf')
      setExportFileName(lastPdfFileName || 'relatorio.pdf')
      setExportErr(null)
      setExportStage('ready')
      setExportProgress(100)
      setExportModal(true)
      setExportMinimized(false)
      setExportMaximized(false)
      setExportPos({x:0, y:0})
    }catch(e:any){
      const msg = e?.message || 'Falha ao abrir PDF'
      setError(msg)
      if (/Login failed for user/i.test(msg)) setSqlModal(true)
    }
  }

  async function openHistoryItem(it: { fileName: string, format: 'csv'|'xlsx'|'pdf', requestUrl: string, savedPath?: string }){
    try{
      setError(null)
      const h: Record<string,string> = {}
      const t = localStorage.getItem('rf_token')
      if (t) h['Authorization'] = `Bearer ${t}`
      const cid = localStorage.getItem('rf_client_id')
      if (cid) h['X-Client-Id'] = cid

      setReportsModal(false)
      setExportFmt(it.format)
      setExportFileName(it.fileName)
      setExportErr(null)
      setExportStage('generating')
      setExportModal(true)
      setExportMinimized(false)
      setExportMaximized(false)
      setExportPos({x:0, y:0})
      if (exportUrl && exportUrl !== pdfUrl) URL.revokeObjectURL(exportUrl)
      setExportUrl(null)
      setExportProgress(25) // Checkpoint inicial

      if (exportTimerRef.current) clearInterval(exportTimerRef.current)
      let waitSeconds = 0
      exportTimerRef.current = setInterval(() => {
        waitSeconds++
        setExportProgress(p => {
          if (waitSeconds === 1) return 35 // Checkpoint 35%
          if (waitSeconds === 2) return 50 // Checkpoint 50%
          return p
        })
      }, 1000)

      const blobUrl = it.savedPath
        ? await api.fetchSavedReport(it.savedPath)
        : await (async () => {
            const res = await fetch(it.requestUrl, { headers: h })
            if(!res.ok){
              let msg = `HTTP ${res.status}`
              try{
                const j: any = await res.json()
                msg = j?.detail || j?.title || j?.error || j?.message || msg
              }catch{}
              throw new Error(msg)
            }
            // Inicia download real -> 65%
            if (exportTimerRef.current) clearInterval(exportTimerRef.current)
            exportTimerRef.current = null
            setExportProgress(65)

            const ct = res.headers.get('Content-Type') || 'application/octet-stream'
            const lenRaw = res.headers.get('Content-Length')
            const total = lenRaw ? Number(lenRaw) : NaN
            const body = res.body
            if (!body || !('getReader' in body)) {
              const blob = await res.blob()
              return URL.createObjectURL(blob)
            }
            const reader = body.getReader()
            const chunks: BlobPart[] = []
            let received = 0
            while (true){
              const { done, value } = await reader.read()
              if (done) break
              if (value){
                const copy = new Uint8Array(value.byteLength)
                copy.set(value)
                chunks.push(copy)
                received += value.byteLength

                if (Number.isFinite(total) && total > 0){
                  const ratio = received / total
                  let pct = 65
                  if (ratio >= 0.9) pct = 90
                  else if (ratio >= 0.5) pct = 75
                  else pct = 65
                  setExportProgress(p => Math.max(p, pct))
                } else {
                  const estimatedPct = 65 + Math.floor(received / 100000)
                  let pct = 65
                  if (estimatedPct >= 90) pct = 90
                  else if (estimatedPct >= 75) pct = 75
                  else pct = 65
                  setExportProgress(p => Math.max(p, pct))
                }
              }
            }
            const blob = new Blob(chunks, { type: ct })
            return URL.createObjectURL(blob)
          })()
      setExportUrl(blobUrl)
      if (it.format === 'pdf'){
        if (pdfUrl && pdfUrl.startsWith('blob:')) URL.revokeObjectURL(pdfUrl)
        setPdfUrl(blobUrl)
      }else{
        const a = document.createElement('a')
        a.href = blobUrl
        a.download = it.fileName
        a.click()
      }
      if (exportTimerRef.current) clearInterval(exportTimerRef.current)
      exportTimerRef.current = null
      setExportProgress(100)
      setExportStage('ready')
    }catch(e:any){
      const msg = e?.message || 'Falha ao abrir relatório'
      setError(msg)
      setExportErr(msg)
      setExportStage('error')
      if (exportTimerRef.current) clearInterval(exportTimerRef.current)
      exportTimerRef.current = null
    }
  }

  function deleteHistoryItem(id: string){
    setExportHistory(prev => {
      const next = prev.filter(x => x.id !== id)
      saveExportHistory(next)
      return next
    })
  }

  async function applySqlAuth(){
    if (!sqlUser || !sqlPwd) return
    setApplyingSql(true)
    try{
      await api.setSqlAuth({ user: sqlUser, pwd: sqlPwd })
      const conns = await api.getConnections() as any
      const ensureTls = (s: string) => {
        const up = s.trim().replace(/;+\s*$/,'')
        const hasEnc = /Encrypt\s*=\s*True/i.test(up)
        const hasTrust = /TrustServerCertificate\s*=\s*True/i.test(up)
        let out = up
        if (!hasEnc) out += ';Encrypt=True'
        if (!hasTrust) out += ';TrustServerCertificate=True'
        return out
      }
      const applySql = (s: string) => {
        let out = s.replace(/Integrated\s*Security\s*=\s*True/ig, 'Integrated Security=False')
        out = out.replace(/Trusted_Connection\s*=\s*Yes/ig, 'Integrated Security=False')
        out = out.replace(/;\s*User\s*ID\s*=\s*[^;]*/ig, '')
        out = out.replace(/;\s*Password\s*=\s*[^;]*/ig, '')
        out = out.replace(/;\s*UID\s*=\s*[^;]*/ig, '')
        out = out.replace(/;\s*PWD\s*=\s*[^;]*/ig, '')
        out += `;User ID=${sqlUser};Password=${sqlPwd}`
        return ensureTls(out)
      }
      const cms = conns?.CMS ? applySql(conns.CMS) : ''
      const logins = conns?.Logins ? applySql(conns.Logins) : ''
      const ems = conns?.EMS ? applySql(conns.EMS) : ''
      await api.setConnectionsRuntime({ CMS: cms, Logins: logins, EMS: ems })
      try { localStorage.setItem('rf_sql_user', sqlUser); localStorage.setItem('rf_sql_pwd', sqlPwd) } catch {}
      setSqlModal(false)
      setError(null)
      if (mode === 'prontas') await runQuick(); else await runPersonalizada()
    }catch{
    }finally{
      setApplyingSql(false)
    }
  }

  React.useEffect(() => {
    try{
      const u = localStorage.getItem('rf_sql_user') || ''
      const p = localStorage.getItem('rf_sql_pwd') || ''
      if (u && p){
        (async () => {
          try { await api.setSqlAuth({ user: u, pwd: p }) } catch {}
          await api.setSqlAuthRuntime({ user: u, pwd: p })
        })()
      }
    }catch{}
  }, [])

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

  const datasetColumns = useMemo(() => {
    if (dataset === 'db-table'){
      if (Array.isArray(data) && data[0]){
        return Object.keys(data[0]).map(k => ({ key: k, label: k }))
      }
      return []
    }
    return DATASET_COLUMNS[dataset]
  }, [dataset, data])

  const quickColumns = useMemo(()=>{
    const d = mapQuickToDataset(quickKind)
    return DATASET_COLUMNS[d]
  }, [quickKind, cpfObter, matriculaObter, empresaObter, crachaObter, nivelObter])

  const visibleColumns = useMemo(()=>{
    if (mode === 'personalizadas'){
      const defs = datasetColumns
      return defs.filter(d => selectedCols.includes(d.key))
    }
    return quickColumns
  }, [mode, selectedCols, data, datasetColumns, quickColumns])

  const tableColumns = useMemo(() => {
    if (mode === 'personalizadas') return visibleColumns
    if (quickKind === 'door-critical') return quickColumns
    if (Array.isArray(data) && data[0]) return Object.keys(data[0]).map(k => ({ key: k, label: k }))
    return []
  }, [mode, visibleColumns, quickKind, quickColumns, data])


  const searchColumnsList = useMemo(()=>{
    if (mode === 'prontas') return quickColumns
    return datasetColumns
  }, [mode, quickColumns, datasetColumns])

  const filteredData = useMemo(()=>{
    const term = (searchTerm || '').toLowerCase().trim()
    if (!term) return data
    const cols = searchColumn === '*' ? (mode === 'personalizadas' ? datasetColumns.map(c=>c.key) : quickColumns.map(c=>c.key)) : [searchColumn]
    return data.filter(row => {
      for (const c of cols){
        const v = getRowValue(row, c)
        if (v !== undefined && v !== null){
          const s = String(v).toLowerCase()
          if (s.includes(term)) return true
        }
      }
      return false
    })
  }, [data, searchTerm, searchColumn, mode, dataset, quickColumns, datasetColumns])

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

  function confirmCloseExport() {
    if (exportTimerRef.current) clearInterval(exportTimerRef.current)
    exportTimerRef.current = null
    if (exportJobPollRef.current) clearInterval(exportJobPollRef.current)
    exportJobPollRef.current = null
    setExportJobId(null)
    if (exportUrl && exportUrl !== pdfUrl && exportUrl.startsWith('blob:')) URL.revokeObjectURL(exportUrl)
    setExportUrl(null)
    setExportModal(false)
    setExportMinimized(false)
    setExportMaximized(false)
    setExportProgress(0)
    setExportPos({x:0, y:0})
    setShowCloseConfirm(false)
  }

  return (
    <section className="queries">
      <h2>Consultas</h2>
      {exportModal && (
        !exportMinimized ? <div className="modal-backdrop show" style={{display:'block'}}></div> : null
      )}
      {exportModal && (
        !exportMinimized ? <div className="modal show" style={{display:'block'}}>
          <div
            className={exportMaximized ? 'modal-dialog' : 'modal-dialog modal-lg'}
            style={exportMaximized ? {position:'fixed', inset:0, margin:0, maxWidth:'100vw', width:'100vw', height:'100vh'} : {transform:`translate(${exportPos.x}px, ${exportPos.y}px)`}}
          >
            <div className="modal-content" style={exportMaximized ? {height:'100vh', borderRadius:0} : undefined}>
              <div className="modal-header" onMouseDown={beginExportDrag} style={exportMaximized ? {cursor:'default'} : {cursor:'move'}}>
                <h5 className="modal-title">Exportação</h5>
                <div className="d-flex align-items-center" style={{gap:8}}>
                  <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => setExportMinimized(true)} title="Minimizar">
                    <i className="bi bi-dash-lg" />
                  </button>
                  <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => setExportMaximized(v => !v)} title={exportMaximized ? 'Restaurar' : 'Maximizar'}>
                    <i className={exportMaximized ? "bi bi-fullscreen-exit" : "bi bi-fullscreen"} />
                  </button>
                  <button type="button" className="btn-close" onClick={() => setShowCloseConfirm(true)}></button>
                </div>
              </div>
              <div className="modal-body">
                {exportStage === 'generating' && (
                  <>
                    <div className="d-flex align-items-center mb-2">
                      <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                      Gerando arquivo {exportFmt.toUpperCase()}...
                    </div>
                    <div className="progress" style={{height:10}}>
                      <div className="progress-bar progress-bar-striped progress-bar-animated" role="progressbar" style={{width: `${exportProgress}%`}} aria-valuenow={exportProgress} aria-valuemin={0} aria-valuemax={100}></div>
                    </div>
                    <div className="text-muted mt-1" style={{fontSize:12}}>
                      Progresso estimado: {exportProgress}%
                    </div>
                  </>
                )}
                {exportStage === 'error' && (
                  <div className="alert alert-danger mb-0">
                    {exportErr || 'Falha ao exportar'}
                  </div>
                )}
                {exportStage === 'ready' && (
                  <>
                    <div className="d-flex align-items-center justify-content-between">
                      <div className="text-muted" style={{fontSize:12}}>
                        Arquivo pronto: {exportFileName}
                      </div>
                      {exportUrl && (
                        <a className="btn btn-sm btn-primary" href={exportUrl} download={exportFileName}>
                          Baixar novamente
                        </a>
                      )}
                    </div>
                    {exportFmt === 'pdf' && pdfUrl && (
                      <div className="mt-2" style={{border:'1px solid #ddd'}}>
                        <iframe title="PDF Export" src={pdfUrl} style={{width:'100%', height: exportMaximized ? 'calc(100vh - 220px)' : '60vh', border:0}} />
                      </div>
                    )}
                  </>
                )}
              </div>
              <div className="modal-footer">
                <button className="btn btn-outline-secondary" onClick={() => setShowCloseConfirm(true)} disabled={exportStage === 'generating'}>
                  Fechar
                </button>
              </div>
            </div>
          </div>
        </div> : null
      )}
      {exportModal && exportMinimized && (
        <div className="export-float">
          <div className="d-flex align-items-center flex-grow-1" style={{gap:12}}>
            <strong style={{fontSize:13, whiteSpace:'nowrap'}}>
              <i className={exportFmt === 'pdf' ? "bi bi-file-earmark-pdf me-2" : "bi bi-file-earmark-excel me-2"} />
              Exportação: {exportFmt.toUpperCase()}
            </strong>
            <div className="flex-grow-1" style={{maxWidth:400}}>
              {exportStage === 'generating' ? (
                <div className="d-flex align-items-center gap-3">
                  <div className="progress flex-grow-1" style={{height:8}}>
                    <div className="progress-bar progress-bar-striped progress-bar-animated" role="progressbar" style={{width: `${exportProgress}%`}} aria-valuenow={exportProgress} aria-valuemin={0} aria-valuemax={100}></div>
                  </div>
                  <span style={{fontSize:12, color:'#374151', minWidth:36}}>{exportProgress}%</span>
                </div>
              ) : (
                <div className="d-flex align-items-center gap-2">
                  <span style={{fontSize:12, color: exportStage === 'error' ? '#dc2626' : '#059669', fontWeight:500}}>
                    {exportStage === 'ready' ? '✓ Arquivo pronto' : `⚠ ${exportErr || 'Falha ao exportar'}`}
                  </span>
                  {exportStage === 'ready' && exportUrl && (
                    <a className="btn btn-sm btn-link p-0 text-primary" style={{fontSize:12, textDecoration:'none'}} href={exportUrl} download={exportFileName}>
                      Baixar agora
                    </a>
                  )}
                </div>
              )}
            </div>
          </div>

          <div className="d-flex align-items-center ms-3" style={{gap:8}}>
            <button className="btn btn-sm btn-outline-secondary d-flex align-items-center gap-1" onClick={() => setExportMinimized(false)} title="Abrir visualização">
              <i className="bi bi-box-arrow-up-right" />
              <span className="d-none d-sm-inline">Maximizar</span>
            </button>
            <button className="btn btn-sm btn-outline-danger d-flex align-items-center gap-1" onClick={() => setShowCloseConfirm(true)} title="Fechar">
              <i className="bi bi-x-lg" />
              <span className="d-none d-sm-inline">Fechar</span>
            </button>
          </div>
        </div>
      )}
      {showCloseConfirm && (
        <>
          <div className="modal-backdrop show" style={{ zIndex: 1070 }}></div>
          <div className="modal show" style={{ display: 'block', zIndex: 1080 }}>
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content shadow-lg border-0" style={{ borderRadius: '15px' }}>
                <div className="modal-header border-0 pb-0">
                  <h5 className="modal-title w-100 text-center mt-3">
                    <i className="bi bi-exclamation-triangle text-warning mb-2 d-block" style={{ fontSize: '2.5rem' }}></i>
                    Confirmar Fechamento
                  </h5>
                </div>
                <div className="modal-body text-center py-4">
                  <p className="mb-0 px-3" style={{ fontSize: '1.1rem', color: '#4b5563' }}>
                    Deseja mesmo fechar a prévia do <strong>{exportFmt.toUpperCase()}</strong>?
                  </p>
                  <small className="text-muted d-block mt-2">
                    {exportStage === 'generating' 
                      ? 'A geração em andamento será interrompida.' 
                      : 'Você poderá abrir este relatório novamente através do histórico.'}
                  </small>
                </div>
                <div className="modal-footer border-0 justify-content-center pb-4 pt-0" style={{ gap: '15px' }}>
                  <button 
                    type="button" 
                    className="btn btn-light px-4" 
                    style={{ borderRadius: '8px', fontWeight: 500, minWidth: '120px' }}
                    onClick={() => setShowCloseConfirm(false)}
                  >
                    Não, manter
                  </button>
                  <button 
                    type="button" 
                    className="btn btn-danger px-4" 
                    style={{ borderRadius: '8px', fontWeight: 500, minWidth: '120px' }}
                    onClick={confirmCloseExport}
                  >
                    Sim, fechar
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
      {reportsModal && (
        <div className="modal-backdrop show" style={{display:'block'}}></div>
      )}
      {reportsModal && (
        <div className="modal show" style={{display:'block'}}>
          <div className="modal-dialog modal-lg">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">Relatórios</h5>
                <button type="button" className="btn-close" onClick={()=> setReportsModal(false)}></button>
              </div>
              <div className="modal-body">
                {exportsToday.length === 0 ? (
                  <div className="text-muted">Nenhum relatório exportado hoje.</div>
                ) : (
                  <div className="table-responsive">
                    <table className="table table-sm align-middle">
                      <thead>
                        <tr>
                          <th style={{width:90}}>Hora</th>
                          <th>Consulta</th>
                          <th>Arquivo</th>
                          <th style={{width:80}}>Tipo</th>
                          <th style={{width:140}}>Ações</th>
                        </tr>
                      </thead>
                      <tbody>
                        {exportsToday.map(it => (
                          <tr key={it.id}>
                            <td>{formatTime(it.ts)}</td>
                            <td>{it.label}</td>
                            <td style={{maxWidth:260, overflow:'hidden', textOverflow:'ellipsis', whiteSpace:'nowrap'}} title={it.fileName}>{it.fileName}</td>
                            <td>{it.format.toUpperCase()}</td>
                            <td>
                              <button className="btn btn-sm btn-primary" onClick={()=> openHistoryItem(it)}>
                                {it.format === 'pdf' ? 'Abrir' : 'Baixar'}
                              </button>
                              <button className="btn btn-sm btn-outline-danger ms-2" onClick={(e)=> { e.stopPropagation(); deleteHistoryItem(it.id) }} title="Excluir da lista">
                                <i className="bi bi-trash" />
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
              <div className="modal-footer">
                <button className="btn btn-outline-secondary" onClick={()=> setReportsModal(false)}>Fechar</button>
              </div>
            </div>
          </div>
        </div>
      )}
      {sqlModal && (
        <div className="modal-backdrop show" style={{display:'block'}}></div>
      )}
      {sqlModal && (
        <div className="modal show" style={{display:'block'}}>
          <div className="modal-dialog">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">Autenticação SQL</h5>
                <button type="button" className="btn-close" onClick={()=> setSqlModal(false)}></button>
              </div>
              <div className="modal-body">
                <div className="input-group mb-2">
                  <span className="input-group-text"><i className="bi bi-person" /></span>
                  <input className="form-control" placeholder="Usuário SQL" value={sqlUser} onChange={e=> setSqlUser(e.target.value)} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-key" /></span>
                  <input className="form-control" type="password" placeholder="Senha SQL" value={sqlPwd} onChange={e=> setSqlPwd(e.target.value)} />
                </div>
              </div>
              <div className="modal-footer">
                <button className="btn btn-secondary" onClick={()=> setSqlModal(false)}>Cancelar</button>
                <button className="btn btn-primary" onClick={applySqlAuth} disabled={applyingSql}>
                  {applyingSql ? 'Aplicando...' : 'Aplicar e reconectar'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
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
            {reportOptions.customQueries && (
              <div className="form-check form-switch">
                <input className="form-check-input" type="checkbox" role="switch"
                  id="switchPersonalizadas"
                  checked={mode==='personalizadas'}
                  onChange={()=>{ setMode('personalizadas'); resetData() }}
                />
                <label className="form-check-label" htmlFor="switchPersonalizadas">Consultas Personalizadas</label>
              </div>
            )}
          </div>
        </div>
        <div className="card-body">

      {mode === 'prontas' && (
        <>
          <div className="queries-ready-switches" style={{marginBottom:12}}>
            {([
              { key:'access-agg', label:'Acessos Agregados' },
              { key:'transit-period', label:'Trânsito por Período' },
              { key:'door-critical', label:'Eventos de Porta' },
              { key:'employees', label:'Funcionários' },
              { key:'external', label:'Externos' },
              { key:'card-by-cpf', label:'Buscar Crachá por CPF' },
              { key:'cpf', label:'CPF (Cadastro/Acessos)' },
              { key:'matricula', label:'Matrícula' },
              { key:'empresa', label:'Empresa' },
              { key:'cracha', label:'Crachá' },
              { key:'nivel', label:'Nível de Acesso' },
              { key:'visitantes', label:'Visitantes' }
            ] as {key:QuickKind,label:string}[]).filter(opt => !!queriesCfg[opt.key]).map(opt => {
              const id = `qsw_${opt.key}`
              return (
                <div key={opt.key} className="queries-ready-switch">
                  <div className="form-check form-switch d-flex align-items-center justify-content-between border rounded px-3 py-2">
                    <label className="form-check-label flex-grow-1 me-2" style={{minWidth:0}} htmlFor={id}>{opt.label}</label>
                    <input
                      id={id}
                      className="form-check-input"
                      type="checkbox"
                      role="switch"
                      disabled={loading}
                      checked={quickKind === opt.key}
                      onChange={() => {
                        setQuickKind(opt.key)
                        setData([])
                        setError(null)
                        setSearchTerm('')
                        setSearchColumn('*')
                        setCurrentPage(1)
                        if (opt.key === 'door-critical') {
                          const today = todayBr()
                          setFilters(prev => ({ ...prev, start: prev.start || today, end: prev.end || today }))
                        }
                      }}
                    />
                  </div>
                </div>
              )
            })}
          </div>

          {!readyQueryEnabled && (
            <div className="alert alert-warning py-2" style={{marginBottom:8}}>
              Nenhuma consulta pronta habilitada para exibição. Ative em Consultas Config.
            </div>
          )}

          {readyQueryEnabled && (
          <div className="queries-row" style={{marginBottom:8}}>
            {(quickKind === 'transit-period') && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <BrDateInput value={filters.start} placeholder="Início (dd/mm/aaaa)" onChange={v=> setFilters({...filters, start: v})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-clock" /></span>
                  <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <BrDateInput value={filters.end} placeholder="Fim (dd/mm/aaaa)" onChange={v=> setFilters({...filters, end: v})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-clock" /></span>
                  <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
                </div>
                {quickKind === 'transit-period' && (
                  <>
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
              </>
            )}
            {(quickKind === 'door-critical') && (
              <>
                <div className="input-group" style={{maxWidth:280}}>
                  <span className="input-group-text"><i className="bi bi-list-task" /></span>
                  <select className="form-select" value={doorMode} onChange={e=> setDoorMode(e.target.value as any)}>
                    <option value="critical">Portas Críticas</option>
                    <option value="general">Portas Gerais</option>
                    <option value="general-by-name">Portas Gerais por Nome</option>
                    <option value="general-by-site">Portas Gerais por Site</option>
                  </select>
                </div>
                {doorMode === 'general-by-site' && (
                  <div className="text-muted" style={{fontSize:12, marginLeft:8}}>
                    Nesta opção o filtro é pelo texto do Acesso (DC), não por lista de portas (TAG).
                  </div>
                )}
                <div className="form-check form-switch d-flex align-items-center gap-2 px-2 py-1" style={{minWidth:200, paddingLeft:0, flexShrink:0, marginLeft:8}}>
                  <input className="form-check-input" type="checkbox" style={{marginLeft:0}} checked={doorAllData} onChange={e=> {
                    const v = e.target.checked
                    setDoorAllData(v)
                    if (v && (doorMode === 'general' || doorMode === 'general-by-name')) setDoorAllSources(false)
                  }} />
                  <label className="form-check-label">Todos os dados</label>
                </div>
                <div className="form-check form-switch d-flex align-items-center gap-2 px-2 py-1" style={{minWidth:220, paddingLeft:0, flexShrink:0, marginLeft:8}}>
                  <input className="form-check-input" type="checkbox" style={{marginLeft:0}} checked={doorAllSources} onChange={e=> { setDoorAllSources(e.target.checked); if (e.target.checked){ setDoorSelectedSources([]); setDoorPickSource('') } }} />
                  <label className="form-check-label">Todas as portas</label>
                  <span className={doorSourcesLoading ? "badge text-bg-warning" : "badge text-bg-secondary"} style={{fontSize:11}}>
                    {doorSourcesLoading ? "Carregando..." : doorSourceFilter.trim() ? `Portas: ${filteredDoorKeys.length}/${activeDoorSources.length}` : `Portas: ${activeDoorSources.length}`}
                  </span>
                </div>
                {doorAllData && doorAllSources && (doorMode === 'general' || doorMode === 'general-by-name') && (
                  <div className="text-muted" style={{fontSize:12, marginLeft:8}}>
                    Consulta em grande volume habilitada. Para obter todos os registros com segurança, use Exportação (CSV Job).
                  </div>
                )}
                {doorSourcesErr && (
                  <div className="text-danger" style={{fontSize:12, marginLeft:8}}>
                    {doorSourcesErr}
                  </div>
                )}
                {!doorAllData && (
                  <>
                    <div style={{display:'flex', flexDirection:'column', gap:4}}>
                      <div className="input-group">
                        <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                        <BrDateInput value={filters.start} placeholder="Início (dd/mm/aaaa)" inputStyle={{maxWidth:170}} onChange={v=> setFilters({...filters, start: v})} />
                      </div>
                      <div className="input-group">
                        <span className="input-group-text"><i className="bi bi-clock" /></span>
                        <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                      </div>
                    </div>
                    <div style={{display:'flex', flexDirection:'column', gap:4}}>
                      <div className="input-group">
                        <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                        <BrDateInput value={filters.end} placeholder="Fim (dd/mm/aaaa)" inputStyle={{maxWidth:170}} onChange={v=> setFilters({...filters, end: v})} />
                      </div>
                      <div className="input-group">
                        <span className="input-group-text"><i className="bi bi-clock" /></span>
                        <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
                      </div>
                    </div>
                  </>
                )}
                {(doorMode !== 'general-by-site') && !doorAllSources && (
                  <div className="d-flex align-items-start" style={{gap:12, minWidth:520}}>
                    <div style={{flex:1, minWidth:320}}>
                      {doorSelectedSources.length === 0 && (
                        <div className="text-muted" style={{fontSize:12, paddingBottom:6}}>Nenhuma porta selecionada</div>
                      )}
                      <div className="input-group mb-2">
                        <span className="input-group-text"><i className="bi bi-search" /></span>
                        <input className="form-control" placeholder="Buscar porta..." value={doorSourceFilter} onChange={e=> setDoorSourceFilter(e.target.value)} />
                        {doorSelectedSources.length === 0 && (
                          <button
                            className="btn btn-outline-secondary"
                            type="button"
                            disabled={!doorSourceFilter.trim() || filteredDoorKeys.length === 0}
                            onClick={() => {
                              if (!doorSourceFilter.trim()) return
                              setDoorAllSources(false)
                              setDoorSelectedSources(filteredDoorKeys)
                              setDoorPickSource('')
                            }}
                            title="Seleciona todas as portas visíveis no filtro"
                          >
                            Selecionar filtradas
                          </button>
                        )}
                        {doorSourceFilter && (
                          <button className="btn btn-outline-secondary" type="button" onClick={()=> setDoorSourceFilter('')}>Limpar</button>
                        )}
                      </div>
                      <div className="input-group">
                        <span className="input-group-text"><i className="bi bi-door-open" /></span>
                        <select
                          className="form-select"
                          value={doorPickSource}
                          disabled={doorSourcesLoading || activeDoorSources.length === 0}
                          onChange={e => {
                            const v = e.target.value
                            setDoorPickSource(v)
                            if (!v) return
                            setDoorAllSources(false)
                            setDoorSelectedSources(prev => prev.includes(v) ? prev : [...prev, v])
                          }}
                        >
                          <option value="">
                            {doorSourcesLoading ? 'Carregando portas...' : activeDoorSources.length === 0 ? 'Nenhuma porta encontrada' : 'Selecionar porta...'}
                          </option>
                          {!doorSourcesLoading && activeDoorSources.length > 0 && filteredDoorSourcesGrouped.map(g => (
                              <optgroup key={g.label} label={g.label}>
                                {g.items.map(it => (
                                  <option key={it.key} value={it.key}>{it.label}</option>
                                ))}
                              </optgroup>
                            ))}
                        </select>
                        {doorSelectedSources.length > 0 && (
                          <button className="btn btn-outline-secondary" type="button" onClick={()=> { setDoorSelectedSources([]); setDoorPickSource('') }}>
                            Limpar
                          </button>
                        )}
                      </div>
                    </div>
                    <div style={{minWidth:200, maxWidth:360}}>
                      {doorSelectedSources.length > 0 ? (
                        <div className="d-flex flex-wrap gap-1">
                          {doorSelectedSources.map(k => (
                            <span key={k} className="badge rounded-pill text-bg-secondary" title={doorSourceLabelByKey.get(k) || k} style={{cursor:'pointer'}} onClick={() => setDoorSelectedSources(prev => prev.filter(x => x !== k))}>
                              {doorShortKey(k)}
                              <span style={{marginLeft:6}}>×</span>
                            </span>
                          ))}
                        </div>
                      ) : null}
                    </div>
                  </div>
                )}
                {doorMode === 'general-by-name' && (
                  <div className="input-group">
                    <span className="input-group-text"><i className="bi bi-tag" /></span>
                    <input className="form-control" placeholder="Nome da Porta/Site" value={doorName} onChange={e=> setDoorName(e.target.value)} />
                  </div>
                )}
                {doorMode === 'general-by-site' && (
                  <div className="input-group">
                    <span className="input-group-text"><i className="bi bi-geo-alt" /></span>
                    <input className="form-control" placeholder="Site" value={doorSite} onChange={e=> setDoorSite(e.target.value)} />
                  </div>
                )}
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
            {quickKind === 'card-by-cpf' && (
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-person-vcard" /></span>
                <input className="form-control" placeholder="CPF" value={filters.cpf || ''} onChange={e=> setFilters({...filters, cpf: e.target.value})} />
              </div>
            )}
            {quickKind === 'cpf' && (
              <>
                <div style={{display:'grid', gridTemplateColumns:'280px 220px 200px 1fr 1fr', gap:8, alignItems:'center'}}>
                  <div style={{minWidth:0}}>
                    <div className="input-group" style={{width:'100%'}}>
                      <span className="input-group-text"><i className="bi bi-list-task" /></span>
                      <select className="form-select" value={cpfObter} onChange={e=> setCpfObter(e.target.value as any)}>
                        <option value="info">Informação de Cadastro</option>
                        <option value="todos">Todos os Acessos</option>
                        <option value="catracas">Somente Catracas</option>
                        <option value="faciais">Somente Faciais</option>
                      </select>
                    </div>
                  </div>
                  <div style={{minWidth:0}}>
                    <div className="input-group" style={{width:'100%'}}>
                      <span className="input-group-text"><i className="bi bi-person-vcard" /></span>
                      <input className="form-control" placeholder="CPF" value={filters.cpf || ''} onChange={e=> setFilters({...filters, cpf: e.target.value})} />
                    </div>
                  </div>
                  <div>
                    {cpfObter !== 'info' && (
                      <div className="form-check form-switch d-flex align-items-center gap-2 px-2 py-1" style={{paddingLeft:0, margin:0}}>
                        <input id="cpfSemPeriodoSwitch" className="form-check-input" type="checkbox" style={{marginLeft:0}} checked={cpfSemPeriodo} onChange={e=> setCpfSemPeriodo(e.target.checked)} />
                        <label className="form-check-label" htmlFor="cpfSemPeriodoSwitch">Sem período</label>
                      </div>
                    )}
                  </div>
                  <div>
                    {cpfObter !== 'info' && !cpfSemPeriodo && (
                      <div>
                        <div className="fw-semibold" style={{fontSize:12, marginLeft:2, marginBottom:2}}>Início</div>
                        <div className="input-group">
                          <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                          <BrDateInput value={filters.start} placeholder="dd/mm/aaaa" onChange={v=> setFilters({...filters, start: v})} />
                        </div>
                        <div className="input-group" style={{marginTop:6}}>
                          <span className="input-group-text"><i className="bi bi-clock" /></span>
                          <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                        </div>
                      </div>
                    )}
                  </div>
                  <div>
                    {cpfObter !== 'info' && !cpfSemPeriodo && (
                      <div>
                        <div className="fw-semibold" style={{fontSize:12, marginLeft:2, marginBottom:2}}>Fim</div>
                        <div className="input-group">
                          <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                          <BrDateInput value={filters.end} placeholder="dd/mm/aaaa" onChange={v=> setFilters({...filters, end: v})} />
                        </div>
                        <div className="input-group" style={{marginTop:6}}>
                          <span className="input-group-text"><i className="bi bi-clock" /></span>
                          <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </>
            )}
            {quickKind === 'matricula' && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-list-task" /></span>
                  <select className="form-select" value={matriculaObter} onChange={e=> setMatriculaObter(e.target.value as any)}>
                    <option value="info">Informação de Cadastro</option>
                    <option value="todos">Todos os Acessos</option>
                    <option value="catracas">Somente Catracas</option>
                  </select>
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-person-badge" /></span>
                  <input className="form-control" placeholder="Matrícula" value={filters.matricula || ''} onChange={e=> setFilters({...filters, matricula: e.target.value})} />
                </div>
                {(matriculaObter !== 'info') && (
                  <>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                      <BrDateInput value={filters.start} placeholder="Início (dd/mm/aaaa)" onChange={v=> setFilters({...filters, start: v})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-clock" /></span>
                      <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                      <BrDateInput value={filters.end} placeholder="Fim (dd/mm/aaaa)" onChange={v=> setFilters({...filters, end: v})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-clock" /></span>
                      <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
                    </div>
                  </>
                )}
              </>
            )}
            {quickKind === 'empresa' && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-list-task" /></span>
                  <select className="form-select" value={empresaObter} onChange={e=> setEmpresaObter(e.target.value as any)}>
                    <option value="info">Informação de Cadastro</option>
                    <option value="todos">Todos os Acessos</option>
                  </select>
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-building" /></span>
                  <input className="form-control" placeholder="Empresa" value={filters.empresa || ''} onChange={e=> setFilters({...filters, empresa: e.target.value})} />
                </div>
                {empresaObter === 'todos' && (
                  <>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                      <BrDateInput value={filters.start} placeholder="Início (dd/mm/aaaa)" onChange={v=> setFilters({...filters, start: v})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-clock" /></span>
                      <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                      <BrDateInput value={filters.end} placeholder="Fim (dd/mm/aaaa)" onChange={v=> setFilters({...filters, end: v})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-clock" /></span>
                      <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
                    </div>
                  </>
                )}
              </>
            )}
            {quickKind === 'cracha' && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-list-task" /></span>
                  <select className="form-select" value={crachaObter} onChange={e=> setCrachaObter(e.target.value as any)}>
                    <option value="info">Informação de Cadastro</option>
                    <option value="todos">Todos os Acessos</option>
                    <option value="catracas">Somente Catracas</option>
                  </select>
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-credit-card-2-front" /></span>
                  <input className="form-control" placeholder="Crachá" value={filters.cracha || ''} onChange={e=> setFilters({...filters, cracha: e.target.value})} />
                </div>
                {crachaObter !== 'info' && (
                  <>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                      <BrDateInput value={filters.start} placeholder="Início (dd/mm/aaaa)" onChange={v=> setFilters({...filters, start: v})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-clock" /></span>
                      <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                      <BrDateInput value={filters.end} placeholder="Fim (dd/mm/aaaa)" onChange={v=> setFilters({...filters, end: v})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-clock" /></span>
                      <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
                    </div>
                  </>
                )}
              </>
            )}
            {quickKind === 'nivel' && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-list-task" /></span>
                  <select className="form-select" value={nivelObter} onChange={e=> setNivelObter(e.target.value as any)}>
                    <option value="todos">Todos os Níveis (Agregado)</option>
                    <option value="acessos">Acessos por Nível</option>
                  </select>
                </div>
                {nivelObter === 'acessos' && (
                  <>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-hash" /></span>
                      <input className="form-control" type="number" placeholder="LevelId (opcional)" value={filters.levelId || ''} onChange={e=> setFilters({...filters, levelId: e.target.value})} />
                    </div>
                    <div className="input-group">
                      <span className="input-group-text"><i className="bi bi-tag" /></span>
                      <input className="form-control" placeholder="Nome do Nível (opcional)" value={filters.levelName || ''} onChange={e=> setFilters({...filters, levelName: e.target.value})} />
                    </div>
                  </>
                )}
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <BrDateInput value={filters.start} placeholder="Início (dd/mm/aaaa)" onChange={v=> setFilters({...filters, start: v})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-clock" /></span>
                  <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <BrDateInput value={filters.end} placeholder="Fim (dd/mm/aaaa)" onChange={v=> setFilters({...filters, end: v})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-clock" /></span>
                  <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
                </div>
              </>
            )}
            {quickKind === 'visitantes' && (
              <div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-list-task" /></span>
                  <select className="form-select" value={visitantesObter} onChange={e=> setVisitantesObter(e.target.value as any)}>
                    <option value="documento">Acessos por Documento</option>
                    <option value="empresa">Acessos por Empresa</option>
                  </select>
                </div>
                {visitantesObter === 'documento' && (
                  <div className="input-group">
                    <span className="input-group-text"><i className="bi bi-file-earmark-text" /></span>
                    <input className="form-control" placeholder="Documento" value={filters.documento || ''} onChange={e=> setFilters({...filters, documento: e.target.value})} />
                  </div>
                )}
                {visitantesObter === 'empresa' && (
                  <div className="input-group">
                    <span className="input-group-text"><i className="bi bi-building" /></span>
                    <input className="form-control" placeholder="Empresa" value={filters.empresa || ''} onChange={e=> setFilters({...filters, empresa: e.target.value})} />
                  </div>
                )}
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <BrDateInput value={filters.start} placeholder="Início (dd/mm/aaaa)" onChange={v=> setFilters({...filters, start: v})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-clock" /></span>
                  <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <BrDateInput value={filters.end} placeholder="Fim (dd/mm/aaaa)" onChange={v=> setFilters({...filters, end: v})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-clock" /></span>
                  <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
                </div>
              </div>
            )}
          </div>
          )}

          {(data.length > 0 || searchTerm) && (
            <div className="queries-row" style={{marginBottom:8}}>
              <select className="form-select" style={{width:220}} value={searchColumn} onChange={e=> { setSearchColumn(e.target.value); setCurrentPage(1) }}>
                <option value="*">Todas as colunas</option>
                {searchColumnsList.map(c => <option key={c.key} value={c.key}>{c.label}</option>)}
              </select>
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-search" /></span>
                <input className="form-control" placeholder="Filtrar resultados (nome, crachá...)" value={searchTerm} onChange={e=> { setSearchTerm(e.target.value); setCurrentPage(1) }} />
                {searchTerm && (
                  <button className="btn btn-outline-secondary" type="button" onClick={()=> { setSearchTerm(''); setCurrentPage(1) }}>Limpar</button>
                )}
              </div>
            </div>
          )}

          <div className="queries-row" style={{marginBottom:12}}>
            <button className="btn btn-primary d-flex align-items-center" onClick={runQuick} disabled={loading}>
              {loading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Consultando...</> : <><i className="bi bi-play-fill me-1" /> Consultar</>}
            </button>
            {progressActive && (
              <div className="progress" style={{width:260, height:38}}>
                <div className={'progress-bar progress-bar-striped' + (loading ? ' progress-bar-animated' : '')} role="progressbar" style={{width: `${progress}%`}} aria-valuenow={progress} aria-valuemin={0} aria-valuemax={100}>
                  {progress}%
                </div>
              </div>
            )}
            {showExportGroup && (
              <div className="export-group">
                <span>Exportar:</span>
                {exportEnabledCsv && exportAllowsCsv && (
                  <button className="btn btn-light btn-icon" title="CSV" onClick={()=> exportData('csv')}>
                    <i className="bi bi-filetype-csv" />
                  </button>
                )}
                {exportEnabledXlsx && exportAllowsXlsx && (
                  <button className="btn btn-light btn-icon" title="XLSX" onClick={()=> exportData('xlsx')}>
                    <i className="bi bi-file-earmark-excel" />
                  </button>
                )}
                {exportEnabledPdf && exportAllowsPdf && (
                  <>
                    <button className="btn btn-light btn-icon" title="PDF" onClick={()=> exportData('pdf')}>
                      <i className="bi bi-file-earmark-pdf" />
                    </button>
                    {exportsToday.length > 0 && (
                      <button className="btn btn-outline-secondary ms-2" onClick={()=> setReportsModal(true)}>
                        <i className="bi bi-journal-text me-1" /> Relatórios
                      </button>
                    )}
                    {((lastPdfSavedPath || lastPdfRequestUrl) || (pdfUrl && exportFmt === 'pdf')) && (
                      <button className="btn btn-outline-secondary ms-2" onClick={openLastPdf}>
                        <i className="bi bi-eye me-1" /> Visualizar PDF
                      </button>
                    )}
                  </>
                )}
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
              {canUseDbTables && <option value="db-table">Tabela do Banco (todas as colunas)</option>}
            </select>
            {(dataset === 'transit') && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <BrDateInput value={filters.start} placeholder="Início (dd/mm/aaaa)" onChange={v=> setFilters({...filters, start: v})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-clock" /></span>
                  <input className="form-control" type="time" step="1" value={filters.startTime || '00:00:00'} onChange={e=> setFilters({...filters, startTime: e.target.value})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-calendar-event" /></span>
                  <BrDateInput value={filters.end} placeholder="Fim (dd/mm/aaaa)" onChange={v=> setFilters({...filters, end: v})} />
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-clock" /></span>
                  <input className="form-control" type="time" step="1" value={filters.endTime || '23:59:59'} onChange={e=> setFilters({...filters, endTime: e.target.value})} />
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
            {dataset === 'db-table' && canUseDbTables && (
              <>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-hdd-network" /></span>
                  <select className="form-select" value={dbTableDb} onChange={e=> { setDbTableDb(e.target.value as any); setDbTableName(''); setData([]) }}>
                    <option value="CMS">CMS</option>
                    <option value="Logins">Logins</option>
                    <option value="EMS">EMSEvents</option>
                  </select>
                </div>
                <div className="input-group">
                  <span className="input-group-text"><i className="bi bi-table" /></span>
                  <select className="form-select" value={dbTableName} onChange={e=> { setDbTableName(e.target.value); setData([]) }}>
                    <option value="">Selecione a tabela</option>
                    {((dbInfo && dbInfo.databases && dbInfo.databases[dbTableDb]?.tables) || []).map((t:string)=>(
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </select>
                </div>
              </>
            )}
          </div>
          <div className="queries-row" style={{marginBottom:8}}>
            <select className="form-select" style={{width:220}} value={searchColumn} onChange={e=> setSearchColumn(e.target.value)}>
              <option value="*">Todas as colunas</option>
              {datasetColumns.map(c => <option key={c.key} value={c.key}>{c.label}</option>)}
            </select>
            <div className="input-group">
              <span className="input-group-text"><i className="bi bi-search" /></span>
              <input className="form-control" placeholder="Pesquisar" value={searchTerm} onChange={e=> { setSearchTerm(e.target.value); setCurrentPage(1) }} />
            </div>
            <button className="btn btn-primary d-flex align-items-center" onClick={runPersonalizada} disabled={loading}>
              {loading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Consultando...</> : <><i className="bi bi-play-fill me-1" /> Consultar</>}
            </button>
            {progressActive && (
              <div className="progress" style={{width:260, height:38}}>
                <div className={'progress-bar progress-bar-striped' + (loading ? ' progress-bar-animated' : '')} role="progressbar" style={{width: `${progress}%`}} aria-valuenow={progress} aria-valuemin={0} aria-valuemax={100}>
                  {progress}%
                </div>
              </div>
            )}
            {showExportGroup && (
              <div className="export-group">
                <span>Exportar:</span>
                {exportEnabledCsv && exportAllowsCsv && (
                  <button className="btn btn-light btn-icon" title="CSV" onClick={()=> exportData('csv')}>
                    <i className="bi bi-filetype-csv" />
                  </button>
                )}
                {exportEnabledXlsx && exportAllowsXlsx && (
                  <button className="btn btn-light btn-icon" title="XLSX" onClick={()=> exportData('xlsx')}>
                    <i className="bi bi-file-earmark-excel" />
                  </button>
                )}
                {exportEnabledPdf && exportAllowsPdf && (
                  <>
                    <button className="btn btn-light btn-icon" title="PDF" onClick={()=> exportData('pdf')}>
                      <i className="bi bi-file-earmark-pdf" />
                    </button>
                    {exportsToday.length > 0 && (
                      <button className="btn btn-outline-secondary ms-2" onClick={()=> setReportsModal(true)}>
                        <i className="bi bi-journal-text me-1" /> Relatórios
                      </button>
                    )}
                    {((lastPdfSavedPath || lastPdfRequestUrl) || (pdfUrl && exportFmt === 'pdf')) && (
                      <button className="btn btn-outline-secondary ms-2" onClick={openLastPdf}>
                        <i className="bi bi-eye me-1" /> Visualizar PDF
                      </button>
                    )}
                  </>
                )}
              </div>
            )}
          </div>
          <div className="queries-cols-row" style={{marginBottom:12}}>
            <div className="queries-cols-list">
              {datasetColumns.map(col => {
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
      {!error && resultTotal != null && resultTotal > maxPreview && (
        <div className="alert alert-warning">
          <i className="bi bi-info-circle me-2" />
          Consulta muito grande ({resultTotal} registros). Mostrando só os primeiros {maxPreview}. Para gerar relatório completo, use Exportar.
        </div>
      )}

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

      <div className={`table-responsive${mode === 'prontas' && quickKind === 'door-critical' ? ' pro-table' : ''}`}>
        <table className="table table-hover table-striped align-middle">
          <thead>
            <tr>
              {tableColumns.map(c=> <th key={c.key}>{c.label}</th>)}
            </tr>
          </thead>
          <tbody>
            {Array.isArray(pageRows) && pageRows.map((row, idx)=> (
              <tr key={idx}>
                {tableColumns.map(c => <td key={c.key}>{String(getRowValue(row, c.key) ?? '')}</td>)}
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
