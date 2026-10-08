#nullable disable
using System.Globalization;

namespace MP2.OrderCalc;

// OrderCalculator v3.2 — NÃO MEXER SEM FALAR COM O JOÃO (o João saiu em 2021).
// Calcula pedido e orçamento. Usado pela loja web, pelo televendas e pelo relatório mensal.
public class OrderCalculator
{
    // Formato de itens: "SKU:QTD;SKU:QTD"  (ex.: "NB-001:1;MS-002:2")
    // tipoFrete: "NORMAL" ou "EXPRESSO"
    public ResultadoPedido Calcular(string clienteId, string itens, string cupom, string uf, string tipoFrete)
    {
        var r = new ResultadoPedido();
        r.CriadoEm = DateTime.Now;
        Console.WriteLine("[OrderCalc] Calculando pedido para " + clienteId);

        if (clienteId == null || clienteId == "") { r.Status = "ERRO"; r.Erro = "Cliente obrigatório"; return r; }
        if (!Db.Clientes.ContainsKey(clienteId)) { r.Status = "ERRO"; r.Erro = "Cliente não encontrado: " + clienteId; return r; }
        var cli = Db.Clientes[clienteId];
        if (cli.Bloqueado) { r.Status = "ERRO"; r.Erro = "Cliente bloqueado"; return r; }
        if (uf == null || uf.Length != 2) { r.Status = "ERRO"; r.Erro = "UF inválida"; return r; }
        uf = uf.ToUpper();
        if (tipoFrete != "NORMAL" && tipoFrete != "EXPRESSO") { r.Status = "ERRO"; r.Erro = "Tipo de frete inválido: " + tipoFrete; return r; }
        if (itens == null || itens.Trim() == "") { r.Status = "ERRO"; r.Erro = "Pedido sem itens"; return r; }

        decimal subtotal = 0;
        decimal descontoItens = 0;
        decimal baseImposto = 0;
        double peso = 0;
        var partes = itens.Split(';');
        for (int i = 0; i < partes.Length; i++)
        {
            var p = partes[i].Trim();
            if (p == "") continue;
            var sp = p.Split(':');
            if (sp.Length != 2) { r.Status = "ERRO"; r.Erro = "Item inválido: " + p; return r; }
            var sku = sp[0].Trim().ToUpper();
            int qtd;
            if (!int.TryParse(sp[1], out qtd)) { r.Status = "ERRO"; r.Erro = "Quantidade inválida: " + p; return r; }
            if (qtd <= 0) { r.Status = "ERRO"; r.Erro = "Quantidade deve ser maior que zero: " + sku; return r; }
            if (!Db.Produtos.ContainsKey(sku)) { r.Status = "ERRO"; r.Erro = "Produto não encontrado: " + sku; return r; }
            var prod = Db.Produtos[sku];
            if (!prod.Ativo) { r.Status = "ERRO"; r.Erro = "Produto inativo: " + sku; return r; }
            if (prod.Estoque < qtd) { r.Status = "ERRO"; r.Erro = "Estoque insuficiente: " + sku; return r; }

            decimal valorLinha = prod.Preco * qtd;
            decimal descLinha = 0;
            if (qtd >= 50) descLinha = Math.Round(valorLinha * 0.10m, 2);
            else if (qtd >= 10) descLinha = Math.Round(valorLinha * 0.05m, 2);
            subtotal += valorLinha;
            descontoItens += descLinha;
            if (prod.Categoria != "LIVRO") baseImposto += valorLinha - descLinha;
            peso += prod.Peso * qtd;
            r.Linhas.Add(sku + " x" + qtd + " = " + (valorLinha - descLinha).ToString("F2", CultureInfo.InvariantCulture));
        }
        if (r.Linhas.Count == 0) { r.Status = "ERRO"; r.Erro = "Pedido sem itens"; return r; }

        // ---- descontos do pedido
        decimal baseDesconto = subtotal - descontoItens;
        decimal descontoCliente = 0;
        if (cli.Tipo == "VIP")
        {
            descontoCliente = Math.Round(baseDesconto * 0.05m, 2);
            if (DateTime.Now.Month == 11 && DateTime.Now.Day >= 24 && DateTime.Now.Day <= 30)
            {
                descontoCliente += Math.Round(baseDesconto * 0.05m, 2);
                r.Mensagens.Add("Black Friday VIP: +5%");
            }
        }
        else if (cli.Tipo == "NOVO")
        {
            if (baseDesconto >= 100)
            {
                descontoCliente = 10;
                r.Mensagens.Add("Desconto de boas-vindas");
            }
        }

        decimal descontoCupom = 0;
        bool freteGratisCupom = false;
        Cupom cup = null;
        if (cupom != null && cupom != "")
        {
            var c = cupom.Trim().ToUpper();
            if (!Db.Cupons.ContainsKey(c))
            {
                r.Mensagens.Add("Cupom inválido: " + c);
            }
            else
            {
                cup = Db.Cupons[c];
                if (cup.Validade < DateTime.Now) { r.Mensagens.Add("Cupom expirado: " + c); cup = null; }
                else if (cup.UsosRestantes <= 0) { r.Mensagens.Add("Cupom esgotado: " + c); cup = null; }
                else if (baseDesconto < cup.MinimoPedido) { r.Mensagens.Add("Pedido abaixo do mínimo do cupom: " + c); cup = null; }
                else
                {
                    if (cup.Tipo == "PERCENT") descontoCupom = Math.Round(baseDesconto * cup.Valor / 100, 2);
                    else if (cup.Tipo == "VALOR") descontoCupom = cup.Valor;
                    else if (cup.Tipo == "FRETE") freteGratisCupom = true;
                }
            }
        }

        // VIP não acumula cupom percentual: vale o maior (decisão comercial de 2018)
        decimal descontoPedido;
        if (cli.Tipo == "VIP" && cup != null && cup.Tipo == "PERCENT")
        {
            if (descontoCupom > descontoCliente) { descontoPedido = descontoCupom; r.Mensagens.Add("Cupom substitui desconto VIP"); }
            else { descontoPedido = descontoCliente; r.Mensagens.Add("Desconto VIP mantido (cupom não acumula)"); }
        }
        else
        {
            descontoPedido = descontoCliente + descontoCupom;
        }

        decimal descontoTotal = descontoItens + descontoPedido;
        decimal teto = Math.Round(subtotal * 0.30m, 2);
        if (descontoTotal > teto) { descontoTotal = teto; r.Mensagens.Add("Desconto limitado a 30%"); }

        // ---- frete
        decimal frete = 0;
        if (uf == "SP") frete = 15;
        else if (uf == "RJ" || uf == "MG" || uf == "ES") frete = 20;
        else if (uf == "PR" || uf == "SC" || uf == "RS") frete = 25;
        else frete = 35;
        if (peso > 10) frete += (decimal)Math.Ceiling(peso - 10) * 2.5m;
        if (tipoFrete == "EXPRESSO") frete = Math.Round(frete * 1.8m, 2);
        if (tipoFrete == "NORMAL" && (subtotal - descontoTotal >= 300 || cli.Tipo == "VIP")) { frete = 0; r.Mensagens.Add("Frete grátis"); }
        if (freteGratisCupom) { frete = 0; r.Mensagens.Add("Frete grátis (cupom)"); }

        // ---- imposto (livro é isento)
        decimal aliq = 0.17m;
        if (uf == "SP") aliq = 0.18m;
        else if (uf == "RJ") aliq = 0.20m;
        decimal imposto = Math.Round(baseImposto * aliq, 2);

        decimal total = subtotal - descontoTotal + frete + imposto;
        total = Math.Round(total, 2);

        // ---- prazo em dias úteis
        int dias = tipoFrete == "EXPRESSO" ? 2 : 5;
        if (!(uf == "SP" || uf == "RJ" || uf == "MG" || uf == "ES")) dias += 2;
        var data = DateTime.Now.Date;
        while (dias > 0)
        {
            data = data.AddDays(1);
            if (data.DayOfWeek != DayOfWeek.Saturday && data.DayOfWeek != DayOfWeek.Sunday) dias--;
        }

        r.Numero = "PED-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Db.ProximoId.ToString("D4");
        Db.ProximoId++;
        r.Status = "OK";
        r.Subtotal = subtotal;
        r.Desconto = descontoTotal;
        r.Frete = frete;
        r.Imposto = imposto;
        r.Total = total;
        r.PrazoEntrega = data;

        // ---- grava (baixa estoque, consome cupom, cliente deixa de ser NOVO)
        for (int i = 0; i < partes.Length; i++)
        {
            var p = partes[i].Trim();
            if (p == "") continue;
            var sp = p.Split(':');
            var sku = sp[0].Trim().ToUpper();
            int qtd = int.Parse(sp[1]);
            Db.Produtos[sku].Estoque -= qtd;
        }
        if (cup != null) cup.UsosRestantes--;
        if (cli.Tipo == "NOVO") cli.Tipo = "NORMAL";
        Db.Pedidos.Add(new PedidoGravado { Numero = r.Numero, ClienteId = clienteId, Total = total, CriadoEm = r.CriadoEm });
        Console.WriteLine("[OrderCalc] Pedido " + r.Numero + " gravado. Total: " + total);
        return r;
    }

    // Orçamento: mesma conta, sem gravar nada. Copiado do Calcular em 2017 e "ajustado".
    public ResultadoPedido CalcularOrcamento(string clienteId, string itens, string uf, string tipoFrete)
    {
        var r = new ResultadoPedido();
        r.CriadoEm = DateTime.Now;

        if (clienteId == null || clienteId == "") { r.Status = "ERRO"; r.Erro = "Cliente obrigatório"; return r; }
        if (!Db.Clientes.ContainsKey(clienteId)) { r.Status = "ERRO"; r.Erro = "Cliente não encontrado: " + clienteId; return r; }
        var cli = Db.Clientes[clienteId];
        if (uf == null || uf.Length != 2) { r.Status = "ERRO"; r.Erro = "UF inválida"; return r; }
        uf = uf.ToUpper();
        if (tipoFrete != "NORMAL" && tipoFrete != "EXPRESSO") { r.Status = "ERRO"; r.Erro = "Tipo de frete inválido: " + tipoFrete; return r; }
        if (itens == null || itens.Trim() == "") { r.Status = "ERRO"; r.Erro = "Pedido sem itens"; return r; }

        decimal subtotal = 0, descontoItens = 0, baseImposto = 0;
        double peso = 0;
        foreach (var item in itens.Split(';'))
        {
            var p = item.Trim();
            if (p == "") continue;
            var sp = p.Split(':');
            if (sp.Length != 2) { r.Status = "ERRO"; r.Erro = "Item inválido: " + p; return r; }
            var sku = sp[0].Trim().ToUpper();
            int qtd;
            if (!int.TryParse(sp[1], out qtd)) { r.Status = "ERRO"; r.Erro = "Quantidade inválida: " + p; return r; }
            if (qtd <= 0) { r.Status = "ERRO"; r.Erro = "Quantidade deve ser maior que zero: " + sku; return r; }
            if (!Db.Produtos.ContainsKey(sku)) { r.Status = "ERRO"; r.Erro = "Produto não encontrado: " + sku; return r; }
            var prod = Db.Produtos[sku];
            if (!prod.Ativo) { r.Status = "ERRO"; r.Erro = "Produto inativo: " + sku; return r; }

            decimal valorLinha = prod.Preco * qtd;
            decimal descLinha = 0;
            if (qtd >= 50) descLinha = Math.Round(valorLinha * 0.10m, 2);
            else if (qtd >= 10) descLinha = Math.Round(valorLinha * 0.05m, 2);
            subtotal += valorLinha;
            descontoItens += descLinha;
            if (prod.Categoria != "LIVRO") baseImposto += valorLinha - descLinha;
            peso += prod.Peso * qtd;
            r.Linhas.Add(sku + " x" + qtd + " = " + (valorLinha - descLinha).ToString("F2", CultureInfo.InvariantCulture));
        }
        if (r.Linhas.Count == 0) { r.Status = "ERRO"; r.Erro = "Pedido sem itens"; return r; }

        decimal baseDesconto = subtotal - descontoItens;
        decimal descontoCliente = 0;
        if (cli.Tipo == "VIP")
        {
            descontoCliente = Math.Round(baseDesconto * 0.05m, 2);
            if (DateTime.Now.Month == 11 && DateTime.Now.Day >= 24 && DateTime.Now.Day <= 30)
            {
                descontoCliente += Math.Round(baseDesconto * 0.05m, 2);
                r.Mensagens.Add("Black Friday VIP: +5%");
            }
        }
        else if (cli.Tipo == "NOVO" && baseDesconto >= 100)
        {
            descontoCliente = 10;
            r.Mensagens.Add("Desconto de boas-vindas");
        }

        decimal descontoTotal = descontoItens + descontoCliente;
        if (descontoTotal > Math.Round(subtotal * 0.30m, 2)) { descontoTotal = Math.Round(subtotal * 0.30m, 2); r.Mensagens.Add("Desconto limitado a 30%"); }

        decimal frete;
        switch (uf)
        {
            case "SP": frete = 15; break;
            case "RJ": case "MG": case "ES": frete = 20; break;
            case "PR": case "SC": case "RS": frete = 25; break;
            default: frete = 35; break;
        }
        if (peso > 10) frete = frete + (decimal)Math.Ceiling(peso - 10) * 2.5m;
        if (tipoFrete == "EXPRESSO") frete = Math.Round(frete * 1.8m, 2);
        // ATENÇÃO: aqui é subtotal ANTES do desconto (no Calcular é depois). Bug ou regra? Ninguém sabe.
        if (tipoFrete == "NORMAL" && (subtotal >= 300 || cli.Tipo == "VIP")) { frete = 0; r.Mensagens.Add("Frete grátis"); }

        decimal imposto = Math.Round(baseImposto * (uf == "SP" ? 0.18m : uf == "RJ" ? 0.20m : 0.17m), 2);

        int dias = (tipoFrete == "EXPRESSO" ? 2 : 5) + (uf == "SP" || uf == "RJ" || uf == "MG" || uf == "ES" ? 0 : 2);
        var data = DateTime.Now.Date;
        while (dias > 0)
        {
            data = data.AddDays(1);
            if (data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday) continue;
            dias--;
        }

        r.Status = "ORCAMENTO";
        r.Subtotal = subtotal;
        r.Desconto = descontoTotal;
        r.Frete = frete;
        r.Imposto = imposto;
        r.Total = Math.Round(subtotal - descontoTotal + frete + imposto, 2);
        r.PrazoEntrega = data;
        return r;
    }
}
