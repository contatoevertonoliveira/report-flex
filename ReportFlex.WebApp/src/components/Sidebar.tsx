import React from 'react'

export function Sidebar({ expanded, onToggle }: { expanded: boolean, onToggle: ()=>void }) {
  const items = [
    { label: 'Clientes', href: '/clientes' },
    { label: 'Prestadores', href: '/prestadores' },
    { label: 'Funcionários', href: '/employees' },
    { label: 'Externos', href: '/external' },
    { label: 'Trânsito', href: '/transit' },
    { label: 'Acessos', href: '/access' },
    { label: 'Relatórios', href: '/reports' },
    { label: 'Login', href: '/login' }
  ]
  function handleLogout(){
    import('../api').then(m=> m.logout())
    window.location.href = '/login'
  }
  return (
    <aside className={expanded ? 'sidebar expanded' : 'sidebar'}>
      <div className="sidebar-header">
        <span>Menu</span>
        <button onClick={onToggle}>{expanded ? '⮜' : '⮞'}</button>
      </div>
      <nav>
        {items.map(i=> <a key={i.label} href={i.href}>{i.label}</a>)}
        <button onClick={handleLogout} style={{marginTop:8}}>Sair</button>
      </nav>
    </aside>
  )
}
