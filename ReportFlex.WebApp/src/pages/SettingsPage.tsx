import React, { useEffect, useState } from 'react'
import { api } from '../api'

export function SettingsPage(){
  const [mode, setMode] = useState<'Real'|'Demo'>('Demo')
  const [loading, setLoading] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [seedCount, setSeedCount] = useState(100)
  const [realPath, setRealPath] = useState('')
  const [dbInfo, setDbInfo] = useState<any | null>(null)
  const [dbInfoErr, setDbInfoErr] = useState<string | null>(null)
  const [reportOptions, setReportOptions] = useState<{ txt: boolean, xlsx: boolean, pdf: boolean, word: boolean, excel: boolean, csv: boolean }>({
    txt: false, xlsx: true, pdf: true, word: false, excel: true, csv: true
  })
  const [reportOptionsLoading, setReportOptionsLoading] = useState(false)

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
        setDbInfoErr(null)
        try{
          const info = await api.getDbInfo()
          setDbInfo(info)
        }catch{
          setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
        }
        try{
          const opts = await api.getReportOptions()
          setReportOptions({
            txt: !!opts.txt,
            xlsx: !!opts.xlsx,
            pdf: !!opts.pdf,
            word: !!opts.word,
            excel: !!opts.excel,
            csv: !!opts.csv
          })
        }catch{}
      }catch{}
    })()
  }, [])

  async function applyMode(next: 'Real'|'Demo'){
    setErr(null); setMsg(null)
    try{
      const r = await api.setDbMode(next)
      setMode(r.mode || next)
      setMsg(`Modo alterado para ${r.mode || next}`)
      setDbInfoErr(null)
      
      // Se mudando para Real, carregar a connection string salva
      if (next === 'Real') {
        try {
          const c = await api.getConnections()
          if (c?.CMS) {
            setRealPath(c.CMS)
          }
        } catch {
          // Ignorar erro ao carregar conexões
        }
      }
      
      try{
        const info = await api.getDbInfo()
        setDbInfo(info)
      }catch{
        setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
      }
    }catch{
      setErr('Falha ao alterar modo')
    }
  }

  async function seedCompanies(){
    setErr(null); setMsg(null); setLoading(true)
    try{
      const r = await api.seedCompanies()
      if (r?.error){
        setErr(r.error)
      }else{
        const summary = Object.entries(r || {}).map(([k,v])=> `${k}:${v}`).join(', ')
        setMsg(`Empresas/funcionários/acessos gerados • ${summary}`)
      }
      try{
        const info = await api.getDbInfo()
        setDbInfo(info)
      }catch{
        // ignore
      }
    }catch(e:any){
      setErr(e?.message || 'Falha ao gerar dados')
    }finally{
      setLoading(false)
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
    setErr(null); setMsg(null); setDbInfoErr(null)
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
      if (r?.CMS){
        setRealPath(r.CMS)
      }
      setMsg('Configuração de conexão salva')
      try{
        const info = await api.getDbInfo()
        setDbInfo(info)
        setDbInfoErr(null)
      }catch{
        setDbInfoErr('Não foi possível carregar informações detalhadas do banco.')
      }
    }catch{
      setErr('Falha ao salvar configuração')
    }
  }

  async function saveReportOptions(){
    setErr(null); setMsg(null)
    setReportOptionsLoading(true)
    try{
      const r = await api.setReportOptions(reportOptions)
      setReportOptions({
        txt: !!r.txt,
        xlsx: !!r.xlsx,
        pdf: !!r.pdf,
        word: !!r.word,
        excel: !!r.excel,
        csv: !!r.csv
      })
      setMsg('Opções de formatos de relatórios salvas')
    }catch(e:any){
      setErr(e?.message || 'Falha ao salvar opções de relatórios')
    }finally{
      setReportOptionsLoading(false)
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
          <div className="d-flex align-items-end flex-wrap" style={{gap:12}}>
            <button className="btn btn-outline-secondary d-flex align-items-center" onClick={seedCompanies} disabled={loading}>
              {loading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Gerando...</> : <><i className="bi bi-people-fill me-1" /> Gerar empresas/funcionários (últimos 30 dias)</>}
            </button>
            <div className="text-muted" style={{fontSize:12}}>Cria empresas solicitadas, 20 funcionários e acessos em dias úteis</div>
          </div>
          {msg && <div className="alert alert-success d-flex align-items-center" style={{gap:8}}><i className="bi bi-check-circle" /> {msg}</div>}
          {err && <div className="alert alert-danger d-flex align-items-center" style={{gap:8}}><i className="bi bi-exclamation-triangle" /> {err}</div>}
        </div>
      </div>
      <div className="card" style={{marginTop:16}}>
        <div className="card-header d-flex align-items-center" style={{gap:8}}>
          <i className="bi bi-database-gear" /> Banco de dados em uso
        </div>
        <div className="card-body">
          {dbInfo && (
            <>
              <p className="text-muted" style={{fontSize:12, marginBottom:8}}>
                Estas informações mostram qual modo está ativo e para quais bancos (Logins e CMS) as conexões estão apontando, incluindo uma lista resumida de tabelas encontradas em cada base.
              </p>
              <div className="mb-2">
                <strong>Modo atual:</strong> {dbInfo.mode || mode}
              </div>
              <div className="row">
                <div className="col-md-6">
                  <h6>Base CMS</h6>
                  {dbInfo.databases?.CMS ? (
                    <>
                      <div style={{wordBreak:'break-all'}}>
                        <span className="text-muted" style={{fontSize:12}}>Connection String efetiva:</span><br/>
                        <code style={{fontSize:12}}>{dbInfo.databases.CMS.connection}</code>
                      </div>
                      <div className="mt-2">
                        <span className="text-muted" style={{fontSize:12}}>Tabelas detectadas (primeiras 20):</span>
                        <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                          {(dbInfo.databases.CMS.tables || []).slice(0,20).map((t:string)=>(
                            <li key={t}>{t}</li>
                          ))}
                        </ul>
                      </div>
                    </>
                  ) : (
                    <div className="text-muted" style={{fontSize:12}}>Não foi possível conectar na base CMS com as configurações atuais.</div>
                  )}
                </div>
                <div className="col-md-6">
                  <h6>Base Logins</h6>
                  {dbInfo.databases?.Logins ? (
                    <>
                      <div style={{wordBreak:'break-all'}}>
                        <span className="text-muted" style={{fontSize:12}}>Connection String efetiva:</span><br/>
                        <code style={{fontSize:12}}>{dbInfo.databases.Logins.connection}</code>
                      </div>
                      <div className="mt-2">
                        <span className="text-muted" style={{fontSize:12}}>Tabelas detectadas (primeiras 20):</span>
                        <ul style={{maxHeight:160, overflowY:'auto', fontSize:12, paddingLeft:18}}>
                          {(dbInfo.databases.Logins.tables || []).slice(0,20).map((t:string)=>(
                            <li key={t}>{t}</li>
                          ))}
                        </ul>
                      </div>
                    </>
                  ) : (
                    <div className="text-muted" style={{fontSize:12}}>Não foi possível conectar na base Logins com as configurações atuais.</div>
                  )}
                </div>
              </div>
            </>
          )}
          {!dbInfo && !dbInfoErr && (
            <div className="text-muted" style={{fontSize:12}}>Carregando informações do banco...</div>
          )}
          {dbInfoErr && (
            <div className="alert alert-warning d-flex align-items-center mt-2" style={{gap:8}}>
              <i className="bi bi-exclamation-triangle" /> {dbInfoErr}
            </div>
          )}
        </div>
      </div>
      <div className="card" style={{marginTop:16}}>
        <div className="card-header d-flex align-items-center" style={{gap:8}}>
          <i className="bi bi-filetype-pdf" /> Formatos de relatórios disponíveis
        </div>
        <div className="card-body d-flex flex-column" style={{gap:12}}>
          <p className="text-muted" style={{fontSize:12, marginBottom:4}}>
            Os formatos ativados aqui aparecerão como botões de sugestão quando uma consulta retornar dados.
          </p>
          <div className="d-flex flex-wrap" style={{gap:12}}>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repTxt" checked={reportOptions.txt} onChange={e=> setReportOptions(o=> ({...o, txt: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repTxt">TXT</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repCsv" checked={reportOptions.csv} onChange={e=> setReportOptions(o=> ({...o, csv: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repCsv">CSV</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repXlsx" checked={reportOptions.xlsx} onChange={e=> setReportOptions(o=> ({...o, xlsx: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repXlsx">XLSX</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repExcel" checked={reportOptions.excel} onChange={e=> setReportOptions(o=> ({...o, excel: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repExcel">Excel (compatível)</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repPdf" checked={reportOptions.pdf} onChange={e=> setReportOptions(o=> ({...o, pdf: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repPdf">PDF</label>
            </div>
            <div className="form-check form-switch">
              <input className="form-check-input" type="checkbox" id="repWord" checked={reportOptions.word} onChange={e=> setReportOptions(o=> ({...o, word: e.target.checked}))} />
              <label className="form-check-label" htmlFor="repWord">Word</label>
            </div>
          </div>
          <div>
            <button className="btn btn-outline-primary d-flex align-items-center" onClick={saveReportOptions} disabled={reportOptionsLoading}>
              {reportOptionsLoading ? <><span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Salvando...</> : <><i className="bi bi-save me-1" /> Salvar formatos</>}
            </button>
          </div>
        </div>
      </div>
    </section>
  )
}
