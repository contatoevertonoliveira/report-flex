import React, { useEffect, useState } from 'react'
import { api } from '../api'

export function SettingsPage(){
  const [mode, setMode] = useState<'Real'|'Demo'>('Demo')
  const [loading, setLoading] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [seedCount, setSeedCount] = useState(100)
  const [realPath, setRealPath] = useState('')

  useEffect(() => {
    (async () => {
      try{
        const r = await api.getDbMode()
        if (r?.mode === 'Demo' || r?.mode === 'Real'){
          setMode(r.mode)
        }
        const c = await api.getConnections()
        if (c?.CMS){
          setRealPath(c.CMS)
        }
      }catch{}
    })()
  }, [])

  async function applyMode(next: 'Real'|'Demo'){
    setErr(null); setMsg(null)
    try{
      const r = await api.setDbMode(next)
      setMode(r.mode || next)
      setMsg(`Modo alterado para ${r.mode || next}`)
    }catch{
      setErr('Falha ao alterar modo')
    }
  }

  async function seed(){
    setErr(null); setMsg(null); setLoading(true)
    try{
      const r = await api.seedDemo(seedCount, 'all')
      if (r?.error){
        setErr(r.error)
      }else{
        const summary = Object.entries(r || {}).map(([k,v])=> `${k}:${v}`).join(', ')
        setMsg(`Dados de teste adicionados com sucesso${summary ? ' • ' + summary : ''}`)
      }
    }catch{
      setErr('Falha ao adicionar dados de teste (verifique se está em modo Demo e ambiente de desenvolvimento)')
    }finally{
      setLoading(false)
    }
  }

  async function saveRealPath(){
    setErr(null); setMsg(null)
    try{
      let cms = realPath
      let logins = realPath
      const hasCatalog = /Initial\s+Catalog\s*=/i.test(realPath) || /Database\s*=/i.test(realPath)
      if (!hasCatalog){
        const base = realPath.endsWith(';') ? realPath : realPath + ';'
        cms = base + 'Initial Catalog=CMS'
        logins = base + 'Initial Catalog=Logins'
      }else{
        cms = realPath.replace(/(Initial\s+Catalog|Database)\s*=\s*Logins/i, '$1=CMS')
        logins = realPath.replace(/(Initial\s+Catalog|Database)\s*=\s*CMS/i, '$1=Logins')
      }
      const r = await api.setConnections({ CMS: cms, Logins: logins })
      setMsg('Configuração de conexão salva')
    }catch{
      setErr('Falha ao salvar configuração')
    }
  }

  return (
    <section>
      <h2>Configurações</h2>
      <div className="card">
        <div className="card-header d-flex align-items-center" style={{gap:8}}>
          <i className="bi bi-gear" /> Preferências do Banco de Dados
        </div>
        <div className="card-body d-flex flex-column" style={{gap:12}}>
          <div className="d-flex align-items-center flex-wrap" style={{gap:12}}>
            <div className="form-check">
              <input className="form-check-input" type="radio" name="dbmode" id="dbReal" checked={mode==='Real'} onChange={()=> applyMode('Real')} />
              <label className="form-check-label" htmlFor="dbReal">Banco Real</label>
            </div>
            <div className="form-check">
              <input className="form-check-input" type="radio" name="dbmode" id="dbDemo" checked={mode==='Demo'} onChange={()=> applyMode('Demo')} />
              <label className="form-check-label" htmlFor="dbDemo">Banco Demo (Teste)</label>
            </div>
          </div>
          {mode === 'Real' && (
            <div className="d-flex align-items-end flex-wrap" style={{gap:12}}>
              <div className="input-group" style={{minWidth:420}}>
                <span className="input-group-text"><i className="bi bi-hdd-network" /></span>
                <input className="form-control" placeholder="Caminho/Connection String do SQL Server (Real)" value={realPath} onChange={e=> setRealPath(e.target.value)} />
              </div>
              <button className="btn btn-outline-success d-flex align-items-center" onClick={saveRealPath}>
                <i className="bi bi-save me-1" /> Salvar configuração
              </button>
            </div>
          )}
          {mode === 'Demo' && (
            <div className="d-flex align-items-end flex-wrap" style={{gap:12}}>
              <div className="input-group" style={{width:180}}>
                <span className="input-group-text"><i className="bi bi-123" /></span>
                <input type="number" min={1} max={1000} className="form-control" value={seedCount} onChange={e=> setSeedCount(Math.max(1, Math.min(1000, parseInt(e.target.value || '0', 10))))} />
              </div>
              <button className="btn btn-outline-primary d-flex align-items-center" onClick={seed} disabled={loading}>
                {loading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Adicionando...</> : <><i className="bi bi-database-add me-1" /> Adicionar dados fictícios</>}
              </button>
              <div className="text-muted" style={{fontSize:12}}>Adiciona registros de teste nas principais tabelas</div>
            </div>
          )}
          {msg && <div className="alert alert-success d-flex align-items-center" style={{gap:8}}><i className="bi bi-check-circle" /> {msg}</div>}
          {err && <div className="alert alert-danger d-flex align-items-center" style={{gap:8}}><i className="bi bi-exclamation-triangle" /> {err}</div>}
        </div>
      </div>
    </section>
  )
}
