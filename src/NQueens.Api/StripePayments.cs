using System.Text;
using Stripe;
using Stripe.Checkout;

namespace NQueens.Api;

/// <summary>
/// Minimal Stripe integration: PaymentIntent (modern card payments) and optional Checkout Session.
/// Configure <c>Stripe:SecretKey</c> and optionally <c>Stripe:WebhookSecret</c> in appsettings or user secrets.
/// </summary>
public static class StripePayments
{
    public static WebApplication MapStripePayments(this WebApplication app)
    {
        var group = app.MapGroup("/api/stripe");

        group.MapPost(
            "/create-payment-intent",
            async (
                CreatePaymentIntentRequest body,
                IConfiguration config,
                CancellationToken cancellationToken) =>
            {
                var secretKey = config["Stripe:SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey))
                    return Results.Problem("Stripe:SecretKey is not configured.", statusCode: 500);

                if (body.Amount is < 1)
                    return Results.BadRequest(new { error = "Amount must be a positive integer (smallest currency unit, e.g. cents)." });

                var currency = string.IsNullOrWhiteSpace(body.Currency) ? "usd" : body.Currency.Trim().ToLowerInvariant();

                try
                {
                    var client = new StripeClient(secretKey);
                    var service = new PaymentIntentService(client);
                    var options = new PaymentIntentCreateOptions
                    {
                        Amount = body.Amount,
                        Currency = currency,
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                        Metadata = body.Metadata,
                    };

                    var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);
                    return Results.Ok(new CreatePaymentIntentResponse(intent.Id, intent.ClientSecret));
                }
                catch (StripeException ex)
                {
                    return Results.BadRequest(new { error = ex.Message, code = ex.StripeError?.Code });
                }
            });

        // Hosted payment page: good next step after raw PaymentIntent + Elements.
        group.MapPost(
            "/create-checkout-session",
            async (
                CreateCheckoutSessionRequest body,
                IConfiguration config,
                HttpRequest request,
                CancellationToken cancellationToken) =>
            {
                var secretKey = config["Stripe:SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey))
                    return Results.Problem("Stripe:SecretKey is not configured.", statusCode: 500);

                if (body.Amount is < 1)
                    return Results.BadRequest(new { error = "Amount must be a positive integer (smallest currency unit)." });

                var currency = string.IsNullOrWhiteSpace(body.Currency) ? "usd" : body.Currency.Trim().ToLowerInvariant();
                var baseUrl = body.SuccessUrl is not null && body.CancelUrl is not null
                    ? null
                    : $"{request.Scheme}://{request.Host}";

                var successUrl = body.SuccessUrl ?? $"{baseUrl}/?paid=1&session_id={{CHECKOUT_SESSION_ID}}";
                var cancelUrl = body.CancelUrl ?? $"{baseUrl}/?canceled=1";

                try
                {
                    var client = new StripeClient(secretKey);
                    var service = new SessionService(client);
                    var options = new SessionCreateOptions
                    {
                        Mode = "payment",
                        SuccessUrl = successUrl,
                        CancelUrl = cancelUrl,
                        LineItems =
                        [
                            new SessionLineItemOptions
                            {
                                Quantity = 1,
                                PriceData = new SessionLineItemPriceDataOptions
                                {
                                    Currency = currency,
                                    UnitAmount = body.Amount,
                                    ProductData = new SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = string.IsNullOrWhiteSpace(body.ProductName) ? "Order" : body.ProductName,
                                    },
                                },
                            },
                        ],
                    };

                    var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
                    return Results.Ok(new CreateCheckoutSessionResponse(session.Id, session.Url));
                }
                catch (StripeException ex)
                {
                    return Results.BadRequest(new { error = ex.Message, code = ex.StripeError?.Code });
                }
            });

        group.MapPost(
            "/webhook",
            async (HttpRequest httpRequest, IConfiguration config, ILoggerFactory loggerFactory) =>
            {
                var webhookSecret = config["Stripe:WebhookSecret"];
                if (string.IsNullOrWhiteSpace(webhookSecret))
                    return Results.Problem("Stripe:WebhookSecret is not configured.", statusCode: 500);

                using var reader = new StreamReader(httpRequest.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);

                if (!httpRequest.Headers.TryGetValue("Stripe-Signature", out var signatureValues))
                    return Results.BadRequest("Missing Stripe-Signature header.");

                var signature = signatureValues.ToString();
                var logger = loggerFactory.CreateLogger("StripeWebhook");

                try
                {
                    var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret, throwOnApiVersionMismatch: false);
                    switch (stripeEvent.Type)
                    {
                        case EventTypes.PaymentIntentSucceeded:
                            if (stripeEvent.Data.Object is PaymentIntent intent)
                                logger.LogInformation("PaymentIntent succeeded: {Id}", intent.Id);
                            break;
                        case EventTypes.CheckoutSessionCompleted:
                            if (stripeEvent.Data.Object is Session session)
                                logger.LogInformation("Checkout session completed: {Id}", session.Id);
                            break;
                        default:
                            logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                            break;
                    }
                }
                catch (StripeException ex)
                {
                    logger.LogWarning(ex, "Webhook signature verification failed.");
                    return Results.BadRequest($"Webhook error: {ex.Message}");
                }

                return Results.Ok();
            });

        return app;
    }
}

public sealed record CreatePaymentIntentRequest(long Amount, string? Currency = null, Dictionary<string, string>? Metadata = null);

public sealed record CreatePaymentIntentResponse(string PaymentIntentId, string? ClientSecret);

public sealed record CreateCheckoutSessionRequest(
    long Amount,
    string? Currency = null,
    string? ProductName = null,
    string? SuccessUrl = null,
    string? CancelUrl = null);

public sealed record CreateCheckoutSessionResponse(string SessionId, string? Url);
