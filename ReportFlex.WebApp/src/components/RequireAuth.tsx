import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { logout } from '../api'

function decodeJwtPayloadSegment(segment: string): any {
  let s = segment.replace(/-/g,'+').replace(/_/g,'/')
  while (s.length % 4 !== 0) s += '='
  return JSON.parse(atob(s))
}

function isTokenValid(): boolean {
  const t = localStorage.getItem('rf_token')
  if(!t) return false
  const last = localStorage.getItem('rf_last_activity')
  if (last) {
    const lastMs = parseInt(last, 10)
    if (!Number.isNaN(lastMs)) {
      const maxInactivityMs = 20 * 60 * 1000
      if (Date.now() - lastMs > maxInactivityMs) {
        logout()
        return false
      }
    }
  }
  try{
    const parts = t.split('.')
    if (parts.length < 2) return false
    const p = decodeJwtPayloadSegment(parts[1])
    if(typeof p.exp === 'number'){
      const now = Math.floor(Date.now()/1000)
      return p.exp > now
    }
    return true
  }catch{
    return false
  }
}

export function RequireAuth({ children }: { children: React.ReactNode }){
  const location = useLocation()
  if(!isTokenValid()){
    return <Navigate to="/login" state={{ from: location }} replace />
  }
  return <>{children}</>
}

export function RequireSuperAdmin({ children }: { children: React.ReactNode }){
  const location = useLocation()
  if(!isTokenValid()){
    return <Navigate to="/login" state={{ from: location }} replace />
  }
  const level = localStorage.getItem('rf_level')
  if (level !== 'SuperAdmin'){
    return <Navigate to="/consultas" state={{ from: location }} replace />
  }
  return <>{children}</>
}

export function RequireNotClient({ children }: { children: React.ReactNode }){
  const location = useLocation()
  if(!isTokenValid()){
    return <Navigate to="/login" state={{ from: location }} replace />
  }
  const level = localStorage.getItem('rf_level')
  if (level === 'Cliente'){
    return <Navigate to="/consultas" state={{ from: location }} replace />
  }
  return <>{children}</>
}
