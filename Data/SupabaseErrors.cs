using System.Text.Json;
using Supabase.Postgrest.Exceptions;

namespace LuxWatch.Data;

/// <summary>What kind of failure a write hit, so a page can react (e.g. refresh a list).</summary>
public enum WriteErrorKind { Duplicate, Invalid, Forbidden, Network, Unknown }

public sealed record WriteError(WriteErrorKind Kind, string Message);

/// <summary>
/// One place that turns Supabase/PostgREST failures into messages for the UI.
/// The HTTP status's first digit says whose problem it is (4xx: the request,
/// 5xx: the server); the Postgres error code in the body says exactly what.
/// </summary>
public static class SupabaseErrors
{
    public static WriteError Map(Exception ex)
    {
        if (ex is PostgrestException pex)
        {
            var code = ReadPostgresCode(pex.Content);
            var status = pex.StatusCode;

            if (code == "23505" || status == 409)
                return new(WriteErrorKind.Duplicate, "You've already reviewed this order.");
            if (code == "23514" || code == "23502" || code == "22P02" || status == 400)
                return new(WriteErrorKind.Invalid, "Please check your ratings and comment.");
            if (code == "42501" || status is 401 or 403)
                return new(WriteErrorKind.Forbidden, "You don't have permission to do that.");
        }

        if (ex is HttpRequestException or TaskCanceledException)
            return new(WriteErrorKind.Network, "Couldn't reach the server. Check your connection and try again.");

        return new(WriteErrorKind.Unknown, "Something went wrong. Please try again.");
    }

    // PostgREST error body: {"code":"23505","details":…,"hint":…,"message":…}
    private static string? ReadPostgresCode(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                   && doc.RootElement.TryGetProperty("code", out var c)
                ? c.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
