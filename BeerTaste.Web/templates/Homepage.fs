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

            title () { raw t.BeerTastingResults }

            style () {
                raw
                    """
                body {
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    max-width: 1200px;
                    margin: 0 auto;
                    padding: 20px;
                    background-color: #ffffff;
                    color: #000000;
                }
                h1 {
                    color: #000000;
                    border-bottom: 2px solid #000000;
                    padding-bottom: 10px;
                    text-align: center;
                }
                .language-selector-container {
                    text-align: right;
                    margin-bottom: 20px;
                }
                .visually-hidden {
                    position: absolute;
                    width: 1px;
                    height: 1px;
                    padding: 0;
                    margin: -1px;
                    overflow: hidden;
                    clip: rect(0, 0, 0, 0);
                    white-space: nowrap;
                    border: 0;
                }
                .noto-color-emoji-regular {
                    font-family: "Noto Color Emoji", sans-serif;
                    font-weight: 400;
                    font-style: normal;
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
