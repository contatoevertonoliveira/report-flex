import React, { useEffect, useState } from 'react'
import { api, setToken } from '../api'
import { useNavigate } from 'react-router-dom'

const logoFullUrl = new URL('../../img/Jumperfour_logo_branco_adap.png', import.meta.url).href

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [showSetup, setShowSetup] = useState(false)
  const [setupLoading, setSetupLoading] = useState(false)
  const [setupError, setSetupError] = useState<string | null>(null)
  const [instances, setInstances] = useState<Array<{ dataSource: string, version?: string | null }>>([])
  const [dataSource, setDataSource] = useState('')
  const [databases, setDatabases] = useState<string[]>([])
  const [cmsDb, setCmsDb] = useState('')
  const [loginsDb, setLoginsDb] = useState('')
  const [emsDb, setEmsDb] = useState('')
  const [cmsTables, setCmsTables] = useState<string[]>([])
  const [loginsTables, setLoginsTables] = useState<string[]>([])
  const [emsTables, setEmsTables] = useState<string[]>([])
  const [initialEmail, setInitialEmail] = useState('')
  const [initialPassword, setInitialPassword] = useState('')
  const [initialName, setInitialName] = useState('SUPERADMIN')
  const navigate = useNavigate()
  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }
  async function loadInstances(){
    setSetupError(null)
    setSetupLoading(true)
    try{
      const res: any = await api.setupSqlInstances()
      const items = Array.isArray(res?.items) ? res.items : []
      setInstances(items)
      if (!dataSource && items.length){
        const ds = String(items[0]?.dataSource || '').trim()
        if (ds) setDataSource(ds)
      }
    }catch(e: any){
      setSetupError(e?.message || 'Falha ao listar instâncias SQL')
    }finally{
      setSetupLoading(false)
    }
  }
  async function loadDatabases(ds: string){
    setSetupError(null)
    setSetupLoading(true)
    try{
      const res: any = await api.setupSqlDatabases(ds)
      const items = Array.isArray(res?.items) ? res.items.map((x: any)=>String(x)) : []
      setDatabases(items)
      const pick = (preferred: string) => items.find((x: string)=> x.toLowerCase() === preferred.toLowerCase()) || ''
      const cms = pick('CMS') || (items[0] || '')
      const logins = pick('Logins') || pick('Login') || (items[0] || '')
      const ems = pick('EMS') || pick('EMSEVENTS') || ''
      if (!cmsDb) setCmsDb(cms)
      if (!loginsDb) setLoginsDb(logins)
      if (!emsDb) setEmsDb(ems)
    }catch(e: any){
      setSetupError(e?.message || 'Falha ao listar bancos')
      setDatabases([])
    }finally{
      setSetupLoading(false)
    }
  }
  async function loadTables(dbName: string, set: (items: string[])=>void){
    if (!showSetup || !dataSource || !dbName) { set([]); return }
    try{
      const res: any = await api.setupSqlTables(dataSource, dbName)
      const items = Array.isArray(res?.items) ? res.items.map((x: any)=>String(x)) : []
      set(items)
    }catch{
      set([])
    }
  }
  useEffect(()=>{
    if (!showSetup) return
    loadInstances()
  },[showSetup])
  useEffect(()=>{
    if (!showSetup) return
    if (!dataSource) return
    loadDatabases(dataSource)
  },[showSetup, dataSource])
  useEffect(()=>{
    if (!showSetup) return
    if (!dataSource) return
    loadTables(cmsDb, setCmsTables)
  },[showSetup, dataSource, cmsDb])
  useEffect(()=>{
    if (!showSetup) return
    if (!dataSource) return
    loadTables(loginsDb, setLoginsTables)
  },[showSetup, dataSource, loginsDb])
  useEffect(()=>{
    if (!showSetup) return
    if (!dataSource) return
    if (!emsDb) { setEmsTables([]); return }
    loadTables(emsDb, setEmsTables)
  },[showSetup, dataSource, emsDb])

  async function applySetup(){
    if (!dataSource || !cmsDb || !loginsDb || !emsDb){
      setSetupError('Selecione a instância e os bancos (CMS, Logins e EMS).')
      return
    }
    setSetupError(null)
    setSetupLoading(true)
    try{
      const res: any = await api.setupApply({
        dataSource,
        cmsDb,
        loginsDb,
        emsDb,
        initialEmail: (initialEmail || '').trim() || undefined,
        initialPassword: initialPassword || undefined,
        initialName: (initialName || '').trim() || undefined
      })
      if (res?.__ok){
        toast('success', 'Banco configurado. Tente logar novamente.')
        setShowSetup(false)
        setSetupError(null)
        await handleLogin()
      }else{
        const msg = res?.error || res?.detail || 'Falha ao aplicar configuração'
        setSetupError(String(msg))
      }
    }catch(e: any){
      setSetupError(e?.message || 'Falha ao aplicar configuração')
    }finally{
      setSetupLoading(false)
    }
  }
  async function handleLogin(){
    if (!email.trim()){
      setError('Informe o email')
      toast('error', 'Informe o email')
      return
    }
    if (!senha){
      setError('Informe a senha')
      toast('error', 'Informe a senha')
      return
    }
    setLoading(true)
    setError(null)
    try{
      const res = await api.signin(email.trim(), senha)
      if (res?.__status === 409 && (res?.errorCode === 'DB_SETUP_REQUIRED' || String(res?.errorCode || '').startsWith('DB_SETUP'))){
        setInitialEmail(email.trim())
        setInitialPassword(senha)
        setShowSetup(true)
        toast('warning', 'Banco não configurado. Configure o SQL local para continuar.')
        return
      }
      if (res?.token){
        setToken(res.token)
        if (res?.nivel) localStorage.setItem('rf_level', res.nivel)
        if (res?.mustChangePassword) localStorage.setItem('rf_pwd_change_required', '1')
        else localStorage.removeItem('rf_pwd_change_required')
        if (res?.clientId){
          localStorage.setItem('rf_client_id', String(res.clientId))
          if (res?.clientName) localStorage.setItem('rf_client_name', res.clientName)
        } else {
          localStorage.removeItem('rf_client_id')
          localStorage.removeItem('rf_client_name')
        }
        localStorage.setItem('rf_last_activity', Date.now().toString())
        try{
          const u = localStorage.getItem('rf_sql_user') || ''
          const p = localStorage.getItem('rf_sql_pwd') || ''
          if (u && p){
            await api.setSqlAuth({ user: u, pwd: p })
            await api.setSqlAuthRuntime({ user: u, pwd: p })
          }
        }catch{}
        toast('success', 'Login realizado com sucesso')
        if (res?.mustChangePassword) navigate('/alterar-senha')
        else navigate('/consultas')
      } else {
        setError('Credenciais inválidas')
        toast('error', 'Credenciais inválidas')
      }
    }catch{
      setError('Falha de autenticação')
      toast('error', 'Falha de autenticação')
    } finally {
      setLoading(false)
    }
  }
  return (
    <section className="login-split">
      <div className="login-left">
        <div className="login-left-inner">
          <img alt="JumperFour" src={logoFullUrl} className="login-left-logo" />
        </div>
      </div>
      <div className="login-right">
        <div className="login-right-inner">
          <div className="login-panel">
            <div className="login-panel-head">
              <div className="login-panel-title">Acesso</div>
              <div className="login-panel-subtitle">Entre com seu email e senha</div>
            </div>
            <form className="login-form" onSubmit={e=>{ e.preventDefault(); handleLogin() }}>
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-envelope" /></span>
                <input
                  className="form-control"
                  value={email}
                  onChange={e=>setEmail(e.target.value)}
                  placeholder="Email"
                  disabled={loading}
                />
              </div>
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-key" /></span>
                <input
                  className="form-control"
                  type="password"
                  value={senha}
                  onChange={e=>setSenha(e.target.value)}
                  placeholder="Senha"
                  disabled={loading}
                />
              </div>
              <button className="btn btn-dark w-100 d-flex align-items-center justify-content-center" type="submit" disabled={loading}>
                {loading
                  ? (<><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Autenticando...</>)
                  : (<><i className="bi bi-box-arrow-in-right me-2" /> Entrar</>)}
              </button>
            </form>
            {error && <div className="alert alert-danger py-2 mt-3 mb-0">{error}</div>}
          </div>
        </div>
      </div>
      {showSetup && (
        <>
          <div className="modal-backdrop show" />
          <div className="modal show" style={{ display: 'block' }} tabIndex={-1} role="dialog" aria-modal="true">
            <div className="modal-dialog modal-lg modal-dialog-centered" role="document">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">Configurar Banco Local</h5>
                  <button type="button" className="btn-close" aria-label="Close" disabled={setupLoading} onClick={()=> setShowSetup(false)} />
                </div>
                <div className="modal-body">
                  <div className="alert alert-info py-2">
                    O sistema não encontrou o banco configurado. Selecione a instância e os bancos locais. A estrutura necessária de login será criada automaticamente no banco Logins.
                  </div>
                  <div className="row g-3">
                    <div className="col-12">
                      <label className="form-label">Instância SQL</label>
                      <div className="d-flex gap-2">
                        <select className="form-select" value={dataSource} onChange={e=>setDataSource(e.target.value)} disabled={setupLoading}>
                          {instances.length === 0 && <option value="">(Nenhuma instância encontrada)</option>}
                          {instances.map((x, idx)=>(
                            <option key={idx} value={x.dataSource}>{x.dataSource}{x.version ? ` (v${x.version})` : ''}</option>
                          ))}
                        </select>
                        <button type="button" className="btn btn-outline-secondary" onClick={loadInstances} disabled={setupLoading}>Atualizar</button>
                      </div>
                    </div>
                    <div className="col-12 col-md-4">
                      <label className="form-label">Banco CMS</label>
                      <select className="form-select" value={cmsDb} onChange={e=>setCmsDb(e.target.value)} disabled={setupLoading || databases.length === 0}>
                        <option value="">Selecione...</option>
                        {databases.map((d)=> <option key={d} value={d}>{d}</option>)}
                      </select>
                    </div>
                    <div className="col-12 col-md-4">
                      <label className="form-label">Banco Logins</label>
                      <select className="form-select" value={loginsDb} onChange={e=>setLoginsDb(e.target.value)} disabled={setupLoading || databases.length === 0}>
                        <option value="">Selecione...</option>
                        {databases.map((d)=> <option key={d} value={d}>{d}</option>)}
                      </select>
                    </div>
                    <div className="col-12 col-md-4">
                      <label className="form-label">Banco EMS</label>
                      <select className="form-select" value={emsDb} onChange={e=>setEmsDb(e.target.value)} disabled={setupLoading || databases.length === 0}>
                        <option value="">Selecione...</option>
                        {databases.map((d)=> <option key={d} value={d}>{d}</option>)}
                      </select>
                    </div>
                    <div className="col-12">
                      <div className="alert alert-light py-2 mb-0">
                        <div className="fw-semibold mb-2">Tabelas (informativo)</div>
                        <div className="row g-2">
                          <div className="col-12 col-md-4">
                            <div className="fw-semibold">CMS: {cmsDb || '-'}</div>
                            <div className="small text-muted">{cmsTables.length ? `${cmsTables.length} tabelas` : 'Sem leitura de tabelas'}</div>
                            {cmsTables.length > 0 && (
                              <div className="border rounded p-2 mt-1" style={{ maxHeight: 140, overflow: 'auto' }}>
                                {cmsTables.map((t)=> <div key={t} className="small">{t}</div>)}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-4">
                            <div className="fw-semibold">Logins: {loginsDb || '-'}</div>
                            <div className="small text-muted">{loginsTables.length ? `${loginsTables.length} tabelas` : 'Sem leitura de tabelas'}</div>
                            {loginsTables.length > 0 && (
                              <div className="border rounded p-2 mt-1" style={{ maxHeight: 140, overflow: 'auto' }}>
                                {loginsTables.map((t)=> <div key={t} className="small">{t}</div>)}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-4">
                            <div className="fw-semibold">EMS: {emsDb || '-'}</div>
                            <div className="small text-muted">{emsTables.length ? `${emsTables.length} tabelas` : 'Sem leitura de tabelas'}</div>
                            {emsTables.length > 0 && (
                              <div className="border rounded p-2 mt-1" style={{ maxHeight: 140, overflow: 'auto' }}>
                                {emsTables.map((t)=> <div key={t} className="small">{t}</div>)}
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                    <div className="col-12">
                      <div className="alert alert-secondary py-2 mb-0">
                        Login inicial: por padrão usa as variáveis do servidor RF_SUPERADMIN_EMAIL e RF_SUPERADMIN_PASSWORD. Se não estiverem definidas, informe abaixo (opcional).
                      </div>
                    </div>
                    <div className="col-12 col-md-5">
                      <label className="form-label">Email inicial (opcional)</label>
                      <input className="form-control" value={initialEmail} onChange={e=>setInitialEmail(e.target.value)} disabled={setupLoading} placeholder="email@exemplo.com" />
                    </div>
                    <div className="col-12 col-md-4">
                      <label className="form-label">Senha inicial (opcional)</label>
                      <input className="form-control" type="password" value={initialPassword} onChange={e=>setInitialPassword(e.target.value)} disabled={setupLoading} placeholder="Senha" />
                    </div>
                    <div className="col-12 col-md-3">
                      <label className="form-label">Nome (opcional)</label>
                      <input className="form-control" value={initialName} onChange={e=>setInitialName(e.target.value)} disabled={setupLoading} placeholder="SUPERADMIN" />
                    </div>
                  </div>
                  {setupError && <div className="alert alert-danger py-2 mt-3 mb-0">{setupError}</div>}
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={()=> setShowSetup(false)} disabled={setupLoading}>Cancelar</button>
                  <button type="button" className="btn btn-dark" onClick={applySetup} disabled={setupLoading}>
                    {setupLoading ? 'Aplicando...' : 'Confirmar'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </section>
  )
}
