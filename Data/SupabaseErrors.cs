using System.Text.Json;
using Supabase.Postgrest.Exceptions;

namespace LuxWatch.Data;

/// <summary>What kind of failure a write hit, so a page can react (e.g. refresh a list).</summary>
public enum WriteErrorKind { Duplicate, Invalid, Forbidden, Unavailable, NotAllowed, NotFound, Network, Unknown }

public sealed record WriteError(WriteErrorKind Kind, string Message);

/// <summary>
/// One place that turns Supabase/PostgREST failures into messages for the UI.
/// The HTTP status's first digit says whose problem it is (4xx: the request,
/// 5xx: the server); the Postgres error code (and, for our own database
/// functions, the message key) in the body says exactly what.
/// </summary>
public static class SupabaseErrors
{
    /// <summary>Review writes (create, helpful, delete/undo).</summary>
    public static WriteError Map(Exception ex)
    {
        if (ex is PostgrestException pex)
        {
            var (code, _) = ReadBody(pex.Content);
            var status = pex.StatusCode;

            if (code == "23505" || status == 409)
                return new(WriteErrorKind.Duplicate, "You've already reviewed this order.");
            if (code == "23514" || code == "23502" || code == "22P02" || status == 400)
                return new(WriteErrorKind.Invalid, "Please check your ratings and comment.");
            if (code == "42501" || status is 401 or 403)
                return new(WriteErrorKind.Forbidden, "You don't have permission to do that.");
        }

        return Fallback(ex);
    }

    /// <summary>
    /// Order functions (rpc/place_order, cancel_order, confirm_delivery). They raise
    /// stable message keys with errcode P0001 (see database/patches/002_ordering.sql).
    /// </summary>
    public static WriteError MapOrder(Exception ex)
    {
        if (ex is PostgrestException pex)
        {
            var (code, message) = ReadBody(pex.Content);

            switch (message)
            {
                case "WATCH_NOT_AVAILABLE":
                    return new(WriteErrorKind.Unavailable, "Sorry, someone just reserved this watch.");
                case "CANNOT_BUY_OWN_WATCH":
                    return new(WriteErrorKind.NotAllowed, "You can't buy your own listing.");
                case "ORDER_NOT_CANCELLABLE":
                    return new(WriteErrorKind.NotAllowed, "This order can no longer be cancelled.");
                case "ORDER_NOT_CONFIRMABLE":
                    return new(WriteErrorKind.NotAllowed, "This order can't be confirmed.");
                case "ORDER_NOT_FOUND":
                case "WATCH_NOT_FOUND":
                case "BUYER_NOT_FOUND":
                    return new(WriteErrorKind.NotFound, "We couldn't find that.");
            }

            // Two buyers raced past the row lock: the one-active-order-per-watch index caught it.
            if (code == "23505" || pex.StatusCode == 409)
                return new(WriteErrorKind.Unavailable, "Sorry, someone just reserved this watch.");
            if (code == "42501" || pex.StatusCode is 401 or 403)
                return new(WriteErrorKind.Forbidden, "You don't have permission to do that.");
        }

        return Fallback(ex);
    }

    private static WriteError Fallback(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException
            ? new(WriteErrorKind.Network, "Couldn't reach the server. Check your connection and try again.")
            : new(WriteErrorKind.Unknown, "Something went wrong. Please try again.");

    // PostgREST error body: {"code":"P0001","details":…,"hint":…,"message":"WATCH_NOT_AVAILABLE"}
    private static (string? Code, string? Message) ReadBody(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return (null, null);
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return (null, null);
            var code = doc.RootElement.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
            var message = doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
            return (code, message);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}
