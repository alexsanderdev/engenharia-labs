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
    private No? _raiz;

    /// <summary>Quantidade de itens na árvore.</summary>
    public int Count { get; private set; }

    /// <summary>Altura em nós: vazia = 0, só a raiz = 1.</summary>
    public int Altura => CalcularAltura(_raiz);

    /// <summary>Insere o valor. Retorna false (e não altera nada) se ele já existir.</summary>
    public bool Inserir(T valor)
    {
        if (_raiz is null)
        {
            _raiz = new No(valor);
            Count++;
            return true;
        }

        var atual = _raiz;
        while (true)
        {
            var comparacao = valor.CompareTo(atual.Valor);
            if (comparacao == 0)
                return false;

            if (comparacao < 0)
            {
                if (atual.Esquerda is null)
                {
                    atual.Esquerda = new No(valor);
                    break;
                }

                atual = atual.Esquerda;
            }
            else
            {
                if (atual.Direita is null)
                {
                    atual.Direita = new No(valor);
                    break;
                }

                atual = atual.Direita;
            }
        }

        Count++;
        return true;
    }

    /// <summary>True se o valor existir. Desce pela esquerda ou direita conforme a comparação.</summary>
    public bool Contem(T valor)
    {
        var atual = _raiz;
        while (atual is not null)
        {
            var comparacao = valor.CompareTo(atual.Valor);
            if (comparacao == 0)
                return true;

            atual = comparacao < 0 ? atual.Esquerda : atual.Direita;
        }

        return false;
    }

    /// <summary>Menor valor (nó mais à esquerda). Árvore vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Minimo()
    {
        var atual = _raiz ?? throw new InvalidOperationException("A árvore está vazia.");
        while (atual.Esquerda is not null)
            atual = atual.Esquerda;

        return atual.Valor;
    }

    /// <summary>Maior valor (nó mais à direita). Árvore vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Maximo()
    {
        var atual = _raiz ?? throw new InvalidOperationException("A árvore está vazia.");
        while (atual.Direita is not null)
            atual = atual.Direita;

        return atual.Valor;
    }

    /// <summary>
    /// Remove o valor tratando os três casos: folha, um filho e dois filhos
    /// (neste último, copia o sucessor em ordem — o menor da subárvore direita — e remove o sucessor).
    /// Retorna false se o valor não existir.
    /// </summary>
    public bool Remover(T valor)
    {
        var removido = false;
        _raiz = Remover(_raiz, valor, ref removido);
        if (removido)
            Count--;

        return removido;
    }

    /// <summary>
    /// Percurso em ordem (esquerda, nó, direita): devolve os valores ORDENADOS.
    /// Implementação iterativa com pilha explícita, para não estourar a pilha de chamadas em árvores degeneradas.
    /// </summary>
    public IEnumerable<T> EmOrdem()
    {
        var pilha = new Stack<No>();
        var atual = _raiz;
        while (atual is not null || pilha.Count > 0)
        {
            while (atual is not null)
            {
                pilha.Push(atual);
                atual = atual.Esquerda;
            }

            atual = pilha.Pop();
            yield return atual.Valor;
            atual = atual.Direita;
        }
    }

    private static No? Remover(No? no, T valor, ref bool removido)
    {
        if (no is null)
            return null;

        var comparacao = valor.CompareTo(no.Valor);
        if (comparacao < 0)
        {
            no.Esquerda = Remover(no.Esquerda, valor, ref removido);
            return no;
        }

        if (comparacao > 0)
        {
            no.Direita = Remover(no.Direita, valor, ref removido);
            return no;
        }

        removido = true;
        if (no.Esquerda is null)
            return no.Direita;
        if (no.Direita is null)
            return no.Esquerda;

        var sucessor = no.Direita;
        while (sucessor.Esquerda is not null)
            sucessor = sucessor.Esquerda;

        no.Valor = sucessor.Valor;
        var ignorado = false;
        no.Direita = Remover(no.Direita, sucessor.Valor, ref ignorado);
        return no;
    }

    private static int CalcularAltura(No? no) =>
        no is null ? 0 : 1 + Math.Max(CalcularAltura(no.Esquerda), CalcularAltura(no.Direita));

    private sealed class No(T valor)
    {
        public T Valor { get; set; } = valor;
        public No? Esquerda { get; set; }
        public No? Direita { get; set; }
    }
}
