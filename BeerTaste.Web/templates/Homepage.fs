module BeerTaste.Web.Templates.Homepage

open Oxpecker.ViewEngine
open BeerTaste.Web.Localization

let view (language: Language) =
    let t = getTranslations language

    html () {
        head () {
            meta (charset = "utf-8")
            meta (name = "viewport", content = "width=device-width, initial-scale=1")
            link (rel = "preconnect", href = "https://fonts.googleapis.com")
            link (rel = "preconnect", href = "https://fonts.gstatic.com", crossorigin = "true")
            link (rel = "stylesheet", href = "https://fonts.googleapis.com/css2?family=Noto+Color+Emoji&display=swap")
            link (rel = "stylesheet", href = "/main.css")

            title () { raw t.BeerTastingResults }

            style () {
                raw
                    """
                h1 {
                    text-align: center;
                }
                .language-selector-container {
                    text-align: right;
                    margin-bottom: 20px;
                }
                .welcome-content {
                    text-align: center;
                    padding: 60px 20px;
                }
                .welcome-icon {
                    font-family: 'Noto Color Emoji', sans-serif;
                    font-size: 4em;
                    margin-bottom: 20px;
                }
                .welcome-text {
                    font-size: 1.2em;
                    color: #333333;
                    line-height: 1.6;
                    max-width: 600px;
                    margin: 0 auto;
                }
                """
            }
        }

        body () {
            div (class' = "language-selector-container") {
                // Visually hidden label for accessibility (screen readers)
                label (for' = "language-selector", class' = "visually-hidden") { raw t.LanguageLabel }

                select (
                    id = "language-selector",
                    name = "language",
                    style = "padding: 5px; font-size: 1.2em;",
                    class' = "noto-color-emoji-regular"
                ) {
                    option (value = "en", selected = (language = English)) { raw "🇬🇧" }
                    option (value = "no", selected = (language = Norwegian)) { raw "🇳🇴" }
                }
            }

            h1 () { raw t.BeerTastingResults }

            div (class' = "welcome-content") {
                div (class' = "welcome-icon") { raw "🍺" }

                div (class' = "welcome-text") {
                    if language = Norwegian then
                        p () { raw "Velkommen til resultatene fra ølsmakingen!" }
                        p () { raw "For å se resultatene fra et arrangement, naviger til:" }
                        p () { raw "<strong>/{{beerTasteGuid}}</strong>" }
                    else
                        p () { raw "Welcome to the beer tasting results!" }
                        p () { raw "To view results from an event, navigate to:" }
                        p () { raw "<strong>/{{beerTasteGuid}}</strong>" }
                }
            }

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
