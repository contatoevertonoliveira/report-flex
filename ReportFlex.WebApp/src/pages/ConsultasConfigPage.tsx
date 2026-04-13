import React from 'react'
import { api } from '../api'

type QuickKey =
  | 'access-agg' | 'transit-period' | 'population' | 'eventos-claviculario' | 'door-critical'
  | 'employees' | 'external' | 'card-by-cpf'
  | 'cpf' | 'matricula' | 'empresa' | 'cracha' | 'nivel' | 'visitantes'

export function ConsultasConfigPage() {
  const [cfg, setCfg] = React.useState<Record<string, boolean>>({})
  const [loading, setLoading] = React.useState(false)
  const [saving, setSaving] = React.useState(false)
  React.useEffect(() => {
    setLoading(true)
    api.getQueriesConfig()
      .then(data => setCfg(data || {}))
      .catch(err => toast('error', err.message || String(err)))
      .finally(()=> setLoading(false))
  }, [])
  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }
  function toggle(key: QuickKey){
    setCfg(prev => ({ ...prev, [key]: !prev[key] }))
  }
  async function handleSave(){
    setSaving(true)
    try{
      await api.setQueriesConfig(cfg)
      toast('success', 'Configurações salvas. Abra a tela Consultas para ver.')
    }catch(err: any){
      toast('error', err.message || String(err))
    }finally{
      setSaving(false)
    }
  }
  const options: Array<{ key: QuickKey, label: string }> = [
    { key:'access-agg', label:'Acessos Agregados' },
    { key:'transit-period', label:'Trânsito por Período' },
    { key:'population', label:'População' },
    { key:'eventos-claviculario', label:'Eventos_Claviculario' },
    { key:'door-critical', label:'Eventos de Porta' },
    { key:'employees', label:'Funcionários' },
    { key:'external', label:'Externos' },
    { key:'card-by-cpf', label:'Buscar Crachá por CPF' },
    { key:'cpf', label:'CPF (Cadastro/Acessos)' },
    { key:'matricula', label:'Matrícula' },
    { key:'empresa', label:'Empresa' },
    { key:'cracha', label:'Crachá' },
    { key:'nivel', label:'Nível de Acesso' },
    { key:'visitantes', label:'Visitantes' }
  ]
  return (
    <div className="container-fluid">
      <div className="card">
        <div className="card-header d-flex justify-content-between align-items-center">
          <div className="d-flex align-items-center gap-2">
            <i className="bi bi-sliders" />
            <strong>Consultas Config</strong>
          </div>
          <div>
            <button className="btn btn-primary" onClick={handleSave} disabled={saving || loading}>
              {saving ? 'Salvando...' : 'Salvar'}
            </button>
          </div>
        </div>
        <div className="card-body">
          <p className="text-muted">Ative/desative as consultas prontas que devem aparecer na tela de Consultas. Por padrão, todas desativadas.</p>
          <div className="queries-ready-switches" style={{marginBottom:12}}>
            {options.map(opt => {
              const id = `cfg_${opt.key}`
              return (
                <div key={opt.key} className="queries-ready-switch">
                  <div className="form-check form-switch d-flex align-items-center justify-content-between border rounded px-3 py-2">
                    <label className="form-check-label flex-grow-1 me-2" htmlFor={id}>{opt.label}</label>
                    <input
                      id={id}
                      className="form-check-input"
                      type="checkbox"
                      role="switch"
                      disabled={loading}
                      checked={!!cfg[opt.key]}
                      onChange={()=> toggle(opt.key)}
                    />
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      </div>
    </div>
  )
}
