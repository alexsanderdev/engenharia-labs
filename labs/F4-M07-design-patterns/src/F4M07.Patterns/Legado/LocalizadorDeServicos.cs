namespace F4M07.Patterns.Legado;

/// <summary>
/// ANTI-PADRÃO: Service Locator estático. A classe que chama <see cref="Resolver{T}"/> mente na assinatura
/// (diz que não precisa de nada) e só quebra em RUNTIME quando alguém esqueceu de registrar.
/// Depois do lab, NADA fora da pasta Legado pode usar esta classe.
/// </summary>
public static class LocalizadorDeServicos
{
    private static readonly Dictionary<Type, Func<object>> _fabricas = [];

    public static void Registrar<T>(Func<T> fabrica) where T : class => _fabricas[typeof(T)] = fabrica;

    public static T Resolver<T>() where T : class =>
        _fabricas.TryGetValue(typeof(T), out var fabrica)
            ? (T)fabrica()
            : throw new InvalidOperationException($"{typeof(T).Name} não registrado no LocalizadorDeServicos.");

    public static void Limpar() => _fabricas.Clear();
}
