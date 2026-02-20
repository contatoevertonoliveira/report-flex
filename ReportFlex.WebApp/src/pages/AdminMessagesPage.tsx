import React, { useEffect, useState } from 'react'
import { api } from '../api'

type MessageItem = {
  Id: number
  FromUsuario?: string | null
  FromNome?: string | null
  FromNivel?: string | null
  ClientId?: number | null
  Assunto?: string | null
  Texto?: string | null
  CreatedAt?: string | null
  Status?: string | null
}

export function AdminMessagesPage(){
  const [items, setItems] = useState<MessageItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    if (typeof window === 'undefined') return
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }

  async function load(){
    setLoading(true)
    setError(null)
    try{
      const res = await api.adminMessages({ page: 1, pageSize: 100 })
      const list = (res as any)?.items ?? res ?? []
      setItems(Array.isArray(list) ? list : [])
    }catch(e:any){
      const msg = e?.message || 'Falha ao carregar mensagens'
      setError(msg)
      toast('error', msg)
    }finally{
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  function formatDate(value?: string | null){
    if (!value) return ''
    const d = new Date(value)
    if (Number.isNaN(d.getTime())) return value
    const pad = (n: number) => n.toString().padStart(2,'0')
    const dd = pad(d.getDate())
    const mm = pad(d.getMonth() + 1)
    const yyyy = d.getFullYear()
    const hh = pad(d.getHours())
    const mi = pad(d.getMinutes())
    return `${dd}/${mm}/${yyyy} ${hh}:${mi}`
  }

  return (
    <section className="container-fluid">
      <div className="row mb-3">
        <div className="col d-flex justify-content-between align-items-center">
          <div>
            <h3 className="mb-1">Inbox de mensagens</h3>
            <p className="text-muted mb-0" style={{fontSize:13}}>
              Mensagens enviadas pelos usuários do sistema para SuperAdmins e Administradores.
            </p>
          </div>
          <button
            type="button"
            className="btn btn-outline-light btn-sm"
            onClick={load}
            disabled={loading}
          >
            <i className="bi bi-arrow-clockwise" /> Atualizar
          </button>
        </div>
      </div>
      {error && (
        <div className="row mb-2">
          <div className="col">
            <div className="alert alert-danger py-2" role="alert" style={{fontSize:13}}>
              {error}
            </div>
          </div>
        </div>
      )}
      <div className="row">
        <div className="col">
          <div className="card">
            <div className="card-body">
              {loading && <div>Carregando...</div>}
              {!loading && items.length === 0 && (
                <div className="text-muted" style={{fontSize:13}}>
                  Nenhuma mensagem encontrada.
                </div>
              )}
              {!loading && items.length > 0 && (
                <div className="table-responsive">
                  <table className="table table-sm align-middle">
                    <thead>
                      <tr>
                        <th style={{width:90}}>Data</th>
                        <th>De</th>
                        <th style={{width:200}}>Assunto</th>
                        <th>Mensagem</th>
                        <th style={{width:90}}>Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {items.map(m => (
                        <tr key={m.Id}>
                          <td>{formatDate(m.CreatedAt)}</td>
                          <td>
                            <div>
                              {m.FromNome || m.FromUsuario || '-'}
                            </div>
                            <div className="text-muted" style={{fontSize:11}}>
                              {(m.FromUsuario || '') + (m.FromNivel ? ` (${m.FromNivel})` : '')}
                            </div>
                            {m.ClientId && (
                              <div className="text-muted" style={{fontSize:11}}>
                                Cliente #{m.ClientId}
                              </div>
                            )}
                          </td>
                          <td>{m.Assunto}</td>
                          <td>
                            <div style={{whiteSpace:'pre-wrap'}}>{m.Texto}</div>
                          </td>
                          <td>{m.Status || 'Novo'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

