using System.Collections;

namespace F1M04.Linq.Tests;

/// <summary>Massa de dados fixa usada pelos testes.</summary>
internal static class Dados
{
    public static List<Produto> Produtos() =>
    [
        new(1, "LIV-001", "Livro Clean Code", "Livros", 120m, true),
        new(2, "LIV-002", "Livro Refactoring", "Livros", 150m, true),
        new(3, "LIV-003", "Livro DDD", "Livros", 200m, false),
        new(4, "ELE-001", "Mouse sem fio", "Eletrônicos", 90m, true),
        new(5, "ELE-002", "Teclado mecânico", "Eletrônicos", 350m, true),
        new(6, "ELE-003", "Monitor 27", "Eletrônicos", 1500m, true),
        new(7, "CAS-001", "Caneca de café", "Casa", 40m, true),
        new(8, "CAS-002", "Luminária", "Casa", 90m, true),
    ];

    public static List<Cliente> Clientes() =>
    [
        new(10, "Ana", "Recife"),
        new(20, "Bruno", "São Paulo"),
        new(30, "Carla", "Curitiba"), // sem pedidos
    ];

    public static List<Pedido> Pedidos() =>
    [
        new(100, 10, new DateOnly(2026, 1, 5), StatusPedido.Concluido, [new(1, 1, 120m), new(4, 2, 90m)]), // 300
        new(101, 20, new DateOnly(2026, 1, 6), StatusPedido.Confirmado, [new(6, 1, 1500m)]),               // 1500
        new(102, 10, new DateOnly(2026, 2, 1), StatusPedido.Criado, [new(7, 3, 40m)]),                      // 120
        new(103, 20, new DateOnly(2026, 2, 2), StatusPedido.Cancelado, [new(5, 10, 350m)]),                 // cancelado
        new(104, 10, new DateOnly(2026, 2, 3), StatusPedido.Concluido, [new(4, 1, 90m), new(7, 1, 40m)]),   // 130
    ];
}

/// <summary>
/// Envolve uma sequência e conta quantas vezes ela foi enumerada (GetEnumerator).
/// Serve para detectar múltipla enumeração.
/// </summary>
internal sealed class EnumeravelContador<T>(IEnumerable<T> origem) : IEnumerable<T>
{
    public int Enumeracoes { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        Enumeracoes++;
        return origem.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
