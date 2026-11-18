using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe qui récupère la réponse de l'API après une connexion réussie
/// </summary>
public class ReponseConnexion
{
    #region Attributs

    private string jetonAcces = "";
    private string? jetonRafraichissement;
    private DateTime? dateExpiration;

    #endregion

    #region Constructeurs

    public ReponseConnexion()
    {
    }

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
    /// Vérifie si le token est présent et pas encore expiré
    /// </summary>
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
