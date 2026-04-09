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
import { RequireAuth, RequireSuperAdmin, RequireNotClient, RequireAdminOrSuper } from './components/RequireAuth'
import { useLocation } from 'react-router-dom'
import { QueriesPage } from './pages/QueriesPage'
import { SettingsPage } from './pages/SettingsPage'
import { MessagesPage } from './pages/MessagesPage'
import { AdminMessagesPage } from './pages/AdminMessagesPage'
import { ConsultasConfigPage } from './pages/ConsultasConfigPage'
import { ChangePasswordPage } from './pages/ChangePasswordPage'
import { LogsPage } from './pages/LogsPage'

export default function App() {
  const [expanded, setExpanded] = React.useState(true)
  const location = useLocation()
  const showSidebar = location.pathname !== '/login'
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

  return (
    <div className="layout">
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
          <Route path="/consultas" element={<RequireAuth><QueriesPage /></RequireAuth>} />
          <Route path="/consultas-config" element={<RequireAdminOrSuper><ConsultasConfigPage /></RequireAdminOrSuper>} />
          <Route path="/logs" element={<RequireAdminOrSuper><LogsPage /></RequireAdminOrSuper>} />
          <Route path="/mensagens" element={<RequireAuth><MessagesPage /></RequireAuth>} />
          <Route path="/inbox" element={<RequireSuperAdmin><AdminMessagesPage /></RequireSuperAdmin>} />
          <Route path="/configuracoes" element={<RequireNotClient><SettingsPage /></RequireNotClient>} />
          <Route path="/clientes" element={<RequireSuperAdmin><ClientesPage /></RequireSuperAdmin>} />
          <Route path="/prestadores" element={<RequireAuth><PrestadoresPage /></RequireAuth>} />
          <Route path="/transit" element={<RequireAuth><TransitPage /></RequireAuth>} />
          <Route path="/employees" element={<RequireAuth><EmployeesPage /></RequireAuth>} />
          <Route path="/external" element={<RequireAuth><ExternalPage /></RequireAuth>} />
          <Route path="/access" element={<RequireAuth><AccessPage /></RequireAuth>} />
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
