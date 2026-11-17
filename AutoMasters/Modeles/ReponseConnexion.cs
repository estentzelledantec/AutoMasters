using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant la réponse à une demande d'authentification réussie (POST /auth/login, POST /auth/refresh).
/// Contient le jeton JWT d'accès et facultativement le jeton de rafraîchissement.
/// </summary>
public class ReponseConnexion
{
    #region Attributs

    private string jetonAcces = "";
    private string? jetonRafraichissement;
    private DateTime? dateExpiration;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public ReponseConnexion()
    {
    }

    /// <summary>
    /// Constructeur avec jeton d'accès.
    /// </summary>
    public ReponseConnexion(string jetonAcces, string? jetonRafraichissement = null, DateTime? dateExpiration = null)
    {
        this.jetonAcces = jetonAcces;
        this.jetonRafraichissement = jetonRafraichissement;
        this.dateExpiration = dateExpiration;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("accessToken")]
    public string JetonAcces
    {
        get => jetonAcces;
        set => jetonAcces = value ?? "";
    }

    /// <summary>
    /// Alias JSON pour "token" si l'API ou le mock retourne cette clé.
    /// </summary>
    [JsonProperty("token")]
    private string? TokenSetter
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(jetonAcces))
                jetonAcces = value;
        }
    }

    [JsonProperty("refreshToken")]
    public string? JetonRafraichissement
    {
        get => jetonRafraichissement;
        set => jetonRafraichissement = value;
    }

    [JsonProperty("expiresAt")]
    public DateTime? DateExpiration
    {
        get => dateExpiration;
        set => dateExpiration = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Vérifie si le jeton est présent et non expiré (avec marge de sécurité de 30 secondes).
    /// </summary>
    [JsonIgnore]
    public bool EstValide
    {
        get
        {
            if (string.IsNullOrWhiteSpace(JetonAcces))
                return false;

            if (DateExpiration.HasValue && DateExpiration.Value <= DateTime.UtcNow.AddSeconds(30))
                return false;

            return true;
        }
    }

    #endregion
}
