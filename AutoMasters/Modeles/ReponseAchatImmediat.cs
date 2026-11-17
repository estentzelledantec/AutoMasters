namespace AutoMasters.Modeles;

/// <summary>
/// Alias DTO pour ReponseAchat spécifique aux achats immédiats.
/// </summary>
public class ReponseAchatImmediat : ReponseAchat
{
    #region Attributs

    #endregion

    #region Constructeurs

    public ReponseAchatImmediat()
    {
    }

    public ReponseAchatImmediat(string statut, string identifiantTransaction, decimal? soldeRestant = null)
        : base(statut, identifiantTransaction, soldeRestant)
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
