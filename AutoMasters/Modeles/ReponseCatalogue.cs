using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant l'enveloppe paginée retournée lors de la consultation du catalogue (GET /cards).
/// </summary>
public class ReponseCatalogue
{
    #region Attributs

    private List<Carte> cartes = new List<Carte>();
    private int page = 1;
    private int taillePage = 20;
    private int total;
    private int totalPages = 1;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public ReponseCatalogue()
    {
    }

    /// <summary>
    /// Constructeur avec liste de cartes.
    /// </summary>
    public ReponseCatalogue(IEnumerable<Carte> listeCartes, int total = 0, int totalPages = 1)
    {
        cartes = new List<Carte>(listeCartes ?? Enumerable.Empty<Carte>());
        this.total = total > 0 ? total : cartes.Count;
        this.totalPages = totalPages;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("items")]
    public List<Carte> Cartes
    {
        get => cartes;
        set => cartes = value ?? new List<Carte>();
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

    [JsonProperty("total")]
    public int Total
    {
        get => total > 0 ? total : cartes.Count;
        set => total = value;
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
    /// Indique si une page suivante existe.
    /// </summary>
    [JsonIgnore]
    public bool APageSuivante => Page < TotalPages;

    /// <summary>
    /// Indique si une page précédente existe.
    /// </summary>
    [JsonIgnore]
    public bool APagePrecedente => Page > 1;

    #endregion
}
