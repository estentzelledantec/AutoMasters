using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AutoMasters.E2ETests.Infrastructure;

public class MockGroup2ApiServer : HttpMessageHandler
{
    public const string DefaultToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.automasters_test_token";
    public const string DefaultUser = "etudiant@sio.lan";
    public const string DefaultPassword = "Password123!";

    public decimal UserBalance { get; set; } = 500000m; // 500,000 coins
    public int BoosterQuota { get; set; } = 3;
    public int BoosterQuotaMax { get; set; } = 10;

    public List<CarteSpec> Catalog { get; } = new();
    public List<CartePossedeeSpec> Portfolio { get; } = new();
    public List<OffreSpec> Offers { get; } = new();
    public HashSet<string> ProcessedIdempotencyKeys { get; } = new();
    public Dictionary<string, string> ActiveReservations { get; } = new(); // offerId -> reservedByUser

    public bool SimulateOffline { get; set; } = false;
    public HttpStatusCode? ForceStatusCode { get; set; }

    public MockGroup2ApiServer()
    {
        SeedDefaultData();
    }

    public void Reset()
    {
        UserBalance = 500000m;
        BoosterQuota = 3;
        ProcessedIdempotencyKeys.Clear();
        ActiveReservations.Clear();
        SimulateOffline = false;
        ForceStatusCode = null;
        SeedDefaultData();
    }

    private void SeedDefaultData()
    {
        Catalog.Clear();
        Portfolio.Clear();
        Offers.Clear();

        var ferrari = new CarteSpec
        {
            Id = "card_101",
            Brand = "Ferrari",
            Model = "LaFerrari Aperta",
            Variant = "Aperta",
            Year = 2017,
            PowerHp = 963,
            Rarity = "LÉGENDAIRE",
            OfficialPrice = 480000m,
            CopiesInCirculation = 210,
            ImageUrl = "https://images.unsplash.com/photo-1583121274602-3e2820c69888",
            Description = "Hypercar hybride V12 à toit ouvert"
        };

        var porsche = new CarteSpec
        {
            Id = "card_104",
            Brand = "Porsche",
            Model = "911 GT3 RS",
            Variant = "992 GT3 RS",
            Year = 2023,
            PowerHp = 525,
            Rarity = "ÉPIQUE",
            OfficialPrice = 223000m,
            CopiesInCirculation = 1200,
            ImageUrl = "https://images.unsplash.com/photo-1614162692292-7ac56d7f7f1e",
            Description = "Athlète haute performance conçue pour les circuits"
        };

        var renault = new CarteSpec
        {
            Id = "card_106",
            Brand = "Renault",
            Model = "Mégane R.S. Ultime",
            Variant = "Ultime",
            Year = 2023,
            PowerHp = 300,
            Rarity = "RARE",
            OfficialPrice = 44000m,
            CopiesInCirculation = 1976,
            ImageUrl = "https://images.unsplash.com/photo-1629897048514-3dd7414fe72a",
            Description = "Série limitée marquant la fin de la lignée Renault Sport"
        };

        var toyota = new CarteSpec
        {
            Id = "card_107",
            Brand = "Toyota",
            Model = "GR Yaris",
            Variant = "Rally Edition",
            Year = 2023,
            PowerHp = 261,
            Rarity = "RARE",
            OfficialPrice = 47000m,
            CopiesInCirculation = 3500,
            ImageUrl = "https://images.unsplash.com/photo-1621135802920-133df287f89c",
            Description = "Homologation WRC dotée d'une transmission GR-FOUR"
        };

        var lotus = new CarteSpec
        {
            Id = "card_108",
            Brand = "Lotus",
            Model = "Emira",
            Variant = "V6 First Edition",
            Year = 2022,
            PowerHp = 405,
            Rarity = "ÉPIQUE",
            OfficialPrice = 89000m,
            CopiesInCirculation = 2500,
            ImageUrl = "https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8",
            Description = "Dernière sportive à moteur thermique de Lotus"
        };

        Catalog.AddRange(new[] { ferrari, porsche, renault, toyota, lotus });

        // Seed Garage matching Mockup 2
        Portfolio.Add(new CartePossedeeSpec
        {
            Id = "copy_porsche",
            Card = porsche,
            CurrentValue = 222000m,
            Quantity = 1,
            PurchasePrice = 198000m // +24 000 P&L
        });

        Portfolio.Add(new CartePossedeeSpec
        {
            Id = "copy_renault",
            Card = renault,
            CurrentValue = 44000m,
            Quantity = 1,
            PurchasePrice = 42000m // +2 000 P&L
        });

        Portfolio.Add(new CartePossedeeSpec
        {
            Id = "copy_toyota",
            Card = toyota,
            CurrentValue = 47000m,
            Quantity = 2, // x2 multiplier
            PurchasePrice = 39500m // +15 000 P&L
        });

        Portfolio.Add(new CartePossedeeSpec
        {
            Id = "copy_lotus",
            Card = lotus,
            CurrentValue = 89000m,
            Quantity = 1,
            PurchasePrice = 91000m // -2 000 P&L
        });

        // Seed Market offers matching Mockup 4
        Offers.Add(new OffreSpec
        {
            Id = "offer_ferrari",
            CopyId = "copy_ferrari_mkt",
            Card = ferrari,
            Seller = "TurboAce",
            Price = 480000m,
            Status = "active"
        });

        Offers.Add(new OffreSpec
        {
            Id = "offer_porsche",
            CopyId = "copy_porsche_mkt",
            Card = porsche,
            Seller = "NightRider",
            Price = 225000m,
            Status = "active"
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (SimulateOffline)
        {
            throw new HttpRequestException("Simulated network failure: Server unreachable.");
        }

        if (ForceStatusCode.HasValue)
        {
            return Task.FromResult(CreateResponse(ForceStatusCode.Value, new ErreurApiSpec
            {
                Code = "FORCED_ERROR",
                Message = "Simulated forced status code"
            }));
        }

        string path = request.RequestUri?.AbsolutePath ?? string.Empty;
        string method = request.Method.Method;

        // 1. POST /auth/login
        if (method == "POST" && path.EndsWith("/auth/login"))
        {
            string body = request.Content?.ReadAsStringAsync().Result ?? string.Empty;
            var demande = JsonConvert.DeserializeObject<DemandeConnexionSpec>(body);

            if (demande == null || string.IsNullOrWhiteSpace(demande.Username) || string.IsNullOrWhiteSpace(demande.Password))
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.BadRequest, new ErreurApiSpec
                {
                    Code = "INVALID_CREDENTIALS",
                    Message = "Identifiant ou mot de passe manquant."
                }));
            }

            if (demande.Username == DefaultUser && demande.Password == DefaultPassword)
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.OK, new ReponseConnexionSpec
                {
                    AccessToken = DefaultToken,
                    RefreshToken = "refresh_token_valid_123",
                    ExpiresAt = DateTime.UtcNow.AddHours(2)
                }));
            }

            return Task.FromResult(CreateResponse(HttpStatusCode.Unauthorized, new ErreurApiSpec
            {
                Code = "UNAUTHORIZED",
                Message = "Identifiants invalides."
            }));
        }

        // 2. POST /auth/refresh
        if (method == "POST" && path.EndsWith("/auth/refresh"))
        {
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, new ReponseConnexionSpec
            {
                AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.automasters_refreshed_token",
                RefreshToken = "new_refresh_token_456",
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            }));
        }

        // 3. GET /cards
        if (method == "GET" && path.EndsWith("/cards"))
        {
            string query = request.RequestUri?.Query ?? string.Empty;
            var filtered = Catalog.AsEnumerable();

            if (query.Contains("search="))
            {
                var search = ExtractQueryParam(query, "search")?.Trim();
                if (!string.IsNullOrEmpty(search))
                {
                    filtered = filtered.Where(c =>
                        c.Brand.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        c.Model.Contains(search, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (query.Contains("rarity="))
            {
                var rarity = ExtractQueryParam(query, "rarity");
                if (!string.IsNullOrEmpty(rarity))
                {
                    filtered = filtered.Where(c => c.Rarity.Equals(rarity, StringComparison.OrdinalIgnoreCase));
                }
            }

            var envelope = new
            {
                items = filtered.ToList(),
                page = 1,
                pageSize = 20,
                total = filtered.Count(),
                totalPages = 1
            };

            return Task.FromResult(CreateResponse(HttpStatusCode.OK, envelope));
        }

        // 4. GET /cards/{id}
        if (method == "GET" && path.Contains("/cards/") && !path.EndsWith("/offers"))
        {
            string id = path.Split('/').Last();
            var card = Catalog.FirstOrDefault(c => c.Id == id);
            if (card != null)
                return Task.FromResult(CreateResponse(HttpStatusCode.OK, card));

            return Task.FromResult(CreateResponse(HttpStatusCode.NotFound, new ErreurApiSpec
            {
                Code = "NOT_FOUND",
                Message = $"Carte {id} introuvable."
            }));
        }

        // 5. GET /cards/{id}/offers or GET /offers
        if (method == "GET" && (path.EndsWith("/offers") || path.Contains("/offers")))
        {
            var activeOffers = Offers.Where(o => o.Status == "active" || o.Status == "reserved").ToList();
            var envelope = new
            {
                items = activeOffers,
                total = activeOffers.Count
            };
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, envelope));
        }

        // 6. POST /orders/buy (Immediate purchase with RM006 idempotency check)
        if (method == "POST" && path.EndsWith("/orders/buy"))
        {
            string body = request.Content?.ReadAsStringAsync().Result ?? string.Empty;
            var req = JsonConvert.DeserializeObject<DemandeAchatSpec>(body);

            // Check Idempotency header (RM006)
            string? idempotencyHeader = request.Headers.Contains("Idempotency-Key")
                ? request.Headers.GetValues("Idempotency-Key").FirstOrDefault()
                : req?.IdempotencyKey;

            if (string.IsNullOrWhiteSpace(idempotencyHeader))
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.BadRequest, new ErreurApiSpec
                {
                    Code = "MISSING_IDEMPOTENCY_KEY",
                    Message = "En-tête Idempotency-Key requis pour sécuriser la transaction."
                }));
            }

            if (ProcessedIdempotencyKeys.Contains(idempotencyHeader))
            {
                // RM006 Idempotency safeguard: return cached success without double-debiting
                return Task.FromResult(CreateResponse(HttpStatusCode.OK, new
                {
                    status = "already_processed",
                    transactionId = "tx_" + idempotencyHeader
                }));
            }

            var offer = Offers.FirstOrDefault(o => o.Id == req?.OfferId);
            if (offer == null || offer.Status == "sold")
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.Conflict, new ErreurApiSpec
                {
                    Code = "OFFER_NOT_AVAILABLE",
                    Message = "L'offre sélectionnée n'est plus disponible."
                }));
            }

            // RM003: Check if reserved by someone else
            if (ActiveReservations.TryGetValue(offer.Id, out string? reservedBy) && reservedBy != DefaultUser)
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.Conflict, new ErreurApiSpec
                {
                    Code = "OFFER_RESERVED",
                    Message = "L'offre est actuellement réservée par un autre acheteur."
                }));
            }

            if (UserBalance < offer.Price)
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.BadRequest, new ErreurApiSpec
                {
                    Code = "INSUFFICIENT_FUNDS",
                    Message = $"Solde insuffisant : {UserBalance} coins disponibles, prix : {offer.Price} coins."
                }));
            }

            // Execute purchase atomically
            UserBalance -= offer.Price;
            offer.Status = "sold";
            ProcessedIdempotencyKeys.Add(idempotencyHeader);
            ActiveReservations.Remove(offer.Id);

            // Transfer card to Portfolio
            if (offer.Card != null)
            {
                var existing = Portfolio.FirstOrDefault(p => p.Card.Id == offer.Card.Id);
                if (existing != null)
                {
                    existing.Quantity++;
                }
                else
                {
                    Portfolio.Add(new CartePossedeeSpec
                    {
                        Id = "copy_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                        Card = offer.Card,
                        CurrentValue = offer.Card.OfficialPrice,
                        Quantity = 1,
                        PurchasePrice = offer.Price
                    });
                }
            }

            return Task.FromResult(CreateResponse(HttpStatusCode.OK, new
            {
                status = "completed",
                transactionId = "tx_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                remainingBalance = UserBalance
            }));
        }

        // 7. POST /offers/{id}/reservations
        if (method == "POST" && path.Contains("/offers/") && path.EndsWith("/reservations"))
        {
            string offerId = path.Split('/')[2];
            var offer = Offers.FirstOrDefault(o => o.Id == offerId);
            if (offer == null || offer.Status != "active")
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.Conflict, new ErreurApiSpec
                {
                    Code = "CANNOT_RESERVE",
                    Message = "Cette offre ne peut pas être réservée."
                }));
            }

            offer.Status = "reserved";
            offer.ReservedBy = DefaultUser;
            offer.ExpiresAt = DateTime.UtcNow.AddMinutes(15);
            ActiveReservations[offer.Id] = DefaultUser;

            return Task.FromResult(CreateResponse(HttpStatusCode.OK, new
            {
                offerId = offer.Id,
                status = "reserved",
                expiresAt = offer.ExpiresAt
            }));
        }

        // 8. POST /offers (Create sell offer)
        if (method == "POST" && path.EndsWith("/offers"))
        {
            string body = request.Content?.ReadAsStringAsync().Result ?? string.Empty;
            var req = JsonConvert.DeserializeObject<DemandeCreationOffreSpec>(body);

            if (req == null || req.Price <= 0)
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.BadRequest, new ErreurApiSpec
                {
                    Code = "INVALID_PRICE",
                    Message = "Le prix de vente doit être strictement supérieur à zéro."
                }));
            }

            var ownedCard = Portfolio.FirstOrDefault(p => p.Id == req.CopyId);
            if (ownedCard == null)
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.NotFound, new ErreurApiSpec
                {
                    Code = "CARD_NOT_OWNED",
                    Message = "Vous ne possédez pas cet exemplaire."
                }));
            }

            var newOffer = new OffreSpec
            {
                Id = "offer_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                CopyId = req.CopyId,
                Card = ownedCard.Card,
                Price = req.Price,
                Seller = DefaultUser,
                Status = "active"
            };

            Offers.Add(newOffer);
            return Task.FromResult(CreateResponse(HttpStatusCode.Created, newOffer));
        }

        // 9. GET /portfolio
        if (method == "GET" && path.EndsWith("/portfolio"))
        {
            var res = new PortefeuilleSpec
            {
                Positions = Portfolio
            };
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, res));
        }

        // 10. POST /boosters/open (5-card unpack)
        if (method == "POST" && path.EndsWith("/boosters/open"))
        {
            if (BoosterQuota <= 0)
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.BadRequest, new ErreurApiSpec
                {
                    Code = "QUOTA_EXHAUSTED",
                    Message = "Aucun booster disponible aujourd'hui."
                }));
            }

            BoosterQuota--;

            // Pick 5 cards from catalog or generated
            var revealed = Catalog.Take(5).ToList();
            while (revealed.Count < 5 && Catalog.Count > 0)
            {
                revealed.Add(Catalog[0]);
            }

            // Atomic add to portfolio
            foreach (var card in revealed)
            {
                var existing = Portfolio.FirstOrDefault(p => p.Card.Id == card.Id);
                if (existing != null)
                {
                    existing.Quantity++;
                }
                else
                {
                    Portfolio.Add(new CartePossedeeSpec
                    {
                        Id = "copy_booster_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                        Card = card,
                        CurrentValue = card.OfficialPrice,
                        Quantity = 1,
                        PurchasePrice = 0m
                    });
                }
            }

            return Task.FromResult(CreateResponse(HttpStatusCode.OK, new
            {
                cards = revealed,
                remainingQuota = BoosterQuota
            }));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static HttpResponseMessage CreateResponse<T>(HttpStatusCode status, T data)
    {
        string json = JsonConvert.SerializeObject(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string ExtractQueryParam(string query, string paramName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(query, $@"[?&]{paramName}=([^&]+)");
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : string.Empty;
    }
}
