import React, { useState } from 'react'
import { api, setToken } from '../api'
import { useNavigate } from 'react-router-dom'

export function LoginPage() {
  const [tokenInput, setTokenInput] = useState('')
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()
  return (
    <section className="login-layout">
      <div className="login-card">
        <div className="login-brand">
          <img alt="Logo" src="http://localhost:5000/images-legacy/Logo_Principal_Fundo2.png" style={{maxWidth:'180px'}} />
        </div>
        <div className="login-actions">
          <input className="form-control" value={tokenInput} onChange={e=>setTokenInput(e.target.value)} placeholder="Token" />
          <button className="btn btn-secondary" onClick={async ()=>{
            try{
              const res = await api.signinToken(tokenInput)
              if (res?.token){
                setToken(res.token)
                if (res?.nivel) localStorage.setItem('rf_level', res.nivel)
                if (res?.clientId){
                  localStorage.setItem('rf_client_id', String(res.clientId))
                  if (res?.clientName) localStorage.setItem('rf_client_name', res.clientName)
                } else {
                  localStorage.removeItem('rf_client_id')
                  localStorage.removeItem('rf_client_name')
                }
                localStorage.setItem('rf_last_activity', Date.now().toString())
                navigate('/consultas')
              }
              else setError('Token inválido')
            }catch{ setError('Falha de autenticação') }
          }}>Entrar</button>
        </div>
        {error && <div style={{color:'red', marginTop:8}}>{error}</div>}
      </div>
    </section>
  )
}
