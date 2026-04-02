import React from 'react'
import { api, setToken } from '../api'
import { useNavigate } from 'react-router-dom'

function meetsPolicy(pwd: string): boolean {
  if (!pwd || pwd.length < 8) return false
  let hasUpper = false
  let hasLower = false
  let hasSpecial = false
  for (const ch of pwd) {
    if (/[A-Z]/.test(ch)) hasUpper = true
    else if (/[a-z]/.test(ch)) hasLower = true
    else if (/[^A-Za-z0-9]/.test(ch)) hasSpecial = true
  }
  return hasUpper && hasLower && hasSpecial
}

export function ChangePasswordPage() {
  const [currentPassword, setCurrentPassword] = React.useState('')
  const [newPassword, setNewPassword] = React.useState('')
  const [confirmPassword, setConfirmPassword] = React.useState('')
  const [loading, setLoading] = React.useState(false)
  const [error, setError] = React.useState<string | null>(null)
  const navigate = useNavigate()

  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }

  async function handleSubmit(e: React.FormEvent){
    e.preventDefault()
    setError(null)
    if (!currentPassword) { setError('Informe a senha atual'); return }
    if (!newPassword) { setError('Informe a nova senha'); return }
    if (newPassword !== confirmPassword) { setError('A confirmação não confere'); return }
    if (!meetsPolicy(newPassword)) { setError('A senha deve ter pelo menos 8 caracteres, uma letra maiúscula, uma letra minúscula e um caractere especial.'); return }
    setLoading(true)
    try{
      const r: any = await api.changePassword({ currentPassword, newPassword })
      if (r?.token){
        setToken(r.token)
      }
      localStorage.removeItem('rf_pwd_change_required')
      toast('success', 'Senha alterada com sucesso')
      navigate('/consultas')
    }catch(e2:any){
      const msg = e2?.message || 'Falha ao alterar senha'
      setError(msg)
      toast('error', msg)
    }finally{
      setLoading(false)
    }
  }

  return (
    <section className="login-layout">
      <div className="login-card">
        <div className="login-brand">
          <img alt="Logo" src="http://localhost:5001/images-legacy/Logo_Principal_Fundo2.png" style={{maxWidth:'180px'}} />
        </div>
        <h5 style={{marginBottom:12}}>Alterar senha</h5>
        <form className="login-actions" onSubmit={handleSubmit}>
          <input className="form-control" type="password" placeholder="Senha atual" value={currentPassword} onChange={e=> setCurrentPassword(e.target.value)} disabled={loading} />
          <input className="form-control" type="password" placeholder="Nova senha" value={newPassword} onChange={e=> setNewPassword(e.target.value)} disabled={loading} />
          <input className="form-control" type="password" placeholder="Confirmar nova senha" value={confirmPassword} onChange={e=> setConfirmPassword(e.target.value)} disabled={loading} />
          <button className="btn btn-secondary" type="submit" disabled={loading}>
            {loading ? (<><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Salvando...</>) : 'Salvar'}
          </button>
        </form>
        <div style={{fontSize:12, marginTop:10, color:'#555'}}>
          Regras: mínimo 8 caracteres, 1 maiúscula, 1 minúscula e 1 caractere especial.
        </div>
        {error && <div style={{color:'red', marginTop:8}}>{error}</div>}
      </div>
    </section>
  )
}
