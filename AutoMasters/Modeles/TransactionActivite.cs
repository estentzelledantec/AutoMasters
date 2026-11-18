using System;
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// Classe qui représente une ligne dans l'historique des activités du joueur
/// </summary>
public class TransactionActivite
{
    #region Attributs

    private string identifiant = "";
    private string utilisateur = "";
    private string typeOperation = "TRANSACTION";
    private string titre = "";
    private string description = "";
    private decimal montant;
    private DateTime dateOperation = DateTime.Now;
    private string icone = "⚡";

    #endregion

    #region Constructeurs

    public TransactionActivite()
    {
    }

    public TransactionActivite(string identifiant, string typeOperation, string titre, string description, decimal montant, string icone = "⚡", DateTime? dateOperation = null, string utilisateur = "")
    {
        this.identifiant = identifiant;
        this.typeOperation = typeOperation;
        this.titre = titre;
        this.description = description;
        this.montant = montant;
        this.icone = icone;
        this.dateOperation = dateOperation ?? DateTime.Now;
        this.utilisateur = utilisateur ?? "";
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("id")]
    public string Identifiant
    {
        get => identifiant;
        set => identifiant = value ?? "";
    }

    [JsonProperty("user")]
    public string Utilisateur
    {
        get => utilisateur;
        set => utilisateur = value ?? "";
    }

    [JsonProperty("type")]
    public string TypeOperation
    {
        get => typeOperation;
        set => typeOperation = value ?? "TRANSACTION";
    }

    [JsonProperty("title")]
    public string Titre
    {
        get => titre;
        set => titre = value ?? "";
    }

    [JsonProperty("description")]
    public string Description
    {
        get => description;
        set => description = value ?? "";
    }

    [JsonProperty("amount")]
    public decimal Montant
    {
        get => montant;
        set => montant = value;
    }

    [JsonProperty("createdAt")]
    public DateTime DateOperation
    {
        get => dateOperation;
        set => dateOperation = value;
    }

    public string Icone
    {
        get => icone;
        set => icone = value ?? "⚡";
    }

    #endregion

    #region Methodes

    public string SousTitre => description;

    public bool EstPositif => montant >= 0;

    public string MontantFormate => montant != 0
        ? $"{(montant > 0 ? "+" : "")}{montant:N0} €"
        : "Opération confirmée";

    public string MontantJetonsFormate
    {
        get
        {
            if (montant == 0)
                return "Opération confirmée";

            decimal jetons = System.Math.Round(montant / Carte.TauxConversionJeton, 2);
            string signe = montant > 0 ? "+" : "";
            return $"{signe}{Carte.FormaterMontantJetons(jetons)}";
        }
    }

    public string DateFormatee => dateOperation.ToString("dd/MM/yyyy HH:mm");

    public override string ToString()
    {
        return $"[{TypeOperation}] {Titre} - {MontantJetonsFormate}";
    }

    #endregion
}
