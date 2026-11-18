using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe qui représente une voiture possédée par le joueur dans son garage
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

    public CartePossedee()
    {
    }

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

    [JsonProperty("acquisitionCost")]
    private decimal? CoutAcquisitionApi
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

    #endregion

    #region Methodes

    public decimal ValeurTotalePosition => valeurActuelle * quantite;

    public bool EstEnGain => GainPerte >= 0;

    public string GainPerteFormate => $"{(EstEnGain ? "+" : "")}{GainPerte:N0} €";

    public string PrixReelFormate => string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "{0:N0} €", ValeurTotalePosition);

    public string ValeurActuelleFormatee => PrixReelFormate;

    /// <summary>
    /// Valeur de la carte convertie en jetons (Prix / 1 000)
    /// </summary>
    public decimal ValeurJetons => System.Math.Round(ValeurTotalePosition / 1000m, 2);

    public string ValeurJetonsFormatee => Carte.FormaterMontantJetons(ValeurJetons);

    public decimal GainPerteJetons => System.Math.Round(GainPerte / 1000m, 2);

    public string GainPerteJetonsFormate => $"{(EstEnGain ? "+" : "")}{Carte.FormaterMontantJetons(GainPerteJetons)}";

    public string QuantiteBadge => quantite > 1 ? $"x{quantite}" : string.Empty;

    /// <summary>
    /// Vérifie si la voiture est actuellement en vente sur le marché
    /// </summary>
    public bool EstEnVenteSurMarche => AutoMasters.Services.ServiceAPI.Instance.ExemplaireEstEnVente(this);

    public bool EstDisponibleDansGarage => !EstEnVenteSurMarche;

    public override string ToString()
    {
        return $"{Carte.Marque} {Carte.Modele} (x{Quantite}) - {ValeurJetonsFormatee}";
    }

    #endregion
}
