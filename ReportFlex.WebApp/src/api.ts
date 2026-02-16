let authToken: string | null = localStorage.getItem('rf_token')
export function setToken(t: string){ authToken = t; localStorage.setItem('rf_token', t) }
function headers(){ const h: Record<string,string> = {}; if(authToken) h['Authorization'] = `Bearer ${authToken}`; const cid = localStorage.getItem('rf_client_id'); if (cid) h['X-Client-Id'] = cid; return h }
export const api = {
  clientes: async () => withAuth(fetch('/api/clientes', { headers: headers() })),
  prestadores: async () => withAuth(fetch('/api/prestadores', { headers: headers() })),
  transitByCard: async (card: string) => withAuth(fetch('/api/cms/transit/by-card?card=' + encodeURIComponent(card), { headers: headers() })),
  signin: async (usuario: string, senha: string) => (await fetch('/api/login/signin', { method: 'POST', headers: { 'Content-Type':'application/json' }, body: JSON.stringify({ usuario, senha }) })).json(),
  signinToken: async (token: string) => {
    const r = await fetch('/api/login/signin-token?token=' + encodeURIComponent(token), { method: 'POST', headers: { 'Content-Type':'application/json' }, body: JSON.stringify({ token }) })
    if(!r.ok) return {}
    return await r.json()
  },
  loginTokens: async () => (await fetch('/api/login/tokens')).json(),
  employeesSearch: async (p: { matricula?: string, empresa?: string, page?: number, pageSize?: number, sort?: string, dir?: 'asc'|'desc' }) => {
    const qs = new URLSearchParams(Object.entries(p).filter(([,v])=> v!=null).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(fetch('/api/cms/employees/search?' + qs, { headers: headers() }))
  },
  employeesByMatricula: async (matricula: string, p: { page?: number, pageSize?: number, sort?: string, dir?: 'asc'|'desc' }={}) => {
    const qs = new URLSearchParams({ matricula, ...Object.fromEntries(Object.entries(p).map(([k,v])=>[k,String(v)])) }).toString()
    return await withAuth(fetch('/api/cms/employees/by-matricula?' + qs, { headers: headers() }))
  },
  externalSearch: async (p: { matricula?: string, empresa?: string, page?: number, pageSize?: number, sort?: string, dir?: 'asc'|'desc' }) => {
    const qs = new URLSearchParams(Object.entries(p).filter(([,v])=> v!=null).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(fetch('/api/cms/external/search?' + qs, { headers: headers() }))
  },
  accessByLevel: async (p: { levelId?: number, levelName?: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).filter(([,v])=> v!=null).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(fetch('/api/cms/access/by-level?' + qs, { headers: headers() }))
  },
  transitByPeriod: async (p: { start: string, end: string, card?: string, terminal?: string, userType?: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(fetch('/api/cms/transit/by-period?' + qs, { headers: headers() }))
  },
  reportsTransit: async (p: { empresa?: string, terminal?: string, start: string, end: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(fetch('/api/reports/transit?' + qs, { headers: headers() }))
  },
  reportsTransitAggregated: async (p: { empresa?: string, start: string, end: string }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(fetch('/api/reports/transit/aggregated?' + qs, { headers: headers() }))
  },
  reportsAccessAggregated: async () => {
    return await withAuth(fetch('/api/reports/access/aggregated', { headers: headers() }))
  },
  getDbMode: async () => withAuth(fetch('/api/admin/db-mode', { headers: headers() })),
  setDbMode: async (mode: 'Real'|'Demo') => withAuth(fetch('/api/admin/db-mode', { method:'POST', headers: { ...headers(), 'Content-Type':'application/json' }, body: JSON.stringify({ mode }) })),
  seedDemo: async (count: number, scope: 'all'|'cms'|'logins' = 'all') => withAuth(fetch(`/api/dev/seed?count=${count}&scope=${scope}`, { method:'POST', headers: headers() })),
  getConnections: async () => withAuth(fetch('/api/admin/connections', { headers: headers() })),
  setConnections: async (p: { CMS?: string, Logins?: string }) => withAuth(fetch('/api/admin/connections', { method:'POST', headers: { ...headers(), 'Content-Type':'application/json' }, body: JSON.stringify(p) })),
  currentClientInfo: async () => withAuth(fetch('/api/client/current', { headers: headers() })),
  adminClientsCreate: async (p: { nome: string, endereco?: string, fone?: string, email?: string, site?: string, ativo?: number, responsavel?: string, token?: string, logoPath?: string }) =>
    withAuth(fetch('/api/admin/clients', { method:'POST', headers: { ...headers(), 'Content-Type':'application/json' }, body: JSON.stringify(p) })),
  adminClientsUpdate: async (id: number, p: { nome?: string, endereco?: string, fone?: string, email?: string, site?: string, ativo?: number, responsavel?: string, logoPath?: string }) =>
    withAuth(fetch(`/api/admin/clients/${id}`, { method:'PUT', headers: { ...headers(), 'Content-Type':'application/json' }, body: JSON.stringify(p) })),
  adminClientsGenerateToken: async (id: number) => withAuth(fetch(`/api/admin/clients/${id}/token`, { method:'POST', headers: headers() })),
  adminClientsDelete: async (id: number) => withAuth(fetch(`/api/admin/clients/${id}`, { method:'DELETE', headers: headers() })),
  adminClientsUploadLogo: async (id: number, file: File) => {
    const fd = new FormData()
    fd.append('file', file)
    const r = await fetch(`/api/admin/clients/${id}/logo`, { method:'POST', headers: headers(), body: fd })
    if(!r.ok) throw new Error('Upload failed')
    return await r.json()
  },
  fetchReportPdf: async (url: string) => {
    const r = await fetch(url, { headers: headers() })
    if(!r.ok) throw new Error('Falha ao obter PDF')
    const b = await r.blob()
    return URL.createObjectURL(b)
  }
}

export function logout(){
  authToken = null
  localStorage.removeItem('rf_token')
  localStorage.removeItem('rf_client_id')
  localStorage.removeItem('rf_client_name')
  localStorage.removeItem('rf_level')
}

async function withAuth(p: Promise<Response>){
  const r = await p
  if(r.status === 401){
    logout()
    window.location.href = '/login'
    throw new Error('Unauthorized')
  }
  const raw = await r.text()
  if (r.ok) {
    if (!raw) return {}
    try { return JSON.parse(raw) } catch { return {} }
  }
  let msg = raw
  try {
    const data = raw ? JSON.parse(raw) : null
    msg = (data && (data.error || data.message)) ? (data.error || data.message) : (raw || `HTTP ${r.status}`)
  } catch {
    msg = raw || `HTTP ${r.status}`
  }
  throw new Error(msg)
}
