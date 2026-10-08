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
        get => TentarObter(chave, out var valor)
            ? valor
            : throw new KeyNotFoundException($"Chave '{chave}' não encontrada.");
        set => Inserir(chave, value, sobrescrever: true);
    }

    /// <summary>
    /// Adiciona um par novo. Chave nula: <see cref="ArgumentNullException"/>;
    /// chave já existente: <see cref="ArgumentException"/>.
    /// </summary>
    public void Adicionar(TKey chave, TValue valor) => Inserir(chave, valor, sobrescrever: false);

    /// <summary>Procura a chave percorrendo apenas o balde correspondente ao hash dela.</summary>
    public bool TentarObter(TKey chave, out TValue valor)
    {
        ArgumentNullException.ThrowIfNull(chave);

        for (var no = _baldes[IndiceDoBalde(chave, _baldes.Length)]; no is not null; no = no.Proximo)
        {
            if (_comparador.Equals(no.Chave, chave))
            {
                valor = no.Valor;
                return true;
            }
        }

        valor = default!;
        return false;
    }

    /// <summary>True se a chave existir.</summary>
    public bool ContemChave(TKey chave) => TentarObter(chave, out _);

    /// <summary>Remove a chave do seu balde. Retorna false se não existir.</summary>
    public bool Remover(TKey chave)
    {
        ArgumentNullException.ThrowIfNull(chave);

        var indice = IndiceDoBalde(chave, _baldes.Length);
        No? anterior = null;
        for (var no = _baldes[indice]; no is not null; anterior = no, no = no.Proximo)
        {
            if (!_comparador.Equals(no.Chave, chave))
                continue;

            if (anterior is null)
                _baldes[indice] = no.Proximo;
            else
                anterior.Proximo = no.Proximo;

            Count--;
            return true;
        }

        return false;
    }

    private void Inserir(TKey chave, TValue valor, bool sobrescrever)
    {
        ArgumentNullException.ThrowIfNull(chave);

        var indice = IndiceDoBalde(chave, _baldes.Length);
        for (var no = _baldes[indice]; no is not null; no = no.Proximo)
        {
            if (!_comparador.Equals(no.Chave, chave))
                continue;

            if (!sobrescrever)
                throw new ArgumentException($"Já existe um item com a chave '{chave}'.", nameof(chave));

            no.Valor = valor;
            return;
        }

        if ((double)(Count + 1) / _baldes.Length > FatorDeCargaMaximo)
        {
            Redimensionar();
            indice = IndiceDoBalde(chave, _baldes.Length);
        }

        _baldes[indice] = new No(chave, valor, _baldes[indice]);
        Count++;
    }

    private void Redimensionar()
    {
        var novos = new No?[_baldes.Length * 2];
        foreach (var cabeca in _baldes)
        {
            var no = cabeca;
            while (no is not null)
            {
                var proximo = no.Proximo;
                var indice = IndiceDoBalde(no.Chave, novos.Length);
                no.Proximo = novos[indice];
                novos[indice] = no;
                no = proximo;
            }
        }

        _baldes = novos;
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
