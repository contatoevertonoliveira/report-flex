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
    <section className="login-split">
      <div className="login-left">
        <div className="login-left-inner">
          <img alt="JumperFour" src="/img/Jumperfour_logo_branco_adap.png" className="login-left-logo" />
        </div>
      </div>
      <div className="login-right">
        <div className="login-right-inner">
          <div className="login-panel">
            <div className="login-panel-head">
              <div className="login-panel-title">Acesso</div>
              <div className="login-panel-subtitle">Entre com seu email e senha</div>
            </div>
            <form className="login-form" onSubmit={e=>{ e.preventDefault(); handleLogin() }}>
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-envelope" /></span>
                <input
                  className="form-control"
                  value={email}
                  onChange={e=>setEmail(e.target.value)}
                  placeholder="Email"
                  disabled={loading}
                />
              </div>
              <div className="input-group">
                <span className="input-group-text"><i className="bi bi-key" /></span>
                <input
                  className="form-control"
                  type="password"
                  value={senha}
                  onChange={e=>setSenha(e.target.value)}
                  placeholder="Senha"
                  disabled={loading}
                />
              </div>
              <button className="btn btn-dark w-100 d-flex align-items-center justify-content-center" type="submit" disabled={loading}>
                {loading
                  ? (<><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Autenticando...</>)
                  : (<><i className="bi bi-box-arrow-in-right me-2" /> Entrar</>)}
              </button>
            </form>
            {error && <div className="alert alert-danger py-2 mt-3 mb-0">{error}</div>}
          </div>
        </div>
      </div>
    </section>
  )
}
