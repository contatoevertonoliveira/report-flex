import React, { useEffect, useState } from 'react'
import { api } from '../api'

export function PrestadoresPage(){
  const [rows, setRows] = useState<any[]>([])
  useEffect(()=>{ api.prestadores().then(setRows) },[])
  return (
    <section>
      <h2>Prestadores</h2>
      <table className="table table-sm">
        <thead><tr>{['SBID','NOME','ENDERECO','FONE','EMAIL','SITE','ATIVO'].map(c=> <th key={c}>{c}</th>)}</tr></thead>
        <tbody>{rows.map((r,i)=> <tr key={i}>{['SBID','NOME','ENDERECO','FONE','EMAIL','SITE','ATIVO'].map(c => <td key={c}>{r[c] ?? ''}</td>)}</tr>)}</tbody>
      </table>
    </section>
  )
}
