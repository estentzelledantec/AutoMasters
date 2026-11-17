using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO envoyé pour créer une offre de vente sur le marché (POST /offers).
/// Lie l'identifiant de l'exemplaire possédé (copyId) au prix de vente demandé.
/// </summary>
public class DemandeCreationOffre
{
    #region Attributs

    private string identifiantExemplaire = "";
    private decimal prix;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public DemandeCreationOffre()
    {
    }

    /// <summary>
    /// Constructeur avec identifiant d'exemplaire et prix demandé.
    /// </summary>
    public DemandeCreationOffre(string identifiantExemplaire, decimal prix)
    {
        this.identifiantExemplaire = identifiantExemplaire;
        this.prix = prix;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("copyId")]
    public string IdentifiantExemplaire
    {
        get => identifiantExemplaire;
        set => identifiantExemplaire = value ?? "";
    }

    [JsonProperty("price")]
    public decimal Prix
    {
        get => prix;
        set => prix = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Valide la conformité métier de la demande (exemplaire renseigné et prix strictement positif).
    /// </summary>
    [JsonIgnore]
    public bool EstValide => !string.IsNullOrWhiteSpace(IdentifiantExemplaire) && Prix > 0;

    #endregion
}
