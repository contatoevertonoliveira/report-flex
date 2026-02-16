let authToken: string | null = localStorage.getItem('rf_token')
export function setToken(t: string){ authToken = t; localStorage.setItem('rf_token', t) }
function headers(){ const h: Record<string,string> = {}; if(authToken) h['Authorization'] = `Bearer ${authToken}`; return h }
export const api = {
  clientes: async () => withAuth(fetch('/api/clientes')),
  prestadores: async () => withAuth(fetch('/api/prestadores')),
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
}

export function logout(){
  authToken = null
  localStorage.removeItem('rf_token')
}

async function withAuth(p: Promise<Response>){
  const r = await p
  if(r.status === 401){
    logout()
    window.location.href = '/login'
    throw new Error('Unauthorized')
  }
  return r.json()
}
