namespace PM01.Estruturas;

/// <summary>
/// Árvore binária de busca (BST) SEM balanceamento. Regra: à esquerda ficam os menores,
/// à direita os maiores; duplicados são rejeitados. Busca/inserção/remoção custam O(altura):
/// O(log n) se a árvore estiver equilibrada, O(n) se degenerar (ex.: inserção já ordenada).
/// <see cref="SortedSet{T}"/> e <see cref="SortedDictionary{TKey, TValue}"/> usam árvores
/// rubro-negras, que se rebalanceiam e garantem O(log n).
/// </summary>
public sealed class ArvoreBinariaDeBusca<T> where T : IComparable<T>
{
    /// <summary>Quantidade de itens na árvore.</summary>
    public int Count { get; private set; }

    /// <summary>Altura em nós: vazia = 0, só a raiz = 1.</summary>
    public int Altura => throw new NotImplementedException("TODO: altura(nó) = nó nulo ? 0 : 1 + max(altura(esquerda), altura(direita)).");

    /// <summary>Insere o valor. Retorna false (e não altera nada) se ele já existir.</summary>
    public bool Inserir(T valor)
    {
        throw new NotImplementedException("TODO: crie uma classe No (Valor, Esquerda, Direita), desça comparando com CompareTo e pendure o nó novo no lugar vazio.");
    }

    /// <summary>True se o valor existir. Desce pela esquerda ou direita conforme a comparação.</summary>
    public bool Contem(T valor)
    {
        throw new NotImplementedException("TODO: desça a partir da raiz: menor vai à esquerda, maior à direita, igual achou.");
    }

    /// <summary>Menor valor (nó mais à esquerda). Árvore vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Minimo()
    {
        throw new NotImplementedException("TODO: desça sempre pela esquerda.");
    }

    /// <summary>Maior valor (nó mais à direita). Árvore vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Maximo()
    {
        throw new NotImplementedException("TODO: desça sempre pela direita.");
    }

    /// <summary>
    /// Remove o valor tratando os três casos: folha, um filho e dois filhos
    /// (neste último, copia o sucessor em ordem — o menor da subárvore direita — e remove o sucessor).
    /// Retorna false se o valor não existir.
    /// </summary>
    public bool Remover(T valor)
    {
        throw new NotImplementedException("TODO: remoção recursiva que devolve a nova subárvore; trate folha, um filho e dois filhos (sucessor em ordem).");
    }

    /// <summary>
    /// Percurso em ordem (esquerda, nó, direita): devolve os valores ORDENADOS.
    /// Implementação iterativa com pilha explícita, para não estourar a pilha de chamadas em árvores degeneradas.
    /// </summary>
    public IEnumerable<T> EmOrdem()
    {
        throw new NotImplementedException("TODO: empilhe tudo à esquerda, desempilhe, yield return o valor, vá para a direita e repita.");
    }
}
