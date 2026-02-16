import React, { useEffect, useState } from 'react'
import { api } from '../api'

export function ClientesPage(){
  const [rows, setRows] = useState<any[]>([])
  useEffect(()=>{ api.clientes().then(setRows) },[])
  return (
    <section>
      <h2>Clientes</h2>
      <table>
        <thead><tr>{['SBID','NOME','ENDERECO','FONE','EMAIL','SITE','ATIVO'].map(c=> <th key={c}>{c}</th>)}</tr></thead>
        <tbody>{rows.map((r,i)=> <tr key={i}>{['SBID','NOME','ENDERECO','FONE','EMAIL','SITE','ATIVO'].map(c => <td key={c}>{r[c] ?? ''}</td>)}</tr>)}</tbody>
      </table>
    </section>
  )
}
