using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe représentant une carte de voiture dans le catalogue (API du Groupe 2).
/// </summary>
public class Carte
{
    #region Attributs

    /// <summary>
    /// Taux de conversion entre le prix réel en euros et les jetons (1 jeton = 1 000 €).
    /// </summary>
    public const decimal TauxConversionJeton = 1000m;

    private string identifiant = "";
    private string marque = "";
    private string modele = "";
    private string? variante;
    private int annee;
    private int puissance;
    private string rarete = "COMMUNE";
    private decimal prixOfficiel;
    private int exemplairesEnCirculation;
    private string imageUrl = "";
    private string? description;
    private int vitesseMax;
    private double zeroACent;
    private decimal variationCours;
    private string deviseCours = "EUR";
    private string horodatageCours = "";

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut requis pour la désérialisation Newtonsoft.Json.
    /// </summary>
    public Carte()
    {
    }

    /// <summary>
    /// Constructeur complet avec paramètres.
    /// </summary>
    public Carte(
        string identifiant,
        string marque,
        string modele,
        string? variante,
        int annee,
        int puissance,
        string rarete,
        decimal prixOfficiel,
        int exemplairesEnCirculation,
        string imageUrl,
        string? description,
        int vitesseMax,
        double zeroACent)
    {
        this.identifiant = identifiant;
        this.marque = marque;
        this.modele = modele;
        this.variante = variante;
        this.annee = annee;
        this.puissance = puissance;
        this.rarete = rarete;
        this.prixOfficiel = prixOfficiel;
        this.exemplairesEnCirculation = exemplairesEnCirculation;
        this.imageUrl = imageUrl;
        this.description = description;
        this.vitesseMax = vitesseMax;
        this.zeroACent = zeroACent;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("id")]
    public string Identifiant
    {
        get => identifiant;
        set => identifiant = value ?? "";
    }

    [JsonProperty("brand")]
    public string Marque
    {
        get => marque;
        set => marque = value ?? "";
    }

    [JsonProperty("model")]
    public string Modele
    {
        get => modele;
        set => modele = value ?? "";
    }

    [JsonProperty("variant")]
    public string? Variante
    {
        get => variante;
        set => variante = value;
    }

    [JsonProperty("year")]
    public int Annee
    {
        get => annee;
        set => annee = value;
    }

    [JsonProperty("powerHp")]
    public int Puissance
    {
        get => puissance;
        set => puissance = value;
    }

    [JsonProperty("rarity")]
    public string Rarete
    {
        get => rarete;
        set => rarete = value ?? "COMMUNE";
    }

    [JsonProperty("officialPrice")]
    public decimal PrixOfficiel
    {
        get => prixOfficiel;
        set => prixOfficiel = value;
    }

    /// <summary>
    /// Alias JSON pour le champ "price" direct (ex: requêtes admin/API).
    /// </summary>
    [JsonProperty("price")]
    private decimal? PriceAliasSetter
    {
        set
        {
            if (value.HasValue && value.Value > 0 && prixOfficiel <= 0)
                prixOfficiel = value.Value;
        }
    }

    /// <summary>
    /// Alias JSON pour l'objet "quote" renvoyé par GET /cards sur l'API Groupe 2 (RM001).
    /// </summary>
    [JsonProperty("quote")]
    private Newtonsoft.Json.Linq.JObject? QuoteAliasSetter
    {
        set
        {
            if (value != null)
            {
                var p = value["price"];
                if (p != null && decimal.TryParse(p.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal quotePrice) && quotePrice > 0)
                {
                    prixOfficiel = quotePrice;
                }

                var ch = value["change"];
                if (ch != null && decimal.TryParse(ch.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal quoteChange))
                {
                    variationCours = quoteChange;
                }

                var cur = value["currency"]?.ToString();
                if (!string.IsNullOrWhiteSpace(cur))
                {
                    deviseCours = cur;
                }

                var ts = value["timestamp"]?.ToString();
                if (!string.IsNullOrWhiteSpace(ts) && System.DateTimeOffset.TryParse(ts, out System.DateTimeOffset dto))
                {
                    horodatageCours = dto.ToLocalTime().ToString("dd/MM/yyyy à HH:mm:ss");
                }
            }
        }
    }

    [JsonIgnore]
    public decimal VariationCours
    {
        get => variationCours;
        set => variationCours = value;
    }

    [JsonIgnore]
    public string DeviseCours
    {
        get => deviseCours;
        set => deviseCours = string.IsNullOrWhiteSpace(value) ? "EUR" : value;
    }

    [JsonIgnore]
    public string HorodatageCours
    {
        get => horodatageCours;
        set => horodatageCours = value ?? "";
    }

    [JsonProperty("copiesInCirculation")]
    public int ExemplairesEnCirculation
    {
        get => exemplairesEnCirculation;
        set => exemplairesEnCirculation = value;
    }

    /// <summary>
    /// Alias JSON pour le champ "circulation" renvoyé par GET /cards sur l'API Groupe 2.
    /// </summary>
    [JsonProperty("circulation")]
    private int? CirculationAliasSetter
    {
        set
        {
            if (value.HasValue && value.Value > 0 && exemplairesEnCirculation <= 0)
                exemplairesEnCirculation = value.Value;
        }
    }

    [JsonProperty("imageUrl")]
    public string ImageUrl
    {
        get => imageUrl;
        set => imageUrl = value ?? "";
    }

    [JsonProperty("description")]
    public string? Description
    {
        get => description;
        set => description = value;
    }

    [JsonProperty("topSpeedKmH")]
    public int VitesseMax
    {
        get => vitesseMax;
        set => vitesseMax = value;
    }

    [JsonProperty("zeroToHundred")]
    public double ZeroACent
    {
        get => zeroACent;
        set => zeroACent = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Nom complet formaté combinant marque, modèle et variante si disponible.
    /// </summary>
    [JsonIgnore]
    public string NomComplet => string.IsNullOrWhiteSpace(Variante)
        ? $"{Marque} {Modele}"
        : $"{Marque} {Modele} ({Variante})";

    /// <summary>
    /// Puissance formatée avec unité (ex: "525 ch").
    /// </summary>
    [JsonIgnore]
    public string PuissanceFormatee => $"{Puissance} ch";

    /// <summary>
    /// Vitesse maximale formatée (ex: "296 km/h").
    /// </summary>
    [JsonIgnore]
    public string VitesseMaxFormatee => $"{VitesseMax} km/h";

    /// <summary>
    /// Accélération 0 à 100 km/h formatée (ex: "3.2 s").
    /// </summary>
    [JsonIgnore]
    public string ZeroACentFormate => $"{ZeroACent:F1} s";

    /// <summary>
    /// Cours / prix officiel formaté avec séparateur de milliers et unité (ex: "223 000 coins").
    /// Conservé pour la compatibilité avec les tests unitaires existants.
    /// </summary>
    [JsonIgnore]
    public string PrixOfficielFormate => $"{PrixOfficiel:N0} coins";

    /// <summary>
    /// Prix réel de la voiture en euros tel que renvoyé par l'API (ex: "100 000 €" ou "42 066,59 €").
    /// </summary>
    [JsonIgnore]
    public string PrixReelFormate
    {
        get
        {
            var cultureFr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            if (PrixOfficiel == System.Math.Truncate(PrixOfficiel))
            {
                return string.Format(cultureFr, "{0:N0} €", PrixOfficiel);
            }
            return string.Format(cultureFr, "{0:N2} €", PrixOfficiel);
        }
    }

    /// <summary>
    /// Variation du cours en direct formatée (ex: "▲ +0,81 %" ou "▼ -1,38 %").
    /// </summary>
    [JsonIgnore]
    public string VariationCoursFormatee
    {
        get
        {
            var cultureFr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            if (VariationCours >= 0)
            {
                return string.Format(cultureFr, "▲ +{0:N2} %", VariationCours);
            }
            return string.Format(cultureFr, "▼ {0:N2} %", VariationCours);
        }
    }

    /// <summary>
    /// Indique si la variation du cours est positive ou nulle (utilisé par les DataTriggers XAML).
    /// </summary>
    [JsonIgnore]
    public bool EstVariationPositive => VariationCours >= 0;

    /// <summary>
    /// Horodatage officiel de la dernière cotation reçue du serveur (Règle métier RM001).
    /// </summary>
    [JsonIgnore]
    public string HorodatageCoursFormate => string.IsNullOrWhiteSpace(HorodatageCours)
        ? $"Dernière cotation serveur : {System.DateTime.Now:dd/MM/yyyy à HH:mm:ss}"
        : $"Dernière cotation serveur : {HorodatageCours}";

    /// <summary>
    /// Valeur proportionnelle de la voiture en jetons de l'application (Prix réel / 1000).
    /// Exemple : 100 000 € = 100 jetons, 35 000 € = 35 jetons, 25 250 € = 25,25 jetons.
    /// </summary>
    [JsonIgnore]
    public decimal ValeurJetons => System.Math.Round(PrixOfficiel / 1000m, 2);

    /// <summary>
    /// Valeur en jetons formatée proprement en français (ex: "100 Jetons", "35 Jetons", "25,25 Jetons").
    /// </summary>
    [JsonIgnore]
    public string ValeurJetonsFormatee => FormaterMontantJetons(ValeurJetons);

    /// <summary>
    /// Double affichage combinant le prix réel en euros et la valeur en jetons (ex: "35 000 € · 35 Jetons").
    /// </summary>
    [JsonIgnore]
    public string PrixCompletFormate => $"{PrixReelFormate} · {ValeurJetonsFormatee}";

    /// <summary>
    /// Formate un montant de jetons avec 0 décimale s'il est entier, ou 2 décimales sinon (ex: 25,25 Jetons).
    /// </summary>
    public static string FormaterMontantJetons(decimal jetons)
    {
        var cultureFr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
        if (jetons == System.Math.Truncate(jetons))
        {
            return string.Format(cultureFr, "{0:N0} Jetons", jetons);
        }
        return string.Format(cultureFr, "{0:N2} Jetons", jetons);
    }

    /// <summary>
    /// Code court de la rareté affiché en haut à gauche de la carte ("UR", "SR", "R", "C").
    /// </summary>
    [JsonIgnore]
    public string BadgeRareteCourt
    {
        get
        {
            string r = (Rarete ?? "").Trim().ToUpperInvariant();
            if (r == "LÉGENDAIRE" || r == "LEGENDAIRE" || r == "LEGENDARY" || r == "UR")
                return "UR";
            if (r == "ÉPIQUE" || r == "EPIQUE" || r == "EPIC" || r == "SR")
                return "SR";
            if (r == "RARE" || r == "R")
                return "R";
            if (r == "PEU COMMUNE" || r == "PEU_COMMUNE" || r == "UNCOMMON" || r == "UC")
                return "UC";
            return "C";
        }
    }

    /// <summary>
    /// Sous-titre regroupant la variante et l'année de la voiture.
    /// </summary>
    [JsonIgnore]
    public string SousTitreCarte => !string.IsNullOrWhiteSpace(Variante)
        ? $"{Variante} · {Annee}"
        : $"Modèle {Annee} · {Puissance} ch";

    /// <summary>
    /// Valeur calculée à partir de la puissance (conservée pour compatibilité avec les tests).
    /// </summary>
    [JsonIgnore]
    public int ScoreAttaque => Puissance > 0 ? Puissance * 10 : 500;

    /// <summary>
    /// Valeur calculée à partir de la vitesse maximale (conservée pour compatibilité avec les tests).
    /// </summary>
    [JsonIgnore]
    public int ScoreDefense => VitesseMax > 0 ? VitesseMax * 25 : 5000;

    public override string ToString()
    {
        return $"{Marque} {Modele} ({Annee}) - {Rarete}";
    }

    #endregion
}

/// <summary>
/// Représente un point d'historique de cotation boursière d'une voiture renvoyé par GET /cards/{id}/quotes (RM001).
/// </summary>
public class PointCotationMarche
{
    #region Attributs

    private decimal prix;
    private decimal variation;
    private string horodatage = "";
    private double hauteurBarre = 40;

    #endregion

    #region Constructeurs

    public PointCotationMarche()
    {
    }

    public PointCotationMarche(decimal prix, decimal variation, string horodatage, double hauteurBarre)
    {
        this.prix = prix;
        this.variation = variation;
        this.horodatage = horodatage;
        this.hauteurBarre = hauteurBarre;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("price")]
    public decimal Prix
    {
        get => prix;
        set => prix = value;
    }

    [JsonProperty("change")]
    public decimal Variation
    {
        get => variation;
        set => variation = value;
    }

    [JsonProperty("timestamp")]
    public string Horodatage
    {
        get => horodatage;
        set => horodatage = value ?? "";
    }

    [JsonIgnore]
    public double HauteurBarre
    {
        get => hauteurBarre;
        set => hauteurBarre = value;
    }

    #endregion

    #region Methodes

    [JsonIgnore]
    public string PrixFormate
    {
        get
        {
            var cultureFr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            if (Prix == System.Math.Truncate(Prix))
                return string.Format(cultureFr, "{0:N0} €", Prix);
            return string.Format(cultureFr, "{0:N2} €", Prix);
        }
    }

    [JsonIgnore]
    public string VariationFormatee
    {
        get
        {
            var cultureFr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            if (Variation >= 0)
                return string.Format(cultureFr, "+{0:N2} %", Variation);
            return string.Format(cultureFr, "{0:N2} %", Variation);
        }
    }

    [JsonIgnore]
    public bool EstEnHausse => Variation >= 0;

    [JsonIgnore]
    public string HeureCourte
    {
        get
        {
            if (System.DateTimeOffset.TryParse(Horodatage, out System.DateTimeOffset dto))
                return dto.ToLocalTime().ToString("HH:mm:ss");
            return Horodatage;
        }
    }

    #endregion
}
