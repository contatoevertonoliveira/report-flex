# Validação — Eventos de Porta (Portas Críticas e Portas Gerais)

Data da validação: 2026-09-14
Banco: `JP4REPORTDEV01` (SQL Server)

## Objetivo

Confirmar que, ao selecionar **todos os dados** e **todas as portas**, as consultas de
Eventos de Porta trazem tudo o que existe no banco, e que o **filtro por data** funciona.

## Fonte dos dados

As duas consultas leem a view `emsevents..ems_vw_EMSevents`, sempre filtrada por:

- `ConditionName IN ('Granted','Denied')`
- `Category IN (16, 5)`

Stored procedures envolvidas (banco `hwreportsview`):

- `dbo.jp4_sp_DoorGeneral` (Portas Gerais) — recebe `@SourceList`
- `dbo.jp4_sp_DoorCritical` (Portas Críticas) — lista interna fixa de 54 TAGs

## Números de referência (banco)

| Métrica | Valor |
|---|---|
| Eventos totais (filtro Granted/Denied + Category 16/5) | **1.513.494** |
| Portas (TAGs) distintas | **578** |
| Período disponível (LocalTime) | 2025-04-14 → 2025-12-06 |
| Portas em `cms..DoorSources` | 578 (bate com a fonte, 0 divergências) |
| Portas em `cms..DoorSourcesCritical` | 54 (bate com a SP crítica, 0 divergências) |

## Resultados das SPs (todo o período, todas as portas)

| Consulta | Linhas retornadas | Portas distintas |
|---|---|---|
| `jp4_sp_DoorGeneral` (578 portas) | **1.513.494** | 578 |
| `jp4_sp_DoorCritical` | **51.623** | 54 |

## Filtro por data (exemplo: 10/06/2025)

| Cenário | Fonte bruta | SP | Resultado |
|---|---|---|---|
| Portas Gerais | 7.926 | 7.926 | OK |
| Portas Críticas | 235 | 235 | OK |

## Conclusão

- **Todas as portas + todos os dados** = traz tudo do banco.
- **Filtro por data** = funciona nos dois modos.

## Observações

1. A coluna `TimeOrder` usa `FieldTime` (com ajuste de -3h) quando o campo está
   preenchido; o filtro de data usa `LocalTime`. Dos 1.513.494 eventos,
   **867.780 (57%)** têm `FieldTime` preenchido. Isso não afeta o total nem o filtro
   por data, mas pode deslocar a hora exibida de alguns registros em relação à borda
   do dia filtrado.
2. A tabela auxiliar `cms..DoorSources` (578) coincide exatamente com as 578 fontes
   reais da view; `cms..DoorSourcesCritical` (54) coincide com a lista interna da SP
   crítica.
