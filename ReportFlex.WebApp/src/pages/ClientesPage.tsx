import React, { useEffect, useState, useRef } from 'react'
import { api } from '../api'

export function ClientesPage(){
  const nivel = (typeof window !== 'undefined' ? (localStorage.getItem('rf_level') || '') : '')
  const isSuperAdmin = nivel === 'SuperAdmin'
  const [clientModalOpen, setClientModalOpen] = useState(false)
  const [rows, setRows] = useState<any[]>([])
  const [form, setForm] = useState<any>({})
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [status, setStatus] = useState<string | null>(null)
  const [saving, setSaving] = useState<boolean>(false)
  const [deleting, setDeleting] = useState<boolean>(false)
  const [showDelete, setShowDelete] = useState<boolean>(false)
  const [defaultReportClientId, setDefaultReportClientId] = useState<number | null>(null)
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const fileRef = useRef<HTMLInputElement | null>(null)
  const [users, setUsers] = useState<any[]>([])
  const [usersLoading, setUsersLoading] = useState(false)
  const [usersErr, setUsersErr] = useState<string | null>(null)
  const [userForm, setUserForm] = useState<{ email: string, nome: string, nivel: 'SuperAdmin'|'Administrador'|'Básico' }>({ email: '', nome: '', nivel: 'Básico' })
  const [creatingUser, setCreatingUser] = useState(false)
  const [tempPwdModalOpen, setTempPwdModalOpen] = useState(false)
  const [tempPwdEmail, setTempPwdEmail] = useState<string>('')
  const [tempPwdValue, setTempPwdValue] = useState<string>('')
  const [tempPwdCopied, setTempPwdCopied] = useState(false)
  const [editUserOpen, setEditUserOpen] = useState(false)
  const [editUserSaving, setEditUserSaving] = useState(false)
  const [editUserForm, setEditUserForm] = useState<{ id: number, email: string, nome: string, nivel: 'SuperAdmin'|'Administrador'|'Básico', isActive: boolean }>({ id: 0, email: '', nome: '', nivel: 'Básico', isActive: true })
  const [deleteUserOpen, setDeleteUserOpen] = useState(false)
  const [deleteUserSaving, setDeleteUserSaving] = useState(false)
  const [deleteUserInfo, setDeleteUserInfo] = useState<{ id: number, nome: string, email: string } | null>(null)
  const [toggleUserOpen, setToggleUserOpen] = useState(false)
  const [toggleUserSaving, setToggleUserSaving] = useState(false)
  const [toggleUserInfo, setToggleUserInfo] = useState<{ id: number, nome: string, email: string, nextActive: boolean } | null>(null)
  function toast(type: 'success'|'error'|'info'|'warning', message: string){
    window.dispatchEvent(new CustomEvent('app:toast', { detail: { type, message } }))
  }
  async function load(){
    try{
      try{
        const dr = await api.getReportDefaultClient()
        const id = typeof (dr as any)?.id === 'number' ? (dr as any).id : null
        setDefaultReportClientId(id)
      }catch{}
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
  async function loadUsers(clientId: number){
    setUsersLoading(true)
    setUsersErr(null)
    try{
      const r: any = await api.adminUsers({ page: 1, pageSize: 200, clientId })
      setUsers(Array.isArray(r?.items) ? r.items : [])
    }catch(e:any){
      setUsers([])
      setUsersErr(e?.message || 'Erro ao carregar usuários')
    }finally{
      setUsersLoading(false)
    }
  }
  useEffect(() => {
    if (!selectedId) { setUsers([]); return }
    loadUsers(selectedId)
  }, [selectedId])
  function pick(r:any){ setSelectedId(r.SBID); setForm({ nome:r.NOME??'', endereco:r.ENDERECO??'', fone:r.FONE??'', email:r.EMAIL??'', site:r.SITE??'', ativo:r.ATIVO??1, responsavel:r.RESPONSAVEL??'', token:r.TOKEN??'', logoPath:r.CAMINHOIMG??'' }) }
  function clear(){ setSelectedId(null); setForm({ nome:'', endereco:'', fone:'', email:'', site:'', ativo:1, responsavel:'', token:'', logoPath:'' }); setLogoFile(null) }
  function openNewClient(){
    if (!isSuperAdmin) return
    clear()
    setClientModalOpen(true)
  }
  function openEditClient(){
    if (!isSuperAdmin) return
    if (selectedId == null){ toast('warning','Selecione um cliente'); return }
    setClientModalOpen(true)
  }
  async function setAsDefaultReportClient(){
    if (selectedId == null) return
    try{
      await api.setReportDefaultClient(selectedId)
      setDefaultReportClientId(selectedId)
      toast('success','Cliente definido como padrão do relatório')
    }catch(e:any){
      toast('error','Erro ao definir padrão: ' + (e?.message || 'desconhecido'))
    }
  }
  async function save(){
    let ok = false
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
          ok = true
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
          ok = true
        } else {
          toast('error','Resposta inesperada do servidor ao atualizar')
          setStatus('Erro ao salvar: resposta inesperada')
        }
      }
      await load()
      if (ok){
        setClientModalOpen(false)
        setLogoFile(null)
      }
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
      setClientModalOpen(false)
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
  async function createUser(){
    if (!selectedId) { toast('warning','Selecione um cliente'); return }
    const email = (userForm.email || '').trim()
    const nome = (userForm.nome || '').trim()
    if (!email || !email.includes('@')) { toast('warning','Informe um email válido'); return }
    if (!nome) { toast('warning','Informe o nome'); return }
    setCreatingUser(true)
    try{
      const r: any = await api.adminUsersCreate({ email, nome, nivel: userForm.nivel, clientId: selectedId })
      if (r?.tempPassword){
        toast('success', `Usuário criado • Senha temporária: ${r.tempPassword}`)
        setTempPwdEmail(email)
        setTempPwdValue(String(r.tempPassword))
        setTempPwdCopied(false)
        setTempPwdModalOpen(true)
      } else {
        toast('success', 'Usuário criado')
      }
      setUserForm({ email: '', nome: '', nivel: 'Básico' })
      await loadUsers(selectedId)
    }catch(e:any){
      toast('error', e?.message || 'Erro ao criar usuário')
    }finally{
      setCreatingUser(false)
    }
  }
  function openEditUser(u: any){
    const id = typeof u?.Id === 'number' ? u.Id : (typeof u?.id === 'number' ? u.id : 0)
    const email = String(u?.Email || u?.email || '')
    const nome = String(u?.Nome || u?.nome || '')
    const nivelRaw = String(u?.Nivel || u?.nivel || 'Básico')
    const nivel = (nivelRaw === 'SuperAdmin' ? 'SuperAdmin' : nivelRaw === 'Administrador' ? 'Administrador' : 'Básico') as any
    const isActive = u?.IsActive === false ? false : true
    setEditUserForm({ id, email, nome, nivel, isActive })
    setEditUserOpen(true)
  }
  async function saveEditUser(){
    const id = editUserForm.id
    if (!id) return
    const email = (editUserForm.email || '').trim()
    const nome = (editUserForm.nome || '').trim()
    if (!email || !email.includes('@')) { toast('warning','Informe um email válido'); return }
    if (!nome) { toast('warning','Informe o nome'); return }
    setEditUserSaving(true)
    try{
      await api.adminUsersUpdate(id, { email, nome, nivel: editUserForm.nivel, isActive: editUserForm.isActive })
      toast('success','Usuário atualizado')
      setEditUserOpen(false)
      await loadUsers(selectedId as number)
    }catch(e:any){
      toast('error', e?.message || 'Erro ao atualizar usuário')
    }finally{
      setEditUserSaving(false)
    }
  }
  function requestToggleUser(u: any){
    const id = typeof u?.Id === 'number' ? u.Id : (typeof u?.id === 'number' ? u.id : 0)
    if (!id) return
    const nome = String(u?.Nome || u?.nome || '')
    const email = String(u?.Email || u?.email || '')
    const nextActive = (u?.IsActive === false || u?.isActive === false) ? true : false
    setToggleUserInfo({ id, nome, email, nextActive })
    setToggleUserOpen(true)
  }
  async function confirmToggleUser(){
    if (!toggleUserInfo) return
    setToggleUserSaving(true)
    try{
      await api.adminUsersUpdate(toggleUserInfo.id, { isActive: toggleUserInfo.nextActive })
      toast('success', toggleUserInfo.nextActive ? 'Usuário desbloqueado' : 'Usuário bloqueado')
      setToggleUserOpen(false)
      setToggleUserInfo(null)
      await loadUsers(selectedId as number)
    }catch(e:any){
      toast('error', e?.message || 'Erro ao alterar status do usuário')
    }finally{
      setToggleUserSaving(false)
    }
  }
  async function resetUserPassword(u: any){
    const id = typeof u?.Id === 'number' ? u.Id : (typeof u?.id === 'number' ? u.id : 0)
    if (!id) return
    try{
      const r: any = await api.adminUsersResetPassword(id)
      const email = String(r?.email || u?.Email || u?.email || '')
      const pwd = String(r?.tempPassword || '')
      if (pwd){
        setTempPwdEmail(email)
        setTempPwdValue(pwd)
        setTempPwdCopied(false)
        setTempPwdModalOpen(true)
        toast('success','Senha temporária gerada')
      }else{
        toast('success','Senha redefinida')
      }
      await loadUsers(selectedId as number)
    }catch(e:any){
      toast('error', e?.message || 'Erro ao redefinir senha')
    }
  }
  function requestDeleteUser(u: any){
    const id = typeof u?.Id === 'number' ? u.Id : (typeof u?.id === 'number' ? u.id : 0)
    const nome = String(u?.Nome || u?.nome || '')
    const email = String(u?.Email || u?.email || '')
    if (!id) return
    setDeleteUserInfo({ id, nome, email })
    setDeleteUserOpen(true)
  }
  async function confirmDeleteUser(){
    if (!deleteUserInfo) return
    setDeleteUserSaving(true)
    try{
      await api.adminUsersDelete(deleteUserInfo.id)
      toast('success','Usuário excluído')
      setDeleteUserOpen(false)
      setDeleteUserInfo(null)
      await loadUsers(selectedId as number)
    }catch(e:any){
      toast('error', e?.message || 'Erro ao excluir usuário')
    }finally{
      setDeleteUserSaving(false)
    }
  }
  const getUserEmail = (u: any) => String(u?.Email || u?.email || '')
  const getUserNome = (u: any) => String(u?.Nome || u?.nome || '')
  const getUserNivel = (u: any) => String(u?.Nivel || u?.nivel || '')
  async function copyTempPassword(){
    const txt = tempPwdValue || ''
    if (!txt) return
    try{
      if (navigator.clipboard && navigator.clipboard.writeText){
        await navigator.clipboard.writeText(txt)
        setTempPwdCopied(true)
        toast('success','Senha copiada')
        return
      }
    }catch{}
    try{
      const ta = document.createElement('textarea')
      ta.value = txt
      document.body.appendChild(ta)
      ta.select()
      document.execCommand('copy')
      document.body.removeChild(ta)
      setTempPwdCopied(true)
      toast('success','Senha copiada')
    }catch{
      toast('error','Não foi possível copiar')
    }
  }
  return (
    <section className="page">
      <h2>Clientes</h2>
      <div className="d-flex flex-wrap align-items-center justify-content-between" style={{gap:12, marginBottom:8}}>
        <div className="text-light">Total: {rows.length}</div>
        {isSuperAdmin && (
          <div className="d-flex flex-wrap" style={{gap:8}}>
            <button className="btn btn-outline-secondary" type="button" onClick={openNewClient}>
              Novo cliente
            </button>
            {selectedId != null && (
              <button className="btn btn-outline-secondary" type="button" onClick={openEditClient}>
                Editar cliente
              </button>
            )}
          </div>
        )}
      </div>

      {rows.length === 0 && <div className="text-muted mb-2">Nenhum cliente cadastrado ainda.</div>}
      <div className="table-responsive">
        <table className="table table-hover table-striped align-middle rf-table-light">
          <thead>
            <tr>
              <th style={{width:70}}>ID</th>
              <th>Nome</th>
              <th style={{width:160}}>Token</th>
              <th style={{width:80}}>Ativo</th>
              <th style={{width:90}}>Padrão</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r,i)=> (
              <tr key={i} onClick={()=>pick(r)} className={selectedId===r.SBID ? 'rf-row-selected' : undefined} style={{cursor:'pointer'}}>
                <td>{r.SBID}</td>
                <td>{r.NOME}</td>
                <td>{isSuperAdmin ? r.TOKEN : (r.TOKEN ? `${'*'.repeat(Math.max(0, String(r.TOKEN).length - 4))}${String(r.TOKEN).slice(-4)}` : '')}</td>
                <td>{r.ATIVO === 1 ? 'Sim' : 'Não'}</td>
                <td>{defaultReportClientId === r.SBID ? 'Sim' : ''}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div style={{marginTop:16}}>
        <h3 style={{marginBottom:8}}>Usuários</h3>
        {!selectedId && (
          <div className="text-muted">Selecione um cliente na lista para ver e gerenciar os usuários.</div>
        )}
        {selectedId && (
          <>
            <div className="d-flex flex-wrap" style={{gap:12}}>
              <div style={{flex:1, minWidth:220}}>
                <label>Email</label>
                <input className="form-control" value={userForm.email} onChange={e=>setUserForm({ ...userForm, email: e.target.value })} />
              </div>
              <div style={{flex:1, minWidth:220}}>
                <label>Nome</label>
                <input className="form-control" value={userForm.nome} onChange={e=>setUserForm({ ...userForm, nome: e.target.value })} />
              </div>
              <div style={{width:200}}>
                <label>Nível</label>
                <select className="form-select" value={userForm.nivel} onChange={e=>setUserForm({ ...userForm, nivel: e.target.value as any })}>
                  {isSuperAdmin && <option value="SuperAdmin">SuperAdmin</option>}
                  <option value="Administrador">Administrador</option>
                  <option value="Básico">Básico</option>
                </select>
              </div>
              <div className="d-flex align-items-end" style={{gap:8}}>
                <button className="btn btn-primary" onClick={createUser} disabled={creatingUser || usersLoading}>
                  {creatingUser ? 'Criando...' : 'Criar usuário'}
                </button>
                <button className="btn btn-outline-secondary" onClick={()=> loadUsers(selectedId)} disabled={usersLoading}>Atualizar</button>
              </div>
            </div>

            {usersErr && <div className="text-danger mt-2">{usersErr}</div>}
            <div className="table-responsive" style={{marginTop:10}}>
              <table className="table table-hover table-striped align-middle rf-table-light">
                <thead>
                  <tr>
                    <th>Nome</th>
                    <th>Login</th>
                    <th style={{width:140}}>Nível</th>
                    <th style={{width:90}}>Ativo</th>
                    <th style={{width:240}}>Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {usersLoading && (
                    <tr><td colSpan={5}>Carregando...</td></tr>
                  )}
                  {!usersLoading && users.length === 0 && (
                    <tr><td colSpan={5}>Nenhum usuário cadastrado para este cliente.</td></tr>
                  )}
                  {!usersLoading && users.map((u:any)=> (
                    <tr
                      key={u.Id ?? u.id ?? u.Email}
                      title={`${getUserNome(u)} - ${getUserEmail(u)}`.trim()}
                    >
                      <td title={getUserNome(u)}>{getUserNome(u)}</td>
                      <td title={getUserEmail(u)}>{getUserEmail(u)}</td>
                      <td>{getUserNivel(u)}</td>
                      <td>{(u.IsActive === false || u.isActive === false) ? 'Não' : 'Sim'}</td>
                      <td>
                        <div className="d-flex flex-nowrap align-items-center" style={{gap:4}}>
                          <button className="btn btn-sm btn-outline-primary btn-icon" type="button" onClick={()=> openEditUser(u)} disabled={usersLoading || creatingUser} title="Editar usuário">
                            <i className="bi bi-pencil-square" />
                          </button>
                          <button className="btn btn-sm btn-outline-secondary btn-icon" type="button" onClick={()=> resetUserPassword(u)} disabled={usersLoading || creatingUser} title="Gerar senha temporária">
                            <i className="bi bi-key" />
                          </button>
                          <button
                            className={(u.IsActive === false || u.isActive === false) ? "btn btn-sm btn-outline-success btn-icon" : "btn btn-sm btn-outline-danger btn-icon"}
                            type="button"
                            onClick={()=> requestToggleUser(u)}
                            disabled={usersLoading || creatingUser}
                            title={(u.IsActive === false || u.isActive === false) ? "Desbloquear usuário" : "Bloquear usuário"}
                          >
                            <i className={(u.IsActive === false || u.isActive === false) ? "bi bi-unlock" : "bi bi-lock"} />
                          </button>
                          {(isSuperAdmin || ((u.Nivel || u.nivel || '') !== 'SuperAdmin')) && (
                            <button
                              className="btn btn-sm btn-outline-danger btn-icon"
                              type="button"
                              onClick={()=> requestDeleteUser(u)}
                              disabled={usersLoading || creatingUser}
                              title="Excluir usuário"
                            >
                              <i className="bi bi-trash" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>

      {clientModalOpen && isSuperAdmin && (
        <div style={{
          position:'fixed', inset:0, background:'rgba(15,23,42,0.65)', display:'flex',
          alignItems:'center', justifyContent:'center', zIndex:10000
        }}>
          <div style={{background:'#fff', color:'#111827', padding:20, borderRadius:8, width:'min(920px, 96vw)', maxHeight:'90vh', overflow:'auto', boxShadow:'0 10px 30px rgba(0,0,0,0.25)'}}>
            <div className="d-flex align-items-center justify-content-between" style={{gap:12, marginBottom:12}}>
              <h5 style={{margin:0}}>{selectedId ? 'Editar cliente' : 'Novo cliente'}</h5>
              <button className="btn btn-sm btn-outline-secondary" type="button" onClick={()=> { setClientModalOpen(false); setLogoFile(null) }}>
                Fechar
              </button>
            </div>

            <div className="row" style={{alignItems:'flex-start', gap:12}}>
              <div style={{flex:1}}>
                <div className="mb-2"><label>Nome</label><input className="form-control" value={form.nome??''} onChange={e=>setForm({...form, nome:e.target.value})} /></div>
                <div className="mb-2"><label>Endereço</label><input className="form-control" value={form.endereco??''} onChange={e=>setForm({...form, endereco:e.target.value})} /></div>
                <div className="mb-2"><label>Telefone</label><input className="form-control" value={form.fone??''} onChange={e=>setForm({...form, fone:e.target.value})} /></div>
                <div className="mb-2"><label>Email</label><input className="form-control" value={form.email??''} onChange={e=>setForm({...form, email:e.target.value})} /></div>
                <div className="mb-2"><label>Site</label><input className="form-control" value={form.site??''} onChange={e=>setForm({...form, site:e.target.value})} /></div>
                <div className="mb-2"><label>Responsável</label><input className="form-control" value={form.responsavel??''} onChange={e=>setForm({...form, responsavel:e.target.value})} /></div>
              </div>
              <div style={{width:320}}>
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
                      <button type="button" className="btn btn-sm btn-outline-secondary" onClick={()=> fileRef.current?.click()}>
                        Escolher imagem
                      </button>
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
              </div>
            </div>

            <div className="d-flex flex-wrap justify-content-end" style={{gap:8, marginTop:12}}>
              {selectedId && (
                <button className="btn btn-outline-success" onClick={setAsDefaultReportClient}>
                  Definir como padrão do relatório
                </button>
              )}
              {selectedId && (
                <button className="btn btn-outline-danger" onClick={()=> setShowDelete(true)}>
                  Excluir
                </button>
              )}
              <button className="btn btn-primary" onClick={save} disabled={saving}>
                {saving ? 'Salvando...' : (selectedId ? 'Salvar alterações' : 'Criar')}
              </button>
            </div>
            {status && <div className="mt-2" style={{color:'#0a6'}}>{status}</div>}
          </div>
        </div>
      )}
      {showDelete && (
        <div style={{
          position:'fixed', inset:0, background:'rgba(15,23,42,0.65)', display:'flex',
          alignItems:'center', justifyContent:'center', zIndex:10000
        }}>
          <div style={{background:'#fff', color:'#111827', padding:20, borderRadius:8, minWidth:320, maxWidth:400, boxShadow:'0 10px 30px rgba(0,0,0,0.25)'}}>
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
      {tempPwdModalOpen && (
        <div style={{
          position:'fixed', inset:0, background:'rgba(15,23,42,0.65)', display:'flex',
          alignItems:'center', justifyContent:'center', zIndex:10000
        }}>
          <div style={{background:'#fff', color:'#111827', padding:20, borderRadius:8, minWidth:360, maxWidth:520, boxShadow:'0 10px 30px rgba(0,0,0,0.25)'}}>
            <h5 style={{marginBottom:10}}>Copiar senha temporária</h5>
            <div className="text-muted" style={{marginBottom:10}}>
              Envie esta senha para o usuário <strong>{tempPwdEmail}</strong>. No primeiro login, ele será obrigado a trocar a senha.
            </div>
            <div className="input-group mb-3">
              <input className="form-control" value={tempPwdValue} readOnly />
              <button className="btn btn-outline-primary" type="button" onClick={copyTempPassword}>
                {tempPwdCopied ? 'Copiado' : 'Copiar'}
              </button>
            </div>
            <div className="d-flex justify-content-end" style={{gap:8}}>
              <button className="btn btn-primary" onClick={() => { setTempPwdModalOpen(false); setTempPwdValue(''); setTempPwdEmail(''); }}>
                Fechar
              </button>
            </div>
          </div>
        </div>
      )}
      {deleteUserOpen && (
        <div style={{
          position:'fixed', inset:0, background:'rgba(15,23,42,0.65)', display:'flex',
          alignItems:'center', justifyContent:'center', zIndex:10000
        }}>
          <div style={{background:'#fff', color:'#111827', padding:20, borderRadius:8, minWidth:360, maxWidth:520, boxShadow:'0 10px 30px rgba(0,0,0,0.25)'}}>
            <h5 style={{marginBottom:12}}>Excluir usuário</h5>
            <p style={{marginBottom:16}}>
              Tem certeza que deseja excluir o usuário <strong>{deleteUserInfo?.nome || deleteUserInfo?.email || deleteUserInfo?.id}</strong>?
            </p>
            <div className="d-flex justify-content-end" style={{gap:8}}>
              <button className="btn btn-secondary" onClick={()=> { setDeleteUserOpen(false); setDeleteUserInfo(null) }} disabled={deleteUserSaving}>Cancelar</button>
              <button className="btn btn-danger" onClick={confirmDeleteUser} disabled={deleteUserSaving}>
                {deleteUserSaving ? 'Excluindo...' : 'Excluir'}
              </button>
            </div>
          </div>
        </div>
      )}
      {toggleUserOpen && (
        <div style={{
          position:'fixed', inset:0, background:'rgba(15,23,42,0.65)', display:'flex',
          alignItems:'center', justifyContent:'center', zIndex:10000
        }}>
          <div style={{background:'#fff', color:'#111827', padding:20, borderRadius:8, minWidth:360, maxWidth:520, boxShadow:'0 10px 30px rgba(0,0,0,0.25)'}}>
            <h5 style={{marginBottom:12}}>{toggleUserInfo?.nextActive ? 'Desbloquear usuário' : 'Bloquear usuário'}</h5>
            <p style={{marginBottom:16}}>
              {toggleUserInfo?.nextActive
                ? <>Deseja desbloquear o usuário <strong>{toggleUserInfo?.nome || toggleUserInfo?.email || toggleUserInfo?.id}</strong>?</>
                : <>Deseja bloquear o usuário <strong>{toggleUserInfo?.nome || toggleUserInfo?.email || toggleUserInfo?.id}</strong>?</>}
            </p>
            <div className="d-flex justify-content-end" style={{gap:8}}>
              <button className="btn btn-secondary" onClick={()=> { setToggleUserOpen(false); setToggleUserInfo(null) }} disabled={toggleUserSaving}>Cancelar</button>
              <button className={toggleUserInfo?.nextActive ? "btn btn-success" : "btn btn-danger"} onClick={confirmToggleUser} disabled={toggleUserSaving}>
                {toggleUserSaving ? (toggleUserInfo?.nextActive ? 'Desbloqueando...' : 'Bloqueando...') : (toggleUserInfo?.nextActive ? 'Desbloquear' : 'Bloquear')}
              </button>
            </div>
          </div>
        </div>
      )}
      {editUserOpen && (
        <div style={{
          position:'fixed', inset:0, background:'rgba(15,23,42,0.65)', display:'flex',
          alignItems:'center', justifyContent:'center', zIndex:10000
        }}>
          <div style={{background:'#fff', color:'#111827', padding:20, borderRadius:8, minWidth:360, maxWidth:520, boxShadow:'0 10px 30px rgba(0,0,0,0.25)'}}>
            <h5 style={{marginBottom:10}}>Editar usuário</h5>
            <div className="mb-2">
              <label>Email</label>
              <input className="form-control" value={editUserForm.email} onChange={e=> setEditUserForm(f => ({ ...f, email: e.target.value }))} disabled={!isSuperAdmin || editUserSaving} />
              {!isSuperAdmin && <div className="text-muted" style={{fontSize:12}}>Somente SuperAdmin pode alterar o email.</div>}
            </div>
            <div className="mb-2">
              <label>Nome</label>
              <input className="form-control" value={editUserForm.nome} onChange={e=> setEditUserForm(f => ({ ...f, nome: e.target.value }))} disabled={editUserSaving} />
            </div>
            <div className="mb-2">
              <label>Nível</label>
              <select className="form-select" value={editUserForm.nivel} onChange={e=> setEditUserForm(f => ({ ...f, nivel: e.target.value as any }))} disabled={editUserSaving}>
                {isSuperAdmin && <option value="SuperAdmin">SuperAdmin</option>}
                <option value="Administrador">Administrador</option>
                <option value="Básico">Básico</option>
              </select>
            </div>
            <div className="mb-3">
              <div className="form-check form-switch">
                <input className="form-check-input" type="checkbox" checked={editUserForm.isActive} onChange={e=> setEditUserForm(f => ({ ...f, isActive: e.target.checked }))} disabled={editUserSaving} />
                <label className="form-check-label">Ativo</label>
              </div>
            </div>
            <div className="d-flex justify-content-end" style={{gap:8}}>
              <button className="btn btn-secondary" onClick={()=> setEditUserOpen(false)} disabled={editUserSaving}>Cancelar</button>
              <button className="btn btn-primary" onClick={saveEditUser} disabled={editUserSaving}>
                {editUserSaving ? 'Salvando...' : 'Salvar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
