using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe qui représente un paquet de cartes (booster) que le joueur peut ouvrir
/// </summary>
public class Booster
{
    #region Attributs

    private int paquetsDisponibles = 3;
    private int paquetsMax = 10;
    private int tempsRestantSecondes = 595;
    private List<Carte> cartesTirees = new List<Carte>();
    private DateTimeOffset? prochainDisponible;

    #endregion

    #region Constructeurs

    public Booster()
    {
    }

    public Booster(int paquetsDisponibles, int tempsRestantSecondes, IEnumerable<Carte>? cartes = null)
    {
        this.paquetsDisponibles = paquetsDisponibles;
        this.tempsRestantSecondes = tempsRestantSecondes;
        if (cartes != null)
            this.cartesTirees = new List<Carte>(cartes);
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("availablePacks")]
    public int PaquetsDisponibles
    {
        get => paquetsDisponibles;
        set => paquetsDisponibles = value;
    }

    [JsonProperty("maxPacks")]
    public int PaquetsMax
    {
        get => paquetsMax;
        set => paquetsMax = value;
    }

    [JsonProperty("cooldownRemaining")]
    public int TempsRestantSecondes
    {
        get => tempsRestantSecondes;
        set => tempsRestantSecondes = value;
    }

    [JsonProperty("openedCards")]
    public List<Carte> CartesTirees
    {
        get => cartesTirees;
        set => cartesTirees = value ?? new List<Carte>();
    }

    [JsonProperty("nextAvailableAt")]
    public DateTimeOffset? ProchainDisponible
    {
        get => prochainDisponible;
        set => prochainDisponible = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Indique s'il reste au moins un booster à ouvrir
    /// </summary>
    public bool EstDisponible => paquetsDisponibles > 0;

    /// <summary>
    /// Texte affiché pour le nombre de paquets restants
    /// </summary>
    public string QuotaFormate => $"{paquetsDisponibles}/{paquetsMax} disponibles";

    /// <summary>
    /// Formate le temps d'attente en minutes et secondes (mm:ss)
    /// </summary>
    public string TempsRestantFormate
    {
        get
        {
            int secondes = Math.Max(0, tempsRestantSecondes);
            int minutes = secondes / 60;
            int sec = secondes % 60;
            return $"{minutes:D2}:{sec:D2}";
        }
    }

    public int NombreCartesParBooster => 3;

    public override string ToString()
    {
        return $"Booster : {QuotaFormate} ({TempsRestantFormate})";
    }

    #endregion
}
