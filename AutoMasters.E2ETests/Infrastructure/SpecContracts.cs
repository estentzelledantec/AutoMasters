using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AutoMasters.E2ETests.Infrastructure;

public class CarteSpec
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("brand")]
    public string Brand { get; set; } = string.Empty;

    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("variant")]
    public string? Variant { get; set; }

    [JsonProperty("year")]
    public int Year { get; set; }

    [JsonProperty("powerHp")]
    public int PowerHp { get; set; }

    [JsonProperty("rarity")]
    public string Rarity { get; set; } = "COMMUNE";

    [JsonProperty("officialPrice")]
    public decimal OfficialPrice { get; set; }

    [JsonProperty("copiesInCirculation")]
    public int CopiesInCirculation { get; set; }

    [JsonProperty("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? Description { get; set; }
}

public class CartePossedeeSpec
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("card")]
    public CarteSpec Card { get; set; } = new();

    [JsonProperty("currentValue")]
    public decimal CurrentValue { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; } = 1;

    [JsonProperty("purchasePrice")]
    public decimal PurchasePrice { get; set; }

    [JsonProperty("gainLoss")]
    public decimal GainLoss => (CurrentValue * Quantity) - (PurchasePrice * Quantity);
}

public class PortefeuilleSpec
{
    [JsonProperty("positions")]
    public List<CartePossedeeSpec> Positions { get; set; } = new();

    [JsonProperty("totalValuation")]
    public decimal TotalValuation
    {
        get
        {
            decimal sum = 0;
            foreach (var pos in Positions)
                sum += pos.CurrentValue * pos.Quantity;
            return sum;
        }
    }

    [JsonProperty("totalGainLoss")]
    public decimal TotalGainLoss
    {
        get
        {
            decimal sum = 0;
            foreach (var pos in Positions)
                sum += pos.GainLoss;
            return sum;
        }
    }
}

public class OffreSpec
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("copyId")]
    public string CopyId { get; set; } = string.Empty;

    [JsonProperty("card")]
    public CarteSpec? Card { get; set; }

    [JsonProperty("seller")]
    public string Seller { get; set; } = string.Empty;

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = "active"; // active, reserved, sold, cancelled

    [JsonProperty("isReserved")]
    public bool IsReserved => Status == "reserved";

    [JsonProperty("reservedBy")]
    public string? ReservedBy { get; set; }

    [JsonProperty("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}

public class DemandeConnexionSpec
{
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;

    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;
}

public class ReponseConnexionSpec
{
    [JsonProperty("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonProperty("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonProperty("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}

public class DemandeAchatSpec
{
    [JsonProperty("offerId")]
    public string OfferId { get; set; } = string.Empty;

    [JsonProperty("idempotencyKey")]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class DemandeCreationOffreSpec
{
    [JsonProperty("copyId")]
    public string CopyId { get; set; } = string.Empty;

    [JsonProperty("price")]
    public decimal Price { get; set; }
}

public class ErreurApiSpec
{
    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonProperty("errors")]
    public List<string>? Errors { get; set; }
}

public class BoosterStateSpec
{
    public int QuotaRemaining { get; set; } = 3;
    public int QuotaMax { get; set; } = 10;
    public int NextRefillSeconds { get; set; } = 595; // 09:55
    public int CardsPerPack { get; set; } = 5;
}
