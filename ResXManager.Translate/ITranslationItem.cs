namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;

    public interface ITranslationItem
    {
        string Source
        {
            get;
        }

        IList<ITranslationMatch> Results
        {
            get;
        }
    }
}