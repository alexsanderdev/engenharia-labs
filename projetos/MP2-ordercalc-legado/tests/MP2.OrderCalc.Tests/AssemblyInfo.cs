// O legado guarda estado em campos estáticos (Db). Testes em paralelo se atropelariam.
// Isto é um sintoma, não uma solução: quando o Db estiver atrás de uma interface,
// você pode remover esta linha.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
