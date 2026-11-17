namespace AutoMasters.Modeles;

/// <summary>
/// Alias DTO pour DemandeAchat spécifique aux achats immédiats.
/// </summary>
public class DemandeAchatImmediat : DemandeAchat
{
    #region Attributs

    #endregion

    #region Constructeurs

    public DemandeAchatImmediat()
    {
    }

    public DemandeAchatImmediat(string identifiantOffre, string? cleIdempotence = null)
        : base(identifiantOffre, cleIdempotence)
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
