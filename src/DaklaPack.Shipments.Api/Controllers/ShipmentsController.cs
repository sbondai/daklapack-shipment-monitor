using DaklaPack.Shipments.Application.Common;
using DaklaPack.Shipments.Application.Shipments.GetShipments;

using Microsoft.AspNetCore.Mvc;

namespace DaklaPack.Shipments.Api.Controllers;

/// <summary>Read access to shipment orders for the operations monitoring view.</summary>
[ApiController]
[Route("api/v1/shipments")]
// No [Produces("application/json")]: it constrains negotiation for every response from this
// controller, which served RFC 7807 failures as application/json instead of problem+json.
public sealed class ShipmentsController(GetShipmentsHandler handler) : ControllerBase
{
    /// <summary>Returns a page of shipments, newest first by default.</summary>
    /// <response code="200">A page of shipments. Empty when the page lies beyond the last one.</response>
    /// <response code="400">Malformed request, e.g. a page below one or an unknown sort field.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<ShipmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ShipmentResponse>>> GetShipments(
        [FromQuery] GetShipmentsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(query, cancellationToken));
}
