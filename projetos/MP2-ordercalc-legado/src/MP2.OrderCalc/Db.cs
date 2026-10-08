#nullable disable
// Contrato legado (outros sistemas leem estes campos públicos): exceção documentada às regras CA1051/CA2211.
#pragma warning disable CA1051, CA2211
namespace MP2.OrderCalc;

// "Banco de dados" do sistema legado. Na vida real era um monte de SqlCommand espalhado;
// aqui são dicionários estáticos para você não precisar de SQL Server.
// REGRA DO PROJETO: trate esta classe como infraestrutura que você NÃO pode mudar
// (outros sistemas leem estas "tabelas"). Você pode envolvê-la, mas não mudar o formato.
public static class Db
{
    public static Dictionary<string, Produto> Produtos = new Dictionary<string, Produto>();
    public static Dictionary<string, Cliente> Clientes = new Dictionary<string, Cliente>();
    public static Dictionary<string, Cupom> Cupons = new Dictionary<string, Cupom>();
    public static List<PedidoGravado> Pedidos = new List<PedidoGravado>();
    public static int ProximoId = 1;

    static Db()
    {
        Reset();
    }

    // Adicionado "temporariamente" em 2019 para os testes. Ainda está aqui.
    public static void Reset()
    {
        Produtos = new Dictionary<string, Produto>();
        Produtos["NB-001"] = new Produto { Sku = "NB-001", Nome = "Notebook Pro 14", Preco = 4500.00m, Peso = 1.8, Categoria = "ELETRONICO", Ativo = true, Estoque = 10 };
        Produtos["MS-002"] = new Produto { Sku = "MS-002", Nome = "Mouse sem fio", Preco = 89.90m, Peso = 0.1, Categoria = "ELETRONICO", Ativo = true, Estoque = 200 };
        Produtos["TC-003"] = new Produto { Sku = "TC-003", Nome = "Teclado mecânico", Preco = 349.90m, Peso = 0.9, Categoria = "ELETRONICO", Ativo = true, Estoque = 50 };
        Produtos["LV-004"] = new Produto { Sku = "LV-004", Nome = "Livro Refactoring", Preco = 189.00m, Peso = 0.7, Categoria = "LIVRO", Ativo = true, Estoque = 30 };
        Produtos["LV-005"] = new Produto { Sku = "LV-005", Nome = "Livro Legacy Code", Preco = 159.90m, Peso = 0.8, Categoria = "LIVRO", Ativo = true, Estoque = 5 };
        Produtos["CD-006"] = new Produto { Sku = "CD-006", Nome = "Cadeira gamer", Preco = 1299.00m, Peso = 18.5, Categoria = "MOVEL", Ativo = true, Estoque = 3 };
        Produtos["MN-007"] = new Produto { Sku = "MN-007", Nome = "Monitor 27", Preco = 1899.00m, Peso = 6.2, Categoria = "ELETRONICO", Ativo = false, Estoque = 12 };
        Produtos["CB-008"] = new Produto { Sku = "CB-008", Nome = "Cabo HDMI", Preco = 29.90m, Peso = 0.05, Categoria = "ELETRONICO", Ativo = true, Estoque = 500 };

        Clientes = new Dictionary<string, Cliente>();
        Clientes["C1"] = new Cliente { Id = "C1", Nome = "Ana", Tipo = "NORMAL", Bloqueado = false };
        Clientes["C2"] = new Cliente { Id = "C2", Nome = "Bruno", Tipo = "VIP", Bloqueado = false };
        Clientes["C3"] = new Cliente { Id = "C3", Nome = "Carla", Tipo = "NOVO", Bloqueado = false };
        Clientes["C4"] = new Cliente { Id = "C4", Nome = "Diego", Tipo = "NORMAL", Bloqueado = true };
        Clientes["C5"] = new Cliente { Id = "C5", Nome = "Eva", Tipo = "VIP", Bloqueado = false };

        Cupons = new Dictionary<string, Cupom>();
        Cupons["BEMVINDO10"] = new Cupom { Codigo = "BEMVINDO10", Tipo = "PERCENT", Valor = 10, Validade = new DateTime(2099, 12, 31), MinimoPedido = 0, UsosRestantes = 1000 };
        Cupons["VALE20"] = new Cupom { Codigo = "VALE20", Tipo = "VALOR", Valor = 20, Validade = new DateTime(2099, 12, 31), MinimoPedido = 100, UsosRestantes = 1000 };
        Cupons["FRETEGRATIS"] = new Cupom { Codigo = "FRETEGRATIS", Tipo = "FRETE", Valor = 0, Validade = new DateTime(2099, 12, 31), MinimoPedido = 150, UsosRestantes = 1000 };
        Cupons["EXPIRADO"] = new Cupom { Codigo = "EXPIRADO", Tipo = "PERCENT", Valor = 15, Validade = new DateTime(2020, 1, 1), MinimoPedido = 0, UsosRestantes = 1000 };
        Cupons["ESGOTADO"] = new Cupom { Codigo = "ESGOTADO", Tipo = "PERCENT", Valor = 50, Validade = new DateTime(2099, 12, 31), MinimoPedido = 0, UsosRestantes = 0 };
        Cupons["MEGA40"] = new Cupom { Codigo = "MEGA40", Tipo = "PERCENT", Valor = 40, Validade = new DateTime(2099, 12, 31), MinimoPedido = 0, UsosRestantes = 1000 };

        Pedidos = new List<PedidoGravado>();
        ProximoId = 1;
    }
}

public class Produto
{
    public string Sku;
    public string Nome;
    public decimal Preco;
    public double Peso; // kg
    public string Categoria; // "ELETRONICO", "LIVRO", "MOVEL"
    public bool Ativo;
    public int Estoque;
}

public class Cliente
{
    public string Id;
    public string Nome;
    public string Tipo; // "NORMAL", "VIP", "NOVO"
    public bool Bloqueado;
}

public class Cupom
{
    public string Codigo;
    public string Tipo; // "PERCENT", "VALOR", "FRETE"
    public decimal Valor;
    public DateTime Validade;
    public decimal MinimoPedido;
    public int UsosRestantes;
}

public class PedidoGravado
{
    public string Numero;
    public string ClienteId;
    public decimal Total;
    public DateTime CriadoEm;
}
