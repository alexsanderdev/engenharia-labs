using F4M05.Api.DTOs;
using F4M05.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F4M05.Api.Controllers;

// CÓDIGO INICIAL: controller "por entidade". Para entender "Cancelar pedido" você abre
// Controllers/, Services/ (interface + classe), Repositories/, DTOs/ e Models/ — 6 arquivos em 5 pastas.
// TODO (Passo 4): a cada fatia criada em Features/Pedidos, APAGUE a action correspondente daqui
// (rota duplicada entre controller e minimal API dá erro de ambiguidade). No fim, apague o controller.

[ApiController]
[Route("pedidos")]
public sealed class PedidosController(IPedidoService service) : ControllerBase
{
    [HttpPost]
    public IActionResult Criar(CriarPedidoDto dto)
    {
        // Validação de entrada (duplicada no service).
        if (dto.ClienteId == Guid.Empty)
            ModelState.AddModelError(nameof(dto.ClienteId), "Cliente obrigatório.");
        if (dto.Itens is null || dto.Itens.Count == 0)
            ModelState.AddModelError(nameof(dto.Itens), "O pedido precisa ter pelo menos um item.");
        else if (dto.Itens.Any(i => i.Quantidade <= 0))
            ModelState.AddModelError(nameof(dto.Itens), "A quantidade deve ser maior que zero.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var pedido = service.Criar(dto);
            return Created($"/pedidos/{pedido.Id}", pedido);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (ArgumentException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }

    [HttpGet("{id:guid}")]
    public IActionResult Obter(Guid id)
    {
        try
        {
            return Ok(service.Obter(id));
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpGet]
    public IActionResult Listar([FromQuery] Guid? clienteId) => Ok(service.Listar(clienteId));

    // try/catch copiado e colado em cada action:

    [HttpPost("{id:guid}/confirmar")]
    public IActionResult Confirmar(Guid id)
    {
        try
        {
            return Ok(service.Confirmar(id));
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpPost("{id:guid}/cancelar")]
    public IActionResult Cancelar(Guid id)
    {
        try
        {
            return Ok(service.Cancelar(id));
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
