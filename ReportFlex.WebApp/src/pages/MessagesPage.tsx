import React, { useState } from 'react'
import { api } from '../api'

export function MessagesPage(){
  const [assunto, setAssunto] = useState('')
  const [texto, setTexto] = useState('')
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    if (typeof window === 'undefined') return
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }

  async function handleSubmit(e: React.FormEvent){
    e.preventDefault()
    const a = assunto.trim()
    const t = texto.trim()
    if (!a || !t){
      setError('Preencha assunto e mensagem')
      return
    }
    setError(null)
    setSending(true)
    try{
      await api.sendMessage({ assunto: a, texto: t })
      toast('success','Mensagem enviada para os administradores')
      setAssunto('')
      setTexto('')
    }catch(e:any){
      const msg = e?.message || 'Falha ao enviar mensagem'
      setError(msg)
      toast('error', msg)
    }finally{
      setSending(false)
    }
  }

  return (
    <section className="container-fluid">
      <div className="row mb-3">
        <div className="col">
          <h3 className="mb-1">Mensagens para administradores</h3>
          <p className="text-muted mb-0" style={{fontSize:13}}>
            Envie sugestões, dúvidas ou solicitações. Sua mensagem será encaminhada aos administradores do sistema.
          </p>
        </div>
      </div>
      <div className="row">
        <div className="col-md-8 col-lg-6">
          <div className="card">
            <div className="card-body">
              <form onSubmit={handleSubmit}>
                <div className="mb-3">
                  <label className="form-label">Assunto</label>
                  <input
                    className="form-control"
                    value={assunto}
                    onChange={e=> setAssunto(e.target.value)}
                    maxLength={200}
                    disabled={sending}
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">Mensagem</label>
                  <textarea
                    className="form-control"
                    rows={6}
                    value={texto}
                    onChange={e=> setTexto(e.target.value)}
                    disabled={sending}
                  />
                  <small className="text-muted">
                    Use este espaço para descrever sua dúvida, sugestão ou solicitação.
                  </small>
                </div>
                {error && (
                  <div className="alert alert-danger py-2" role="alert" style={{fontSize:13}}>
                    {error}
                  </div>
                )}
                <div className="d-flex justify-content-end gap-2">
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={sending}
                  >
                    {sending ? 'Enviando...' : 'Enviar mensagem'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

