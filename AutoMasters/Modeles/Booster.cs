using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Modèle représentant l'état et l'ouverture d'un booster de cartes (Feature F15).
/// Reproduit fidèlement les éléments visuels des maquettes Figma 2 et 3 :
/// - "3/10 disponibles" (PaquetsDisponibles / PaquetsMax)
/// - "09:55" compte à rebours (TempsRestantSecondes)
/// - Ouverture de 5 cartes avec séquence de révélation "Carte X sur 5".
/// </summary>
public class Booster
{
    #region Attributs

    private int paquetsDisponibles = 3;
    private int paquetsMax = 10;
    private int tempsRestantSecondes = 595; // 09:55 par défaut
    private List<Carte> cartesTirees = new List<Carte>();
    private DateTimeOffset? prochainDisponible;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public Booster()
    {
    }

    /// <summary>
    /// Constructeur d'initialisation avec quota et cartes tirées.
    /// </summary>
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

    /// <summary>
    /// Alias JSON pour "remainingQuota" renvoyé par le point de terminaison POST /boosters/open.
    /// </summary>
    [JsonProperty("remainingQuota")]
    private int? RemainingQuotaSetter
    {
        set
        {
            if (value.HasValue)
                paquetsDisponibles = value.Value;
        }
    }

    /// <summary>
    /// Alias JSON pour "stock" utilisé dans certains prototypes BTS.
    /// </summary>
    [JsonProperty("stock")]
    private int? StockSetter
    {
        set
        {
            if (value.HasValue)
                paquetsDisponibles = value.Value;
        }
    }

    [JsonProperty("maxPacks")]
    public int PaquetsMax
    {
        get => paquetsMax;
        set => paquetsMax = value;
    }

    /// <summary>
    /// Alias JSON pour "quotaMax".
    /// </summary>
    [JsonProperty("quotaMax")]
    private int? QuotaMaxSetter
    {
        set
        {
            if (value.HasValue)
                paquetsMax = value.Value;
        }
    }

    [JsonProperty("cooldownRemaining")]
    public int TempsRestantSecondes
    {
        get => tempsRestantSecondes;
        set => tempsRestantSecondes = value;
    }

    /// <summary>
    /// Alias JSON pour "nextRefillSeconds".
    /// </summary>
    [JsonProperty("nextRefillSeconds")]
    private int? NextRefillSecondsSetter
    {
        set
        {
            if (value.HasValue)
                tempsRestantSecondes = value.Value;
        }
    }

    [JsonProperty("openedCards")]
    public List<Carte> CartesTirees
    {
        get => cartesTirees;
        set => cartesTirees = value ?? new List<Carte>();
    }

    /// <summary>
    /// Alias JSON pour "cards" renvoyé par POST /boosters/open.
    /// </summary>
    [JsonProperty("cards")]
    private List<Carte>? CardsSetter
    {
        set
        {
            if (value != null)
                cartesTirees = value;
        }
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
    /// Indique si au moins un booster est disponible pour l'ouverture.
    /// </summary>
    [JsonIgnore]
    public bool EstDisponible => paquetsDisponibles > 0;

    /// <summary>
    /// Libellé du quota formaté pour la modale Figma 2 (ex: "3/10 disponibles").
    /// </summary>
    [JsonIgnore]
    public string QuotaFormate => $"{paquetsDisponibles}/{paquetsMax} disponibles";

    /// <summary>
    /// Compte à rebours formaté mm:ss pour la modale Figma 2 (ex: "09:55").
    /// </summary>
    [JsonIgnore]
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

    /// <summary>
    /// Nombre de cartes par booster (toujours 5 selon les règles métier).
    /// </summary>
    [JsonIgnore]
    public int NombreCartesParBooster => 5;

    public override string ToString()
    {
        return $"Booster: {QuotaFormate} - Recharge dans: {TempsRestantFormate}";
    }

    #endregion
}
