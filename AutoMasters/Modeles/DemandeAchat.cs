using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO envoyé pour exécuter un achat immédiat sur le marché (POST /orders/buy).
/// Intègre la clé d'idempotence obligatoire selon la règle métier RM006.
/// </summary>
public class DemandeAchat
{
    #region Attributs

    private string identifiantOffre = "";
    private string cleIdempotence = Guid.NewGuid().ToString();

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public DemandeAchat()
    {
    }

    /// <summary>
    /// Constructeur avec offre et clé d'idempotence optionnelle.
    /// </summary>
    public DemandeAchat(string identifiantOffre, string? cleIdempotence = null)
    {
        this.identifiantOffre = identifiantOffre;
        if (!string.IsNullOrWhiteSpace(cleIdempotence))
            this.cleIdempotence = cleIdempotence;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("offerId")]
    public string IdentifiantOffre
    {
        get => identifiantOffre;
        set => identifiantOffre = value ?? "";
    }

    [JsonProperty("idempotencyKey")]
    public string CleIdempotence
    {
        get => cleIdempotence;
        set => cleIdempotence = value ?? "";
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Valide que l'identifiant d'offre et la clé d'idempotence sont présents.
    /// </summary>
    [JsonIgnore]
    public bool EstValide => !string.IsNullOrWhiteSpace(IdentifiantOffre) && !string.IsNullOrWhiteSpace(CleIdempotence);

    #endregion
}
