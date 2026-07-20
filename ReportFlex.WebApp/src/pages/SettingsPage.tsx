import React, { useEffect, useState } from 'react'
import { api } from '../api'

export function SettingsPage(){
  const [tab, setTab] = useState<'Relatorios'|'Banco'|'Telas'>('Relatorios')
  const [mode, setMode] = useState<'Real'|'Demo'>('Demo')
  const [loading, setLoading] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [seedCount, setSeedCount] = useState(100)
  const [cmsPath, setCmsPath] = useState('')
  const [loginsPath, setLoginsPath] = useState('')
  const [emsPath, setEmsPath] = useState('')
  const [hwrPath, setHwrPath] = useState('')
  const [clavPath, setClavPath] = useState('')
  const [dbInfo, setDbInfo] = useState<any | null>(null)
  const [dbInfoErr, setDbInfoErr] = useState<string | null>(null)
  const [reportOptions, setReportOptions] = useState<{ xlsx: boolean, pdf: boolean, excel: boolean, cover: boolean, coverOrientation: 'portrait'|'landscape', reportOrientation: 'portrait'|'landscape', customQueries: boolean }>({
    xlsx: true, pdf: true, excel: true, cover: false, coverOrientation: 'landscape', reportOrientation: 'landscape', customQueries: true
  })
  const [reportOptionsLoading, setReportOptionsLoading] = useState(false)
  const [useSqlAuth, setUseSqlAuth] = useState(false)
  const [sqlUser, setSqlUser] = useState('')
  const [sqlPwd, setSqlPwd] = useState('')
  const [sqlLogins, setSqlLogins] = useState<string[]>([])
  const [authStatus, setAuthStatus] = useState<any | null>(null)
  const [authMode, setAuthMode] = useState<any | null>(null)
  const [loginOnlyStatus, setLoginOnlyStatus] = useState<any | null>(null)
  const [sqlInstancesLoading, setSqlInstancesLoading] = useState(false)
  const [sqlInstances, setSqlInstances] = useState<Array<{ dataSource: string, server?: string|null, instance?: string|null, version?: string|null }>>([])
  const [sqlInstance, setSqlInstance] = useState('')
  const [sqlDatabasesLoading, setSqlDatabasesLoading] = useState(false)
  const [sqlDatabases, setSqlDatabases] = useState<string[]>([])
  const [sqlDbCms, setSqlDbCms] = useState('CMS')
  const [sqlDbLogins, setSqlDbLogins] = useState('Logins')
  const [sqlDbEms, setSqlDbEms] = useState('EMS')
  const [sqlTablesLoading, setSqlTablesLoading] = useState<Record<string, boolean>>({})
  const [sqlTables, setSqlTables] = useState<Record<string, string[]>>({})
  const [sqlDiscoverErr, setSqlDiscoverErr] = useState<string | null>(null)
  const [sqlApplyLoading, setSqlApplyLoading] = useState(false)
  const [setupModalOpen, setSetupModalOpen] = useState(false)
  const [setupLoading, setSetupLoading] = useState(false)
  const [setupError, setSetupError] = useState<string | null>(null)
  const [setupInstances, setSetupInstances] = useState<Array<{ dataSource: string, version?: string | null }>>([])
  const [setupDataSource, setSetupDataSource] = useState('')
  const [setupDatabases, setSetupDatabases] = useState<string[]>([])
  const [setupCmsDb, setSetupCmsDb] = useState('')
  const [setupLoginsDb, setSetupLoginsDb] = useState('')
  const [setupEmsDb, setSetupEmsDb] = useState('')
  const [setupHwrDb, setSetupHwrDb] = useState('')
  const [setupClavDb, setSetupClavDb] = useState('')
  const [setupCmsTables, setSetupCmsTables] = useState<string[]>([])
  const [setupLoginsTables, setSetupLoginsTables] = useState<string[]>([])
  const [setupEmsTables, setSetupEmsTables] = useState<string[]>([])
  const [setupHwrTables, setSetupHwrTables] = useState<string[]>([])
  const [setupClavTables, setSetupClavTables] = useState<string[]>([])
  const [setupInitialEmail, setSetupInitialEmail] = useState('')
  const [setupInitialPassword, setSetupInitialPassword] = useState('')
  const [setupInitialName, setSetupInitialName] = useState('SUPERADMIN')
  const [setupTest, setSetupTest] = useState<{ cms?: string, logins?: string, ems?: string, hwr?: string, clav?: string } | null>(null)
  const [dbObjectMapItems, setDbObjectMapItems] = useState<Array<{ key: string, label: string, connection: string, defaultValue: string, value: string }>>([])
  const [dbObjectMapLoading, setDbObjectMapLoading] = useState(false)
  const [screensCfg, setScreensCfg] = useState<Record<string, { enabled: boolean, lockedBy?: string }>>({})
  const [screensCfgLoading, setScreensCfgLoading] = useState(false)
  const [screensCfgErr, setScreensCfgErr] = useState<string | null>(null)
  const nivel = (typeof window !== 'undefined' ? (localStorage.getItem('rf_level') || '') : '')
  const isSuperAdmin = nivel === 'SuperAdmin'
  const isAdmin = nivel === 'Administrador'

  function normalizeReportOptions(opts: any){
    const active = !!opts?.excel ? 'excel' : (!!opts?.xlsx ? 'xlsx' : 'pdf')
    return {
      xlsx: active === 'xlsx',
      pdf: active === 'pdf',
      excel: active === 'excel',
      cover: !!opts?.cover,
      coverOrientation: (opts?.coverOrientation === 'portrait' ? 'portrait' : 'landscape') as 'portrait'|'landscape',
      reportOrientation: (opts?.reportOrientation === 'portrait' ? 'portrait' : 'landscape') as 'portrait'|'landscape',
      customQueries: !!opts?.customQueries
    }
  }

  function setExclusiveReportFormat(format: 'xlsx'|'excel'|'pdf'){
    setReportOptions(o => ({
      ...o,
      xlsx: format === 'xlsx',
      excel: format === 'excel',
      pdf: format === 'pdf'
    }))
  }

  useEffect(() => {
    (async () => {
      try{
        try{
          const owner = `${localStorage.getItem('rf_client_id') || ''}|${(localStorage.getItem('rf_token') || '').slice(-16)}`
          const ro = localStorage.getItem('rf_report_options')
          const roOwner = localStorage.getItem('rf_report_options_owner')
          if (ro && roOwner === owner){
            const opts: any = JSON.parse(ro)
            setReportOptions(normalizeReportOptions(opts))
          }
        }catch{}
        try{
          const owner = `${localStorage.getItem('rf_client_id') || ''}|${(localStorage.getItem('rf_token') || '').slice(-16)}`
          const sc = localStorage.getItem('rf_admin_screens_config')
          const scOwner = localStorage.getItem('rf_admin_screens_config_owner')
          if (sc && scOwner === owner){
            const cached: any = JSON.parse(sc)
            if (cached && typeof cached === 'object') setScreensCfg(cached || {})
          }
        }catch{}
        const r = await api.getDbMode()
        if (r?.mode === 'Demo' || r?.mode === 'Real'){ setMode(r.mode) }
        const c = await api.getConnections()
        if (c?.CMS){
          setCmsPath(c.CMS)
        }
        if (c?.Logins){
          setLoginsPath(c.Logins)
        }
        if (c?.EMS){
          setEmsPath(c.EMS)
        }
        if (c?.HWR){
          setHwrPath(c.HWR)
        }
        if (c?.CLAV){
          setClavPath(c.CLAV)
        }
        if (!c?.CMS) {
          setCmsPath('Data Source=JP4REPORTDEV01;Initial Catalog=CMS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
        }
        if (!c?.Logins) {
          setLoginsPath('Data Source=JP4REPORTDEV01;Initial Catalog=logins;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
        }
        if (!c?.EMS) {
          setEmsPath('Data Source=JP4REPORTDEV01;Initial Catalog=EMS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
        }
        if (!c?.HWR) {
          setHwrPath('Data Source=JP4REPORTDEV01;Initial Catalog=hwreportsview;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
        }
        if (!c?.CLAV) {
          setClavPath('Data Source=JP4REPORTDEV01;Initial Catalog=claviculario;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
        }
        try{
          setDbObjectMapLoading(true)
          const objectMap: any = await api.getDbObjectMap()
          const items = Array.isArray(objectMap?.items) ? objectMap.items : []
          setDbObjectMapItems(items.map((it: any) => ({
            key: String(it?.key || ''),
            label: String(it?.label || it?.key || ''),
            connection: String(it?.connection || ''),
            defaultValue: String(it?.defaultValue || ''),
            value: String(it?.value || '')
          })))
        }catch{
          setDbObjectMapItems([])
        }finally{
          setDbObjectMapLoading(false)
        }
        setDbInfoErr(null)
        try{
          const info = await api.getDbInfo()
          setDbInfo(info)
        }catch{
          setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
        }
        try{
          const lg = await api.getSqlLogins()
          const list = Array.isArray(lg?.logins) ? lg.logins.map((x:any)=> x?.name).filter((x:any)=> !!x) : []
          setSqlLogins(list)
        }catch{}
        try{
          const st = await api.testSqlAuth()
          setAuthStatus(st)
        }catch{}
        try{
          const md = await api.getSqlAuthMode()
          setAuthMode(md)
        }catch{}
        try{
          const lo = await api.testSqlLoginOnly()
          setLoginOnlyStatus(lo)
        }catch{}
        try{
          const opts = await api.getReportOptions()
          setReportOptions(normalizeReportOptions(opts))
          try{
            const owner = `${localStorage.getItem('rf_client_id') || ''}|${(localStorage.getItem('rf_token') || '').slice(-16)}`
            localStorage.setItem('rf_report_options', JSON.stringify(opts || {}))
            localStorage.setItem('rf_report_options_owner', owner)
            localStorage.setItem('rf_report_options_ts', String(Date.now()))
          }catch{}
        }catch{}
        try{
          setScreensCfgErr(null)
          setScreensCfgLoading(true)
          const sc: any = await api.adminGetScreensConfig()
          const obj: any = sc && typeof sc === 'object' ? sc : {}
          const out: Record<string, { enabled: boolean, lockedBy?: string }> = {}
          for (const k of Object.keys(obj)){
            const it = obj[k]
            out[k] = { enabled: !!it?.enabled, lockedBy: it?.lockedBy }
          }
          setScreensCfg(out)
          try{
            const owner = `${localStorage.getItem('rf_client_id') || ''}|${(localStorage.getItem('rf_token') || '').slice(-16)}`
            localStorage.setItem('rf_admin_screens_config', JSON.stringify(out || {}))
            localStorage.setItem('rf_admin_screens_config_owner', owner)
            localStorage.setItem('rf_admin_screens_config_ts', String(Date.now()))
          }catch{}
        }catch(e:any){
          setScreensCfgErr(e?.message || 'Falha ao carregar configuração de telas')
        }finally{
          setScreensCfgLoading(false)
        }
      }catch{}
    })()
  }, [])

  async function loadSqlInstances(){
    setSqlDiscoverErr(null)
    setSqlInstancesLoading(true)
    try{
      const r: any = await api.sqlInstances()
      const items = Array.isArray(r?.items) ? r.items : []
      setSqlInstances(items)
      if (!sqlInstance && items[0]?.dataSource) setSqlInstance(String(items[0].dataSource))
    }catch(e:any){
      setSqlDiscoverErr(e?.message || 'Falha ao buscar instâncias SQL')
      setSqlInstances([])
    }finally{
      setSqlInstancesLoading(false)
    }
  }

  async function loadSqlDatabases(){
    if (!sqlInstance) return
    setSqlDiscoverErr(null)
    setSqlDatabasesLoading(true)
    try{
      const r: any = await api.sqlDatabases(sqlInstance)
      const items = Array.isArray(r?.items) ? r.items.map((x:any)=> String(x)) : []
      setSqlDatabases(items)
      if (items.includes('CMS')) setSqlDbCms('CMS')
      if (items.includes('Logins')) setSqlDbLogins('Logins')
      if (items.includes('EMS')) setSqlDbEms('EMS')
      else if (items.includes('EMSEVENTS')) setSqlDbEms('EMSEVENTS')
      else if (items.includes('hwreportsview')) setSqlDbEms('hwreportsview')
    }catch(e:any){
      setSqlDiscoverErr(e?.message || 'Falha ao listar bases')
      setSqlDatabases([])
    }finally{
      setSqlDatabasesLoading(false)
    }
  }

  async function loadSqlTablesPreview(database: string){
    if (!sqlInstance || !database) return
    setSqlTablesLoading(prev => ({ ...prev, [database]: true }))
    try{
      const r: any = await api.sqlTables(sqlInstance, database)
      const items = Array.isArray(r?.items) ? r.items.map((x:any)=> String(x)) : []
      setSqlTables(prev => ({ ...prev, [database]: items }))
    }catch(e:any){
      setSqlTables(prev => ({ ...prev, [database]: [String(e?.message || 'Falha ao listar tabelas')] }))
    }finally{
      setSqlTablesLoading(prev => ({ ...prev, [database]: false }))
    }
  }

  async function loadSetupInstances(){
    setSetupError(null)
    setSetupLoading(true)
    try{
      const res: any = await api.setupSqlInstances()
      const items = Array.isArray(res?.items) ? res.items : []
      setSetupInstances(items)
      if (!setupDataSource && items.length){
        const ds = String(items[0]?.dataSource || '').trim()
        if (ds) setSetupDataSource(ds)
      }
    }catch(e:any){
      setSetupInstances([])
      setSetupError(e?.message || 'Falha ao listar instâncias SQL')
    }finally{
      setSetupLoading(false)
    }
  }

  async function loadSetupDatabases(ds: string){
    if (!ds) return
    setSetupError(null)
    setSetupLoading(true)
    try{
      const res: any = await api.setupSqlDatabases(ds)
      const items = Array.isArray(res?.items) ? res.items.map((x:any)=> String(x)) : []
      setSetupDatabases(items)
      const pick = (preferred: string) => items.find((x: string)=> x.toLowerCase() === preferred.toLowerCase()) || ''
      const cms = pick('CMS') || (items[0] || '')
      const logins = pick('Logins') || pick('Login') || (items[0] || '')
      const ems = pick('EMS') || pick('EMSEVENTS') || (items[0] || '')
      const hwr = pick('hwreportsview') || pick('HWR') || pick('HWRREPORTS') || (items[0] || '')
      const clav = pick('claviculario') || pick('CLAV') || (items[0] || '')
      if (!setupCmsDb) setSetupCmsDb(cms)
      if (!setupLoginsDb) setSetupLoginsDb(logins)
      if (!setupEmsDb) setSetupEmsDb(ems)
      if (!setupHwrDb) setSetupHwrDb(hwr)
      if (!setupClavDb) setSetupClavDb(clav)
    }catch(e:any){
      setSetupDatabases([])
      setSetupError(e?.message || 'Falha ao listar bancos')
    }finally{
      setSetupLoading(false)
    }
  }

  async function loadSetupTables(database: string, set: (items: string[])=>void){
    if (!setupDataSource || !database) { set([]); return }
    try{
      const res: any = await api.setupSqlTables(setupDataSource, database)
      const items = Array.isArray(res?.items) ? res.items.map((x:any)=> String(x)) : []
      set(items)
    }catch(e:any){
      set([])
    }
  }

  useEffect(()=>{
    if (!setupModalOpen) return
    loadSetupInstances()
  },[setupModalOpen])

  useEffect(()=>{
    if (!setupModalOpen) return
    if (!setupDataSource) return
    loadSetupDatabases(setupDataSource)
  },[setupModalOpen, setupDataSource])

  useEffect(()=>{
    if (!setupModalOpen) return
    loadSetupTables(setupCmsDb, setSetupCmsTables)
  },[setupModalOpen, setupDataSource, setupCmsDb])

  useEffect(()=>{
    if (!setupModalOpen) return
    loadSetupTables(setupLoginsDb, setSetupLoginsTables)
  },[setupModalOpen, setupDataSource, setupLoginsDb])

  useEffect(()=>{
    if (!setupModalOpen) return
    if (!setupEmsDb) { setSetupEmsTables([]); return }
    loadSetupTables(setupEmsDb, setSetupEmsTables)
  },[setupModalOpen, setupDataSource, setupEmsDb])

  useEffect(()=>{
    if (!setupModalOpen) return
    if (!setupHwrDb) { setSetupHwrTables([]); return }
    loadSetupTables(setupHwrDb, setSetupHwrTables)
  },[setupModalOpen, setupDataSource, setupHwrDb])

  useEffect(()=>{
    if (!setupModalOpen) return
    if (!setupClavDb) { setSetupClavTables([]); return }
    loadSetupTables(setupClavDb, setSetupClavTables)
  },[setupModalOpen, setupDataSource, setupClavDb])

  async function testSetupConnections(){
    setSetupError(null)
    setSetupTest(null)
    if (!setupDataSource || !setupCmsDb || !setupLoginsDb || !setupEmsDb || !setupHwrDb || !setupClavDb){
      setSetupError('Selecione a instância e os bancos (CMS, Logins, EMS, HWR e CLAV).')
      return
    }
    setSetupLoading(true)
    try{
      const out: any = {}
      try{ await api.setupSqlTables(setupDataSource, setupCmsDb); out.cms = 'OK' }catch(e:any){ out.cms = e?.message || 'Falha' }
      try{ await api.setupSqlTables(setupDataSource, setupLoginsDb); out.logins = 'OK' }catch(e:any){ out.logins = e?.message || 'Falha' }
      try{ await api.setupSqlTables(setupDataSource, setupEmsDb); out.ems = 'OK' }catch(e:any){ out.ems = e?.message || 'Falha' }
      try{ await api.setupSqlTables(setupDataSource, setupHwrDb); out.hwr = 'OK' }catch(e:any){ out.hwr = e?.message || 'Falha' }
      try{ await api.setupSqlTables(setupDataSource, setupClavDb); out.clav = 'OK' }catch(e:any){ out.clav = e?.message || 'Falha' }
      setSetupTest(out)
    }finally{
      setSetupLoading(false)
    }
  }

  async function applySetupFromSettings(){
    setSetupError(null)
    setSetupTest(null)
    if (!setupDataSource || !setupCmsDb || !setupLoginsDb || !setupEmsDb || !setupHwrDb || !setupClavDb){
      setSetupError('Selecione a instância e os bancos (CMS, Logins, EMS, HWR e CLAV).')
      return
    }
    setSetupLoading(true)
    try{
      const res: any = await api.setupApply({
        dataSource: setupDataSource,
        cmsDb: setupCmsDb,
        loginsDb: setupLoginsDb,
        emsDb: setupEmsDb,
        hwrDb: setupHwrDb,
        clavDb: setupClavDb,
        initialEmail: (setupInitialEmail || '').trim() || undefined,
        initialPassword: setupInitialPassword || undefined,
        initialName: (setupInitialName || '').trim() || undefined
      })
      if (!res?.__ok){
        setSetupError(String(res?.error || res?.detail || 'Falha ao aplicar configuração'))
        return
      }
      setMsg(res?.createdFirstUser ? 'Conexão aplicada e SuperAdmin inicial criado (exigirá troca de senha no primeiro login).' : 'Conexão aplicada. Usuários já existiam, então nenhum SuperAdmin inicial foi criado.')
      setErr(null)
      setSetupModalOpen(false)
      try{
        const r = await api.getDbMode()
        if (r?.mode === 'Demo' || r?.mode === 'Real'){ setMode(r.mode) }
      }catch{}
      let conns: any | null = null
      let info: any | null = null
      let st: any | null = null
      try{
        conns = await api.getConnections()
        if (conns?.CMS) setCmsPath(conns.CMS)
        if (conns?.Logins) setLoginsPath(conns.Logins)
        if (conns?.EMS) setEmsPath(conns.EMS)
        if (conns?.HWR) setHwrPath(conns.HWR)
        if (conns?.CLAV) setClavPath(conns.CLAV)
      }catch{}
      try{
        info = await api.getDbInfo()
        setDbInfo(info)
        setDbInfoErr(null)
      }catch{
        setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
      }
      try{
        st = await api.testSqlAuth()
        setAuthStatus(st)
      }catch{}
      try{
        const keys = ['CMS','Logins','EMS','HWR','CLAV'] as const
        const configured: Record<typeof keys[number], boolean> = {
          CMS: !!String(conns?.CMS || '').trim(),
          Logins: !!String(conns?.Logins || '').trim(),
          EMS: !!String(conns?.EMS || '').trim(),
          HWR: !!String(conns?.HWR || '').trim(),
          CLAV: !!String(conns?.CLAV || '').trim()
        }
        const isOk = (k: typeof keys[number]) => {
          const aok = st?.[k]?.ok
          if (typeof aok === 'boolean') return aok
          return !!info?.databases?.[k]
        }
        const failed = keys.filter(k => configured[k] && !isOk(k))
        if (failed.length === 0) {
          setMsg('Conexões salvas e testadas: tudo OK.')
        } else {
          const failedDetails = failed.map(k => {
            const e = st?.[k]?.error
            return e ? `${k} (${String(e)})` : k
          }).join(', ')
          setErr('Conexão salva, mas falhou o teste em: ' + failedDetails)
        }
      }catch{}
    }catch(e:any){
      setSetupError(e?.message || 'Falha ao aplicar configuração')
    }finally{
      setSetupLoading(false)
    }
  }

  function buildConnFromWizard(database: string){
    const ds = sqlInstance
    if (!ds || !database) return ''
    const base = `Data Source=${ds};Initial Catalog=${database};Encrypt=True;TrustServerCertificate=True;`
    if (useSqlAuth && sqlUser){
      const u = `User ID=${sqlUser};`
      const p = sqlPwd ? `Password=${sqlPwd};` : ''
      return base + 'Integrated Security=False;' + u + p
    }
    return base + 'Integrated Security=True;'
  }

  async function applySqlWizardConnections(){
    if (!sqlInstance) return
    setErr(null); setMsg(null); setDbInfoErr(null); setSqlDiscoverErr(null)
    setSqlApplyLoading(true)
    try{
      if (useSqlAuth && sqlUser){
        await api.setSqlAuth({ user: sqlUser, pwd: sqlPwd })
      }
      const cms = buildConnFromWizard(sqlDbCms)
      const logins = buildConnFromWizard(sqlDbLogins)
      const ems = buildConnFromWizard(sqlDbEms)
      const findDb = (wanted: string) => sqlDatabases.find(x => String(x).toLowerCase() === wanted.toLowerCase())
      const hwrDb = findDb('hwreportsview')
      const clavDb = findDb('claviculario') || findDb('CLAV') || findDb('clav') || findDb('Clav')
      const hwr = hwrDb ? buildConnFromWizard(hwrDb) : ''
      const clav = clavDb ? buildConnFromWizard(clavDb) : ''
      const r: any = await api.setConnections({ CMS: cms, Logins: logins, EMS: ems, HWR: hwr || undefined, CLAV: clav || undefined })
      if (r?.CMS) setCmsPath(r.CMS)
      if (r?.Logins) setLoginsPath(r.Logins)
      if (r?.EMS) setEmsPath(r.EMS)
      if (r?.HWR) setHwrPath(r.HWR)
      if (r?.CLAV) setClavPath(r.CLAV)
      setMsg('Configuração de conexão salva')
      let info: any | null = null
      let st: any | null = null
      try{
        info = await api.getDbInfo()
        setDbInfo(info)
        setDbInfoErr(null)
      }catch{
        setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
      }
      try{
        st = await api.testSqlAuth()
        setAuthStatus(st)
      }catch{}
      try{
        const keys = ['CMS','Logins','EMS','HWR','CLAV'] as const
        const configured: Record<typeof keys[number], boolean> = {
          CMS: !!cms.trim(),
          Logins: !!logins.trim(),
          EMS: !!ems.trim(),
          HWR: !!hwr.trim(),
          CLAV: !!clav.trim()
        }
        const isOk = (k: typeof keys[number]) => {
          const aok = st?.[k]?.ok
          if (typeof aok === 'boolean') return aok
          return !!info?.databases?.[k]
        }
        const failed = keys.filter(k => configured[k] && !isOk(k))
        if (failed.length === 0) {
          setMsg('Conexões salvas e testadas: tudo OK.')
        } else {
          const failedDetails = failed.map(k => {
            const e = st?.[k]?.error
            return e ? `${k} (${String(e)})` : k
          }).join(', ')
          setErr('Conexão salva, mas falhou o teste em: ' + failedDetails)
        }
      }catch{}
      try{
        const md = await api.getSqlAuthMode()
        setAuthMode(md)
      }catch{}
      try{
        const lo = await api.testSqlLoginOnly()
        setLoginOnlyStatus(lo)
      }catch{}
    }catch(e:any){
      setErr(e?.message || 'Falha ao salvar configuração')
    }finally{
      setSqlApplyLoading(false)
    }
  }

  async function applyRecommended(){
    setErr(null); setMsg(null); setDbInfoErr(null)
    try{
      setCmsPath('Data Source=JP4REPORTDEV01;Initial Catalog=CMS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
      setLoginsPath('Data Source=JP4REPORTDEV01;Initial Catalog=Logins;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
      setEmsPath('Data Source=JP4REPORTDEV01;Initial Catalog=EMSEVENTS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
      setHwrPath('Data Source=JP4REPORTDEV01;Initial Catalog=hwreportsview;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
      setClavPath('')
      await saveConnections()
    }catch{
      setErr('Falha ao aplicar configuração recomendada')
    }
  }

  async function applyMode(next: 'Real'|'Demo'){
    setErr(null); setMsg(null)
    try{
      const r = await api.setDbMode(next)
      setMode(r.mode || next)
      setMsg(`Modo alterado para ${r.mode || next}`)
      setDbInfoErr(null)
      
      // Se mudando para Real, carregar a connection string salva
      if (next === 'Real') {
        try {
          const c = await api.getConnections()
          if (c?.CMS) {
            setCmsPath(c.CMS)
          }
          if (c?.Logins) {
            setLoginsPath(c.Logins)
          }
          if (c?.EMS) {
            setEmsPath(c.EMS)
          }
          if (c?.HWR) {
            setHwrPath(c.HWR)
          }
          if (c?.CLAV) {
            setClavPath(c.CLAV)
          }
        } catch {
          // Ignorar erro ao carregar conexões
        }
      }
      
      try{
        const info = await api.getDbInfo()
        setDbInfo(info)
      }catch{
        setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
      }
    }catch{
      setErr('Falha ao alterar modo')
    }
  }

  async function seedCompanies(){
    setErr(null); setMsg(null); setLoading(true)
    try{
      const r = await api.seedCompanies()
      if (r?.error){
        setErr(r.error)
      }else{
        const summary = Object.entries(r || {}).map(([k,v])=> `${k}:${v}`).join(', ')
        setMsg(`Empresas/funcionários/acessos gerados • ${summary}`)
      }
      try{
        const info = await api.getDbInfo()
        setDbInfo(info)
      }catch{
        // ignore
      }
    }catch(e:any){
      setErr(e?.message || 'Falha ao gerar dados')
    }finally{
      setLoading(false)
    }
  }
  async function seed(){
    setErr(null); setMsg(null); setLoading(true)
    try{
      const r = await api.seedDemo(seedCount, 'all')
      if (r?.error){
        setErr(r.error)
      }else{
        const summary = Object.entries(r || {}).map(([k,v])=> `${k}:${v}`).join(', ')
        setMsg(`Dados de teste adicionados com sucesso${summary ? ' • ' + summary : ''}`)
      }
    }catch{
      setErr('Falha ao adicionar dados de teste (verifique se está em modo Demo e ambiente de desenvolvimento)')
    }finally{
      setLoading(false)
    }
  }

  async function saveConnections(){
    setErr(null); setMsg(null); setDbInfoErr(null)
    try{
      let cms = cmsPath
      let logins = loginsPath
      let ems = emsPath
      let hwr = hwrPath
      let clav = clavPath
      const ensureTls = (s: string) => {
        const up = s.trim().replace(/;+\s*$/,'')
        const hasEnc = /Encrypt\s*=\s*True/i.test(up)
        const hasTrust = /TrustServerCertificate\s*=\s*True/i.test(up)
        let out = up
        if (!hasEnc) out += ';Encrypt=True'
        if (!hasTrust) out += ';TrustServerCertificate=True'
        return out
      }
      const applySqlAuth = (s: string) => {
        let out = s.replace(/Integrated\s*Security\s*=\s*True/ig, 'Integrated Security=False')
        out = out.replace(/Trusted_Connection\s*=\s*Yes/ig, 'Integrated Security=False')
        out = out.replace(/;\s*User\s*ID\s*=\s*[^;]*/ig, '')
        out = out.replace(/;\s*Password\s*=\s*[^;]*/ig, '')
        out = out.replace(/;\s*UID\s*=\s*[^;]*/ig, '')
        out = out.replace(/;\s*PWD\s*=\s*[^;]*/ig, '')
        if (sqlUser) out += `;User ID=${sqlUser}`
        if (sqlPwd) out += `;Password=${sqlPwd}`
        return out
      }
      const ensureCatalog = (s: string, catalog: string) => {
        let out = s.trim().replace(/;+\s*$/,'')
        if (/(Initial\s*Catalog|Database)\s*=\s*[^;]*/i.test(out)) {
          out = out.replace(/(Initial\s*Catalog|Database)\s*=\s*[^;]*/i, `$1=${catalog}`)
          return out
        }
        return out ? (out + `;Initial Catalog=${catalog}`) : `Initial Catalog=${catalog}`
      }
      if (!clav || !clav.trim()){
        const base = String(cms || '').trim()
        clav = base ? ensureCatalog(base, 'claviculario') : 'Data Source=JP4REPORTDEV01;Initial Catalog=claviculario;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
      }
      if (useSqlAuth && sqlUser){
        await api.setSqlAuth({ user: sqlUser, pwd: sqlPwd })
      }
      cms = ensureTls(useSqlAuth ? applySqlAuth(cms) : cms)
      logins = ensureTls(useSqlAuth ? applySqlAuth(logins) : logins)
      ems = ensureTls(useSqlAuth ? applySqlAuth(ems) : ems)
      hwr = hwr ? ensureTls(useSqlAuth ? applySqlAuth(hwr) : hwr) : ''
      clav = ensureTls(useSqlAuth ? applySqlAuth(clav) : clav)
      const r = await api.setConnections({ CMS: cms, Logins: logins, EMS: ems, HWR: hwr || undefined, CLAV: clav })
      if (r?.CMS){
        setCmsPath(r.CMS)
      }
      if (r?.Logins){
        setLoginsPath(r.Logins)
      }
      if (r?.EMS){
        setEmsPath(r.EMS)
      }
      if (r?.HWR){
        setHwrPath(r.HWR)
      }
      if (r?.CLAV){
        setClavPath(r.CLAV)
      }
      setMsg('Configuração de conexão salva')
      let info: any | null = null
      let st: any | null = null
      try{
        info = await api.getDbInfo()
        setDbInfo(info)
        setDbInfoErr(null)
      }catch{
        setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
      }
      try{
        st = await api.testSqlAuth()
        setAuthStatus(st)
      }catch{}
      try{
        const keys = ['CMS','Logins','EMS','HWR','CLAV'] as const
        const configured: Record<typeof keys[number], boolean> = {
          CMS: !!cms.trim(),
          Logins: !!logins.trim(),
          EMS: !!ems.trim(),
          HWR: !!hwr.trim(),
          CLAV: !!clav.trim()
        }
        const isOk = (k: typeof keys[number]) => {
          const aok = st?.[k]?.ok
          if (typeof aok === 'boolean') return aok
          return !!info?.databases?.[k]
        }
        const failed = keys.filter(k => configured[k] && !isOk(k))
        if (failed.length === 0) {
          setMsg('Conexões salvas e testadas: tudo OK.')
        } else {
          const failedDetails = failed.map(k => {
            const e = st?.[k]?.error
            return e ? `${k} (${String(e)})` : k
          }).join(', ')
          setErr('Conexão salva, mas falhou o teste em: ' + failedDetails)
        }
      }catch{}
    }catch(e:any){
      setErr(e?.message || 'Falha ao salvar configuração')
    }
  }

  async function saveDbObjectMap(){
    setErr(null); setMsg(null)
    setDbObjectMapLoading(true)
    try{
      const payload: Record<string, string> = {}
      for (const item of dbObjectMapItems){
        payload[item.key] = (item.value || '').trim()
      }
      const r: any = await api.setDbObjectMap(payload)
      const items = Array.isArray(r?.items) ? r.items : []
      setDbObjectMapItems(items.map((it: any) => ({
        key: String(it?.key || ''),
        label: String(it?.label || it?.key || ''),
        connection: String(it?.connection || ''),
        defaultValue: String(it?.defaultValue || ''),
        value: String(it?.value || '')
      })))
      setMsg('Mapeamento de objetos salvo')
    }catch(e:any){
      setErr(e?.message || 'Falha ao salvar mapeamento de objetos')
    }finally{
      setDbObjectMapLoading(false)
    }
  }

  async function saveReportOptions(){
    setErr(null); setMsg(null)
    setReportOptionsLoading(true)
    try{
      const r = await api.setReportOptions(reportOptions as any)
      setReportOptions(normalizeReportOptions(r))
      try{
        const owner = `${localStorage.getItem('rf_client_id') || ''}|${(localStorage.getItem('rf_token') || '').slice(-16)}`
        localStorage.setItem('rf_report_options', JSON.stringify(r || {}))
        localStorage.setItem('rf_report_options_owner', owner)
        localStorage.setItem('rf_report_options_ts', String(Date.now()))
      }catch{}
      setMsg('Opções de formatos de relatórios salvas')
    }catch(e:any){
      setErr(e?.message || 'Falha ao salvar opções de relatórios')
    }finally{
      setReportOptionsLoading(false)
    }
  }

  const screensCatalog = React.useMemo(() => ([
    { key: 'consultas', label: 'Consultas', desc: 'Tela principal de consultas e geração de relatórios' },
    { key: 'mensagens', label: 'Mensagens', desc: 'Mensagens do sistema' },
    { key: 'prestadores', label: 'Prestadores', desc: 'Consulta de prestadores' },
    { key: 'transit', label: 'Trânsitos', desc: 'Consulta de trânsitos' },
    { key: 'employees', label: 'Funcionários', desc: 'Consulta de funcionários' },
    { key: 'external', label: 'Externos', desc: 'Consulta de externos' },
    { key: 'access', label: 'Acessos', desc: 'Consulta de acessos' },
    { key: 'logs', label: 'Logs', desc: 'Auditoria de eventos relevantes' },
    { key: 'consultas-config', label: 'Config Consultas', desc: 'Ativar/desativar consultas' },
    { key: 'configuracoes', label: 'Configurações', desc: 'Banco, relatórios e telas' },
    { key: 'clientes', label: 'Clientes', desc: 'Cadastro de clientes e usuários' },
    { key: 'inbox', label: 'Inbox', desc: 'Inbox administrativo' }
  ]), [])

  async function reloadScreensConfig(){
    setScreensCfgErr(null); setErr(null); setMsg(null)
    setScreensCfgLoading(true)
    try{
      const sc: any = await api.adminGetScreensConfig()
      const obj: any = sc && typeof sc === 'object' ? sc : {}
      const out: Record<string, { enabled: boolean, lockedBy?: string }> = {}
      for (const k of Object.keys(obj)){
        const it = obj[k]
        out[k] = { enabled: !!it?.enabled, lockedBy: it?.lockedBy }
      }
      setScreensCfg(out)
      try{
        const owner = `${localStorage.getItem('rf_client_id') || ''}|${(localStorage.getItem('rf_token') || '').slice(-16)}`
        localStorage.setItem('rf_admin_screens_config', JSON.stringify(out || {}))
        localStorage.setItem('rf_admin_screens_config_owner', owner)
        localStorage.setItem('rf_admin_screens_config_ts', String(Date.now()))
      }catch{}
    }catch(e:any){
      setScreensCfgErr(e?.message || 'Falha ao recarregar configuração de telas')
    }finally{
      setScreensCfgLoading(false)
    }
  }

  async function saveScreensConfig(){
    setMsg(null); setErr(null); setScreensCfgErr(null)
    setScreensCfgLoading(true)
    try{
      const payload: Record<string, boolean> = {}
      for (const it of screensCatalog){
        payload[it.key] = !!screensCfg?.[it.key]?.enabled
      }
      const r: any = await api.adminSetScreensConfig(payload)
      const out: Record<string, { enabled: boolean, lockedBy?: string }> = {}
      for (const k of Object.keys(r || {})){
        const it = (r as any)[k]
        out[k] = { enabled: !!it?.enabled, lockedBy: it?.lockedBy }
      }
      setScreensCfg(out)
      try{
        const simple: Record<string, boolean> = {}
        for (const k of Object.keys(out)) simple[k] = !!out[k].enabled
        localStorage.setItem('rf_screens_config', JSON.stringify(simple))
        localStorage.setItem('rf_screens_config_owner', `${localStorage.getItem('rf_client_id') || ''}|${(localStorage.getItem('rf_token') || '').slice(-16)}`)
        localStorage.setItem('rf_screens_config_ts', String(Date.now()))
        window.dispatchEvent(new Event('rf:screens-config'))
      }catch{}
      try{
        const owner = `${localStorage.getItem('rf_client_id') || ''}|${(localStorage.getItem('rf_token') || '').slice(-16)}`
        localStorage.setItem('rf_admin_screens_config', JSON.stringify(out || {}))
        localStorage.setItem('rf_admin_screens_config_owner', owner)
        localStorage.setItem('rf_admin_screens_config_ts', String(Date.now()))
      }catch{}
      setMsg('Configuração de telas salva')
    }catch(e:any){
      const m = String(e?.message || '')
      if (m.includes('403')) setErr('Você não tem permissão para alterar uma tela travada por um nível superior.')
      else setErr(e?.message || 'Falha ao salvar configuração de telas')
    }finally{
      setScreensCfgLoading(false)
    }
  }

  const extractConnValue = (conn: string, key: 'Data Source'|'Server'|'Initial Catalog'|'Database') => {
    if (!conn) return ''
    const re = new RegExp(`(?:^|;)\\s*${key.replace(' ', '\\\\s+')}\\s*=\\s*([^;]+)`, 'i')
    const m = conn.match(re)
    return (m?.[1] || '').trim()
  }

  const summarizeDb = (key: 'CMS'|'Logins'|'EMS'|'HWR'|'CLAV') => {
    const db = dbInfo?.databases?.[key]
    if (!db) return null
    const conn = String(db.connection || '')
    const dataSource = extractConnValue(conn, 'Data Source') || extractConnValue(conn, 'Server')
    const catalog = extractConnValue(conn, 'Initial Catalog') || extractConnValue(conn, 'Database')
    const tables = Array.isArray(db.tables) ? db.tables : []
    const procedures = Array.isArray(db.procedures) ? db.procedures : []
    return { key, conn, dataSource, catalog, tables, procedures }
  }

  const dbCms = summarizeDb('CMS')
  const dbLogins = summarizeDb('Logins')
  const dbEms = summarizeDb('EMS')
  const dbHwr = summarizeDb('HWR')
  const dbClav = summarizeDb('CLAV')

  return (
    <section>
      <h2>Configurações</h2>
      <ul className="nav nav-tabs" style={{marginBottom:12}}>
        <li className="nav-item">
          <button className={'nav-link' + (tab === 'Relatorios' ? ' active' : '')} type="button" onClick={()=> setTab('Relatorios')}>Relatórios</button>
        </li>
        <li className="nav-item">
          <button className={'nav-link' + (tab === 'Telas' ? ' active' : '')} type="button" onClick={()=> setTab('Telas')}>Telas</button>
        </li>
        <li className="nav-item">
          <button className={'nav-link' + (tab === 'Banco' ? ' active' : '')} type="button" onClick={()=> setTab('Banco')}>Banco</button>
        </li>
      </ul>
      {msg && <div className="alert alert-success d-flex align-items-center" style={{gap:8}}><i className="bi bi-check-circle" /> {msg}</div>}
      {err && <div className="alert alert-danger d-flex align-items-center" style={{gap:8}}><i className="bi bi-exclamation-triangle" /> {err}</div>}
      {tab === 'Banco' && (
      <>
      <div style={{display:'flex', flexWrap:'wrap', gap:12, alignItems:'flex-start'}}>
        <div style={{flex:'1 1 720px', minWidth:360}}>
          <div className="card h-100">
            <div className="card-header d-flex align-items-center" style={{gap:8}}>
              <i className="bi bi-database-gear" /> Banco de Dados
            </div>
            <div className="card-body p-3">
              <div className="d-flex flex-wrap align-items-end" style={{gap:12}}>
                <div className="d-flex align-items-center flex-wrap" style={{gap:12}}>
                  <div className="form-check">
                    <input className="form-check-input" type="radio" name="dbmode" id="dbReal" checked={mode==='Real'} onChange={()=> applyMode('Real')} />
                    <label className="form-check-label" htmlFor="dbReal">Banco Real</label>
                  </div>
                  <div className="form-check">
                    <input className="form-check-input" type="radio" name="dbmode" id="dbDemo" checked={mode==='Demo'} onChange={()=> applyMode('Demo')} />
                    <label className="form-check-label" htmlFor="dbDemo">Banco Demo</label>
                  </div>
                </div>
                <button type="button" className="btn btn-outline-dark btn-sm d-flex align-items-center" onClick={()=> setSetupModalOpen(true)}>
                  <i className="bi bi-plug me-2" /> Assistente
                </button>
              </div>
              <div className="text-muted" style={{fontSize:12, marginTop:4}}>
                Configure as conexões do SQL Server e valide a estrutura das bases.
              </div>

              {mode === 'Demo' && (
                <div className="alert alert-info mt-2 mb-0 d-flex align-items-center py-2" style={{gap:8}}>
                  <i className="bi bi-info-circle" />
                  <div>
                    <div className="fw-semibold">Modo Demo</div>
                    <div style={{fontSize:12}}>Recomendado apenas para testes.</div>
                  </div>
                </div>
              )}

              {mode === 'Real' && (
                <>
                  <hr className="my-2" />
                  <div className="row g-2">
                    <div className="col-12">
                      <label className="form-label" style={{marginBottom:4}}>Conexão CMS</label>
                      <div className="input-group input-group-sm">
                        <span className="input-group-text"><i className="bi bi-hdd-network" /></span>
                        <input className="form-control" value={cmsPath} onChange={e=> setCmsPath(e.target.value)} placeholder="Ex: Data Source=SERVIDOR;Initial Catalog=CMS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" />
                      </div>
                    </div>

                    <div className="col-12">
                      <label className="form-label" style={{marginBottom:4}}>Conexão Logins</label>
                      <div className="input-group input-group-sm">
                        <span className="input-group-text"><i className="bi bi-hdd-network" /></span>
                        <input className="form-control" value={loginsPath} onChange={e=> setLoginsPath(e.target.value)} placeholder="Ex: Data Source=SERVIDOR;Initial Catalog=Logins;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" />
                      </div>
                    </div>

                    <div className="col-12">
                      <label className="form-label" style={{marginBottom:4}}>Conexão EMS</label>
                      <div className="input-group input-group-sm">
                        <span className="input-group-text"><i className="bi bi-hdd-stack" /></span>
                        <input className="form-control" value={emsPath} onChange={e=> setEmsPath(e.target.value)} placeholder="Ex: ...;Initial Catalog=EMSEVENTS;..." />
                      </div>
                    </div>

                    <div className="col-12">
                      <label className="form-label" style={{marginBottom:4}}>Conexão HWR</label>
                      <div className="input-group input-group-sm">
                        <span className="input-group-text"><i className="bi bi-hdd-stack" /></span>
                        <input className="form-control" value={hwrPath} onChange={e=> setHwrPath(e.target.value)} placeholder="Ex: ...;Initial Catalog=hwreportsview;..." />
                      </div>
                    </div>

                    <div className="col-12">
                      <label className="form-label" style={{marginBottom:4}}>Conexão CLAV (Claviculário)</label>
                      <div className="input-group input-group-sm">
                        <span className="input-group-text"><i className="bi bi-hdd-stack" /></span>
                        <input className="form-control" value={clavPath} onChange={e=> setClavPath(e.target.value)} placeholder="Ex: Data Source=SERVIDOR;Initial Catalog=claviculario;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" />
                      </div>
                    </div>

                    <div className="col-12">
                      <div className="form-check form-switch">
                        <input className="form-check-input" type="checkbox" id="switchSqlAuth" checked={useSqlAuth} onChange={()=> setUseSqlAuth(!useSqlAuth)} />
                        <label className="form-check-label" htmlFor="switchSqlAuth">Usar Autenticação SQL</label>
                      </div>
                      {useSqlAuth && (
                        <div className="row g-2" style={{marginTop:6}}>
                          <div className="col-12 col-md-6">
                            <div className="input-group input-group-sm">
                              <span className="input-group-text"><i className="bi bi-person" /></span>
                              <input className="form-control" placeholder="Usuário SQL" value={sqlUser} onChange={e=> setSqlUser(e.target.value)} />
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <div className="input-group input-group-sm">
                              <span className="input-group-text"><i className="bi bi-key" /></span>
                              <input className="form-control" type="password" placeholder="Senha SQL" value={sqlPwd} onChange={e=> setSqlPwd(e.target.value)} />
                            </div>
                          </div>
                        </div>
                      )}
                    </div>

                    <div className="col-12 d-flex justify-content-end">
                      <button className="btn btn-primary btn-sm d-flex align-items-center" onClick={saveConnections}>
                        <i className="bi bi-save me-2" /> Salvar
                      </button>
                    </div>
                  </div>

                  <div className="mt-2">
                    <details className="border rounded p-2" style={{background:'#f8fafc'}}>
                      <summary className="d-flex align-items-center" style={{gap:8, cursor:'pointer'}}>
                        <i className="bi bi-magic" />
                        <span className="fw-semibold">Assistente de conexão (instância e bases)</span>
                      </summary>
                      <div style={{marginTop:10}}>
                        <div className="text-muted" style={{fontSize:12, marginBottom:8}}>
                          Use quando quiser montar as conexões escolhendo a instância e os bancos.
                        </div>
                        <div className="d-flex flex-wrap align-items-end" style={{gap:10}}>
                      <button
                        type="button"
                        className="btn btn-outline-secondary btn-sm d-flex align-items-center"
                        onClick={loadSqlInstances}
                        disabled={sqlInstancesLoading}
                      >
                        {sqlInstancesLoading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Buscando...</> : <><i className="bi bi-search me-1" /> Buscar instâncias</>}
                      </button>
                      <div style={{minWidth:260, flex:1}}>
                        <label className="form-label" style={{fontSize:12, marginBottom:4}}>Instância SQL</label>
                        <input
                          className="form-control form-control-sm"
                          list="sqlInstancesDatalist"
                          placeholder="Ex: .\\SQLEXPRESS ou SERVIDOR\\INSTANCIA"
                          value={sqlInstance}
                          onChange={e=> { setSqlInstance(e.target.value); setSqlDatabases([]); setSqlTables({}); }}
                        />
                        <datalist id="sqlInstancesDatalist">
                          {sqlInstances.map((it, idx) => (
                            <option key={(it.dataSource || '') + '_' + idx} value={it.dataSource}>
                              {it.dataSource}{it.version ? ` (v${it.version})` : ''}
                            </option>
                          ))}
                        </datalist>
                      </div>
                      <button
                        type="button"
                        className="btn btn-outline-primary btn-sm d-flex align-items-center"
                        onClick={loadSqlDatabases}
                        disabled={!sqlInstance || sqlDatabasesLoading}
                      >
                        {sqlDatabasesLoading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Carregando...</> : <><i className="bi bi-database me-1" /> Carregar bases</>}
                      </button>
                    </div>
                    {sqlDiscoverErr && (
                      <div className="alert alert-warning py-2 mt-2 mb-0">
                        {sqlDiscoverErr}
                      </div>
                    )}
                    {sqlDatabases.length > 0 && (
                      <>
                        <div className="row g-2" style={{marginTop:10}}>
                          <div className="col-md-4">
                            <label className="form-label" style={{fontSize:12, marginBottom:4}}>Base CMS</label>
                            <select className="form-select form-select-sm" value={sqlDbCms} onChange={e=> { setSqlDbCms(e.target.value); }}>
                              {sqlDatabases.map(db => <option key={db} value={db}>{db}</option>)}
                            </select>
                            <div style={{marginTop:6}}>
                              <button type="button" className="btn btn-sm btn-outline-secondary" onClick={()=> loadSqlTablesPreview(sqlDbCms)} disabled={!!sqlTablesLoading[sqlDbCms]}>
                                {sqlTablesLoading[sqlDbCms] ? 'Carregando...' : 'Ver tabelas (informativo)'}
                              </button>
                            </div>
                            {sqlTables[sqlDbCms] && (
                              <div className="mt-2" style={{maxHeight:120, overflowY:'auto', fontSize:12, border:'1px solid #e5e7eb', borderRadius:6, padding:'6px 8px', background:'#ffffff'}}>
                                {sqlTables[sqlDbCms].slice(0, 30).map((t, i) => <div key={i}>{t}</div>)}
                              </div>
                            )}
                          </div>
                          <div className="col-md-4">
                            <label className="form-label" style={{fontSize:12, marginBottom:4}}>Base Logins</label>
                            <select className="form-select form-select-sm" value={sqlDbLogins} onChange={e=> { setSqlDbLogins(e.target.value); }}>
                              {sqlDatabases.map(db => <option key={db} value={db}>{db}</option>)}
                            </select>
                            <div style={{marginTop:6}}>
                              <button type="button" className="btn btn-sm btn-outline-secondary" onClick={()=> loadSqlTablesPreview(sqlDbLogins)} disabled={!!sqlTablesLoading[sqlDbLogins]}>
                                {sqlTablesLoading[sqlDbLogins] ? 'Carregando...' : 'Ver tabelas (informativo)'}
                              </button>
                            </div>
                            {sqlTables[sqlDbLogins] && (
                              <div className="mt-2" style={{maxHeight:120, overflowY:'auto', fontSize:12, border:'1px solid #e5e7eb', borderRadius:6, padding:'6px 8px', background:'#ffffff'}}>
                                {sqlTables[sqlDbLogins].slice(0, 30).map((t, i) => <div key={i}>{t}</div>)}
                              </div>
                            )}
                          </div>
                          <div className="col-md-4">
                            <label className="form-label" style={{fontSize:12, marginBottom:4}}>Base EMS</label>
                            <select className="form-select form-select-sm" value={sqlDbEms} onChange={e=> { setSqlDbEms(e.target.value); }}>
                              {sqlDatabases.map(db => <option key={db} value={db}>{db}</option>)}
                            </select>
                            <div style={{marginTop:6}}>
                              <button type="button" className="btn btn-sm btn-outline-secondary" onClick={()=> loadSqlTablesPreview(sqlDbEms)} disabled={!!sqlTablesLoading[sqlDbEms]}>
                                {sqlTablesLoading[sqlDbEms] ? 'Carregando...' : 'Ver tabelas (informativo)'}
                              </button>
                            </div>
                            {sqlTables[sqlDbEms] && (
                              <div className="mt-2" style={{maxHeight:120, overflowY:'auto', fontSize:12, border:'1px solid #e5e7eb', borderRadius:6, padding:'6px 8px', background:'#ffffff'}}>
                                {sqlTables[sqlDbEms].slice(0, 30).map((t, i) => <div key={i}>{t}</div>)}
                              </div>
                            )}
                          </div>
                        </div>
                        <div className="d-flex justify-content-end" style={{marginTop:10}}>
                          <button
                            type="button"
                            className="btn btn-primary btn-sm d-flex align-items-center"
                            onClick={applySqlWizardConnections}
                            disabled={sqlApplyLoading || !sqlInstance}
                          >
                            {sqlApplyLoading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Aplicando...</> : <><i className="bi bi-check2-circle me-1" /> Aplicar conexões</>}
                          </button>
                        </div>
                      </>
                    )}
                  </div>
                    </details>
                  </div>

                  <div className="col-12">
                    <details className="border rounded p-2 mt-2">
                      <summary className="d-flex align-items-center" style={{gap:8, cursor:'pointer'}}>
                        <i className="bi bi-diagram-3" />
                        <span className="fw-semibold">Mapeamento de Objetos</span>
                      </summary>
                      <div style={{marginTop:10}}>
                        <div className="alert alert-warning py-2" style={{marginBottom:10}}>
                          Use esta área quando a base local tiver o mesmo conteúdo esperado pelo sistema, mas com nomes diferentes de tabelas, views, funções ou procedures. Se deixar em branco, o sistema volta ao nome padrão.
                        </div>
                        {dbObjectMapLoading && dbObjectMapItems.length === 0 ? (
                          <div className="text-muted" style={{fontSize:12}}>Carregando mapeamentos...</div>
                        ) : (
                          <div className="table-responsive">
                            <table className="table table-sm align-middle">
                              <thead>
                                <tr>
                                  <th>Conexão</th>
                                  <th>Objeto esperado</th>
                                  <th>Nome local</th>
                                </tr>
                              </thead>
                              <tbody>
                                {dbObjectMapItems.map((item) => (
                                  <tr key={item.key}>
                                    <td><span className="badge text-bg-secondary">{item.connection}</span></td>
                                    <td>
                                      <div className="fw-semibold">{item.label}</div>
                                      <div className="text-muted" style={{fontSize:12}}>Padrão: {item.defaultValue}</div>
                                    </td>
                                    <td>
                                      <input
                                        className="form-control form-control-sm"
                                        value={item.value}
                                        placeholder={item.defaultValue}
                                        onChange={e => setDbObjectMapItems(prev => prev.map(x => x.key === item.key ? { ...x, value: e.target.value } : x))}
                                      />
                                    </td>
                                  </tr>
                                ))}
                                {dbObjectMapItems.length === 0 && (
                                  <tr>
                                    <td colSpan={3} className="text-muted">Nenhum mapeamento disponível.</td>
                                  </tr>
                                )}
                              </tbody>
                            </table>
                          </div>
                        )}
                        <div className="d-flex justify-content-end" style={{marginTop:10}}>
                          <button type="button" className="btn btn-outline-primary btn-sm" onClick={saveDbObjectMap} disabled={dbObjectMapLoading || dbObjectMapItems.length === 0}>
                            {dbObjectMapLoading ? 'Salvando...' : 'Salvar mapeamento'}
                          </button>
                        </div>
                      </div>
                    </details>
                  </div>

                  <details className="border rounded p-2 mt-2">
                    <summary className="d-flex align-items-center" style={{gap:8, cursor:'pointer'}}>
                      <i className="bi bi-shield-check" />
                      <span className="fw-semibold">Diagnóstico</span>
                    </summary>
                    <div style={{marginTop:10}}>
                      <div className="alert alert-info d-flex align-items-center py-2" style={{gap:8, marginBottom:10}}>
                        <i className="bi bi-person-badge" />
                        <div>
                          <div><strong>Identidade do servidor:</strong> {dbInfo?.identity || 'desconhecida'}</div>
                          <div className="text-muted" style={{fontSize:12}}>Em Windows Authentication, este usuário precisa de permissão nas bases.</div>
                        </div>
                      </div>
                      <div className="row g-2" style={{fontSize:12}}>
                        <div className="col-12 col-md-4">CMS: {authStatus?.CMS?.ok ? (`OK (${authStatus?.CMS?.user||''})`) : (`Falha: ${authStatus?.CMS?.error||''}`)}</div>
                        <div className="col-12 col-md-4">Logins: {authStatus?.Logins?.ok ? (`OK (${authStatus?.Logins?.user||''})`) : (`Falha: ${authStatus?.Logins?.error||''}`)}</div>
                        <div className="col-12 col-md-4">EMS: {authStatus?.EMS?.ok ? (`OK (${authStatus?.EMS?.user||''})`) : (`Falha: ${authStatus?.EMS?.error||''}`)}</div>
                        <div className="col-12 col-md-4">HWR: {authStatus?.HWR?.ok ? (`OK (${authStatus?.HWR?.user||''})`) : (`Falha: ${authStatus?.HWR?.error||''}`)}</div>
                        <div className="col-12 col-md-4">CLAV: {authStatus?.CLAV?.ok ? (`OK (${authStatus?.CLAV?.user||''})`) : (`Falha: ${authStatus?.CLAV?.error||''}`)}</div>
                        <div className="col-12 col-md-4">Modo do servidor: {authMode?.mode || 'desconhecido'}</div>
                        <div className="col-12 col-md-8">
                          Teste login (master): {loginOnlyStatus?.skipped ? (`N/A: ${loginOnlyStatus?.reason||''}`) : (loginOnlyStatus?.ok ? (`OK (${loginOnlyStatus?.user||''})`) : (`Falha: ${loginOnlyStatus?.error||''}`))}
                        </div>
                      </div>
                    </div>
                  </details>
                </>
              )}
            </div>
          </div>
        </div>

        <div style={{flex:'0 1 520px', minWidth:320}}>
          <div className="card h-100">
            <div className="card-header d-flex align-items-center" style={{gap:8}}>
              <i className="bi bi-activity" /> Status das Conexões
            </div>
            <div className="card-body p-3">
              {!dbInfo && !dbInfoErr && (
                <div className="text-muted" style={{fontSize:12}}>Carregando informações do banco...</div>
              )}
              {dbInfoErr && (
                <div className="alert alert-warning d-flex align-items-center py-2" style={{gap:8, marginBottom:0}}>
                  <i className="bi bi-exclamation-triangle" /> {dbInfoErr}
                </div>
              )}
              {dbInfo && (
                <>
                  <div className="text-muted" style={{fontSize:12, marginBottom:8}}>
                    Modo atual: <strong>{dbInfo.mode || mode}</strong>
                  </div>

                  {!!dbInfo.error && (
                    <div className="alert alert-warning d-flex align-items-center py-2" style={{gap:8, marginBottom:10}}>
                      <i className="bi bi-exclamation-triangle" /> {String(dbInfo.error)}
                    </div>
                  )}

                  <div className="table-responsive">
                    <table className="table table-sm table-dark table-striped table-hover align-middle mb-0">
                      <thead>
                        <tr>
                          <th>Base</th>
                          <th>Status</th>
                          <th>Servidor</th>
                          <th>Catalog</th>
                          <th style={{width:110}}>Tabelas</th>
                          <th style={{width:120}}>Procedures</th>
                        </tr>
                      </thead>
                      <tbody>
                        {[
                          { label: 'CMS', db: dbCms },
                          { label: 'Logins', db: dbLogins },
                          { label: 'EMS', db: dbEms },
                          { label: 'HWR', db: dbHwr },
                          { label: 'CLAV', db: dbClav }
                        ].map((it) => (
                          <tr key={it.label}>
                            <td className="fw-semibold">{it.label}</td>
                            <td>
                              {(() => {
                                const st = (authStatus as any)?.[it.label]
                                const ok = (typeof st?.ok === 'boolean') ? st.ok : !!it.db
                                return ok ? <span className="badge text-bg-success">OK</span> : <span className="badge text-bg-danger">Falha</span>
                              })()}
                            </td>
                            <td style={{fontSize:12}}>{it.db?.dataSource || '-'}</td>
                            <td style={{fontSize:12}}>{it.db?.catalog || '-'}</td>
                            <td style={{fontSize:12}}>{it.db ? it.db.tables.length : '-'}</td>
                            <td style={{fontSize:12}}>{it.db ? it.db.procedures.length : '-'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  <details className="border rounded p-2 mt-2">
                    <summary className="d-flex align-items-center justify-content-between" style={{cursor:'pointer'}}>
                      <span className="fw-semibold">Detalhes</span>
                      <span className="text-muted" style={{fontSize:12}}>Connection string e tabelas</span>
                    </summary>
                    <div className="row g-2" style={{marginTop:8}}>
                      {[dbCms, dbLogins, dbEms].filter(Boolean).map((db: any) => (
                        <div className="col-12 col-lg-4" key={db.key}>
                          <div className="border rounded p-2">
                            <div className="d-flex align-items-center justify-content-between">
                              <div className="fw-semibold">{db.key}</div>
                              <div className="text-muted" style={{fontSize:12}}>{db.tables.length} • {db.procedures.length}</div>
                            </div>
                            <div style={{marginTop:6}}>
                              <div className="text-muted" style={{fontSize:12}}>Connection string:</div>
                              <code style={{fontSize:12, wordBreak:'break-all'}}>{db.conn}</code>
                              <div style={{marginTop:6}}>
                                <div className="text-muted" style={{fontSize:12}}>Tabelas (20):</div>
                                <div style={{maxHeight:110, overflowY:'auto', fontSize:12}}>
                                  {db.tables.slice(0,20).map((t: string) => <div key={t}>{t}</div>)}
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </details>
                </>
              )}
            </div>
          </div>
        </div>
      </div>
      {setupModalOpen && (
        <>
          <div className="modal-backdrop show" />
          <div className="modal show" style={{ display: 'block' }} tabIndex={-1} role="dialog" aria-modal="true">
            <div className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable" role="document">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">Testar Configuração de Banco (Modo Instalação)</h5>
                  <button type="button" className="btn-close" aria-label="Close" disabled={setupLoading} onClick={()=> setSetupModalOpen(false)} />
                </div>
                <div className="modal-body">
                  <div className="alert alert-info py-2 mb-2">
                    Selecione instância e bancos. A lista de tabelas é apenas informativa. Ao confirmar, as conexões serão aplicadas e, se não existir nenhum usuário, será criado o primeiro SuperAdmin.
                  </div>
                  <div style={{display:'flex', flexWrap:'wrap', gap:12, alignItems:'flex-start'}}>
                    <div style={{flex:'1 1 520px', minWidth:320}}>
                      <div className="row g-2">
                        <div className="col-12">
                          <label className="form-label" style={{marginBottom:4}}>Instância SQL</label>
                          <div className="d-flex gap-2">
                            <select className="form-select form-select-sm" value={setupDataSource} onChange={e=>{ setSetupDataSource(e.target.value); setSetupDatabases([]); setSetupTest(null) }} disabled={setupLoading}>
                              {setupInstances.length === 0 && <option value="">(Nenhuma instância encontrada)</option>}
                              {setupInstances.map((x, idx)=>(
                                <option key={idx} value={x.dataSource}>{x.dataSource}{x.version ? ` (v${x.version})` : ''}</option>
                              ))}
                            </select>
                            <button type="button" className="btn btn-outline-secondary btn-sm" onClick={loadSetupInstances} disabled={setupLoading}>Atualizar</button>
                          </div>
                        </div>
                        <div className="col-12 col-sm-6">
                          <label className="form-label" style={{marginBottom:4}}>Banco CMS</label>
                          <select className="form-select form-select-sm" value={setupCmsDb} onChange={e=>{ setSetupCmsDb(e.target.value); setSetupTest(null) }} disabled={setupLoading || setupDatabases.length === 0}>
                            <option value="">Selecione...</option>
                            {setupDatabases.map((d)=> <option key={d} value={d}>{d}</option>)}
                          </select>
                        </div>
                        <div className="col-12 col-sm-6">
                          <label className="form-label" style={{marginBottom:4}}>Banco Logins</label>
                          <select className="form-select form-select-sm" value={setupLoginsDb} onChange={e=>{ setSetupLoginsDb(e.target.value); setSetupTest(null) }} disabled={setupLoading || setupDatabases.length === 0}>
                            <option value="">Selecione...</option>
                            {setupDatabases.map((d)=> <option key={d} value={d}>{d}</option>)}
                          </select>
                        </div>
                        <div className="col-12">
                          <label className="form-label" style={{marginBottom:4}}>Banco EMS</label>
                          <select className="form-select form-select-sm" value={setupEmsDb} onChange={e=>{ setSetupEmsDb(e.target.value); setSetupTest(null) }} disabled={setupLoading || setupDatabases.length === 0}>
                            <option value="">Selecione...</option>
                            {setupDatabases.map((d)=> <option key={d} value={d}>{d}</option>)}
                          </select>
                        </div>
                        <div className="col-12 col-sm-6">
                          <label className="form-label" style={{marginBottom:4}}>Banco HWR</label>
                          <select className="form-select form-select-sm" value={setupHwrDb} onChange={e=>{ setSetupHwrDb(e.target.value); setSetupTest(null) }} disabled={setupLoading || setupDatabases.length === 0}>
                            <option value="">Selecione...</option>
                            {setupDatabases.map((d)=> <option key={d} value={d}>{d}</option>)}
                          </select>
                        </div>
                        <div className="col-12 col-sm-6">
                          <label className="form-label" style={{marginBottom:4}}>Banco CLAV</label>
                          <select className="form-select form-select-sm" value={setupClavDb} onChange={e=>{ setSetupClavDb(e.target.value); setSetupTest(null) }} disabled={setupLoading || setupDatabases.length === 0}>
                            <option value="">Selecione...</option>
                            {setupDatabases.map((d)=> <option key={d} value={d}>{d}</option>)}
                          </select>
                        </div>
                        <div className="col-12">
                          <div className="alert alert-secondary py-2 mb-0" style={{fontSize:12}}>
                            Primeiro usuário: será SuperAdmin. Por padrão usa RF_SUPERADMIN_EMAIL e RF_SUPERADMIN_PASSWORD no servidor. Se não estiverem definidos, informe abaixo.
                          </div>
                        </div>
                        <div className="col-12 col-sm-6">
                          <label className="form-label" style={{marginBottom:4}}>Email inicial</label>
                          <input className="form-control form-control-sm" value={setupInitialEmail} onChange={e=>setSetupInitialEmail(e.target.value)} disabled={setupLoading} placeholder="email@exemplo.com" />
                        </div>
                        <div className="col-12 col-sm-6">
                          <label className="form-label" style={{marginBottom:4}}>Senha inicial</label>
                          <input className="form-control form-control-sm" type="password" value={setupInitialPassword} onChange={e=>setSetupInitialPassword(e.target.value)} disabled={setupLoading} placeholder="Senha" />
                        </div>
                        <div className="col-12">
                          <label className="form-label" style={{marginBottom:4}}>Nome</label>
                          <input className="form-control form-control-sm" value={setupInitialName} onChange={e=>setSetupInitialName(e.target.value)} disabled={setupLoading} placeholder="SUPERADMIN" />
                        </div>
                      </div>
                    </div>

                    <div style={{flex:'1 1 520px', minWidth:320}}>
                      <div className="alert alert-light py-2 mb-2">
                        <div className="fw-semibold">Tabelas (informativo)</div>
                        <div className="text-muted" style={{fontSize:12}}>
                          CMS: {setupCmsTables.length ? `${setupCmsTables.length}` : '0'} • Logins: {setupLoginsTables.length ? `${setupLoginsTables.length}` : '0'} • EMS: {setupEmsTables.length ? `${setupEmsTables.length}` : '0'} • HWR: {setupHwrTables.length ? `${setupHwrTables.length}` : '0'} • CLAV: {setupClavTables.length ? `${setupClavTables.length}` : '0'}
                        </div>
                        <div style={{display:'flex', gap:8, flexWrap:'wrap', marginTop:8}}>
                          <div style={{flex:'1 1 180px', minWidth:180}}>
                            <div className="fw-semibold" style={{fontSize:12}}>CMS: {setupCmsDb || '-'}</div>
                            {setupCmsTables.length > 0 ? (
                              <div className="border rounded p-2 mt-1" style={{ maxHeight: 140, overflow: 'auto' }}>
                                {setupCmsTables.map((t)=> <div key={t} className="small">{t}</div>)}
                              </div>
                            ) : (
                              <div className="text-muted" style={{fontSize:12}}>Sem leitura de tabelas</div>
                            )}
                          </div>
                          <div style={{flex:'1 1 180px', minWidth:180}}>
                            <div className="fw-semibold" style={{fontSize:12}}>Logins: {setupLoginsDb || '-'}</div>
                            {setupLoginsTables.length > 0 ? (
                              <div className="border rounded p-2 mt-1" style={{ maxHeight: 140, overflow: 'auto' }}>
                                {setupLoginsTables.map((t)=> <div key={t} className="small">{t}</div>)}
                              </div>
                            ) : (
                              <div className="text-muted" style={{fontSize:12}}>Sem leitura de tabelas</div>
                            )}
                          </div>
                          <div style={{flex:'1 1 180px', minWidth:180}}>
                            <div className="fw-semibold" style={{fontSize:12}}>EMS: {setupEmsDb || '-'}</div>
                            {setupEmsTables.length > 0 ? (
                              <div className="border rounded p-2 mt-1" style={{ maxHeight: 140, overflow: 'auto' }}>
                                {setupEmsTables.map((t)=> <div key={t} className="small">{t}</div>)}
                              </div>
                            ) : (
                              <div className="text-muted" style={{fontSize:12}}>Sem leitura de tabelas</div>
                            )}
                          </div>
                          <div style={{flex:'1 1 180px', minWidth:180}}>
                            <div className="fw-semibold" style={{fontSize:12}}>HWR: {setupHwrDb || '-'}</div>
                            {setupHwrTables.length > 0 ? (
                              <div className="border rounded p-2 mt-1" style={{ maxHeight: 140, overflow: 'auto' }}>
                                {setupHwrTables.map((t)=> <div key={t} className="small">{t}</div>)}
                              </div>
                            ) : (
                              <div className="text-muted" style={{fontSize:12}}>Sem leitura de tabelas</div>
                            )}
                          </div>
                          <div style={{flex:'1 1 180px', minWidth:180}}>
                            <div className="fw-semibold" style={{fontSize:12}}>CLAV: {setupClavDb || '-'}</div>
                            {setupClavTables.length > 0 ? (
                              <div className="border rounded p-2 mt-1" style={{ maxHeight: 140, overflow: 'auto' }}>
                                {setupClavTables.map((t)=> <div key={t} className="small">{t}</div>)}
                              </div>
                            ) : (
                              <div className="text-muted" style={{fontSize:12}}>Sem leitura de tabelas</div>
                            )}
                          </div>
                        </div>
                      </div>

                      {setupTest && (
                        <div className="alert alert-light py-2 mb-2">
                          <div style={{fontSize:12}}>Teste CMS: {setupTest.cms || '-'}</div>
                          <div style={{fontSize:12}}>Teste Logins: {setupTest.logins || '-'}</div>
                          <div style={{fontSize:12}}>Teste EMS: {setupTest.ems || '-'}</div>
                          <div style={{fontSize:12}}>Teste HWR: {setupTest.hwr || '-'}</div>
                          <div style={{fontSize:12}}>Teste CLAV: {setupTest.clav || '-'}</div>
                        </div>
                      )}
                      {setupError && <div className="alert alert-danger py-2 mb-0">{setupError}</div>}
                    </div>
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={()=> setSetupModalOpen(false)} disabled={setupLoading}>Cancelar</button>
                  <button type="button" className="btn btn-outline-primary" onClick={testSetupConnections} disabled={setupLoading}>Testar conexão</button>
                  <button type="button" className="btn btn-dark" onClick={applySetupFromSettings} disabled={setupLoading}>
                    {setupLoading ? 'Aplicando...' : 'Confirmar'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
      </>
      )}
      {tab === 'Telas' && (
      <div className="card" style={{marginTop:16}}>
        <div className="card-header d-flex align-items-center" style={{gap:8}}>
          <i className="bi bi-layout-text-sidebar-reverse" /> Telas do Sistema
        </div>
        <div className="card-body">
          <div className="text-muted" style={{fontSize:12, marginBottom:12}}>
            Se uma tela foi habilitada/desabilitada por um nível superior, somente um nível igual ou superior consegue alterar novamente.
          </div>
          {screensCfgErr && <div className="alert alert-danger py-2">{screensCfgErr}</div>}
          <div className="table-responsive">
            <table className="table table-hover table-striped align-middle rf-table-light">
              <thead>
                <tr>
                  <th style={{width:220}}>Tela</th>
                  <th>Descrição</th>
                  <th style={{width:160}}>Status</th>
                  <th style={{width:170}}>Travado por</th>
                </tr>
              </thead>
              <tbody>
                {screensCatalog.map(it => {
                  const current = screensCfg?.[it.key]
                  const enabled = current ? !!current.enabled : true
                  const lockedBy = (current?.lockedBy || 'SuperAdmin')
                  const lockRank = lockedBy === 'SuperAdmin' ? 2 : lockedBy === 'Administrador' ? 1 : 0
                  const actorRank = isSuperAdmin ? 2 : isAdmin ? 1 : 0
                  const canEdit = actorRank >= lockRank
                  return (
                    <tr key={it.key}>
                      <td><strong>{it.label}</strong><div className="text-muted" style={{fontSize:12}}>{it.key}</div></td>
                      <td>{it.desc}</td>
                      <td>
                        <div className="form-check form-switch d-flex align-items-center gap-2">
                          <input className="form-check-input" type="checkbox" checked={enabled} disabled={!canEdit || screensCfgLoading} onChange={e => {
                            const v = e.target.checked
                            setScreensCfg(prev => ({ ...prev, [it.key]: { enabled: v, lockedBy: prev?.[it.key]?.lockedBy } }))
                          }} />
                          <span>{enabled ? 'Ativa' : 'Desativada'}</span>
                        </div>
                        {!canEdit && (
                          <div className="text-muted" style={{fontSize:12}}>Requer {lockedBy}</div>
                        )}
                      </td>
                      <td>{lockedBy}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <div className="d-flex gap-2">
            <button className="btn btn-outline-primary d-flex align-items-center" onClick={saveScreensConfig} disabled={screensCfgLoading}>
              {screensCfgLoading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Salvando...</> : <><i className="bi bi-save me-1" /> Salvar telas</>}
            </button>
            <button className="btn btn-outline-secondary d-flex align-items-center" onClick={reloadScreensConfig} disabled={screensCfgLoading}>
              <i className="bi bi-arrow-clockwise me-1" /> Recarregar
            </button>
          </div>
        </div>
      </div>
      )}
      {tab === 'Relatorios' && (
      <div className="card" style={{marginTop:16}}>
        <div className="card-header d-flex align-items-center" style={{gap:8}}>
          <i className="bi bi-filetype-pdf" /> Formatos de relatórios disponíveis
        </div>
        <div className="card-body d-flex flex-column" style={{gap:12}}>
          <p className="text-muted" style={{fontSize:12, marginBottom:4}}>
            Apenas um formato pode ficar ativo por vez. Ao selecionar um, os demais serão desativados automaticamente.
          </p>
          <div className="d-flex flex-wrap" style={{gap:12}}>
            <div className="form-check">
              <input className="form-check-input" type="radio" name="reportFormat" id="repXlsx" checked={reportOptions.xlsx} onChange={()=> setExclusiveReportFormat('xlsx')} />
              <label className="form-check-label" htmlFor="repXlsx">XLSX</label>
            </div>
            <div className="form-check">
              <input className="form-check-input" type="radio" name="reportFormat" id="repExcel" checked={reportOptions.excel} onChange={()=> setExclusiveReportFormat('excel')} />
              <label className="form-check-label" htmlFor="repExcel">Excel (compatível)</label>
            </div>
            <div className="form-check">
              <input className="form-check-input" type="radio" name="reportFormat" id="repPdf" checked={reportOptions.pdf} onChange={()=> setExclusiveReportFormat('pdf')} />
              <label className="form-check-label" htmlFor="repPdf">PDF</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repPdfCover" checked={reportOptions.cover} onChange={e=> setReportOptions(o=> ({...o, cover: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repPdfCover">Incluir capa nos PDFs</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repCustomQueries" checked={reportOptions.customQueries} onChange={e=> setReportOptions(o=> ({...o, customQueries: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repCustomQueries">Consultas Personalizadas</label>
            </div>
          </div>
          <div className="row" style={{rowGap:12}}>
            <div className="col-md-6">
              <label className="form-label" htmlFor="repCoverOri">Orientação da capa (PDF)</label>
              <select id="repCoverOri" className="form-select" value={reportOptions.coverOrientation} onChange={e=> setReportOptions(o=> ({...o, coverOrientation: (e.target.value === 'portrait' ? 'portrait' : 'landscape')}))}>
                <option value="landscape">Paisagem</option>
                <option value="portrait">Retrato</option>
              </select>
            </div>
            <div className="col-md-6">
              <label className="form-label" htmlFor="repBodyOri">Orientação do relatório (PDF)</label>
              <select id="repBodyOri" className="form-select" value={reportOptions.reportOrientation} onChange={e=> setReportOptions(o=> ({...o, reportOrientation: (e.target.value === 'portrait' ? 'portrait' : 'landscape')}))}>
                <option value="landscape">Paisagem</option>
                <option value="portrait">Retrato</option>
              </select>
            </div>
          </div>
          <div>
            <button className="btn btn-outline-primary d-flex align-items-center" onClick={saveReportOptions} disabled={reportOptionsLoading}>
              {reportOptionsLoading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Salvando...</> : <><i className="bi bi-save me-1" /> Salvar formatos</>}
            </button>
          </div>
        </div>
      </div>
      )}
    </section>
  )
}
