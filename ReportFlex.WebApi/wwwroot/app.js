const api = {
  clientes: () => fetch('/api/clientes').then(r => r.json()),
  prestadores: () => fetch('/api/prestadores').then(r => r.json()),
  login: (token) => fetch('/api/login/check?token=' + encodeURIComponent(token)).then(r => r.ok ? r.json() : null),
  transitByCard: (card) => fetch('/api/cms/transit/by-card?card=' + encodeURIComponent(card)).then(r => r.json())
};

const Table = ({ rows, columns }) => {
  return React.createElement('table', null,
    React.createElement('thead', null,
      React.createElement('tr', null, columns.map(c => React.createElement('th', { key: c }, c)))
    ),
    React.createElement('tbody', null,
      rows.map((row, i) =>
        React.createElement('tr', { key: i }, columns.map(c => React.createElement('td', { key: c }, row[c] ?? '')))
      )
    )
  );
};

const App = () => {
  const [clientes, setClientes] = React.useState([]);
  const [prestadores, setPrestadores] = React.useState([]);
  const [login, setLogin] = React.useState(null);
  const [transit, setTransit] = React.useState([]);

  React.useEffect(() => {
    api.clientes().then(setClientes);
    api.prestadores().then(setPrestadores);
  }, []);

  React.useEffect(() => {
    const btnLogin = document.getElementById('btnLogin');
    const btnTransit = document.getElementById('btnTransit');
    btnLogin.onclick = async () => {
      const token = document.getElementById('token').value.trim();
      const res = await api.login(token);
      setLogin(res);
      document.getElementById('loginInfo').textContent = res ? `${res.nome} | ${res.usuario} | ${res.nivel}` : 'Token inválido';
    };
    btnTransit.onclick = async () => {
      const card = document.getElementById('card').value.trim();
      const res = await api.transitByCard(card);
      setTransit(res);
    };
  }, []);

  return React.createElement(React.Fragment, null,
    React.createElement('div', null,
      React.createElement(Table, { rows: clientes, columns: ['SBID','NOME','ENDERECO','FONE','EMAIL','SITE','ATIVO'] })
    ),
    React.createElement('div', { style: { marginTop: 12 } },
      React.createElement(Table, { rows: prestadores, columns: ['SBID','NOME','ENDERECO','FONE','EMAIL','SITE','ATIVO'] })
    ),
    React.createElement('div', { style: { marginTop: 12 } },
      React.createElement(Table, { rows: transit, columns: ['SbiID','Name','CardNumber','Direction','UserType','Terminal','TerminalDescription','TransitDate'] })
    )
  );
};

ReactDOM.createRoot(document.getElementById('clientes')).render(React.createElement(App));
ReactDOM.createRoot(document.getElementById('prestadores')).render(React.createElement(App));
ReactDOM.createRoot(document.getElementById('transit')).render(React.createElement(App));
