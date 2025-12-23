using MediatR;
using Microsoft.AspNetCore.Mvc;
using ServicoPix.Application.UseCases.SolicitarPix.Commands;
using ServicoPix.Application.UseCases.SolicitarPix.Queries;

namespace ServicoPix.Api.Controllers
{
    [ApiController]
    [Route("api/v1/pix")]
    public class PixController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PixController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Registra uma solicitação de PIX e enfileira para processamento.
        /// </summary>
        /// <returns>Retorna o Protocolo (ID) para acompanhamento.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Solicitar([FromBody] SolicitarPixCommand command)
        {
            // O ValidationBehavior roda antes disso. Se inválido, nem chega aqui.
            var protocoloId = await _mediator.Send(command);

            // Retornamos 202 (Accepted) porque a operação é Assíncrona via Fila.
            // O cliente recebe o ID e depois consulta o status.
            return Accepted(new
            {
                Protocolo = protocoloId,
                Mensagem = "Solicitação recebida com sucesso."
            });
        }

        /// <summary>
        /// Consulta o status atual de uma transação pelo Protocolo.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PixDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterStatus(Guid id)
        {
            var query = new ObterPixPorIdQuery(id);
            var resultado = await _mediator.Send(query);

            if (resultado == null)
                return NotFound(new { Mensagem = "Transação não encontrada." });

            return Ok(resultado);
        }
    }
}