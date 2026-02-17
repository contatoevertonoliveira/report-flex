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
  const [isNarrow, setIsNarrow] = React.useState<boolean>(() => typeof window !== 'undefined' ? window.innerWidth <= 768 : false)
  const location = useLocation()
  React.useEffect(() => {
    const cid = typeof window !== 'undefined' ? localStorage.getItem('rf_client_id') : null
    if (!cid) return
    api.currentClientInfo().then(info => {
      if (info && info.id){
        setClientName(info.nome || null)
        setClientResp(info.responsavel || null)
        setClientLogo(info.logoPath || null)
      }
    }).catch(()=>{})
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
    { label: 'Opções Relatórios', href: '/reports', icon: 'bi-file-earmark-bar-graph' },
    ...(isClient ? [] : [{ label: 'Configurações', href: '/configuracoes', icon: 'bi-gear' }]),
    ...(isSuperAdmin ? [{ label: 'Clientes', href: '/clientes', icon: 'bi-people' }] : [])
  ]
  function handleSwitchUser(){
    window.location.href = '/login'
  }
  function handleLogout(){
    import('../api').then(m=> m.logout())
    window.location.href = '/login'
  }
  const compact = isNarrow ? true : !expanded
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
          <div className="d-flex align-items-center" style={{gap:8}}>
            <div style={{
              width:36, height:36, borderRadius:'50%', overflow:'hidden',
              background:'rgb(0, 149, 66)', display:'flex', alignItems:'center', justifyContent:'center', color:'#ffffff', fontSize:14, flexShrink:0
            }}>
              {clientLogo
                ? <img src={clientLogo} alt="Cliente" style={{width:'100%', height:'100%', objectFit:'cover'}} />
                : (clientResp ? clientResp.charAt(0).toUpperCase() : (clientName ? clientName.charAt(0).toUpperCase() : '?'))}
            </div>
            {!compact && (
              <div style={{fontSize:12, lineHeight:1.3, color:'#ffffff'}}>
                {clientResp && <div>Responsável: <strong>{clientResp}</strong></div>}
                {clientName && <div>Empresa: <strong>{clientName}</strong></div>}
              </div>
            )}
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
    </aside>
  )
}
