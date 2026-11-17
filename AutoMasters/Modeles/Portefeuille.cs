using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Modèle représentant le portefeuille (garage) du collectionneur (GET /portfolio).
/// Agrège la liste des cartes possédées, la valorisation globale et la performance nette.
/// </summary>
public class Portefeuille
{
    #region Attributs

    private ObservableCollection<CartePossedee> cartesPossedees = new ObservableCollection<CartePossedee>();
    private decimal? valeurTotale;
    private decimal? totalGainPerte;
    private decimal solde = 25000m;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut requis pour la désérialisation Newtonsoft.Json.
    /// </summary>
    public Portefeuille()
    {
    }

    /// <summary>
    /// Constructeur avec liste initiale de cartes possédées.
    /// </summary>
    public Portefeuille(IEnumerable<CartePossedee>? cartes)
    {
        cartesPossedees = new ObservableCollection<CartePossedee>(cartes ?? Enumerable.Empty<CartePossedee>());
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("positions")]
    public ObservableCollection<CartePossedee> CartesPossedees
    {
        get => cartesPossedees;
        set => cartesPossedees = value ?? new ObservableCollection<CartePossedee>();
    }

    /// <summary>
    /// Alias pour compatibilité avec le code appelant et les suites de tests unitaires.
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<CartePossedee> Positions
    {
        get => cartesPossedees;
        set => cartesPossedees = value ?? new ObservableCollection<CartePossedee>();
    }

    /// <summary>
    /// Alias JSON pour "ownedCards" si l'API ou le mock envoie cette clé.
    /// </summary>
    [JsonProperty("ownedCards")]
    private ObservableCollection<CartePossedee>? OwnedCardsSetter
    {
        set
        {
            if (value != null)
                cartesPossedees = value;
        }
    }

    [JsonProperty("totalValuation")]
    public decimal ValeurTotale
    {
        get
        {
            if (valeurTotale.HasValue)
                return valeurTotale.Value;

            decimal total = 0;
            foreach (var pos in cartesPossedees)
                total += pos.ValeurTotalePosition;
            return total;
        }
        set => valeurTotale = value;
    }

    [JsonProperty("totalProfitLoss")]
    public decimal TotalGainPerte
    {
        get
        {
            if (totalGainPerte.HasValue)
                return totalGainPerte.Value;

            decimal total = 0;
            foreach (var pos in cartesPossedees)
                total += pos.GainPerte;
            return total;
        }
        set => totalGainPerte = value;
    }

    /// <summary>
    /// Alias JSON pour "totalGainLoss" conforme aux spécifications d'évaluation.
    /// </summary>
    [JsonProperty("totalGainLoss")]
    private decimal? TotalGainLossSetter
    {
        set
        {
            if (value.HasValue)
                totalGainPerte = value.Value;
        }
    }

    [JsonProperty("balance")]
    public decimal Solde
    {
        get => solde;
        set => solde = value;
    }

    /// <summary>
    /// Réinitialise les totaux figés afin de forcer le recalcul dynamique à partir des positions enrichies.
    /// </summary>
    public void RecalculerValorisation()
    {
        valeurTotale = null;
        totalGainPerte = null;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Nombre total d'exemplaires physiques/numériques dans le garage.
    /// </summary>
    [JsonIgnore]
    public int NombreTotalCartes
    {
        get
        {
            int total = 0;
            foreach (var pos in cartesPossedees)
                total += pos.Quantite;
            return total;
        }
    }

    /// <summary>
    /// Nombre de modèles distincts possédés.
    /// </summary>
    [JsonIgnore]
    public int NombreModelesDistincts => cartesPossedees.Count;

    /// <summary>
    /// Indique si la valorisation globale du portefeuille est en plus-value.
    /// </summary>
    [JsonIgnore]
    public bool EstEnGain => TotalGainPerte >= 0;

    /// <summary>
    /// Valeur totale formatée pour l'affichage (ex: "482 500 coins" comme sur la maquette Figma 2).
    /// </summary>
    [JsonIgnore]
    public string ValeurTotaleFormatee => $"{ValeurTotale:N0} coins";

    /// <summary>
    /// Performance globale nette formatée avec signe (ex: "+39 000 coins").
    /// </summary>
    [JsonIgnore]
    public string TotalGainPerteFormate => $"{(EstEnGain ? "+" : "")}{TotalGainPerte:N0} coins";

    /// <summary>
    /// Patrimoine total en jetons (somme de la valeur des voitures possédées divisée par 1000).
    /// </summary>
    [JsonIgnore]
    public decimal PatrimoineJetons => System.Math.Round(ValeurTotale / 1000m, 2);

    /// <summary>
    /// Patrimoine total formaté en jetons (ex: "482,50 Jetons").
    /// </summary>
    [JsonIgnore]
    public string PatrimoineJetonsFormate => Carte.FormaterMontantJetons(PatrimoineJetons);

    /// <summary>
    /// Valeur réelle totale du parc automobile en euros (ex: "482 500 €").
    /// </summary>
    [JsonIgnore]
    public string PatrimoineEurosFormate => string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "{0:N0} €", ValeurTotale);

    /// <summary>
    /// Solde liquide disponible dans le portefeuille en jetons (Solde / 1000) pour acheter sur le marché.
    /// </summary>
    [JsonIgnore]
    public decimal SoldeJetons => System.Math.Round(Solde / 1000m, 2);

    /// <summary>
    /// Solde liquide du portefeuille formaté en jetons (ex: "350 Jetons").
    /// </summary>
    [JsonIgnore]
    public string SoldeJetonsFormate => Carte.FormaterMontantJetons(SoldeJetons);

    public override string ToString()
    {
        return $"Portefeuille: {NombreTotalCartes} cartes ({NombreModelesDistincts} modèles) - Valeur: {ValeurTotaleFormatee} (P&L: {TotalGainPerteFormate})";
    }

    #endregion
}
