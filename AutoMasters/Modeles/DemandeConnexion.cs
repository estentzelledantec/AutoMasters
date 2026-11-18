using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe envoyée en JSON lors de la connexion à l'API (POST /auth/login)
/// </summary>
public class DemandeConnexion
{
    #region Attributs

    private string identifiant = "";
    private string motDePasse = "";

    #endregion

    #region Constructeurs

    public DemandeConnexion()
    {
    }

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

    [JsonProperty("password")]
    public string MotDePasse
    {
        get => motDePasse;
        set => motDePasse = value ?? "";
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Vérifie que l'identifiant et le mot de passe sont bien remplis
    /// </summary>
    public bool VerifierChamps()
    {
        return !string.IsNullOrWhiteSpace(Identifiant) && !string.IsNullOrWhiteSpace(MotDePasse);
    }

    #endregion
}
