module BeerTaste.Web.Templates.Layout

open Oxpecker.ViewEngine
open BeerTaste.Web.Localization

let topNavigation (beerTasteGuid: string) (t: Translations) (currentLanguage: Language) =
    div (class' = "nav") {
        a (class' = "nav-button", href = $"/{beerTasteGuid}") {
            span (class' = "icon") { raw "🏠" }
            raw t.Home
        }

        a (class' = "nav-button", href = $"/{beerTasteGuid}/beers") { raw t.Beers }
        a (class' = "nav-button", href = $"/{beerTasteGuid}/tasters") { raw t.Tasters }
        a (class' = "nav-button", href = $"/{beerTasteGuid}/scores") { raw t.Scores }
        a (class' = "nav-button", href = $"/{beerTasteGuid}/results") { raw t.Results }

        div (style = "float: right;") {
            // Visually hidden label for accessibility (screen readers)
            label (for' = "language-selector", class' = "visually-hidden") { raw t.LanguageLabel }

            select (
                id = "language-selector",
                name = "language",
                style = "padding: 5px; font-size: 1.2em;",
                class' = "noto-color-emoji-regular"
            ) {
                option (value = "en", selected = (currentLanguage = English)) { raw "🇬🇧" }
                option (value = "no", selected = (currentLanguage = Norwegian)) { raw "🇳🇴" }
            }
        }
    }

let layout (pageTitle: string) (beerTasteGuid: string) (language: Language) (content: HtmlElement list) =
    let t = getTranslations language

    html () {
        head () {
            meta (charset = "utf-8")
            meta (name = "viewport", content = "width=device-width, initial-scale=1")
            link (rel = "preconnect", href = "https://fonts.googleapis.com")
            link (rel = "preconnect", href = "https://fonts.gstatic.com", crossorigin = "true")
            link (rel = "stylesheet", href = "https://fonts.googleapis.com/css2?family=Noto+Color+Emoji&display=swap")
            link (rel = "stylesheet", href = "/main.css")

            title () { raw pageTitle }
        }

        body () {
            topNavigation beerTasteGuid t language

            for element in content do
                element

            // JavaScript for language switching
            script () {
                raw
                    """
                    document.getElementById('language-selector').addEventListener('change', function() {
                        const selectedLanguage = this.value;
                        const expiryDate = new Date();
                        expiryDate.setFullYear(expiryDate.getFullYear() + 1);
                        document.cookie = 'beertaste-language=' + selectedLanguage + '; expires=' + expiryDate.toUTCString() + '; path=/; SameSite=Lax';
                        location.reload();
                    });
                    """
            }
        }
    }
