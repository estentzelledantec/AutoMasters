using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe qui représente une annonce de vente d'une voiture sur le marché
/// </summary>
public class Offre
{
    #region Attributs

    private string identifiant = "";
    private string identifiantExemplaire = "";
    private Carte? carte;
    private string vendeur = "";
    private decimal prix;
    private string statut = "active";
    private bool estReservee;
    private string? reservePar;
    private DateTime? dateExpiration;

    #endregion

    #region Constructeurs

    public Offre()
    {
    }

    public Offre(string identifiant, string identifiantExemplaire, decimal prix, string vendeur = "", string statut = "active", Carte? carte = null)
    {
        this.identifiant = identifiant;
        this.identifiantExemplaire = identifiantExemplaire;
        this.prix = prix;
        this.vendeur = vendeur;
        this.statut = statut;
        this.carte = carte;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("id")]
    public string Identifiant
    {
        get => identifiant;
        set => identifiant = value ?? "";
    }

    [JsonProperty("copyId")]
    public string IdentifiantExemplaire
    {
        get => identifiantExemplaire;
        set => identifiantExemplaire = value ?? "";
    }

    [JsonProperty("card")]
    public Carte? Carte
    {
        get => carte;
        set => carte = value;
    }

    [JsonProperty("seller")]
    public string Vendeur
    {
        get => vendeur;
        set => vendeur = value ?? "";
    }

    [JsonProperty("price")]
    public decimal Prix
    {
        get => prix;
        set => prix = value;
    }

    [JsonProperty("status")]
    public string Statut
    {
        get => statut;
        set => statut = value ?? "active";
    }

    [JsonProperty("isReserved")]
    public bool EstReservee
    {
        get => estReservee || string.Equals(statut, "reserved", StringComparison.OrdinalIgnoreCase);
        set => estReservee = value;
    }

    [JsonProperty("reservedBy")]
    public string? ReservePar
    {
        get => reservePar;
        set => reservePar = value;
    }

    [JsonProperty("expiresAt")]
    public DateTime? DateExpiration
    {
        get => dateExpiration;
        set => dateExpiration = value;
    }

    #endregion

    #region Methodes

    public bool EstDisponibleAchat => string.Equals(statut, "active", StringComparison.OrdinalIgnoreCase) && !EstReservee;

    /// <summary>
    /// Indique si l'offre a été mise en ligne par le joueur connecté (il ne peut pas acheter sa propre vente)
    /// </summary>
    public bool EstMonOffre => !string.IsNullOrWhiteSpace(vendeur) &&
        string.Equals(vendeur.Trim(), AutoMasters.Services.ServiceAPI.Instance.UtilisateurConnecte?.Trim(), StringComparison.OrdinalIgnoreCase);

    public bool EstAchetableParMoi => !EstMonOffre;

    public string PrixFormate => $"{Prix:N0} €";

    public decimal PrixReelEuros => Prix;

    public string PrixReelFormate => string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "{0:N0} €", PrixReelEuros);

    /// <summary>
    /// Prix de vente converti en jetons (Prix / 1 000)
    /// </summary>
    public decimal ValeurJetons => System.Math.Round(PrixReelEuros / 1000m, 2);

    public string ValeurJetonsFormatee => Carte.FormaterMontantJetons(ValeurJetons);

    public string StatutLibelle
    {
        get
        {
            if (Statut == "active")
                return "En vente";
            if (Statut == "reserved")
                return "Réservée";
            if (Statut == "sold")
                return "Vendue";
            if (Statut == "cancelled")
                return "Annulée";
            return Statut;
        }
    }

    public override string ToString()
    {
        return $"Offre #{Identifiant} - {Carte?.Modele ?? IdentifiantExemplaire} à {ValeurJetonsFormatee} ({StatutLibelle})";
    }

    #endregion
}
