import React, { useEffect, useState, useRef } from 'react'
import { api } from '../api'

export function ClientesPage(){
  const [rows, setRows] = useState<any[]>([])
  const [form, setForm] = useState<any>({})
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [status, setStatus] = useState<string | null>(null)
  const [saving, setSaving] = useState<boolean>(false)
  const [deleting, setDeleting] = useState<boolean>(false)
  const [showDelete, setShowDelete] = useState<boolean>(false)
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const fileRef = useRef<HTMLInputElement | null>(null)
  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }
  async function load(){
    try{
      const data = await api.clientes()
      if (Array.isArray(data)){
        const norm = data.map((x:any)=> ({
          SBID: x.SBID ?? x.sbid ?? x.Id ?? x.id,
          NOME: x.NOME ?? x.nome ?? '',
          ENDERECO: x.ENDERECO ?? x.endereco ?? '',
          FONE: x.FONE ?? x.fone ?? '',
          EMAIL: x.EMAIL ?? x.email ?? '',
          SITE: x.SITE ?? x.site ?? '',
          ATIVO: x.ATIVO ?? x.ativo ?? 1,
          CAMINHOIMG: x.CAMINHOIMG ?? x.caminhoimg ?? x.logoPath ?? '',
          RESPONSAVEL: x.RESPONSAVEL ?? x.responsavel ?? '',
          TOKEN: x.TOKEN ?? x.token ?? x.CLIENT_TOKEN ?? x.client_TOKEN ?? ''
        }))
        setRows(norm)
      }else{
        setRows([])
        toast('error','Resposta inesperada de /api/clientes')
      }
    }catch(e:any){
      toast('error','Erro ao carregar clientes: ' + (e?.message || 'desconhecido'))
    }
  }
  useEffect(()=>{ load() },[])
  function pick(r:any){ setSelectedId(r.SBID); setForm({ nome:r.NOME??'', endereco:r.ENDERECO??'', fone:r.FONE??'', email:r.EMAIL??'', site:r.SITE??'', ativo:r.ATIVO??1, responsavel:r.RESPONSAVEL??'', token:r.TOKEN??'', logoPath:r.CAMINHOIMG??'' }) }
  function clear(){ setSelectedId(null); setForm({ nome:'', endereco:'', fone:'', email:'', site:'', ativo:1, responsavel:'', token:'', logoPath:'' }); setLogoFile(null) }
  async function save(){
    try{
      setSaving(true)
      setStatus(null)
      if (!form.nome){
        setStatus('Informe o nome')
        toast('warning','Informe o nome do cliente')
        return
      }
      if (selectedId == null){
        toast('info','Salvando cliente...')
        const r = await api.adminClientsCreate(form)
        if (r && typeof r.id === 'number'){
          setStatus('Criado: ' + r.id + (r?.token ? ' • Token: ' + r.token : ''))
          toast('success', 'Cliente criado com sucesso' + (r?.token ? ` • Token: ${r.token}` : ''))
          setSelectedId(r.id)
          setForm((f:any)=> ({ ...f, token: r.token }))
          if (logoFile){
            await uploadLogo(logoFile, r.id)
          }
        } else {
          toast('error','Resposta inesperada do servidor ao criar cliente')
          setStatus('Erro ao salvar: resposta inesperada')
        }
      }else{
        toast('info','Salvando alterações...')
        const r = await api.adminClientsUpdate(selectedId, form)
        if (r !== undefined){
          if (logoFile){
            await uploadLogo(logoFile, selectedId)
          }
          setStatus('Atualizado')
          toast('success', 'Cliente atualizado')
        } else {
          toast('error','Resposta inesperada do servidor ao atualizar')
          setStatus('Erro ao salvar: resposta inesperada')
        }
      }
      await load()
    }catch(e:any){
      setStatus('Erro ao salvar: ' + (e?.message || 'desconhecido'))
      toast('error', 'Erro ao salvar: ' + (e?.message || 'desconhecido'))
    } finally {
      setSaving(false)
    }
  }
  async function genToken(){
    try{
      if (selectedId == null){
        const rnd = Math.floor(Math.random()*10000).toString().padStart(4,'0')
        setForm((f:any)=> ({ ...f, token: rnd }))
        setStatus('Token sugerido (será validado ao salvar)')
        toast('info', 'Token sugerido no formulário')
        return
      }
      const r = await api.adminClientsGenerateToken(selectedId)
      setForm((f:any)=> ({ ...f, token: r.token }))
      setStatus('Token gerado')
      toast('success', 'Token gerado')
      await load()
    }catch(e:any){
      setStatus('Erro ao gerar token: ' + (e?.message || 'desconhecido'))
      toast('error', 'Erro ao gerar token: ' + (e?.message || 'desconhecido'))
    }
  }
  async function uploadLogo(file: File, overrideId?: number){
    try{
      const id = overrideId ?? selectedId
      if (id == null){ setStatus('Selecione um cliente'); return }
      const r = await api.adminClientsUploadLogo(id, file)
      setForm((f:any)=> ({ ...f, logoPath: r.logoPath }))
      setStatus('Logo atualizada')
      toast('success', 'Logomarca atualizada')
      await load()
    }catch(e:any){
      setStatus('Erro no upload: ' + (e?.message || 'desconhecido'))
      toast('error', 'Erro no upload: ' + (e?.message || 'desconhecido'))
    }
  }
  function handleLogoChange(file: File){
    setLogoFile(file)
    setForm((f:any)=> ({ ...f, logoPath: URL.createObjectURL(file) }))
  }
  async function confirmDelete(){
    if (selectedId == null) return
    setDeleting(true)
    try{
      await api.adminClientsDelete(selectedId)
      toast('success','Cliente excluído')
      setSelectedId(null)
      setForm({ nome:'', endereco:'', fone:'', email:'', site:'', ativo:1, responsavel:'', token:'', logoPath:'' })
      await load()
    }catch(e:any){
      toast('error','Erro ao excluir: ' + (e?.message || 'desconhecido'))
    }finally{
      setDeleting(false)
      setShowDelete(false)
    }
  }
  return (
    <section style={{display:'grid', gridTemplateColumns:'1fr 380px', gap:16}}>
      <div>
        <h2>Clientes</h2>
        <div className="mb-2">Total: {rows.length}</div>
        {rows.length === 0 && <div className="text-muted mb-2">Nenhum cliente cadastrado ainda.</div>}
        <table className="table table-striped">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nome</th>
              <th>Token</th>
              <th>Ativo</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r,i)=> (
              <tr key={i} onClick={()=>pick(r)} style={{cursor:'pointer', backgroundColor: selectedId===r.SBID ? '#f0f6ff' : undefined}}>
                <td>{r.SBID}</td>
                <td>{r.NOME}</td>
                <td>{r.TOKEN}</td>
                <td>{r.ATIVO === 1 ? 'Sim' : 'Não'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div>
        <h3>{selectedId ? 'Editar Cliente' : 'Novo Cliente'}</h3>
        <div className="mb-2"><label>Nome</label><input className="form-control" value={form.nome??''} onChange={e=>setForm({...form, nome:e.target.value})} /></div>
        <div className="mb-2"><label>Endereço</label><input className="form-control" value={form.endereco??''} onChange={e=>setForm({...form, endereco:e.target.value})} /></div>
        <div className="mb-2"><label>Telefone</label><input className="form-control" value={form.fone??''} onChange={e=>setForm({...form, fone:e.target.value})} /></div>
        <div className="mb-2"><label>Email</label><input className="form-control" value={form.email??''} onChange={e=>setForm({...form, email:e.target.value})} /></div>
        <div className="mb-2"><label>Site</label><input className="form-control" value={form.site??''} onChange={e=>setForm({...form, site:e.target.value})} /></div>
        <div className="mb-2"><label>Responsável</label><input className="form-control" value={form.responsavel??''} onChange={e=>setForm({...form, responsavel:e.target.value})} /></div>
        <div className="mb-2"><label>Ativo</label>
          <select className="form-select" value={form.ativo??1} onChange={e=>setForm({...form, ativo: Number(e.target.value) })}>
            <option value={1}>Sim</option>
            <option value={0}>Não</option>
          </select>
        </div>
        <div className="mb-2"><label>Token</label>
          <div className="d-flex gap-2">
            <input className="form-control" value={form.token??''} onChange={e=>setForm({...form, token:e.target.value})} />
            <button className="btn btn-outline-secondary" onClick={genToken}>Gerar</button>
          </div>
        </div>
        <div className="mb-2">
          <label>Logomarca</label>
          <div className="d-flex align-items-center gap-2">
            <div
              onClick={()=> fileRef.current?.click()}
              style={{
                width:64, height:64, borderRadius:'50%', overflow:'hidden',
                background:'#e9ecef', display:'flex', alignItems:'center', justifyContent:'center',
                cursor:'pointer', border:'2px dashed #adb5bd', flexShrink:0
              }}
            >
              {form.logoPath
                ? <img alt="logo" src={form.logoPath} style={{width:'100%', height:'100%', objectFit:'cover'}} />
                : <span style={{fontSize:18, color:'#6c757d'}}>{(form.nome || '?').toString().charAt(0).toUpperCase()}</span>}
            </div>
            <div className="d-flex flex-column" style={{gap:4}}>
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                onClick={()=> fileRef.current?.click()}
              >
                Escolher imagem
              </button>
              <small className="text-muted">Clique na bola ou no botão para selecionar uma foto.</small>
            </div>
            <input
              ref={fileRef}
              type="file"
              accept="image/*"
              style={{display:'none'}}
              onChange={e=> e.target.files && handleLogoChange(e.target.files[0])}
            />
          </div>
        </div>
        <div className="d-flex gap-2">
          <button className="btn btn-primary" onClick={save} disabled={saving}>
            {saving ? 'Salvando...' : (selectedId ? 'Salvar alterações' : 'Criar')}
          </button>
          <button className="btn btn-secondary" onClick={clear}>Novo</button>
          {selectedId && (
            <button className="btn btn-outline-danger" onClick={()=> setShowDelete(true)}>
              Excluir
            </button>
          )}
        </div>
        {status && <div className="mt-2" style={{color:'#0a6'}}> {status} </div>}
      </div>
      {showDelete && (
        <div style={{
          position:'fixed', inset:0, background:'rgba(15,23,42,0.65)', display:'flex',
          alignItems:'center', justifyContent:'center', zIndex:10000
        }}>
          <div style={{background:'#fff', padding:20, borderRadius:8, minWidth:320, maxWidth:400, boxShadow:'0 10px 30px rgba(0,0,0,0.25)'}}>
            <h5 style={{marginBottom:12}}>Excluir cliente</h5>
            <p style={{marginBottom:16}}>Tem certeza que deseja excluir o cliente <strong>{form.nome || selectedId}</strong>? Esta ação não poderá ser desfeita.</p>
            <div className="d-flex justify-content-end" style={{gap:8}}>
              <button className="btn btn-secondary" onClick={()=> setShowDelete(false)} disabled={deleting}>Cancelar</button>
              <button className="btn btn-danger" onClick={confirmDelete} disabled={deleting}>
                {deleting ? 'Excluindo...' : 'Excluir'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
