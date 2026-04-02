import React, { useState } from 'react'
import { api, setToken } from '../api'
import { useNavigate } from 'react-router-dom'

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }
  async function handleLogin(){
    if (!email.trim()){
      setError('Informe o email')
      toast('error', 'Informe o email')
      return
    }
    if (!senha){
      setError('Informe a senha')
      toast('error', 'Informe a senha')
      return
    }
    setLoading(true)
    setError(null)
    try{
      const res = await api.signin(email.trim(), senha)
      if (res?.token){
        setToken(res.token)
        if (res?.nivel) localStorage.setItem('rf_level', res.nivel)
        if (res?.mustChangePassword) localStorage.setItem('rf_pwd_change_required', '1')
        else localStorage.removeItem('rf_pwd_change_required')
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
        if (res?.mustChangePassword) navigate('/alterar-senha')
        else navigate('/consultas')
      } else {
        setError('Credenciais inválidas')
        toast('error', 'Credenciais inválidas')
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
          <img alt="Logo" src="http://localhost:5001/images-legacy/Logo_Principal_Fundo2.png" style={{maxWidth:'180px'}} />
        </div>
        <form className="login-actions" onSubmit={e=>{ e.preventDefault(); handleLogin() }}>
          <input
            className="form-control"
            value={email}
            onChange={e=>setEmail(e.target.value)}
            placeholder="Email"
            disabled={loading}
          />
          <input
            className="form-control"
            type="password"
            value={senha}
            onChange={e=>setSenha(e.target.value)}
            placeholder="Senha"
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
