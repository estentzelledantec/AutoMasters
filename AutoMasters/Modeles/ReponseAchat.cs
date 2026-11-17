using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant la confirmation d'un achat exécuté sur le marché (POST /orders/buy).
/// </summary>
public class ReponseAchat
{
    #region Attributs

    private string statut = "";
    private string identifiantTransaction = "";
    private decimal? soldeRestant;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public ReponseAchat()
    {
    }

    /// <summary>
    /// Constructeur avec statut et identifiant de transaction.
    /// </summary>
    public ReponseAchat(string statut, string identifiantTransaction, decimal? soldeRestant = null)
    {
        this.statut = statut;
        this.identifiantTransaction = identifiantTransaction;
        this.soldeRestant = soldeRestant;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("status")]
    public string Statut
    {
        get => statut;
        set => statut = value ?? "";
    }

    [JsonProperty("transactionId")]
    public string IdentifiantTransaction
    {
        get => identifiantTransaction;
        set => identifiantTransaction = value ?? "";
    }

    [JsonProperty("remainingBalance")]
    public decimal? SoldeRestant
    {
        get => soldeRestant;
        set => soldeRestant = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Indique si la transaction a été confirmée avec succès.
    /// </summary>
    [JsonIgnore]
    public bool EstSucces => string.Equals(Statut, "completed", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(Statut, "already_processed", StringComparison.OrdinalIgnoreCase);

    #endregion
}
