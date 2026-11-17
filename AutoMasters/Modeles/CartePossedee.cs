using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Modèle représentant un exemplaire de carte possédé par le joueur dans son garage (GET /portfolio).
/// Gère la quantité, le prix d'achat initial, la valeur actuelle et le gain/perte.
/// </summary>
public class CartePossedee
{
    #region Attributs

    private string identifiant = "";
    private string? identifiantExemplaire;
    private Carte carte = new Carte();
    private int quantite = 1;
    private decimal prixAchat;
    private decimal valeurActuelle;
    private decimal? gainPerte;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut requis pour la désérialisation Newtonsoft.Json.
    /// </summary>
    public CartePossedee()
    {
    }

    /// <summary>
    /// Constructeur complet avec initialisation des paramètres.
    /// </summary>
    public CartePossedee(string identifiant, Carte carte, decimal valeurActuelle, int quantite = 1, decimal prixAchat = 0)
    {
        this.identifiant = identifiant;
        this.identifiantExemplaire = identifiant;
        this.carte = carte ?? new Carte();
        this.valeurActuelle = valeurActuelle;
        this.quantite = quantite;
        this.prixAchat = prixAchat;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("id")]
    public string Identifiant
    {
        get => string.IsNullOrEmpty(identifiant) ? (identifiantExemplaire ?? "") : identifiant;
        set => identifiant = value ?? "";
    }

    [JsonProperty("copyId")]
    public string? IdentifiantExemplaire
    {
        get => identifiantExemplaire ?? identifiant;
        set => identifiantExemplaire = value;
    }

    [JsonProperty("card")]
    public Carte Carte
    {
        get => carte;
        set => carte = value ?? new Carte();
    }

    [JsonProperty("quantity")]
    public int Quantite
    {
        get => quantite;
        set => quantite = value;
    }

    [JsonProperty("purchasePrice")]
    public decimal PrixAchat
    {
        get => prixAchat;
        set => prixAchat = value;
    }

    /// <summary>
    /// Alias JSON pour "acquisitionPrice" si l'API ou le mock envoie cette clé.
    /// </summary>
    [JsonProperty("acquisitionPrice")]
    private decimal? AcquisitionPriceSetter
    {
        set
        {
            if (value.HasValue)
                prixAchat = value.Value;
        }
    }

    /// <summary>
    /// Alias JSON pour "acquisitionCost" renvoyé par GET /portfolio sur l'API Groupe 2.
    /// </summary>
    [JsonProperty("acquisitionCost")]
    private decimal? AcquisitionCostSetter
    {
        set
        {
            if (value.HasValue && prixAchat == 0)
                prixAchat = value.Value;
        }
    }

    [JsonProperty("currentValue")]
    public decimal ValeurActuelle
    {
        get => valeurActuelle;
        set => valeurActuelle = value;
    }

    [JsonProperty("gainLoss")]
    public decimal GainPerte
    {
        get => gainPerte.HasValue
            ? gainPerte.Value
            : (valeurActuelle * quantite) - (prixAchat * quantite);
        set => gainPerte = value;
    }

    /// <summary>
    /// Alias JSON pour "profitLoss" si l'API ou le mock envoie cette clé.
    /// </summary>
    [JsonProperty("profitLoss")]
    private decimal? ProfitLossSetter
    {
        set
        {
            if (value.HasValue)
                gainPerte = value.Value;
        }
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Valeur totale pour cette position (quantité * valeur unitaire actuelle).
    /// </summary>
    [JsonIgnore]
    public decimal ValeurTotalePosition => valeurActuelle * quantite;

    /// <summary>
    /// Indique si la position est en plus-value (positive ou nulle).
    /// </summary>
    [JsonIgnore]
    public bool EstEnGain => GainPerte >= 0;

    /// <summary>
    /// Libellé formaté du gain/perte avec signe (+/-) et unité (ex: "+24 000 coins").
    /// </summary>
    [JsonIgnore]
    public string GainPerteFormate => $"{(EstEnGain ? "+" : "")}{GainPerte:N0} coins";

    /// <summary>
    /// Prix réel en euros de la position (ex: "100 000 €").
    /// </summary>
    [JsonIgnore]
    public string PrixReelFormate => string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "{0:N0} €", ValeurTotalePosition);

    /// <summary>
    /// Alias de la valeur actuelle en euros pour l'affichage XAML.
    /// </summary>
    [JsonIgnore]
    public string ValeurActuelleFormatee => PrixReelFormate;

    /// <summary>
    /// Valeur en jetons de la carte possédée dans le patrimoine (Prix réel / 1000).
    /// Exemple : 100 000 € = 100 Jetons, 35 000 € = 35 Jetons, 25 250 € = 25,25 Jetons.
    /// </summary>
    [JsonIgnore]
    public decimal ValeurJetons => System.Math.Round(ValeurTotalePosition / 1000m, 2);

    /// <summary>
    /// Valeur en jetons formatée pour l'affichage dans le Garage (ex: "100 Jetons", "25,25 Jetons").
    /// </summary>
    [JsonIgnore]
    public string ValeurJetonsFormatee => Carte.FormaterMontantJetons(ValeurJetons);

    /// <summary>
    /// Plus/moins-value convertie en jetons (GainPerte / 1000).
    /// </summary>
    [JsonIgnore]
    public decimal GainPerteJetons => System.Math.Round(GainPerte / 1000m, 2);

    /// <summary>
    /// Plus/moins-value formatée en jetons avec signe (ex: "+12 Jetons").
    /// </summary>
    [JsonIgnore]
    public string GainPerteJetonsFormate => $"{(EstEnGain ? "+" : "")}{Carte.FormaterMontantJetons(GainPerteJetons)}";

    /// <summary>
    /// Badge d'exemplaires multiples visible sur la maquette Figma 2 (ex: "x2").
    /// </summary>
    [JsonIgnore]
    public string QuantiteBadge => quantite > 1 ? $"x{quantite}" : string.Empty;

    /// <summary>
    /// Indique si cet exemplaire est actuellement en vente sur le marché public (tant qu'il n'est pas encore vendu).
    /// </summary>
    [JsonIgnore]
    public bool EstEnVenteSurMarche => AutoMasters.Services.ServiceAPI.Instance.ExemplaireEstEnVente(this);

    /// <summary>
    /// Indique si l'exemplaire est libre dans le garage (non engagé dans une vente en cours).
    /// </summary>
    [JsonIgnore]
    public bool EstDisponibleDansGarage => !EstEnVenteSurMarche;

    public override string ToString()
    {
        return $"{Carte.Marque} {Carte.Modele} (x{Quantite}) - Valeur: {ValeurActuelle:N0} coins (P&L: {GainPerteFormate})";
    }

    #endregion
}
