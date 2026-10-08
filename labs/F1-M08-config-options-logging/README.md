# F1-M08 — Configuração, Options e Logging

**Módulo:** Configuração, Options e Logging (Fase 1)
**Tempo:** 1h30

Precedência de providers de configuração, Options pattern validado na inicialização, `IOptionsMonitor` reagindo a mudanças e logging estruturado com `[LoggerMessage]`, verificado com `FakeLogger` (`Microsoft.Extensions.Diagnostics.Testing`).

```bash
dotnet test labs/F1-M08-config-options-logging/tests/F1M08.Config.Tests
```

1. `ConfigurationFactory.cs` — JSON base → JSON do ambiente → variáveis de ambiente → memória (`ConfigurationFactoryTests`).
2. `CatalogOptions.cs` — atributos `[Required]`/`[Range]`.
3. `CatalogServiceCollectionExtensions.cs` — `AddOptions().Bind().ValidateDataAnnotations().Validate().ValidateOnStart()` (`CatalogOptionsTests`).
4. `ProductPriceService.cs` — regra de limite de aumento com `IOptionsMonitor` e mensagens `[LoggerMessage]` (`ProductPriceServiceLoggingTests`).

**Não altere os testes.**
