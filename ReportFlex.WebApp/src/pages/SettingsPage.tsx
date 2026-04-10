import React, { useEffect, useState } from 'react'
import { api } from '../api'

export function SettingsPage(){
  const [tab, setTab] = useState<'Relatorios'|'Banco'|'Telas'>('Relatorios')
  const [mode, setMode] = useState<'Real'|'Demo'>('Demo')
  const [loading, setLoading] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [seedCount, setSeedCount] = useState(100)
  const [realPath, setRealPath] = useState('')
  const [emsPath, setEmsPath] = useState('')
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
  const [screensCfg, setScreensCfg] = useState<Record<string, { enabled: boolean, lockedBy?: string }>>({})
  const [screensCfgLoading, setScreensCfgLoading] = useState(false)
  const [screensCfgErr, setScreensCfgErr] = useState<string | null>(null)
  const nivel = (typeof window !== 'undefined' ? (localStorage.getItem('rf_level') || '') : '')
  const isSuperAdmin = nivel === 'SuperAdmin'
  const isAdmin = nivel === 'Administrador'

  useEffect(() => {
    (async () => {
      try{
        const r = await api.getDbMode()
        if (r?.mode === 'Demo' || r?.mode === 'Real'){ setMode(r.mode) }
        const c = await api.getConnections()
        if (c?.CMS){
          setRealPath(c.CMS)
        }
        if (c?.EMS){
          setEmsPath(c.EMS)
        }
        if (!c?.CMS) {
          setRealPath('Data Source=JP4REPORTDEV01;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
        }
        if (!c?.EMS) {
          setEmsPath('Data Source=JP4REPORTDEV01;Initial Catalog=hwreportsview;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
        }
        // Aplicar automaticamente JP4REPORTDEV01 como padrão e modo Real se ainda não houver configuração persistida
        try{
          if (r?.mode !== 'Real' || !c?.CMS) {
            await api.setDbMode('Real')
            setMode('Real')
          }
          if (!c?.CMS) {
            await saveRealPath()
          }
        }catch{}
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
          setReportOptions({
            xlsx: !!opts.xlsx,
            pdf: !!opts.pdf,
            excel: !!opts.excel,
            cover: !!opts.cover,
            coverOrientation: (opts.coverOrientation === 'portrait' ? 'portrait' : 'landscape'),
            reportOrientation: (opts.reportOrientation === 'portrait' ? 'portrait' : 'landscape'),
            customQueries: !!opts.customQueries
          })
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
        }catch(e:any){
          setScreensCfgErr(e?.message || 'Falha ao carregar configuração de telas')
        }finally{
          setScreensCfgLoading(false)
        }
      }catch{}
    })()
  }, [])

  async function applyRecommended(){
    setErr(null); setMsg(null); setDbInfoErr(null)
    try{
      setRealPath('Data Source=JP4REPORTDEV01;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
      setEmsPath('Data Source=JP4REPORTDEV01;Initial Catalog=EMSEVENTS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
      await saveRealPath()
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
            setRealPath(c.CMS)
          }
          if (c?.EMS) {
            setEmsPath(c.EMS)
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

  async function saveRealPath(){
    setErr(null); setMsg(null); setDbInfoErr(null)
    try{
      let cms = realPath
      let logins = realPath
      let ems = emsPath
      const hasCatalog = /Initial\s+Catalog\s*=/i.test(realPath) || /Database\s*=/i.test(realPath)
      if (!hasCatalog){
        const base = realPath.endsWith(';') ? realPath : realPath + ';'
        cms = base + 'Initial Catalog=CMS'
        logins = base + 'Initial Catalog=Logins'
        if (!ems) ems = base + 'Initial Catalog=hwreportsview'
      }else{
        cms = realPath.replace(/(Initial\s+Catalog|Database)\s*=\s*Logins/i, '$1=CMS')
        logins = realPath.replace(/(Initial\s+Catalog|Database)\s*=\s*CMS/i, '$1=Logins')
        if (!ems) ems = realPath.replace(/(Initial\s+Catalog|Database)\s*=\s*[^;]+/i, '$1=hwreportsview')
      }
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
      if (useSqlAuth && sqlUser){
        await api.setSqlAuth({ user: sqlUser, pwd: sqlPwd })
      }
      cms = ensureTls(useSqlAuth ? applySqlAuth(cms) : cms)
      logins = ensureTls(useSqlAuth ? applySqlAuth(logins) : logins)
      ems = ensureTls(useSqlAuth ? applySqlAuth(ems) : ems)
      const r = await api.setConnections({ CMS: cms, Logins: logins, EMS: ems })
      if (r?.CMS){
        setRealPath(r.CMS)
      }
      if (r?.EMS){
        setEmsPath(r.EMS)
      }
      setMsg('Configuração de conexão salva')
      try{
        const info = await api.getDbInfo()
        setDbInfo(info)
        setDbInfoErr(null)
      }catch{
        setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
      }
    }catch{
      setErr('Falha ao salvar configuração')
    }
  }

  async function saveReportOptions(){
    setErr(null); setMsg(null)
    setReportOptionsLoading(true)
    try{
      const r = await api.setReportOptions(reportOptions as any)
      setReportOptions({
        xlsx: !!r.xlsx,
        pdf: !!r.pdf,
        excel: !!r.excel,
        cover: !!r.cover,
        coverOrientation: (r.coverOrientation === 'portrait' ? 'portrait' : 'landscape'),
        reportOrientation: (r.reportOrientation === 'portrait' ? 'portrait' : 'landscape'),
        customQueries: !!r.customQueries
      })
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
        localStorage.setItem('rf_screens_config_ts', String(Date.now()))
        window.dispatchEvent(new Event('rf:screens-config'))
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
      <div className="card">
        <div className="card-header d-flex align-items-center" style={{gap:8}}>
          <i className="bi bi-gear" /> Preferências do Banco de Dados
        </div>
        <div className="card-body d-flex flex-column" style={{gap:12}}>
          <div className="d-flex align-items-center flex-wrap" style={{gap:12}}>
            <div className="form-check">
              <input className="form-check-input" type="radio" name="dbmode" id="dbReal" checked={mode==='Real'} onChange={()=> applyMode('Real')} />
              <label className="form-check-label" htmlFor="dbReal">Banco Real</label>
            </div>
            <div className="form-check">
              <input className="form-check-input" type="radio" name="dbmode" id="dbDemo" checked={mode==='Demo'} onChange={()=> applyMode('Demo')} />
              <label className="form-check-label" htmlFor="dbDemo">Banco Demo (Teste)</label>
            </div>
          </div>
          {mode === 'Real' && (
            <>
              <div className="alert alert-info d-flex align-items-center" style={{gap:8}}>
                <i className="bi bi-person-badge" />
                <div>
                  <div><strong>Identidade do servidor:</strong> {dbInfo?.identity || 'desconhecida'}</div>
                  <div className="text-muted" style={{fontSize:12}}>Para Windows Authentication, este usuário deve ter permissão nas bases.</div>
                </div>
              </div>
              <div className="d-flex align-items-end flex-wrap" style={{gap:12}}>
                <div className="input-group" style={{minWidth:420}}>
                  <span className="input-group-text"><i className="bi bi-hdd-network" /></span>
                  <input className="form-control" placeholder="Caminho/Connection String do SQL Server (Real)" value={realPath} onChange={e=> setRealPath(e.target.value)} />
                </div>
                <button className="btn btn-outline-success d-flex align-items-center" onClick={saveRealPath}>
                  <i className="bi bi-save me-1" /> Salvar configuração
                </button>
                <button className="btn btn-outline-primary d-flex align-items-center" onClick={applyRecommended}>
                  <i className="bi bi-plug me-1" /> Usar JP4REPORTDEV01 (Windows Auth)
                </button>
              </div>
              <div className="d-flex align-items-end flex-wrap" style={{gap:12}}>
                <div className="form-check form-switch">
                  <input className="form-check-input" type="checkbox" id="switchSqlAuth" checked={useSqlAuth} onChange={()=> setUseSqlAuth(!useSqlAuth)} />
                  <label className="form-check-label" htmlFor="switchSqlAuth">Usar Autenticação SQL (desativa Windows Auth)</label>
                </div>
                {useSqlAuth && (
                  <>
                    <div className="input-group" style={{minWidth:220}}>
                      <span className="input-group-text"><i className="bi bi-person" /></span>
                      <input className="form-control" placeholder="Usuário SQL" value={sqlUser} onChange={e=> setSqlUser(e.target.value)} />
                    </div>
                    <div className="input-group" style={{minWidth:220}}>
                      <span className="input-group-text"><i className="bi bi-key" /></span>
                      <input className="form-control" type="password" placeholder="Senha SQL" value={sqlPwd} onChange={e=> setSqlPwd(e.target.value)} />
                    </div>
                  </>
                )}
              </div>
              <div className="row mt-2">
                <div className="col-md-6">
                  <h6>Logins SQL do servidor</h6>
                  <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                    {sqlLogins.slice(0,50).map(n=> <li key={n}>{n}</li>)}
                  </ul>
                </div>
                <div className="col-md-6">
                  <h6>Status de autenticação atual</h6>
                  <div className="d-flex flex-column" style={{fontSize:12}}>
                    <div>CMS: {authStatus?.CMS?.ok ? (`OK (${authStatus?.CMS?.user||''})`) : (`Falha: ${authStatus?.CMS?.error||''}`)}</div>
                    <div>Logins: {authStatus?.Logins?.ok ? (`OK (${authStatus?.Logins?.user||''})`) : (`Falha: ${authStatus?.Logins?.error||''}`)}</div>
                    <div>EMS: {authStatus?.EMS?.ok ? (`OK (${authStatus?.EMS?.user||''})`) : (`Falha: ${authStatus?.EMS?.error||''}`)}</div>
                    <div>Modo do servidor: {authMode?.mode || 'desconhecido'}</div>
                    <div>Teste login (master): {loginOnlyStatus?.ok ? (`OK (${loginOnlyStatus?.user||''})`) : (`Falha: ${loginOnlyStatus?.error||''}`)}</div>
                  </div>
                </div>
              </div>
              <div className="d-flex align-items-end flex-wrap" style={{gap:12}}>
                <div className="input-group" style={{minWidth:420}}>
                  <span className="input-group-text"><i className="bi bi-hdd-network" /></span>
                  <input className="form-control" placeholder="(opcional) Connection string específica para EMSEVENTS" value={emsPath} onChange={e=> setEmsPath(e.target.value)} />
                </div>
                <button className="btn btn-outline-success d-flex align-items-center" onClick={saveRealPath}>
                  <i className="bi bi-save me-1" /> Salvar configuração
                </button>
              </div>
            </>
          )}
          {mode === 'Demo' && (
            <div className="d-flex align-items-end flex-wrap" style={{gap:12}}>
              <div className="input-group" style={{width:180}}>
                <span className="input-group-text"><i className="bi bi-123" /></span>
                <input type="number" min={1} max={1000} className="form-control" value={seedCount} onChange={e=> setSeedCount(Math.max(1, Math.min(1000, parseInt(e.target.value || '0', 10))))} />
              </div>
              <button className="btn btn-outline-primary d-flex align-items-center" onClick={seed} disabled={loading}>
                {loading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Adicionando...</> : <><i className="bi bi-database-add me-1" /> Adicionar dados fictícios</>}
              </button>
              <div className="text-muted" style={{fontSize:12}}>Adiciona registros de teste nas principais tabelas</div>
            </div>
          )}
          <div className="d-flex align-items-end flex-wrap" style={{gap:12}}>
            <button className="btn btn-outline-secondary d-flex align-items-center" onClick={seedCompanies} disabled={loading}>
              {loading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Gerando...</> : <><i className="bi bi-people-fill me-1" /> Gerar empresas/funcionários (últimos 30 dias)</>}
            </button>
            <div className="text-muted" style={{fontSize:12}}>Cria empresas solicitadas, 20 funcionários e acessos em dias úteis</div>
          </div>
        </div>
      </div>
      <div className="card" style={{marginTop:16}}>
        <div className="card-header d-flex align-items-center" style={{gap:8}}>
          <i className="bi bi-database-gear" /> Banco de dados em uso
        </div>
        <div className="card-body">
          {dbInfo && (
            <>
              <p className="text-muted" style={{fontSize:12, marginBottom:8}}>
                Estas informações mostram qual modo está ativo e para quais bancos (Logins e CMS) as conexões estão apontando, incluindo uma lista resumida de tabelas encontradas em cada base.
              </p>
              <div className="mb-2">
                <strong>Modo atual:</strong> {dbInfo.mode || mode}
              </div>
              <div className="row">
                <div className="col-md-4">
                  <h6>Base CMS</h6>
                  {dbInfo.databases?.CMS ? (
                    <>
                      <div style={{wordBreak:'break-all'}}>
                        <span className="text-muted" style={{fontSize:12}}>Connection String efetiva:</span><br/>
                        <code style={{fontSize:12}}>{dbInfo.databases.CMS.connection}</code>
                      </div>
                      <div className="mt-2">
                        <span className="text-muted" style={{fontSize:12}}>Tabelas detectadas (primeiras 20):</span>
                        <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                          {(dbInfo.databases.CMS.tables || []).slice(0,20).map((t:string)=>(
                            <li key={t}>{t}</li>
                          ))}
                        </ul>
                      </div>
                      <div className="mt-2">
                        <span className="text-muted" style={{fontSize:12}}>Procedures detectadas (primeiras 20):</span>
                        <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                          {(dbInfo.databases.CMS.procedures || []).slice(0,20).map((p:string)=>(
                            <li key={p}>{p}</li>
                          ))}
                        </ul>
                      </div>
                    </>
                  ) : (
                    <div className="text-muted" style={{fontSize:12}}>Não foi possível conectar na base CMS com as configurações atuais.</div>
                  )}
                </div>
                <div className="col-md-4">
                  <h6>Base Logins</h6>
                  {dbInfo.databases?.Logins ? (
                    <>
                      <div style={{wordBreak:'break-all'}}>
                        <span className="text-muted" style={{fontSize:12}}>Connection String efetiva:</span><br/>
                        <code style={{fontSize:12}}>{dbInfo.databases.Logins.connection}</code>
                      </div>
                      <div className="mt-2">
                        <span className="text-muted" style={{fontSize:12}}>Tabelas detectadas (primeiras 20):</span>
                        <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                          {(dbInfo.databases.Logins.tables || []).slice(0,20).map((t:string)=>(
                            <li key={t}>{t}</li>
                          ))}
                        </ul>
                      </div>
                      <div className="mt-2">
                        <span className="text-muted" style={{fontSize:12}}>Procedures detectadas (primeiras 20):</span>
                        <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                          {(dbInfo.databases.Logins.procedures || []).slice(0,20).map((p:string)=>(
                            <li key={p}>{p}</li>
                          ))}
                        </ul>
                      </div>
                    </>
                  ) : (
                    <div className="text-muted" style={{fontSize:12}}>Não foi possível conectar na base Logins com as configurações atuais.</div>
                  )}
                </div>
                <div className="col-md-4">
                  <h6>Base EMSEVENTS</h6>
                  {dbInfo.databases?.EMS ? (
                    <>
                      <div style={{wordBreak:'break-all'}}>
                        <span className="text-muted" style={{fontSize:12}}>Connection String efetiva:</span><br/>
                        <code style={{fontSize:12}}>{dbInfo.databases.EMS.connection}</code>
                      </div>
                      <div className="mt-2">
                        <span className="text-muted" style={{fontSize:12}}>Tabelas detectadas (primeiras 20):</span>
                        <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                          {(dbInfo.databases.EMS.tables || []).slice(0,20).map((t:string)=>(
                            <li key={t}>{t}</li>
                          ))}
                        </ul>
                      </div>
                      <div className="mt-2">
                        <span className="text-muted" style={{fontSize:12}}>Procedures detectadas (primeiras 20):</span>
                        <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                          {(dbInfo.databases.EMS.procedures || []).slice(0,20).map((p:string)=>(
                            <li key={p}>{p}</li>
                          ))}
                        </ul>
                      </div>
                    </>
                  ) : (
                    <div className="text-muted" style={{fontSize:12}}>Não foi possível conectar na base EMSEvents com as configurações atuais.</div>
                  )}
                </div>
              </div>
            </>
          )}
          {!dbInfo && !dbInfoErr && (
            <div className="text-muted" style={{fontSize:12}}>Carregando informações do banco...</div>
          )}
          {dbInfoErr && (
            <div className="alert alert-warning d-flex align-items-center mt-2" style={{gap:8}}>
              <i className="bi bi-exclamation-triangle" /> {dbInfoErr}
            </div>
          )}
        </div>
      </div>
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
            Os formatos ativados aqui aparecerão como botões de sugestão quando uma consulta retornar dados.
          </p>
          <div className="d-flex flex-wrap" style={{gap:12}}>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repXlsx" checked={reportOptions.xlsx} onChange={e=> setReportOptions(o=> ({...o, xlsx: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repXlsx">XLSX</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repExcel" checked={reportOptions.excel} onChange={e=> setReportOptions(o=> ({...o, excel: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repExcel">Excel (compatível)</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repPdf" checked={reportOptions.pdf} onChange={e=> setReportOptions(o=> ({...o, pdf: e.target.checked}))} />
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
