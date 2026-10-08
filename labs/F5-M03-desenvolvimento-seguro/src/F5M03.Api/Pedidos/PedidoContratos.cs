namespace F5M03.Api.Pedidos;

// TODO (Passos 1, 2 e 3 do Lab): hoje o endpoint recebe e devolve a ENTIDADE Pedido.
// Crie aqui:
//  - o contrato de entrada (ex.: CriarPedidoRequest(ClienteId, Itens) e ItemPedidoRequest(ProdutoId, Quantidade)):
//    o cliente escolhe O QUE e QUANTO; preço, total, status, custo e observação são do servidor;
//  - o contrato de saída (ex.: PedidoResponse(Id, ClienteId, Itens, Total, Status, CriadoEm)) sem CustoTotal
//    nem ObservacaoInterna;
//  - o validador (ex.: ValidadorDePedido.Validar): ClienteId obrigatório; 1..20 itens (campo "Itens");
//    quantidade 1..100 (campo "Itens[i].Quantidade"); produto existente e ativo (campo "Itens[i].ProdutoId").
