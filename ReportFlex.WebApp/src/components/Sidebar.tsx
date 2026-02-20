import React from 'react'
import { api } from '../api'
import { useLocation } from 'react-router-dom'

export function Sidebar({ expanded, onToggle }: { expanded: boolean, onToggle: ()=>void }) {
  const level = typeof window !== 'undefined' ? localStorage.getItem('rf_level') : null
  const isSuperAdmin = level === 'SuperAdmin'
  const isClient = level === 'Cliente'
  const [clientName, setClientName] = React.useState<string | null>(null)
  const [clientResp, setClientResp] = React.useState<string | null>(null)
  const [clientLogo, setClientLogo] = React.useState<string | null>(null)
  const [clientToken, setClientToken] = React.useState<string | null>(null)
  const [clientTokenMasked, setClientTokenMasked] = React.useState<string | null>(null)
  const [clientTokenVisible, setClientTokenVisible] = React.useState(false)
  const tokenTimerRef = React.useRef<number | null>(null)
  const [profileOpen, setProfileOpen] = React.useState(false)
  const [profileForm, setProfileForm] = React.useState<{ nome?: string, responsavel?: string, endereco?: string, fone?: string, email?: string, site?: string }>({})
  const [profileSaving, setProfileSaving] = React.useState(false)
  const fileRef = React.useRef<HTMLInputElement | null>(null)
  const [isNarrow, setIsNarrow] = React.useState<boolean>(() => typeof window !== 'undefined' ? window.innerWidth <= 768 : false)
  const location = useLocation()
  React.useEffect(() => {
    api.currentClientInfo().then(async info => {
      if (info && info.id){
        setClientName(info.nome || null)
        setClientResp(info.responsavel || null)
        setClientLogo(info.logoPath || null)
        setProfileForm({
          nome: info.nome || '',
          responsavel: info.responsavel || '',
          endereco: info.endereco || '',
          fone: info.fone || '',
          email: info.email || '',
          site: info.site || ''
        })
        const tok = (info as any)?.clientToken || null
        setClientToken(tok)
        if (tok){
          const visible = String(tok).length >= 4 ? String(tok).substring(String(tok).length - 4) : String(tok)
          const mask = new Array(Math.max(0, String(tok).length - visible.length)).fill('*').join('') + visible
          setClientTokenMasked(mask)
        } else {
          setClientTokenMasked(null)
        }
      }
    }).catch(()=>{})
  }, [])
  React.useEffect(() => {
    return () => {
      if (tokenTimerRef.current != null){
        window.clearTimeout(tokenTimerRef.current)
        tokenTimerRef.current = null
      }
    }
  }, [])
  React.useEffect(() => {
    if (typeof window === 'undefined') return
    const handler = () => {
      setIsNarrow(window.innerWidth <= 768)
    }
    handler()
    window.addEventListener('resize', handler)
    return () => window.removeEventListener('resize', handler)
  }, [])
  const links = [
    { label: 'Consultas', href: '/consultas', icon: 'bi-search' },
    { label: 'Mensagens', href: '/mensagens', icon: 'bi-chat-dots' },
    ...(isClient ? [] : [{ label: 'Configurações', href: '/configuracoes', icon: 'bi-gear' }]),
    ...(isSuperAdmin ? [{ label: 'Clientes', href: '/clientes', icon: 'bi-people' }] : []),
    ...(isSuperAdmin ? [{ label: 'Inbox', href: '/inbox', icon: 'bi-inbox' }] : [])
  ]
  function handleSwitchUser(){
    window.location.href = '/login'
  }
  function handleLogout(){
    import('../api').then(m=> m.logout())
    window.location.href = '/login'
  }
  const compact = isNarrow ? true : !expanded
  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    if (typeof window === 'undefined') return
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }
  async function handleProfileSave(){
    if (!isClient) return
    setProfileSaving(true)
    try{
      await api.clientUpdateProfile(profileForm)
      toast('success','Dados do perfil atualizados')
      const info = await api.currentClientInfo()
      if (info && info.id){
        setClientName(info.nome || null)
        setClientResp(info.responsavel || null)
        setClientLogo(info.logoPath || null)
      }
      setProfileOpen(false)
    }catch(e:any){
      toast('error', e?.message || 'Falha ao salvar perfil')
    }finally{
      setProfileSaving(false)
    }
  }
  async function handleRegenerateToken(){
    if (!isClient) return
    try{
      const r = await api.clientRegenerateToken()
      if (r?.token){
        const token = String(r.token)
        const visible = token.length >= 4 ? token.substring(token.length - 4) : token
        const mask = new Array(Math.max(0, token.length - visible.length)).fill('*').join('') + visible
        setClientToken(token)
        setClientTokenMasked(mask)
        toast('success','Novo token gerado')
      }
    }catch(e:any){
      const msg = String(e?.message || '')
      if (msg.includes('404')) {
        toast('error','Falha ao gerar: servidor não reconhece o endpoint. Reinicie o WebApi para aplicar as rotas novas.')
      } else {
        toast('error', msg || 'Falha ao gerar novo token')
      }
    }
  }
  function showTokenTemporarily(){
    if (!clientToken) return
    if (tokenTimerRef.current != null){
      window.clearTimeout(tokenTimerRef.current)
      tokenTimerRef.current = null
    }
    setClientTokenVisible(true)
    tokenTimerRef.current = window.setTimeout(() => {
      setClientTokenVisible(false)
      tokenTimerRef.current = null
    }, 10000)
  }
  function handleToggleTokenVisible(){
    if (!clientToken){
      toast('error','Token indisponível')
      return
    }
    if (clientTokenVisible){
      if (tokenTimerRef.current != null){
        window.clearTimeout(tokenTimerRef.current)
        tokenTimerRef.current = null
      }
      setClientTokenVisible(false)
    } else {
      showTokenTemporarily()
    }
  }
  async function handleCopyToken(){
    const tok = clientToken
    if (!tok) { toast('error','Token indisponível'); return }
    try{
      if (navigator.clipboard && navigator.clipboard.writeText){
        await navigator.clipboard.writeText(tok)
        toast('success','Token copiado')
        return
      }
    }catch{}
    try{
      const ta = document.createElement('textarea')
      ta.value = tok
      document.body.appendChild(ta)
      ta.select()
      document.execCommand('copy')
      document.body.removeChild(ta)
      toast('success','Token copiado')
    }catch{
      toast('error','Não foi possível copiar')
    }
  }
  async function handleLogoChange(e: React.ChangeEvent<HTMLInputElement>){
    if (!isClient) return
    const file = e.target.files?.[0]
    if (!file) return
    try{
      const r = await api.clientUploadLogo(file)
      if (r?.logoPath){
        setClientLogo(r.logoPath)
        toast('success','Foto atualizada')
      }
    }catch(e:any){
      toast('error', e?.message || 'Falha ao enviar foto')
    }finally{
      if (fileRef.current) fileRef.current.value = ''
    }
  }
  return (
    <aside className={compact ? 'sidebar' : 'sidebar expanded'}>
      <div className="sidebar-top">
        <div className="sidebar-logo">
          <img src={compact ? "/img/logo-report2.png" : "/img/logo-report.png"} alt="Report Flex" />
        </div>
        <nav>
          {links.map(l=> {
            const isActive = location.pathname === l.href
            return (
              <a
                key={l.href}
                href={l.href}
                className={`d-flex align-items-center gap-2${isActive ? ' active' : ''}`}
                title={compact ? l.label : undefined}
              >
                <i className={`bi ${l.icon}`} />
                {!compact && <span>{l.label}</span>}
              </a>
            )
          })}
          {isNarrow && (
            <>
              <button
                type="button"
                className="d-flex align-items-center gap-2 sidebar-nav-btn"
                onClick={handleSwitchUser}
                title={compact ? 'Trocar de Usuário' : undefined}
              >
                <i className="bi bi-person-gear" /> {!compact && <span>Trocar de Usuário</span>}
              </button>
              <button
                type="button"
                className="d-flex align-items-center gap-2 sidebar-nav-btn"
                onClick={handleLogout}
                title={compact ? 'Sair' : undefined}
              >
                <i className="bi bi-box-arrow-right" /> {!compact && <span>Sair</span>}
              </button>
            </>
          )}
        </nav>
      </div>
      <div className="sidebar-footer" style={{display:'flex', flexDirection:'column', gap:8, marginTop:8}}>
        {(clientName || clientResp) && (
          <div
            role="button"
            onClick={()=> { if (isClient) setProfileOpen(true) }}
            className="btn btn-link p-0 text-start"
            style={{textDecoration:'none', color:'inherit'}}
          >
            <div className="d-flex align-items-center" style={{gap:8}}>
              <div style={{
                width:40, height:40, borderRadius:'50%', overflow:'hidden',
                background:'rgb(0, 149, 66)', display:'flex', alignItems:'center', justifyContent:'center', color:'#ffffff', fontSize:16, flexShrink:0, border:'2px solid rgba(255,255,255,0.7)'
              }}>
                {clientLogo
                  ? <img src={clientLogo} alt={clientName || 'Cliente'} style={{width:'100%', height:'100%', objectFit:'cover'}} />
                  : (clientName ? clientName.charAt(0).toUpperCase() : (clientResp ? clientResp.charAt(0).toUpperCase() : '?'))}
              </div>
              {!compact && (
                <div style={{fontSize:12, lineHeight:1.3, color:'#ffffff'}}>
                  {clientResp && <div><strong>{clientResp}</strong></div>}
                  {clientName && <div style={{opacity:0.85}}>{clientName}</div>}
                  {isClient && clientToken && (
                    <div style={{opacity:0.7, fontSize:11, display:'flex', alignItems:'center', gap:4}}>
                      <span>Token:</span>
                      <span>{clientTokenVisible ? clientToken : '••••••••'}</span>
                      <button
                        type="button"
                        onClick={(e)=> { e.stopPropagation(); handleToggleTokenVisible() }}
                        className="btn btn-sm btn-link p-0"
                        style={{color:'#ffffff'}}
                        title={clientTokenVisible ? 'Ocultar token' : 'Mostrar token'}
                      >
                        <i className={`bi ${clientTokenVisible ? 'bi-eye-slash' : 'bi-eye'}`} />
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        )}
        {!isNarrow && (
          <div className="d-flex flex-column" style={{gap:6, marginTop:4}}>
            <button
              className="btn btn-outline-secondary d-flex align-items-center gap-2"
              onClick={handleSwitchUser}
              style={{borderColor:'lightgray', color:'#ffffff'}}
              title={compact ? 'Trocar de Usuário' : undefined}
            >
              <i className="bi bi-person-gear" /> {!compact && <span>Trocar de Usuário</span>}
            </button>
            <button
              className="btn btn-outline-danger d-flex align-items-center gap-2"
              onClick={handleLogout}
              style={{borderColor:'rgb(241, 233, 0)', color:'rgb(241, 233, 0)'}}
              title={compact ? 'Sair' : undefined}
            >
              <i className="bi bi-box-arrow-right" /> {!compact && <span>Sair</span>}
            </button>
          </div>
        )}
        <div style={{marginTop:4}}>
          <button className="btn btn-sm btn-light" onClick={onToggle} title={expanded ? 'Recolher' : 'Expandir'} style={{background:'rgb(0, 149, 66)', borderColor:'rgb(0, 149, 66)', color:'#ffffff'}}>
            <i className="bi bi-list" />
          </button>
        </div>
      </div>
      {profileOpen && isClient && (
        <div style={{
          position:'fixed', inset:0, background:'rgba(0,0,0,0.5)', display:'flex', alignItems:'center', justifyContent:'center', zIndex: 2000
        }}>
          <div className="card" style={{minWidth:320, maxWidth:420}}>
            <div className="card-header d-flex justify-content-between align-items-center">
              <span>Perfil do Cliente</span>
              <button type="button" className="btn-close" aria-label="Close" onClick={()=> setProfileOpen(false)} />
            </div>
            <div className="card-body">
              <div className="d-flex justify-content-center mb-3">
                <div style={{
                  width:72, height:72, borderRadius:'50%', overflow:'hidden',
                  background:'rgb(0, 149, 66)', display:'flex', alignItems:'center', justifyContent:'center', color:'#ffffff', fontSize:28, flexShrink:0, border:'2px solid rgba(0,0,0,0.15)'
                }}>
                  {clientLogo
                    ? <img src={clientLogo} alt={clientName || 'Cliente'} style={{width:'100%', height:'100%', objectFit:'cover'}} />
                    : (clientName ? clientName.charAt(0).toUpperCase() : (clientResp ? clientResp.charAt(0).toUpperCase() : '?'))}
                </div>
              </div>
              <div className="mb-2">
                <button
                  type="button"
                  className="btn btn-outline-secondary w-100"
                  onClick={()=> fileRef.current && fileRef.current.click()}
                >
                  Trocar foto
                </button>
                <input
                  ref={fileRef}
                  type="file"
                  accept="image/*"
                  style={{display:'none'}}
                  onChange={handleLogoChange}
                />
              </div>
              <div className="mb-2">
                <label className="form-label">Nome da empresa</label>
                <input className="form-control" value={profileForm.nome ?? ''} onChange={e=> setProfileForm({...profileForm, nome:e.target.value})} />
              </div>
              <div className="mb-2">
                <label className="form-label">Responsável</label>
                <input className="form-control" value={profileForm.responsavel ?? ''} onChange={e=> setProfileForm({...profileForm, responsavel:e.target.value})} />
              </div>
              <div className="mb-2">
                <label className="form-label">Telefone</label>
                <input className="form-control" value={profileForm.fone ?? ''} onChange={e=> setProfileForm({...profileForm, fone:e.target.value})} />
              </div>
              <div className="mb-2">
                <label className="form-label">Email</label>
                <input className="form-control" value={profileForm.email ?? ''} onChange={e=> setProfileForm({...profileForm, email:e.target.value})} />
              </div>
              <div className="mb-2">
                <label className="form-label">Site</label>
                <input className="form-control" value={profileForm.site ?? ''} onChange={e=> setProfileForm({...profileForm, site:e.target.value})} />
              </div>
              <div className="mb-2">
                <label className="form-label">Endereço</label>
                <input className="form-control" value={profileForm.endereco ?? ''} onChange={e=> setProfileForm({...profileForm, endereco:e.target.value})} />
              </div>
              <div className="mb-3">
                <label className="form-label">Token do cliente</label>
                <div className="input-group">
                  <input
                    className="form-control"
                    readOnly
                    value={clientTokenVisible ? (clientToken ?? '') : ''}
                    placeholder={clientToken ? '••••••••' : ''}
                  />
                  <button type="button" className="btn btn-outline-secondary" onClick={handleToggleTokenVisible}>
                    <i className={`bi ${clientTokenVisible ? 'bi-eye-slash' : 'bi-eye'}`} />
                  </button>
                  <button type="button" className="btn btn-outline-secondary" onClick={handleCopyToken}>Copiar</button>
                </div>
                <div className="d-flex justify-content-between align-items-center mt-2">
                  <small style={{opacity:0.8}}>Atual: {clientTokenMasked ?? '-'}</small>
                  <button type="button" className="btn btn-outline-secondary" onClick={handleRegenerateToken}>
                    Gerar novo token
                  </button>
                </div>
              </div>
            </div>
            <div className="card-footer d-flex justify-content-end gap-2">
              <button type="button" className="btn btn-light" onClick={()=> setProfileOpen(false)} disabled={profileSaving}>Cancelar</button>
              <button type="button" className="btn btn-primary" onClick={handleProfileSave} disabled={profileSaving}>
                {profileSaving ? 'Salvando...' : 'Salvar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </aside>
  )
}
