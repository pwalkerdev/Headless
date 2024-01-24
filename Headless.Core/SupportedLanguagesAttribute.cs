namespace Headless.Core
{
    public class SupportedLanguagesAttribute : System.Attribute
    {
        public string[] SupportedLanguages { get; set; }

        public SupportedLanguagesAttribute(params string[] supportedLanguages)
        {
            SupportedLanguages = supportedLanguages;
        }
    }
}
