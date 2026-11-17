using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant la confirmation d'une réservation temporaire d'offre (POST /offers/{id}/reservations).
/// Conforme à la règle métier RM004 (date d'expiration obligatoire).
/// </summary>
public class ReponseReservation
{
    #region Attributs

    private string identifiantOffre = "";
    private string statut = "reserved";
    private DateTime dateExpiration;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public ReponseReservation()
    {
    }

    /// <summary>
    /// Constructeur avec identifiant, statut et date d'expiration.
    /// </summary>
    public ReponseReservation(string identifiantOffre, string statut, DateTime dateExpiration)
    {
        this.identifiantOffre = identifiantOffre;
        this.statut = statut;
        this.dateExpiration = dateExpiration;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("offerId")]
    public string IdentifiantOffre
    {
        get => identifiantOffre;
        set => identifiantOffre = value ?? "";
    }

    [JsonProperty("status")]
    public string Statut
    {
        get => statut;
        set => statut = value ?? "reserved";
    }

    [JsonProperty("expiresAt")]
    public DateTime DateExpiration
    {
        get => dateExpiration;
        set => dateExpiration = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Temps restant avant expiration de la réservation.
    /// </summary>
    [JsonIgnore]
    public TimeSpan TempsRestant => DateExpiration > DateTime.UtcNow
        ? DateExpiration - DateTime.UtcNow
        : TimeSpan.Zero;

    /// <summary>
    /// Indique si la réservation est encore valide.
    /// </summary>
    [JsonIgnore]
    public bool EstValide => DateExpiration > DateTime.UtcNow;

    #endregion
}
