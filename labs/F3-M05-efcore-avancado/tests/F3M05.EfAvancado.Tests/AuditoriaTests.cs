using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M05.EfAvancado.Tests;

/// <summary>Passo 1: SaveChangesInterceptor de auditoria (base dos passos que usam datas).</summary>
public sealed class AuditoriaTests(SqlServerFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task Inserir_PreencheCriadoEmEAtualizadoEmComORelogio()
    {
        var agora = Relogio.GetUtcNow();

        var produto = await SemearProdutoAsync("Teclado", 199m);

        await using var db = NovoContexto();
        var salvo = await db.Produtos.AsNoTracking().SingleAsync(p => p.Id == produto.Id, Ct);
        salvo.CriadoEm.ShouldBe(agora);
        salvo.AtualizadoEm.ShouldBe(agora);
    }

    [Fact]
    public async Task Alterar_AtualizaSoAtualizadoEm_TambemNoSaveChangesSincrono()
    {
        var produto = await SemearProdutoAsync("Mouse", 89m);
        var criadoEm = Relogio.GetUtcNow();
        Relogio.Advance(TimeSpan.FromHours(3));

        await using (var db = NovoContexto())
        {
            var carregado = await db.Produtos.SingleAsync(p => p.Id == produto.Id, Ct);
            carregado.Nome = "Mouse sem fio";
            carregado.CriadoEm = DateTimeOffset.MinValue; // tentativa de adulterar: deve ser ignorada
            db.SaveChanges(); // síncrono de propósito
        }

        await using var verificacao = NovoContexto();
        var salvo = await verificacao.Produtos.AsNoTracking().SingleAsync(p => p.Id == produto.Id, Ct);
        salvo.Nome.ShouldBe("Mouse sem fio");
        salvo.CriadoEm.ShouldBe(criadoEm, "CriadoEm nunca é regravado");
        salvo.AtualizadoEm.ShouldBe(Relogio.GetUtcNow());
    }
}
