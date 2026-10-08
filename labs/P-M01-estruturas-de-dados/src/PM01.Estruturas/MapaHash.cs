namespace PM01.Estruturas;

/// <summary>
/// Tabela hash com encadeamento separado (separate chaining), equivalente didático de
/// <see cref="Dictionary{TKey, TValue}"/>. Cada balde é uma lista ligada de nós.
/// Busca, inserção e remoção são O(1) em média e O(n) no pior caso (todas as chaves no mesmo balde).
/// Quando <c>Count / QuantidadeDeBaldes</c> passaria de 0,75, o número de baldes dobra e tudo é redistribuído.
/// </summary>
public sealed class MapaHash<TKey, TValue> where TKey : notnull
{
    private const int BaldesIniciais = 8;
    private const double FatorDeCargaMaximo = 0.75;

    private readonly IEqualityComparer<TKey> _comparador;
    private No?[] _baldes = new No?[BaldesIniciais];

    /// <summary>Cria o mapa. Sem comparador, usa <see cref="EqualityComparer{T}.Default"/>.</summary>
    public MapaHash(IEqualityComparer<TKey>? comparador = null)
    {
        _comparador = comparador ?? EqualityComparer<TKey>.Default;
    }

    /// <summary>Quantidade de pares chave/valor.</summary>
    public int Count { get; private set; }

    /// <summary>Quantidade de baldes (começa em 8 e dobra ao redimensionar).</summary>
    public int QuantidadeDeBaldes => _baldes.Length;

    /// <summary>
    /// get: devolve o valor ou lança <see cref="KeyNotFoundException"/>.
    /// set: insere ou sobrescreve (não lança para chave existente).
    /// </summary>
    public TValue this[TKey chave]
    {
        get => throw new NotImplementedException("TODO: use TentarObter; se não achar, lance KeyNotFoundException.");
        set => throw new NotImplementedException("TODO: insira ou sobrescreva o valor (sem lançar para chave existente).");
    }

    /// <summary>
    /// Adiciona um par novo. Chave nula: <see cref="ArgumentNullException"/>;
    /// chave já existente: <see cref="ArgumentException"/>.
    /// </summary>
    public void Adicionar(TKey chave, TValue valor)
    {
        throw new NotImplementedException("TODO: calcule o balde, percorra a lista ligada procurando a chave, redimensione se passar de 0,75 e insira um No novo na cabeça do balde.");
    }

    /// <summary>Procura a chave percorrendo apenas o balde correspondente ao hash dela.</summary>
    public bool TentarObter(TKey chave, out TValue valor)
    {
        throw new NotImplementedException("TODO: IndiceDoBalde(chave) e percorra os nós comparando com _comparador.Equals.");
    }

    /// <summary>True se a chave existir.</summary>
    public bool ContemChave(TKey chave) => TentarObter(chave, out _);

    /// <summary>Remove a chave do seu balde. Retorna false se não existir.</summary>
    public bool Remover(TKey chave)
    {
        throw new NotImplementedException("TODO: percorra o balde guardando o nó anterior e religue anterior.Proximo (ou a cabeça do balde).");
    }

    // & 0x7FFFFFFF zera o bit de sinal: GetHashCode pode ser negativo e % de negativo é negativo em C#.
    private int IndiceDoBalde(TKey chave, int quantidadeDeBaldes) =>
        (_comparador.GetHashCode(chave) & 0x7FFFFFFF) % quantidadeDeBaldes;

    private sealed class No(TKey chave, TValue valor, No? proximo)
    {
        public TKey Chave { get; } = chave;
        public TValue Valor { get; set; } = valor;
        public No? Proximo { get; set; } = proximo;
    }
}
