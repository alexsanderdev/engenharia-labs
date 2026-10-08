namespace F4M02.Pedidos.Domain.Comum;

/// <summary>
/// Raiz de agregado: a única entidade do agregado que o mundo de fora pode referenciar e chamar.
/// Ela protege as invariantes de tudo que está dentro da fronteira e acumula os
/// <see cref="IEventoDeDominio"/> que aconteceram, para a camada de aplicação despachar
/// <b>depois</b> de salvar.
/// </summary>
public abstract class RaizDeAgregado<TId> : Entidade<TId>
    where TId : notnull
{
    private readonly List<IEventoDeDominio> _eventosDeDominio = [];

    protected RaizDeAgregado(TId id)
        : base(id)
    {
    }

    /// <summary>Eventos registrados desde a criação/carga (ou desde o último <see cref="LimparEventos"/>), em ordem.</summary>
    public IReadOnlyList<IEventoDeDominio> EventosDeDominio =>
        throw new NotImplementedException("TODO (Passo 4): exponha _eventosDeDominio como SOMENTE LEITURA (AsReadOnly), nunca a List em si.");

    /// <summary>Registra um fato que acabou de acontecer. Só a própria raiz registra eventos.</summary>
    protected void Registrar(IEventoDeDominio evento) =>
        throw new NotImplementedException($"TODO (Passo 4): adicione o evento à lista (hoje com {_eventosDeDominio.Count}).");

    /// <summary>Esvazia a lista. Chamado pela infraestrutura depois de despachar os eventos.</summary>
    public void LimparEventos() =>
        throw new NotImplementedException("TODO (Passo 4): limpe a lista.");
}
