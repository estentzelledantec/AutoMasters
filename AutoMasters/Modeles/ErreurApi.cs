using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant le format normalisé des réponses d'erreur renvoyées par l'API Group 2.
/// </summary>
public class ErreurApi
{
    #region Attributs

    private string? code;
    private string? message;
    private string? error;
    private string? detail;
    private string? title;
    private string? correlationId;
    private List<string>? erreurs;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public ErreurApi()
    {
    }

    /// <summary>
    /// Constructeur avec message et code d'erreur.
    /// </summary>
    public ErreurApi(string message, string? code = null, string? correlationId = null)
    {
        this.message = message;
        this.code = code;
        this.correlationId = correlationId;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("code")]
    public string? Code
    {
        get => code;
        set => code = value;
    }

    /// <summary>
    /// Message d'erreur principal. Résout avec priorité la clé standard "message",
    /// puis les clés alternatives "detail" (RFC 7807), "error" (standard REST) ou "title" (RFC 7807).
    /// Renvoie null si aucun message textuel valide n'est présent afin de ne jamais masquer
    /// les messages de fallback dans les services appelants.
    /// </summary>
    [JsonProperty("message")]
    public string? Message
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(message))
                return message;
            if (!string.IsNullOrWhiteSpace(detail))
                return detail;
            if (!string.IsNullOrWhiteSpace(error))
                return error;
            if (!string.IsNullOrWhiteSpace(title))
                return title;
            return null;
        }
        set => message = value;
    }

    /// <summary>
    /// Clé d'erreur alternative (ex: OAuth, microservices Flask/Express).
    /// </summary>
    [JsonProperty("error")]
    public string? Error
    {
        get => error;
        set => error = value;
    }

    /// <summary>
    /// Clé d'erreur alternative Problem Details RFC 7807 fournissant l'explication détaillée.
    /// </summary>
    [JsonProperty("detail")]
    public string? Detail
    {
        get => detail;
        set => detail = value;
    }

    /// <summary>
    /// Clé d'erreur alternative Problem Details RFC 7807 fournissant le résumé court ou titre.
    /// </summary>
    [JsonProperty("title")]
    public string? Title
    {
        get => title;
        set => title = value;
    }

    [JsonProperty("correlationId")]
    public string? CorrelationId
    {
        get => correlationId;
        set => correlationId = value;
    }

    [JsonProperty("errors")]
    public List<string>? Erreurs
    {
        get => erreurs;
        set => erreurs = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Format lisible pour affichage dans une alerte ou un bandeau d'erreur utilisateur.
    /// </summary>
    public override string ToString()
    {
        string? msg = Message;
        if (string.IsNullOrWhiteSpace(Code))
            return msg ?? string.Empty;

        return string.IsNullOrWhiteSpace(msg) ? $"[{Code}]" : $"[{Code}] {msg}";
    }

    #endregion
}
