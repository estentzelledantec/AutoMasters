using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant l'enveloppe retournée lors de la consultation des offres du marché (GET /cards/{id}/offers, GET /offers).
/// </summary>
public class ReponseOffres
{
    #region Attributs

    private ObservableCollection<Offre> offres = new ObservableCollection<Offre>();
    private int total;
    private int page = 1;
    private int taillePage = 20;
    private int totalPages = 1;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public ReponseOffres()
    {
    }

    /// <summary>
    /// Constructeur avec collection d'offres.
    /// </summary>
    public ReponseOffres(IEnumerable<Offre> listeOffres)
    {
        offres = new ObservableCollection<Offre>(listeOffres ?? Enumerable.Empty<Offre>());
        total = offres.Count;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("items")]
    public ObservableCollection<Offre> Offres
    {
        get => offres;
        set => offres = value ?? new ObservableCollection<Offre>();
    }

    [JsonProperty("total")]
    public int Total
    {
        get => total > 0 ? total : offres.Count;
        set => total = value;
    }

    [JsonProperty("page")]
    public int Page
    {
        get => page;
        set => page = value;
    }

    [JsonProperty("pageSize")]
    public int TaillePage
    {
        get => taillePage;
        set => taillePage = value;
    }

    [JsonProperty("totalPages")]
    public int TotalPages
    {
        get => totalPages;
        set => totalPages = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Retourne le nombre d'offres actuellement actives (non réservées et non vendues).
    /// </summary>
    [JsonIgnore]
    public int NombreOffresActives => offres.Count(o => o.EstDisponibleAchat);

    #endregion
}
