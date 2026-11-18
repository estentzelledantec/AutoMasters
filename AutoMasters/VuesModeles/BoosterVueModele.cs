using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel qui gère l'ouverture d'un booster de 3 cartes
/// </summary>
public partial class BoosterVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;
    private List<Carte> cartesTirees = new List<Carte>();
    private bool finDejaDeclenchee = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LibelleRareteActuelle))]
    private Carte? carteActuelle;

    [ObservableProperty]
    private int indexCarteActuelle = 0;

    [ObservableProperty]
    private int numeroCarteActuelle = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibiliteSegment4))]
    [NotifyPropertyChangedFor(nameof(VisibiliteSegment5))]
    private int nombreCartesBooster = 3;

    [ObservableProperty]
    private string texteProgression = "Carte 1 sur 3";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstFaceCachee))]
    [NotifyPropertyChangedFor(nameof(LibelleRareteActuelle))]
    private bool estRetournee = false;

    [ObservableProperty]
    private bool estDerniereCarte = false;

    [ObservableProperty]
    private bool estTermine = false;

    [ObservableProperty]
    private string libelleBoutonAction = "RETOURNER ▶";

    [ObservableProperty]
    private bool segment1Actif = true;

    [ObservableProperty]
    private bool segment2Actif = false;

    [ObservableProperty]
    private bool segment3Actif = false;

    [ObservableProperty]
    private bool segment4Actif = false;

    [ObservableProperty]
    private bool segment5Actif = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    [ObservableProperty]
    private string message = "";

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut utilisant l'instance Singleton de ServiceAPI.
    /// </summary>
    public BoosterVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP.
    /// </summary>
    public BoosterVueModele(ServiceAPI? serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    /// <summary>
    /// Indique si la carte est encore retournée (face cachée)
    /// </summary>
    public bool EstFaceCachee => !EstRetournee;

    public bool VisibiliteSegment4 => NombreCartesBooster >= 4;
    public bool VisibiliteSegment5 => NombreCartesBooster >= 5;

    /// <summary>
    /// Affiche une étoile devant les cartes légendaires
    /// </summary>
    public string LibelleRareteActuelle => CarteActuelle?.Rarete == "LÉGENDAIRE"
        ? "★ LÉGENDAIRE"
        : (CarteActuelle?.Rarete ?? "COMMUNE");

    /// <summary>
    /// Événement déclenché quand les 3 cartes ont été découvertes
    /// </summary>
    public event Action? SurBoosterTermine;

    #endregion

    #region Methodes

    /// <summary>
    /// Appelle l'API pour ouvrir un booster et tirer les 3 cartes
    /// </summary>
    [RelayCommand]
    public async Task DemarrerOuvertureAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        Message = "";

        try
        {
            var cartes = await serviceApi.OuvrirBoosterAsync();
            if (cartes == null || cartes.Count == 0)
                throw new Exception("Le serveur n'a délivré aucune carte pour ce booster.");

            cartesTirees = cartes;
            NombreCartesBooster = cartesTirees.Count;
            IndexCarteActuelle = 0;
            NumeroCarteActuelle = 1;
            TexteProgression = $"Carte 1 sur {NombreCartesBooster}";
            CarteActuelle = cartesTirees[0];
            EstRetournee = false;
            EstDerniereCarte = false;
            EstTermine = false;
            finDejaDeclenchee = false;
            LibelleBoutonAction = "RETOURNER ▶";

            MettreAJourSegments(1);
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Retourne la carte actuelle ou passe à la carte suivante
    /// </summary>
    [RelayCommand]
    public void ActionCarteSuivante()
    {
        if (cartesTirees.Count == 0 || EstTermine)
            return;

        if (!EstRetournee)
        {
            // Action de retournement (Flip card)
            EstRetournee = true;

            if (IndexCarteActuelle < cartesTirees.Count - 1)
            {
                LibelleBoutonAction = "CARTE SUIVANTE ▶";
            }
            else
            {
                EstDerniereCarte = true;
                LibelleBoutonAction = "TERMINER & AJOUTER AU GARAGE";
            }
        }
        else
        {
            // Passage à la carte suivante ou terminaison
            if (IndexCarteActuelle < cartesTirees.Count - 1)
            {
                IndexCarteActuelle++;
                NumeroCarteActuelle = IndexCarteActuelle + 1;
                CarteActuelle = cartesTirees[IndexCarteActuelle];
                EstRetournee = false;
                TexteProgression = $"Carte {NumeroCarteActuelle} sur {NombreCartesBooster}";
                LibelleBoutonAction = "RETOURNER ▶";

                MettreAJourSegments(NumeroCarteActuelle);
            }
            else
            {
                // Fin de l'ouverture du booster : on prévient la page du garage
                if (!finDejaDeclenchee)
                {
                    finDejaDeclenchee = true;
                    EstTermine = true;
                    try
                    {
                        if (MainThread.IsMainThread)
                        {
                            SurBoosterTermine?.Invoke();
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(() => SurBoosterTermine?.Invoke());
                        }
                    }
                    catch
                    {
                        SurBoosterTermine?.Invoke();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Met à jour la barre de progression en haut de l'écran
    /// </summary>
    private void MettreAJourSegments(int etape)
    {
        Segment1Actif = etape >= 1;
        Segment2Actif = etape >= 2;
        Segment3Actif = etape >= 3;
        Segment4Actif = etape >= 4;
        Segment5Actif = etape >= 5;
    }

    #endregion
}
