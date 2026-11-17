using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant la requête d'authentification envoyée à l'API (POST /auth/login).
/// Accepte les clés JSON "username" et "email".
/// </summary>
public class DemandeConnexion
{
    #region Attributs

    private string identifiant = "";
    private string motDePasse = "";

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public DemandeConnexion()
    {
    }

    /// <summary>
    /// Constructeur avec identifiant et mot de passe.
    /// </summary>
    public DemandeConnexion(string identifiant, string motDePasse)
    {
        this.identifiant = identifiant;
        this.motDePasse = motDePasse;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("username")]
    public string Identifiant
    {
        get => identifiant;
        set => identifiant = value ?? "";
    }

    /// <summary>
    /// Alias JSON pour "email" afin de tolérer les deux conventions.
    /// </summary>
    [JsonProperty("email")]
    private string? EmailSetter
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(identifiant))
                identifiant = value;
        }
    }

    [JsonProperty("password")]
    public string MotDePasse
    {
        get => motDePasse;
        set => motDePasse = value ?? "";
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Valide que les deux champs obligatoires sont renseignés.
    /// </summary>
    [JsonIgnore]
    public bool EstValide => !string.IsNullOrWhiteSpace(Identifiant) && !string.IsNullOrWhiteSpace(MotDePasse);

    #endregion
}
