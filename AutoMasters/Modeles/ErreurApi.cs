using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe permettant de lire les messages d'erreur renvoyés en JSON par l'API
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

    public ErreurApi()
    {
    }

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
    /// Récupère le message d'erreur quel que soit le nom du champ dans le JSON
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

    [JsonProperty("error")]
    public string? Error
    {
        get => error;
        set => error = value;
    }

    [JsonProperty("detail")]
    public string? Detail
    {
        get => detail;
        set => detail = value;
    }

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

    public override string ToString()
    {
        string? msg = Message;
        if (string.IsNullOrWhiteSpace(Code))
            return msg ?? string.Empty;

        return string.IsNullOrWhiteSpace(msg) ? $"[{Code}]" : $"[{Code}] {msg}";
    }

    #endregion
}
