import React from 'react'
import { Sidebar } from './components/Sidebar'
import { Routes, Route, Navigate } from 'react-router-dom'
import { ClientesPage } from './pages/ClientesPage'
import { PrestadoresPage } from './pages/PrestadoresPage'
import { TransitPage } from './pages/TransitPage'
import { EmployeesPage } from './pages/EmployeesPage'
import { ExternalPage } from './pages/ExternalPage'
import { AccessPage } from './pages/AccessPage'
import { LoginPage } from './pages/LoginPage'
import { RequireAuth, RequireSuperAdmin, RequireNotClient, RequireAdminOrSuper, RequireScreenEnabled } from './components/RequireAuth'
import { useLocation } from 'react-router-dom'
import { QueriesPage } from './pages/QueriesPage'
import { SettingsPage } from './pages/SettingsPage'
import { MessagesPage } from './pages/MessagesPage'
import { AdminMessagesPage } from './pages/AdminMessagesPage'
import { ConsultasConfigPage } from './pages/ConsultasConfigPage'
import { ChangePasswordPage } from './pages/ChangePasswordPage'
import { LogsPage } from './pages/LogsPage'
import { api } from './api'

export default function App() {
  const [expanded, setExpanded] = React.useState(() => {
    try {
      const saved = localStorage.getItem('rf_sidebar_expanded')
      return saved !== null ? JSON.parse(saved) : true
    } catch {
      return true
    }
  })

  React.useEffect(() => {
    localStorage.setItem('rf_sidebar_expanded', JSON.stringify(expanded))
  }, [expanded])

  const location = useLocation()
  const showSidebar = location.pathname !== '/login'
  const screensFetchedRef = React.useRef(false)
  const [, setScreensCfgTick] = React.useState(0)
  const [toasts, setToasts] = React.useState<Array<{ id: number, type: 'success'|'error'|'info'|'warning', message: string }>>([])

  React.useEffect(() => {
    const update = () => {
      if (localStorage.getItem('rf_token')) {
        localStorage.setItem('rf_last_activity', Date.now().toString())
      }
    }
    const events = ['click','keydown','mousemove','scroll']
    events.forEach(e => window.addEventListener(e, update))
    if (localStorage.getItem('rf_token') && !localStorage.getItem('rf_last_activity')) {
      update()
    }
    return () => {
      events.forEach(e => window.removeEventListener(e, update))
    }
  }, [])

  React.useEffect(() => {
    const handler = (e: Event) => {
      const ev = e as CustomEvent<{ type: 'success'|'error'|'info'|'warning', message: string }>
      const id = Date.now() + Math.random()
      setToasts(prev => [...prev, { id, type: ev.detail.type, message: ev.detail.message }])
      setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 4000)
    }
    window.addEventListener('app:toast', handler as EventListener)
    return () => window.removeEventListener('app:toast', handler as EventListener)
  }, [])

  React.useEffect(() => {
    const handler = () => {
      setScreensCfgTick(Date.now())
    }
    window.addEventListener('rf:screens-config', handler as EventListener)
    window.addEventListener('storage', handler)
    return () => {
      window.removeEventListener('rf:screens-config', handler as EventListener)
      window.removeEventListener('storage', handler)
    }
  }, [])

  React.useEffect(() => {
    if (!showSidebar) return
    if (!localStorage.getItem('rf_token')) return
    if (screensFetchedRef.current) return
    screensFetchedRef.current = true
    ;(async () => {
      try{
        const cfg: any = await api.getScreensConfig()
        if (cfg && typeof cfg === 'object'){
          localStorage.setItem('rf_screens_config', JSON.stringify(cfg))
          localStorage.setItem('rf_screens_config_ts', String(Date.now()))
          window.dispatchEvent(new Event('rf:screens-config'))
        }
      }catch{}
    })()
  }, [showSidebar])

  return (
    <div className="layout" style={{ '--sidebar-width': showSidebar ? (expanded ? '250px' : '80px') : '0px' } as React.CSSProperties}>
      {showSidebar && <Sidebar expanded={expanded} onToggle={() => setExpanded(!expanded)} />}
      <main style={{ overflow: showSidebar ? 'auto' : 'hidden', position:'relative' }}>
        {location.pathname !== '/login' && (
          <div className="page-logo-mark">
            <img src="/img/reportFlex.png" alt="Report Flex" />
          </div>
        )}
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/alterar-senha" element={<RequireAuth><ChangePasswordPage /></RequireAuth>} />
          <Route path="/consultas" element={<RequireScreenEnabled screenKey="consultas"><RequireAuth><QueriesPage /></RequireAuth></RequireScreenEnabled>} />
          <Route path="/consultas-config" element={<RequireScreenEnabled screenKey="consultas-config"><RequireAdminOrSuper><ConsultasConfigPage /></RequireAdminOrSuper></RequireScreenEnabled>} />
          <Route path="/logs" element={<RequireScreenEnabled screenKey="logs"><RequireAdminOrSuper><LogsPage /></RequireAdminOrSuper></RequireScreenEnabled>} />
          <Route path="/mensagens" element={<RequireScreenEnabled screenKey="mensagens"><RequireAuth><MessagesPage /></RequireAuth></RequireScreenEnabled>} />
          <Route path="/inbox" element={<RequireScreenEnabled screenKey="inbox"><RequireSuperAdmin><AdminMessagesPage /></RequireSuperAdmin></RequireScreenEnabled>} />
          <Route path="/configuracoes" element={<RequireScreenEnabled screenKey="configuracoes"><RequireAdminOrSuper><SettingsPage /></RequireAdminOrSuper></RequireScreenEnabled>} />
          <Route path="/clientes" element={<RequireScreenEnabled screenKey="clientes"><RequireAdminOrSuper><ClientesPage /></RequireAdminOrSuper></RequireScreenEnabled>} />
          <Route path="/prestadores" element={<RequireScreenEnabled screenKey="prestadores"><RequireAuth><PrestadoresPage /></RequireAuth></RequireScreenEnabled>} />
          <Route path="/transit" element={<RequireScreenEnabled screenKey="transit"><RequireAuth><TransitPage /></RequireAuth></RequireScreenEnabled>} />
          <Route path="/employees" element={<RequireScreenEnabled screenKey="employees"><RequireAuth><EmployeesPage /></RequireAuth></RequireScreenEnabled>} />
          <Route path="/external" element={<RequireScreenEnabled screenKey="external"><RequireAuth><ExternalPage /></RequireAuth></RequireScreenEnabled>} />
          <Route path="/access" element={<RequireScreenEnabled screenKey="access"><RequireAuth><AccessPage /></RequireAuth></RequireScreenEnabled>} />
          <Route path="*" element={<Navigate to="/login" />} />
        </Routes>
      </main>
      <div style={{
        position:'fixed', top:16, right:16, display:'flex', flexDirection:'column', gap:8, zIndex: 9999
      }}>
        {toasts.map(t => (
          <div key={t.id} style={{
            minWidth: 260,
            background: t.type==='success' ? '#0f5132' : t.type==='error' ? '#842029' : t.type==='warning' ? '#664d03' : '#084298',
            color: 'white', padding: '10px 12px', borderRadius: 6, boxShadow:'0 2px 10px rgba(0,0,0,0.15)'
          }}>
            <strong style={{textTransform:'capitalize'}}>{t.type}</strong>
            <div style={{fontSize: 13, marginTop: 4}}>{t.message}</div>
          </div>
        ))}
      </div>
    </div>
  )
}
