# Report-Flex 1.0 (Para computadores que utilizam o sistema EBI (BMS) por Honeywell .
Teste de apoio aos Relatórios do Sistema EBI.

## Web (ReportFlex.WebApp + ReportFlex.WebApi)

Este repositório contém:
- ReportFlex.WebApp: Frontend (React + TypeScript + Vite)
- ReportFlex.WebApi: Backend (ASP.NET Minimal API) que expõe as rotas `/api/*`

### Execução (ambiente de desenvolvimento)

- Frontend:
  - `cd ReportFlex.WebApp`
  - `npm install`
  - `npm run dev` (porta 5173)
- Backend:
  - `cd ReportFlex.WebApi`
  - `dotnet run` (porta 5001)

### Execução offline (sem internet)

Para rodar em um computador restrito, a estratégia é:
- Compilar o frontend (Vite) e servir os arquivos estáticos pelo backend.
- Publicar o backend em modo self-contained (Windows), zipar e levar em um pendrive.

Checklist mínimo:
- O frontend não pode depender de CDNs externos (fonts/Bootstrap/ícones). Tudo deve estar empacotado no build.
- As conexões do SQL Server devem ser configuradas via `.env`/Configurações (aba Banco).

### Configuração do banco (Assistente de Conexão)

Em **Configurações → Banco** existe um **Assistente de Conexão** para:
- Detectar/selecionar instância do SQL Server (ou digitar manualmente).
- Listar bases existentes na instância.
- Escolher quais bases serão usadas como CMS / Logins / EMS.
- Visualizar uma amostra de tabelas antes de aplicar.

Observações:
- A descoberta de instâncias/bases/tabelas usa sempre Windows Authentication.
- Após “Aplicar conexões”, o backend persiste as connection strings e a própria tela já exibe as tabelas detectadas por base.

### Versionamento

- Não há tags de versão no repositório no momento; o versionamento atual é pelo histórico do Git (commits/branch).
- Versão do frontend: `ReportFlex.WebApp/package.json` (`0.0.1`).
- Backend: `ReportFlex.WebApi` (TargetFramework `net10.0`).

Quando trabalhei como Operador de BMS pela empresa Honeywell do Brasil, desenvolvi muitas habilidades.

E uma das ideias e a principal foi identificar problemas que as vezes passam desapercebido, como a geração de relatórios.

OBS: Observei que a empresa sempre tinha que contratar operadores que necessitassem entender de SQL somente para gerar relatórios quando os clientes pediam, e vi que isso podia ser resolvido com a programação.

Buscando agilizar processos e buscas por relatórios diversos, resolvi colocá-los todos em uma pequena plataforma desenvolvida por mim em C# (Microsoft Visual Studio).

Segue abaixo alguma telas:

![Tela Principal](https://github.com/contatoevertonoliveira/report-flex/blob/main/img/tela1.jpg?raw=true)

![](https://github.com/contatoevertonoliveira/report-flex/blob/main/img/tela2.jpg?raw=true)

![](https://github.com/contatoevertonoliveira/report-flex/blob/main/img/tela10.jpg?raw=true)

** Imagens e logos meramente ilustrativas **

- Pode ser definido o cabeçalho personalizado com a logomarca da empresa licenciada para usar o software;

- Pode ser definido usuários;

- Pode usar usuários da própria plataforma EBI;

- Relatórios de Visitantes;

- Relatórios de Funcionários;

- Relatórios por nível de acesso;

- Relatórios por catracas específicas;

- Relatórios por horário e data;

- Enfim, pode ser gerado relatórios diversos e de formas diversas também, sempre utilizando o dados cadastrados e coletados pelo BMS;

- Consultar diversas, pelo RG, CPF, Crachá, Código Funcionário etc.;

- Gera relatórios em pdf, xls, docx;

- <b>O relatório fica pronto em segundos, e a resposta ao cliente é quase que imediata.</b>

  

** Enfim aqui mostrando um pouco das minhas qualidades e curiosidades nessa vida ainda curta de programador.



### :left_right_arrow: Linguagens utilizadas:

* C#



Desenvolvido por Everton F. de Oliveira
