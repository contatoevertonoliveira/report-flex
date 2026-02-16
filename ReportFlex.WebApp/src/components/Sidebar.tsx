import React from 'react'
import { api } from '../api'

export function Sidebar({ expanded, onToggle }: { expanded: boolean, onToggle: ()=>void }) {
  const level = typeof window !== 'undefined' ? localStorage.getItem('rf_level') : null
  const isSuperAdmin = level === 'SuperAdmin'
  const isClient = level === 'Cliente'
  const [clientName, setClientName] = React.useState<string | null>(null)
  const [clientResp, setClientResp] = React.useState<string | null>(null)
  const [clientLogo, setClientLogo] = React.useState<string | null>(null)
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
  return (
    <aside className={expanded ? 'sidebar expanded' : 'sidebar'}>
      <div className="sidebar-top">
        <div className="sidebar-logo">
          <img src={expanded ? "/img/logo-report.png" : "/img/logo-report2.png"} alt="Report Flex" />
        </div>
        <nav>
          {links.map(l=> (
            <a key={l.href} href={l.href} className="d-flex align-items-center gap-2">
              <i className={`bi ${l.icon}`} />
              {expanded && <span>{l.label}</span>}
            </a>
          ))}
        </nav>
      </div>
      <div className="sidebar-footer" style={{display:'flex', flexDirection:'column', gap:8, marginTop:8}}>
        {(clientName || clientResp) && (
          <div className="d-flex align-items-center" style={{gap:8}}>
            <div style={{
              width:36, height:36, borderRadius:'50%', overflow:'hidden',
              background:'#0f172a', display:'flex', alignItems:'center', justifyContent:'center', color:'#e5e7eb', fontSize:14, flexShrink:0
            }}>
              {clientLogo
                ? <img src={clientLogo} alt="Cliente" style={{width:'100%', height:'100%', objectFit:'cover'}} />
                : (clientResp ? clientResp.charAt(0).toUpperCase() : (clientName ? clientName.charAt(0).toUpperCase() : '?'))}
            </div>
            {expanded && (
              <div style={{fontSize:12, lineHeight:1.3, color:'#e5e7eb'}}>
                {clientResp && <div>Responsável: <strong>{clientResp}</strong></div>}
                {clientName && <div>Empresa: <strong>{clientName}</strong></div>}
              </div>
            )}
          </div>
        )}
        <div className="d-flex flex-column" style={{gap:6, marginTop:4}}>
          <button className="btn btn-outline-secondary d-flex align-items-center gap-2" onClick={handleSwitchUser}>
            <i className="bi bi-person-gear" /> {expanded && <span>Trocar de Usuário</span>}
          </button>
          <button className="btn btn-outline-danger d-flex align-items-center gap-2" onClick={handleLogout}>
            <i className="bi bi-box-arrow-right" /> {expanded && <span>Sair</span>}
          </button>
        </div>
        <div style={{marginTop:4}}>
          <button className="btn btn-sm btn-light" onClick={onToggle} title={expanded ? 'Recolher' : 'Expandir'}>
            <i className="bi bi-list" />
          </button>
        </div>
      </div>
    </aside>
  )
}
