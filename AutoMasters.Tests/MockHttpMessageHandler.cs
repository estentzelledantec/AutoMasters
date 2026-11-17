using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace AutoMasters.Tests;

/// <summary>
/// Gestionnaire HTTP simulé et déterministe pour tester ServiceAPI
/// en mode 100% hors-ligne sans dépendance réseau ou sandbox.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    #region Attributs

    private readonly List<HttpRequestMessage> _requetesEnregistrees = new();
    private readonly Dictionary<(HttpMethod Methode, string Chemin), Func<HttpRequestMessage, HttpResponseMessage>> _routes = new();
    private Func<HttpRequestMessage, HttpResponseMessage>? _gestionnaireParDefaut;
    private bool _simulerPanneReseau;
    private bool _simulerDelaiDepasse;
    private HttpStatusCode? _forcerCodeStatut;

    #endregion

    #region Constructeurs

    public MockHttpMessageHandler()
    {
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> gestionnaireParDefaut)
    {
        _gestionnaireParDefaut = gestionnaireParDefaut;
    }

    #endregion

    #region Getter/Setter

    public IReadOnlyList<HttpRequestMessage> RequetesEnregistrees => _requetesEnregistrees;

    public HttpRequestMessage? DerniereRequete => _requetesEnregistrees.LastOrDefault();

    public bool SimulerPanneReseau
    {
        get => _simulerPanneReseau;
        set => _simulerPanneReseau = value;
    }

    public bool SimulerDelaiDepasse
    {
        get => _simulerDelaiDepasse;
        set => _simulerDelaiDepasse = value;
    }

    public HttpStatusCode? ForcerCodeStatut
    {
        get => _forcerCodeStatut;
        set => _forcerCodeStatut = value;
    }

    #endregion

    #region Methodes

    public MockHttpMessageHandler EnregistrerRoute(HttpMethod methode, string chemin, HttpStatusCode statut, object? corpsJson = null)
    {
        string cheminNormalise = NormaliserChemin(chemin);
        _routes[(methode, cheminNormalise)] = req => CreerReponseJson(statut, corpsJson);
        return this;
    }

    public MockHttpMessageHandler EnregistrerRoute(HttpMethod methode, string chemin, Func<HttpRequestMessage, HttpResponseMessage> gestionnaire)
    {
        string cheminNormalise = NormaliserChemin(chemin);
        _routes[(methode, cheminNormalise)] = gestionnaire;
        return this;
    }

    public MockHttpMessageHandler QuandGet(string chemin, HttpStatusCode statut, object? corpsJson = null)
        => EnregistrerRoute(HttpMethod.Get, chemin, statut, corpsJson);

    public MockHttpMessageHandler QuandPost(string chemin, HttpStatusCode statut, object? corpsJson = null)
        => EnregistrerRoute(HttpMethod.Post, chemin, statut, corpsJson);

    public MockHttpMessageHandler QuandPatch(string chemin, HttpStatusCode statut, object? corpsJson = null)
        => EnregistrerRoute(HttpMethod.Patch, chemin, statut, corpsJson);

    public MockHttpMessageHandler QuandDelete(string chemin, HttpStatusCode statut, object? corpsJson = null)
        => EnregistrerRoute(HttpMethod.Delete, chemin, statut, corpsJson);

    public void Reinitialiser()
    {
        _requetesEnregistrees.Clear();
        _routes.Clear();
        _gestionnaireParDefaut = null;
        _simulerPanneReseau = false;
        _simulerDelaiDepasse = false;
        _forcerCodeStatut = null;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requetesEnregistrees.Add(request);

        if (_simulerPanneReseau)
        {
            throw new HttpRequestException("Échec de connexion réseau simulé : serveur inaccessible.");
        }

        if (_simulerDelaiDepasse)
        {
            throw new TaskCanceledException("Délai d'attente de la requête dépassé (Timeout).");
        }

        if (_forcerCodeStatut.HasValue)
        {
            return Task.FromResult(CreerReponseJson(_forcerCodeStatut.Value, new
            {
                code = "ERREUR_FORCEE",
                message = "Code d'erreur HTTP forcé par le simulateur."
            }));
        }

        string cheminRequete = NormaliserChemin(request.RequestUri?.AbsolutePath ?? string.Empty);

        // Correspondance exacte (Méthode + Chemin)
        if (_routes.TryGetValue((request.Method, cheminRequete), out var gestionnaire))
        {
            return Task.FromResult(gestionnaire(request));
        }

        // Correspondance partielle / suffixe
        var routeCorrespondante = _routes.FirstOrDefault(r =>
            r.Key.Methode == request.Method &&
            (cheminRequete.EndsWith(r.Key.Chemin, StringComparison.OrdinalIgnoreCase) ||
             cheminRequete.StartsWith(r.Key.Chemin, StringComparison.OrdinalIgnoreCase)));

        if (routeCorrespondante.Value != null)
        {
            return Task.FromResult(routeCorrespondante.Value(request));
        }

        if (_gestionnaireParDefaut != null)
        {
            return Task.FromResult(_gestionnaireParDefaut(request));
        }

        // Réponse 404 par défaut si non intercepté
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                JsonConvert.SerializeObject(new { code = "NOT_FOUND", message = $"Endpoint simulé non trouvé : {request.Method} {cheminRequete}" }),
                Encoding.UTF8,
                "application/json")
        });
    }

    private static string NormaliserChemin(string chemin)
    {
        if (string.IsNullOrWhiteSpace(chemin))
            return "/";
        return "/" + chemin.Trim().TrimStart('/');
    }

    public static HttpResponseMessage CreerReponseJson(HttpStatusCode statut, object? corps)
    {
        var reponse = new HttpResponseMessage(statut);
        if (corps != null)
        {
            string json = corps is string corpsChaine ? corpsChaine : JsonConvert.SerializeObject(corps);
            reponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        else
        {
            reponse.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        }
        return reponse;
    }

    public static HttpResponseMessage CreerReponseBrute(HttpStatusCode statut, string corpsBrut, string typeMime = "text/html")
    {
        return new HttpResponseMessage(statut)
        {
            Content = new StringContent(corpsBrut, Encoding.UTF8, typeMime)
        };
    }

    #endregion
}
