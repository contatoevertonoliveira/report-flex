import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'

function isTokenValid(): boolean {
  const t = localStorage.getItem('rf_token')
  if(!t) return false
  try{
    const p = JSON.parse(atob(t.split('.')[1].replace(/-/g,'+').replace(/_/g,'/')))
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
