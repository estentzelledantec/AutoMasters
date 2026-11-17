using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Modèle représentant une offre de vente sur le marché numérique (GET /cards/{id}/offers, POST /offers).
/// Prend en charge le verrouillage par réservation temporaire (RM003, RM004).
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

    /// <summary>
    /// Constructeur par défaut requis pour la désérialisation Newtonsoft.Json.
    /// </summary>
    public Offre()
    {
    }

    /// <summary>
    /// Constructeur d'initialisation complète.
    /// </summary>
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

    /// <summary>
    /// Indique si l'offre est active et disponible pour un achat immédiat.
    /// </summary>
    [JsonIgnore]
    public bool EstDisponibleAchat => string.Equals(statut, "active", StringComparison.OrdinalIgnoreCase) && !EstReservee;

    /// <summary>
    /// Indique si cette annonce a été publiée par le joueur actuellement connecté.
    /// Tant qu'elle n'est pas vendue, c'est toujours sa voiture : il ne peut ni l'acheter ni la réserver lui-même.
    /// </summary>
    [JsonIgnore]
    public bool EstMonOffre => !string.IsNullOrWhiteSpace(vendeur) &&
        string.Equals(vendeur.Trim(), AutoMasters.Services.ServiceAPI.Instance.UtilisateurConnecte?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indique si l'offre appartient à un autre joueur et peut donc être achetée ou réservée.
    /// </summary>
    [JsonIgnore]
    public bool EstAchetableParMoi => !EstMonOffre;

    /// <summary>
    /// Prix formaté avec unité pour l'affichage (ex: "480 000 coins").
    /// </summary>
    [JsonIgnore]
    public string PrixFormate => $"{Prix:N0} coins";

    /// <summary>
    /// Prix réel équivalent en euros de l'offre.
    /// </summary>
    [JsonIgnore]
    public decimal PrixReelEuros => Prix >= 1000m ? Prix : Prix * 1000m;

    /// <summary>
    /// Prix réel formaté en euros (ex: "325 000 €").
    /// </summary>
    [JsonIgnore]
    public string PrixReelFormate => string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "{0:N0} €", PrixReelEuros);

    /// <summary>
    /// Prix demandé en jetons sur le marché (Prix réel / 1000, ex: 325 000 € = 325 Jetons).
    /// </summary>
    [JsonIgnore]
    public decimal ValeurJetons => System.Math.Round(PrixReelEuros / 1000m, 2);

    /// <summary>
    /// Prix demandé en jetons formaté pour la place de marché (ex: "325 Jetons").
    /// </summary>
    [JsonIgnore]
    public string ValeurJetonsFormatee => Carte.FormaterMontantJetons(ValeurJetons);

    /// <summary>
    /// Libellé du statut en français pour l'affichage.
    /// </summary>
    [JsonIgnore]
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
        return $"Offre #{Identifiant} - {Carte?.Modele ?? IdentifiantExemplaire} à {PrixFormate} ({StatutLibelle})";
    }

    #endregion
}
