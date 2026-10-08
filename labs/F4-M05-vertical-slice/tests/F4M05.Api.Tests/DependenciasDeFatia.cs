using Mono.Cecil;
using Mono.Cecil.Cil;

namespace F4M05.Api.Tests;

/// <summary>
/// Lê o IL da Api (Mono.Cecil, que já vem com o NetArchTest) e lista TODOS os tipos que uma fatia usa:
/// herança, interfaces, campos, propriedades, parâmetros, retornos e chamadas dentro dos métodos
/// (inclusive lambdas dos endpoints, que viram tipos aninhados gerados pelo compilador).
/// </summary>
internal static class DependenciasDeFatia
{
    public static IReadOnlySet<string> TiposRaizUsadosPor(string caminhoDoAssembly, string nomeCompletoDaFatia)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(caminhoDoAssembly);
        var fatia = assembly.MainModule.GetType(nomeCompletoDaFatia)
            ?? throw new InvalidOperationException($"Tipo {nomeCompletoDaFatia} não encontrado.");

        var usados = new HashSet<string>(StringComparer.Ordinal);
        Visitar(fatia, usados);
        return usados;
    }

    private static void Visitar(TypeDefinition tipo, HashSet<string> usados)
    {
        Registrar(tipo.BaseType, usados);
        foreach (var i in tipo.Interfaces) Registrar(i.InterfaceType, usados);
        foreach (var f in tipo.Fields) Registrar(f.FieldType, usados);
        foreach (var p in tipo.Properties) Registrar(p.PropertyType, usados);

        foreach (var m in tipo.Methods)
        {
            Registrar(m.ReturnType, usados);
            foreach (var p in m.Parameters) Registrar(p.ParameterType, usados);
            if (!m.HasBody) continue;
            foreach (var v in m.Body.Variables) Registrar(v.VariableType, usados);
            foreach (var instrucao in m.Body.Instructions) RegistrarOperando(instrucao, usados);
        }

        foreach (var aninhado in tipo.NestedTypes) Visitar(aninhado, usados);
    }

    private static void RegistrarOperando(Instruction instrucao, HashSet<string> usados)
    {
        switch (instrucao.Operand)
        {
            case TypeReference t:
                Registrar(t, usados);
                break;
            case MethodReference m:
                Registrar(m.DeclaringType, usados);
                Registrar(m.ReturnType, usados);
                foreach (var p in m.Parameters) Registrar(p.ParameterType, usados);
                if (m is GenericInstanceMethod g)
                    foreach (var a in g.GenericArguments) Registrar(a, usados);
                break;
            case FieldReference f:
                Registrar(f.DeclaringType, usados);
                Registrar(f.FieldType, usados);
                break;
        }
    }

    private static void Registrar(TypeReference? tipo, HashSet<string> usados)
    {
        switch (tipo)
        {
            case null:
                return;
            case GenericInstanceType generico:
                Registrar(generico.ElementType, usados);
                foreach (var a in generico.GenericArguments) Registrar(a, usados);
                return;
            case TypeSpecification especificacao: // arrays, ref, ponteiros
                Registrar(especificacao.ElementType, usados);
                return;
        }

        var raiz = tipo;
        while (raiz.DeclaringType is not null) raiz = raiz.DeclaringType;
        usados.Add(raiz.FullName);
    }
}
