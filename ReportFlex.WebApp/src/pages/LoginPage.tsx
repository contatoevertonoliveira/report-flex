import React, { useState } from 'react'
import { api, setToken } from '../api'
import { useNavigate } from 'react-router-dom'

export function LoginPage() {
  const [tokenInput, setTokenInput] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }
  async function handleLogin(){
    if (!tokenInput.trim()){
      setError('Informe o token')
      toast('error', 'Informe o token')
      return
    }
    setLoading(true)
    setError(null)
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
        try{
          const u = localStorage.getItem('rf_sql_user') || ''
          const p = localStorage.getItem('rf_sql_pwd') || ''
          if (u && p){
            await api.setSqlAuth({ user: u, pwd: p })
            await api.setSqlAuthRuntime({ user: u, pwd: p })
          }
        }catch{}
        toast('success', 'Login realizado com sucesso')
        navigate('/consultas')
      } else {
        setError('Token inválido')
        toast('error', 'Token inválido')
      }
    }catch{
      setError('Falha de autenticação')
      toast('error', 'Falha de autenticação')
    } finally {
      setLoading(false)
    }
  }
  return (
    <section className="login-layout">
      <div className="login-card">
        <div className="login-brand">
          <img alt="Logo" src="http://localhost:5000/images-legacy/Logo_Principal_Fundo2.png" style={{maxWidth:'180px'}} />
        </div>
        <form className="login-actions" onSubmit={e=>{ e.preventDefault(); handleLogin() }}>
          <input
            className="form-control"
            value={tokenInput}
            onChange={e=>setTokenInput(e.target.value)}
            placeholder="Token"
            disabled={loading}
          />
          <button className="btn btn-secondary" type="submit" disabled={loading}>
            {loading
              ? (<><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Autenticando...</>)
              : 'Entrar'}
          </button>
        </form>
        {error && <div style={{color:'red', marginTop:8}}>{error}</div>}
      </div>
    </section>
  )
}
