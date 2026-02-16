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
import { RequireAuth } from './components/RequireAuth'
import { ReportsPage } from './pages/ReportsPage'
import { useLocation } from 'react-router-dom'

export default function App() {
  const [expanded, setExpanded] = React.useState(true)
  const location = useLocation()
  const showSidebar = location.pathname !== '/login'

  return (
    <div className="layout">
      {showSidebar && <Sidebar expanded={expanded} onToggle={() => setExpanded(!expanded)} />}
      <main style={{ overflow: showSidebar ? 'auto' : 'hidden' }}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/clientes" element={<RequireAuth><ClientesPage /></RequireAuth>} />
          <Route path="/prestadores" element={<RequireAuth><PrestadoresPage /></RequireAuth>} />
          <Route path="/transit" element={<RequireAuth><TransitPage /></RequireAuth>} />
          <Route path="/employees" element={<RequireAuth><EmployeesPage /></RequireAuth>} />
          <Route path="/external" element={<RequireAuth><ExternalPage /></RequireAuth>} />
          <Route path="/access" element={<RequireAuth><AccessPage /></RequireAuth>} />
          <Route path="/reports" element={<RequireAuth><ReportsPage /></RequireAuth>} />
          <Route path="*" element={<Navigate to="/login" />} />
        </Routes>
      </main>
    </div>
  )
}
