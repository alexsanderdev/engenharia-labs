# F6-M08 — Consistência Eventual

**Módulo:** Consistência Eventual (Fase 6)
**Tempo:** 2–3 horas

O lado de leitura do OrderFlow sem broker real: a **fonte da verdade** (pedidos) publica eventos numa
`FilaComAtraso<T>` (um `Channel<T>` cujas entregas só acontecem quando o `FakeTimeProvider` anda), e uma
**projeção** "resumo de pedidos do cliente" consome esses eventos — atrasados, duplicados, fora de ordem e às vezes perdidos.
Você implementa:

- **Versão por agregado** na projeção: ignorar duplicata/antigo, **adiar** evento que chega antes da hora (buffer) e drenar quando a lacuna fecha; rebuild a partir do histórico.
- **Read-your-writes** com **token de consistência** (`X-Consistency-Token`): a leitura espera a projeção alcançar a versão escrita por um tempo limitado e, se estourar, lê da fonte.
- **Job de reconciliação**: compara fonte × projeção, respeita uma janela de tolerância (o que está "no fio" não é corrigido) e corrige ausentes, atrasados, divergentes e fantasmas.
- **API assíncrona**: `POST /pedidos` → `202 Accepted` + `Location: /operacoes/{id}` + `Retry-After`; o status traz o token quando conclui.

```bash
dotnet test labs/F6-M08-consistencia-eventual
# ou só o projeto de testes:
dotnet test labs/F6-M08-consistencia-eventual/tests/F6M08.Consistencia.Tests
dotnet run --project labs/F6-M08-consistencia-eventual/src/F6M08.Consistencia   # http://localhost:5608 (veja o .http)
```

No início, os **32 casos de teste** (24 métodos) falham. Ordem sugerida:

1. `Consistencia/TokenDeConsistencia.cs` — `TryParse` (`ProjecaoTests.Token_*`).
2. `Projecao/ProjecaoResumoDoCliente.cs` — `Aplicar`, `EventosAdiados`, `Reconstruir`, `Remover` com adiados (`ProjecaoTests`, `JanelaDeInconsistenciaTests`).
3. Mesma classe: `AguardarVersaoAsync`; depois `Consistencia/ServicoDeConsulta.cs` (`ReadYourWritesTests`).
4. `Projecao/ProjecaoResumoDoCliente.Corrigir` e `Reconciliacao/Reconciliador.cs` (`ReconciliacaoTests`).
5. `Api/PedidoEndpoints.cs` — 202 + Location, status e resumo com token (`ApiAssincronaTests`).

**Por que os testes são determinísticos:** nenhum teste "dorme". A fila só entrega quando o teste chama
`FakeTimeProvider.Advance`, então dá para afirmar "a projeção ainda está velha" e, um `Advance` depois, "convergiu".
Os testes de API usam polling curto no `Location` (é o que um cliente de 202 faz) e um limite de segurança de 5 s
para nunca pendurar.

Prontos (leia, não altere): `Dominio/Eventos.cs`, `Fonte/FonteDePedidos.cs`, `Mensageria/` (fila com atraso e projetor),
`Projecao/Modelos.cs`, `Consistencia/ConsistenciaOptions.cs`, `Reconciliacao/ReconciliacaoPeriodica.cs`,
`Api/Operacoes.cs`, `Api/Contratos.cs` e `Program.cs`. **Não altere os testes.**
