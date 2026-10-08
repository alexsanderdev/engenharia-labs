namespace F5M03.Api.Clientes;

// VULNERÁVEL — corrija (Passos 1, 2, 3 e 5 do Lab).
//
// Este DTO foi "copiado da entidade" para economizar tempo. Problemas:
//  - IsAdmin está no contrato de entrada: qualquer pessoa vira admin no cadastro (mass assignment);
//  - não há nenhuma validação (tamanho, formato, CPF, força mínima de senha);
//  - é um record: o ToString() gerado imprime TODAS as propriedades, inclusive a senha.
//
// TODO:
//  - crie um contrato de entrada só com o que o cliente pode escolher (ex.: CadastrarClienteRequest(Nome, Email, Cpf, Senha));
//  - crie um contrato de SAÍDA (ex.: ClienteResponse(Id, Nome, Email, CpfMascarado)) — nunca devolva a entidade;
//  - crie um validador (ex.: ValidadorDeCliente.Validar) usando Validacao/ErrosDeValidacao.cs:
//      Nome 2..100 (após Trim), Email válido e <= 254, CPF com dígitos verificadores válidos
//      (aceite "52998224725" ou "529.982.247-25"; recuse outros caracteres), Senha 12..128;
//  - (defesa em profundidade) sobrescreva ToString() do contrato de entrada para não imprimir a senha.

public sealed record NovoClienteRequest(string Nome, string Email, string Cpf, string Senha, bool IsAdmin = false);
