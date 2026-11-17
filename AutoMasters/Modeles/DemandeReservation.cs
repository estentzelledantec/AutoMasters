using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant la demande de réservation d'une offre (POST /offers/{id}/reservations).
/// </summary>
public class DemandeReservation
{
    #region Attributs

    private string identifiantOffre = "";

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public DemandeReservation()
    {
    }

    /// <summary>
    /// Constructeur avec identifiant d'offre.
    /// </summary>
    public DemandeReservation(string identifiantOffre)
    {
        this.identifiantOffre = identifiantOffre;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("offerId")]
    public string IdentifiantOffre
    {
        get => identifiantOffre;
        set => identifiantOffre = value ?? "";
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Valide que l'identifiant d'offre est renseigné.
    /// </summary>
    [JsonIgnore]
    public bool EstValide => !string.IsNullOrWhiteSpace(IdentifiantOffre);

    #endregion
}
