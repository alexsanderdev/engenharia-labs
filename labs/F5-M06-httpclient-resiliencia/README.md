# F5-M06 — HttpClient e Resiliência

**Módulo:** HttpClient e Resiliência (Fase 5)
**Tempo:** 2–3 horas

Integração do OrderFlow com um **gateway de pagamento** externo: typed client via `IHttpClientFactory`, `DelegatingHandler` de API key e correlation id, pipeline de resiliência com `Microsoft.Extensions.Http.Resilience` (Polly v8) — timeout total e por tentativa, retry com backoff exponencial + jitter **só para falha transitória em operação idempotente** (o POST de cobrança usa `Idempotency-Key`), respeito ao `Retry-After`, circuit breaker e fallback na consulta de status — e erros HTTP traduzidos para resultados de domínio (nenhuma `HttpRequestException` chega ao caso de uso).

O gateway é simulado com **WireMock.Net** (servidor HTTP real em porta aleatória: respostas lentas, 500, 503/429 com `Retry-After`, falha intermitente, corpo corrompido) e um socket que derruba conexões (falha de rede). O tempo do pipeline é um `FakeTimeProvider` instrumentado (`Infra/RelogioDeTeste.cs`): os testes esperam o Polly agendar o timer e avançam o relógio, sem `sleep`.

```bash
dotnet test labs/F5-M06-httpclient-resiliencia
# ou só o projeto de testes:
dotnet test labs/F5-M06-httpclient-resiliencia/tests/F5M06.Pagamentos.Tests
```

No início, os 37 casos de teste falham. Ordem sugerida:

1. `Gateway/ResilienciaGateway.cs` — `EhIdempotente`, `DeveRetentar` e as fábricas de estratégias (`PoliticaTests`).
2. `DependencyInjection.cs` — options, typed client nomeado, `Timeout` infinito no `HttpClient` (`RegistroEHandlerTests`).
3. `Gateway/CorrelacaoEApiKeyHandler.cs` — `X-Api-Key` e `X-Correlation-Id`; registre o handler **antes** do resilience handler.
4. `Gateway/GatewayPagamentoClient.cs` — `CobrarAsync`, `EstornarAsync`, `MotivoDaFalha` (`MapeamentoTests`).
5. `ResilienciaGateway.Configurar` + `AddResilienceHandler` — retry, `Retry-After`, backoff (`RetryTests`).
6. Timeouts total e por tentativa (`TimeoutTests`).
7. Circuit breaker (`CircuitBreakerTests`).
8. `ConsultarStatusAsync` com fallback (`FallbackTests`).
9. `Checkout/CheckoutService.cs` — chave de idempotência derivada do pedido (`CheckoutTests`).

Já vêm prontos: options, contratos (DTOs), resultados de domínio, `Pedido`, `UltimoStatusConhecido` e toda a infra de teste (`Infra/`). **Não altere os testes.**

A suíte roda em ~2 s (o primeiro teste de cada classe paga o start do WireMock). Nenhum teste depende de rede externa; a chave de API é gerada por execução.
