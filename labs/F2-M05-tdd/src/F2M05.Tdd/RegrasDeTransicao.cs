namespace F2M05.Tdd;

/// <summary>
/// Tabela de transições permitidas. Esta classe NÃO existia no início do lab:
/// ela nasceu no passo de refactor, quando a terceira regra "if (Status != X) throw" apareceu
/// e a duplicação ficou evidente. É o design emergindo dos testes.
/// </summary>
public static class RegrasDeTransicao
{
    private static readonly HashSet<(StatusPedido De, StatusPedido Para)> Permitidas =
    [
        (StatusPedido.Created, StatusPedido.Confirmed),
        (StatusPedido.Confirmed, StatusPedido.Completed),
        (StatusPedido.Created, StatusPedido.Cancelled),
    ];

    /// <summary>Indica se a transição <paramref name="de"/> → <paramref name="para"/> é permitida.</summary>
    public static bool Permitida(StatusPedido de, StatusPedido para) => Permitidas.Contains((de, para));
}
