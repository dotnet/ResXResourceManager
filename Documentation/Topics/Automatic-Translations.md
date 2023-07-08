## Automated Translations
To speed up the localization process you can make use of automated translations.

### Usage
Switch to the `Translate` tab to use this feature. 

If the translation service needs credentials, enter them in the translation engine configuration. Follow the link to the translation service to learn about the service specifics.
If you don't want to use a service, uncheck the check box next to the name. 

After selecting source and target language, all entires that have no translation in the target language will be displayed and the translation runs in the background. When the translation is complete, the results can be reviewed or edited before accepting the result.

All results can be reviewed by opening the combo box in the target column. The results are ordered by the quality of the translation that the service reports. If you had already a translation of the same term in your project, the quality is the number of occurrences of this translation. Existing translations will have a higher quality rank than translations by a service.

![Translations](Automatic%20Translations_Translations.png)

### Azure Open AI

#### Pros
- Much more context aware
- Higher quality translations
- Ability to use comments to guide the model
- Better understanding of placeholders (such as {0}), seems to put them in better locations

#### Cons
- Much slower
- More expensive (depending on your token cost at Azure)

#### Configuration
- Add a new Azure OpenAI resource using the portal or CLI.
- Deploy the "text-davinci-003" (completion based) or "gpt-3.5-turbo" (chat based) model.
- Copy the API key, URL to the endpoint and name of the deployment and model into the settings of the translator

#### Addtional Settings
- You can add a custom prompt to your request to improve the translation quality or behavior, e.g. "preserve the html tags in the results"
- You can include the comments in your resources in the prompt, to guid the model with some additional hints about the context

