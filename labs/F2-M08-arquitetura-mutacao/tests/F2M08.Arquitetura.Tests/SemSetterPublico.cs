using Mono.Cecil;
using NetArchTest.Rules;

namespace F2M08.Arquitetura.Tests;

/// <summary>
/// Regra customizada do NetArchTest (via Mono.Cecil, que o NetArchTest já usa por baixo):
/// o tipo não pode ter propriedade com setter público (nem <c>init</c> público).
/// Estado de entidade muda por MÉTODOS com intenção (Cancelar, AlterarPreco), não por atribuição.
/// </summary>
internal sealed class SemSetterPublico : ICustomRule
{
    public bool MeetsRule(TypeDefinition type) =>
        !type.Properties.Any(p => p.SetMethod is { IsPublic: true });
}
