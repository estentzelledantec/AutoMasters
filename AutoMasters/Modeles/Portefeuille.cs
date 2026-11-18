using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe qui représente le garage et le solde du joueur
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

    public Portefeuille()
    {
    }

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

    [JsonProperty("balance")]
    public decimal Solde
    {
        get => solde;
        set => solde = value;
    }

    /// <summary>
    /// Permet de recalculer la valeur totale du garage après l'ajout ou la vente d'une voiture
    /// </summary>
    public void RecalculerValorisation()
    {
        valeurTotale = null;
        totalGainPerte = null;
    }

    #endregion

    #region Methodes

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

    public int NombreModelesDistincts => cartesPossedees.Count;

    public bool EstEnGain => TotalGainPerte >= 0;

    public string ValeurTotaleFormatee => $"{ValeurTotale:N0} €";

    public string TotalGainPerteFormate => $"{(EstEnGain ? "+" : "")}{TotalGainPerte:N0} €";

    public decimal PatrimoineJetons => System.Math.Round(ValeurTotale / 1000m, 2);

    public string PatrimoineJetonsFormate => Carte.FormaterMontantJetons(PatrimoineJetons);

    public string PatrimoineEurosFormate => string.Format(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "{0:N0} €", ValeurTotale);

    public decimal SoldeJetons => System.Math.Round(Solde / 1000m, 2);

    public string SoldeJetonsFormate => Carte.FormaterMontantJetons(SoldeJetons);

    public override string ToString()
    {
        return $"Portefeuille : {NombreTotalCartes} cartes ({PatrimoineJetonsFormate})";
    }

    #endregion
}
