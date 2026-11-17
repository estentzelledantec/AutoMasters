using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO envoyé pour modifier le prix d'une offre de vente existante (PATCH /offers/{id}).
/// </summary>
public class DemandeModificationOffre
{
    #region Attributs

    private decimal prix;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public DemandeModificationOffre()
    {
    }

    /// <summary>
    /// Constructeur avec le nouveau prix demandé.
    /// </summary>
    public DemandeModificationOffre(decimal prix)
    {
        this.prix = prix;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("price")]
    public decimal Prix
    {
        get => prix;
        set => prix = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Valide que le prix est strictement supérieur à zéro.
    /// </summary>
    [JsonIgnore]
    public bool EstValide => Prix > 0;

    #endregion
}
