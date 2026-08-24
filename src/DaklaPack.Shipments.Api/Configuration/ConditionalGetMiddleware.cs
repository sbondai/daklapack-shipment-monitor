using System.Globalization;
using System.Security.Cryptography;

using Microsoft.Net.Http.Headers;

namespace DaklaPack.Shipments.Api.Configuration;

/// <summary>
/// Answers repeated identical reads with 304 Not Modified instead of the body again.
/// </summary>
/// <remarks>
/// The monitoring view polls every fifteen seconds and the list changes far less often, so with a
/// room of operators watching, the dominant traffic is the same page fetched over and over.
///
/// Scoped to the shipment endpoints at the composition root rather than applied globally: buffering
/// every response to hash it would tax the OpenAPI document and the docs UI for no benefit, and
/// would break anything streamed.
///
/// Two honest limits. The response is buffered to be hashed, costing memory proportional to the
/// page - bounded because page size is capped. And this saves bandwidth, not server work: the query
/// still runs. Removing the work needs a cache, which belongs with a datastore whose reads are
/// worth avoiding rather than an in-memory array.
/// </remarks>
internal sealed class ConditionalGetMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);

            // 304 is only ever correct for a cacheable success; anything else passes through.
            if (context.Response.StatusCode != StatusCodes.Status200OK)
            {
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody, context.RequestAborted);
                return;
            }

            var etag = ComputeETag(buffer);
            context.Response.Headers.ETag = etag.ToString();

            if (IsNoneMatch(context.Request, etag))
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.ContentLength = null;
                context.Response.Headers.Remove(HeaderNames.ContentType);
                return;
            }

            context.Response.ContentLength = buffer.Length;
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    /// <summary>
    /// Evaluates <c>If-None-Match</c> per RFC 9110 §13.1.2.
    /// </summary>
    /// <remarks>
    /// A string comparison against the raw header is not enough: the field is a comma-separated
    /// list, <c>*</c> matches any existing representation, and the comparison is explicitly the
    /// weak one, so <c>W/"x"</c> has to match <c>"x"</c>. Getting this wrong fails open - the client
    /// silently re-downloads every poll and the optimisation quietly does nothing.
    /// </remarks>
    private static bool IsNoneMatch(HttpRequest request, EntityTagHeaderValue etag)
    {
        var header = request.Headers.IfNoneMatch;
        if (header.Count == 0)
        {
            return false;
        }

        if (!EntityTagHeaderValue.TryParseList(header, out var candidates))
        {
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (candidate.Equals(EntityTagHeaderValue.Any)
                || candidate.Compare(etag, useStrongComparison: false))
            {
                return true;
            }
        }

        return false;
    }

    // Hashing the payload makes the tag correct by construction: any change a client would see
    // changes it, and nothing else does. A server-issued version would be cheaper once one exists,
    // because it would not require rendering the response to discover it has not changed.
    //
    // Weak, because the tag identifies the response semantically rather than byte-for-byte: content
    // negotiation may re-encode the same page, and claiming octet equality would be a stronger
    // promise than is being kept.
    private static EntityTagHeaderValue ComputeETag(MemoryStream buffer)
    {
        buffer.Position = 0;
        var hash = SHA256.HashData(buffer.ToArray());

        var value = string.Create(
            CultureInfo.InvariantCulture,
            $"\"{Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant()}\"");

        return new EntityTagHeaderValue(value, isWeak: true);
    }
}
