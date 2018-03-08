namespace tomenglertde.ResXManager.Translators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.Diagnostics.Contracts;

	using JetBrains.Annotations;

	using Microsoft.TerminologyService;

	using tomenglertde.ResXManager.Infrastructure;

	using TomsToolbox.Desktop;

	[Export(typeof(ITranslator))]
	public class MSTerminologyTranslator : TranslatorBase
	{
		[NotNull]
		private static readonly Uri _uri = new Uri("https://www.microsoft.com/en-us/language/default.aspx");

		public MSTerminologyTranslator()
			: base("MSTerm", "Microsoft Terminology", _uri, GetCredentials())
		{
		}

		[NotNull]
		[ItemNotNull]
		private static IList<ICredentialItem> GetCredentials()
		{
			Contract.Ensures(Contract.Result<IList<ICredentialItem>>() != null);

			return new ICredentialItem[] { };
		}

		public override void Translate(ITranslationSession translationSession)
		{
			using (var client = new TerminologyClient())
			{
				var translationSources = new TranslationSources() {TranslationSource.UiStrings};
				foreach (var item in translationSession.Items)
				{
					if (translationSession.IsCanceled)
						break;

					Contract.Assume(item != null);

					var targetCulture = item.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage;

					try
					{
						var response = client.GetTranslations(item.Source, translationSession.SourceLanguage.IetfLanguageTag,
							targetCulture.IetfLanguageTag, SearchStringComparison.CaseInsensitive, SearchOperator.Contains,
							translationSources, false, 5, false, null);
						if (response != null)
						{
							translationSession.Dispatcher.BeginInvoke(() =>
							{
								Contract.Requires(item != null);
								Contract.Requires(response != null);

								foreach (var match in response)
								{
									Contract.Assume(match != null);
									foreach (var trans in match.Translations)
										item.Results.Add(new TranslationMatch(this, trans.TranslatedText, match.ConfidenceLevel / 100.0));
								}
							});
						}
					}
					catch (Exception ex)
					{
						translationSession.AddMessage(DisplayName + ": " + ex.Message);
						break;
					}
				}
			}
		}
	}
}