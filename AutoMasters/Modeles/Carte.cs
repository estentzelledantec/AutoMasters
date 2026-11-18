using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe qui représente une carte de voiture dans le catalogue
/// </summary>
public class Carte
{
    #region Attributs

    /// <summary>
    /// 1 jeton dans l'application équivaut à 1 000 €
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

    public Carte()
    {
    }

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

    // [JsonProperty] permet de faire le lien entre le nom du champ en anglais dans le JSON de l'API et notre propriété C#
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
    /// Lecture du sous-objet "quote" renvoyé par l'API pour récupérer le prix, la variation et la date
    /// </summary>
    [JsonProperty("quote")]
    private Newtonsoft.Json.Linq.JObject? CotationApi
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

    public decimal VariationCours
    {
        get => variationCours;
        set => variationCours = value;
    }

    public string DeviseCours
    {
        get => deviseCours;
        set => deviseCours = string.IsNullOrWhiteSpace(value) ? "EUR" : value;
    }

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

    [JsonProperty("circulation")]
    private int? CirculationApi
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
    /// Affiche la marque, le modèle et la variante
    /// </summary>
    public string NomComplet => string.IsNullOrWhiteSpace(Variante)
        ? $"{Marque} {Modele}"
        : $"{Marque} {Modele} ({Variante})";

    public string PuissanceFormatee => $"{Puissance} ch";

    public string VitesseMaxFormatee => $"{VitesseMax} km/h";

    public string ZeroACentFormate => $"{ZeroACent:F1} s";

    /// <summary>
    /// Affiche le prix en euros (ex : 165 000 €)
    /// </summary>
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
    /// Affiche le pourcentage de variation avec une flèche (▲ ou ▼)
    /// </summary>
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

    public bool EstVariationPositive => VariationCours >= 0;

    public string HorodatageCoursFormate => string.IsNullOrWhiteSpace(HorodatageCours)
        ? $"Dernière cotation serveur : {System.DateTime.Now:dd/MM/yyyy à HH:mm:ss}"
        : $"Dernière cotation serveur : {HorodatageCours}";

    /// <summary>
    /// Calcule la valeur en jetons (Prix / 1 000)
    /// </summary>
    public decimal ValeurJetons => System.Math.Round(PrixOfficiel / 1000m, 2);

    public string ValeurJetonsFormatee => FormaterMontantJetons(ValeurJetons);

    public string PrixCompletFormate => $"{PrixReelFormate} · {ValeurJetonsFormatee}";

    /// <summary>
    /// Formate un montant en jetons (sans virgule si c'est un nombre rond)
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
    /// Renvoie le code court de rareté (UR, SR, R, C) pour le badge sur la carte
    /// </summary>
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

    public string SousTitreCarte => !string.IsNullOrWhiteSpace(Variante)
        ? $"{Variante} · {Annee}"
        : $"Modèle {Annee} · {Puissance} ch";

    public override string ToString()
    {
        return $"{Marque} {Modele} ({Annee}) - {Rarete}";
    }

    #endregion
}

/// <summary>
/// Classe qui représente un point de la courbe de marché d'une voiture
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

    public double HauteurBarre
    {
        get => hauteurBarre;
        set => hauteurBarre = value;
    }

    #endregion

    #region Methodes

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

    public bool EstEnHausse => Variation >= 0;

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
