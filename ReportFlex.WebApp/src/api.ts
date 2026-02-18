let authToken: string | null = localStorage.getItem('rf_token')
const API_BASE = (() => {
  try{
    const override = localStorage.getItem('rf_api_base')
    if (override && /^https?:\/\//i.test(override)) return override.replace(/\/+$/,'')
  }catch{}
  try{
    const origin = window.location.origin || ''
    if (origin.includes('localhost:5000') || origin.includes('127.0.0.1:5000')) return ''
  }catch{}
  return 'http://localhost:5000'
})()
export function setToken(t: string){ authToken = t; localStorage.setItem('rf_token', t) }
function headers(){ const h: Record<string,string> = {}; if(authToken) h['Authorization'] = `Bearer ${authToken}`; const cid = localStorage.getItem('rf_client_id'); if (cid) h['X-Client-Id'] = cid; return h }

async function apiFetch(path: string, init?: RequestInit){
  // Try API_BASE first, then fallback to relative path if 404/not reachable
  const urls = [API_BASE ? (API_BASE + path) : path];
  if (API_BASE) urls.push(path);
  let lastErr: any = null;
  for (const url of urls){
    try{
      const r = await fetch(url, init);
      if (r.status === 404 && url !== path) {
        // try fallback
        continue;
      }
      return r;
    }catch(e){
      lastErr = e;
      if (url !== path) continue;
      throw e;
    }
  }
  if (lastErr) throw lastErr;
  // Fallback: do a final attempt relative
  return await fetch(path, init);
}
export const api = {
  clientes: async () => withAuth(apiFetch('/api/clientes', { headers: headers() })),
  prestadores: async () => withAuth(apiFetch('/api/prestadores', { headers: headers() })),
  transitByCard: async (card: string) => withAuth(apiFetch('/api/cms/transit/by-card?card=' + encodeURIComponent(card), { headers: headers() })),
  cardByCpf: async (cpf: string) => withAuth(apiFetch('/api/cms/card/by-cpf?cpf=' + encodeURIComponent(cpf), { headers: headers() })),
  personByCardInfo: async (card: string) => withAuth(apiFetch('/api/cms/person/by-card-info?card=' + encodeURIComponent(card), { headers: headers() })),
  personByMatriculaInfo: async (matricula: string) => withAuth(apiFetch('/api/cms/person/by-matricula-info?matricula=' + encodeURIComponent(matricula), { headers: headers() })),
  transitByMatricula: async (p: { matricula: string, start: string, end: string, onlyTurnstiles?: boolean, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/transit/by-matricula?' + qs, { headers: headers() }))
  },
  companyByNameInfo: async (empresa: string) => withAuth(apiFetch('/api/cms/company/by-name-info?empresa=' + encodeURIComponent(empresa), { headers: headers() })),
  transitByEmpresa: async (p: { empresa: string, start: string, end: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/transit/by-empresa?' + qs, { headers: headers() }))
  },
  signin: async (usuario: string, senha: string) => (await apiFetch('/api/login/signin', { method: 'POST', headers: { 'Content-Type':'application/json' }, body: JSON.stringify({ usuario, senha }) })).json(),
  signinToken: async (token: string) => {
    const r = await apiFetch('/api/login/signin-token?token=' + encodeURIComponent(token))
    if(!r.ok) return {}
    return await r.json()
  },
  loginTokens: async () => (await apiFetch('/api/login/tokens')).json(),
  employeesSearch: async (p: { matricula?: string, empresa?: string, page?: number, pageSize?: number, sort?: string, dir?: 'asc'|'desc' }) => {
    const qs = new URLSearchParams(Object.entries(p).filter(([,v])=> v!=null).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/employees/search?' + qs, { headers: headers() }))
  },
  employeesByMatricula: async (matricula: string, p: { page?: number, pageSize?: number, sort?: string, dir?: 'asc'|'desc' }={}) => {
    const qs = new URLSearchParams({ matricula, ...Object.fromEntries(Object.entries(p).map(([k,v])=>[k,String(v)])) }).toString()
    return await withAuth(apiFetch('/api/cms/employees/by-matricula?' + qs, { headers: headers() }))
  },
  externalSearch: async (p: { matricula?: string, empresa?: string, page?: number, pageSize?: number, sort?: string, dir?: 'asc'|'desc' }) => {
    const qs = new URLSearchParams(Object.entries(p).filter(([,v])=> v!=null).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/external/search?' + qs, { headers: headers() }))
  },
  accessByLevel: async (p: { levelId?: number, levelName?: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).filter(([,v])=> v!=null).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/access/by-level?' + qs, { headers: headers() }))
  },
  reportsAccessByLevelPeriod: async (p: { start: string, end: string }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/reports/access/by-level-period?' + qs, { headers: headers() }))
  },
  transitByLevel: async (p: { levelId?: number, levelName?: string, start: string, end: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).filter(([,v])=> v!=null).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/transit/by-level?' + qs, { headers: headers() }))
  },
  visitorsByDocument: async (p: { documento: string, start: string, end: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/visitors/by-document?' + qs, { headers: headers() }))
  },
  visitorsByCompany: async (p: { empresa: string, start: string, end: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/visitors/by-company?' + qs, { headers: headers() }))
  },
  transitByCardPeriod: async (p: { card: string, start: string, end: string, onlyTurnstiles?: boolean, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/transit/by-card-period?' + qs, { headers: headers() }))
  },
  transitByPeriod: async (p: { start: string, end: string, card?: string, terminal?: string, userType?: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/cms/transit/by-period?' + qs, { headers: headers() }))
  },
  reportsTransit: async (p: { empresa?: string, terminal?: string, start: string, end: string, page?: number, pageSize?: number }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/reports/transit?' + qs, { headers: headers() }))
  },
  reportsTransitAggregated: async (p: { empresa?: string, start: string, end: string }) => {
    const qs = new URLSearchParams(Object.entries(p).map(([k,v])=>[k,String(v)])).toString()
    return await withAuth(apiFetch('/api/reports/transit/aggregated?' + qs, { headers: headers() }))
  },
  reportsAccessAggregated: async () => {
    return await withAuth(apiFetch('/api/reports/access/aggregated', { headers: headers() }))
  },
  seedCompanies: async () => withAuth(apiFetch('/api/dev/seed-companies', { method:'POST', headers: headers() })),
  getDbMode: async () => withAuth(apiFetch('/api/admin/db-mode', { headers: headers() })),
  setDbMode: async (mode: 'Real'|'Demo') => withAuth(apiFetch('/api/admin/db-mode', { method:'POST', headers: { ...headers(), 'Content-Type':'application/json' }, body: JSON.stringify({ mode }) })),
  seedDemo: async (count: number, scope: 'all'|'cms'|'logins' = 'all') => withAuth(apiFetch(`/api/dev/seed?count=${count}&scope=${scope}`, { method:'POST', headers: headers() })),
  getConnections: async () => withAuth(apiFetch('/api/admin/connections', { headers: headers() })),
  setConnections: async (p: { CMS?: string, Logins?: string }) => withAuth(apiFetch('/api/admin/connections', { method:'POST', headers: { ...headers(), 'Content-Type':'application/json' }, body: JSON.stringify(p) })),
  getDbInfo: async () => withAuth(apiFetch('/api/admin/db-info', { headers: headers() })),
  currentClientInfo: async () => withAuth(apiFetch('/api/client/current', { headers: headers() })),
  adminClientsCreate: async (p: { nome: string, endereco?: string, fone?: string, email?: string, site?: string, ativo?: number, responsavel?: string, token?: string, logoPath?: string }) =>
    withAuth(apiFetch('/api/admin/clients', { method:'POST', headers: { ...headers(), 'Content-Type':'application/json' }, body: JSON.stringify(p) })),
  adminClientsUpdate: async (id: number, p: { nome?: string, endereco?: string, fone?: string, email?: string, site?: string, ativo?: number, responsavel?: string, logoPath?: string }) =>
    withAuth(apiFetch(`/api/admin/clients/${id}`, { method:'PUT', headers: { ...headers(), 'Content-Type':'application/json' }, body: JSON.stringify(p) })),
  adminClientsGenerateToken: async (id: number) => withAuth(apiFetch(`/api/admin/clients/${id}/token`, { method:'POST', headers: headers() })),
  adminClientsDelete: async (id: number) => withAuth(apiFetch(`/api/admin/clients/${id}`, { method:'DELETE', headers: headers() })),
  adminClientsUploadLogo: async (id: number, file: File) => {
    const fd = new FormData()
    fd.append('file', file)
    const r = await apiFetch(`/api/admin/clients/${id}/logo`, { method:'POST', headers: headers(), body: fd })
    if(!r.ok) throw new Error('Upload failed')
    return await r.json()
  },
  fetchReportPdf: async (url: string) => {
    const r = await apiFetch(url.startsWith('/api/') ? url : url, { headers: headers() })
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
