import React from 'react'

export function DataTable({ columns, rows, onHeaderClick }: { columns: { key: string, label: string, sortable?: boolean }[], rows: any[], onHeaderClick?: (key: string)=>void }){
  return (
    <div className="table-responsive">
      <table className="table table-sm">
        <thead className="table-light">
          <tr>
            {columns.map(c=> (
              <th key={c.key}
                  onClick={()=> c.sortable && onHeaderClick && onHeaderClick(c.key)}
                  style={{cursor: c.sortable?'pointer':'default'}}>
                {c.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((r,i)=> (
            <tr key={i}>
              {columns.map(c=> <td key={c.key}>{r[c.key] ?? ''}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
