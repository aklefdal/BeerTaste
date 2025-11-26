module BeerTaste.Web.Templates.Layout

open Oxpecker.ViewEngine
open BeerTaste.Web.Localization

let topNavigation (beerTasteGuid: string) (t: Translations) (currentLanguage: Language) =
    div (class' = "nav") {
        a (href = $"/{beerTasteGuid}") { raw $"🏠 {t.Home}" }
        a (href = $"/{beerTasteGuid}/beers") { raw t.Beers }
        a (href = $"/{beerTasteGuid}/tasters") { raw t.Tasters }
        a (href = $"/{beerTasteGuid}/scores") { raw t.Scores }
        a (href = $"/{beerTasteGuid}/results") { raw t.Results }

        div (style = "float: right;") {
            // Visually hidden label for accessibility (screen readers)
            label (for' = "language-selector", class' = "visually-hidden") { raw t.LanguageLabel }

            select (id = "language-selector", name = "language", style = "padding: 5px; font-size: 1.2em;", class' = "noto-color-emoji-regular") {
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

            title () { raw pageTitle }

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
                }
                h2 {
                    color: #333333;
                    margin-top: 30px;
                }
                table {
                    width: 100%;
                    border-collapse: collapse;
                    background-color: white;
                    border: 1px solid #cccccc;
                    margin-top: 20px;
                }
                th {
                    background-color: #000000;
                    color: white;
                    padding: 12px;
                    text-align: left;
                    font-weight: 600;
                    border-bottom: 2px solid #000000;
                }
                td {
                    padding: 10px 12px;
                    border-bottom: 1px solid #dddddd;
                    color: #000000;
                }
                tr:hover {
                    background-color: #f0f0f0;
                }
                tr:nth-child(even) {
                    background-color: #fafafa;
                }
                .value {
                    text-align: right;
                    font-weight: 500;
                }
                .supportingvalue {
                    text-align: right;
                }
                .nav {
                    margin: 20px 0;
                }
                .nav a {
                    display: inline-block;
                    margin-right: 15px;
                    padding: 10px 20px;
                    background-color: #ffffff;
                    color: #000000;
                    text-decoration: none;
                    border: 1px solid #000000;
                    border-radius: 3px;
                    transition: all 0.2s;
                }
                .nav a:hover {
                    background-color: #000000;
                    color: #ffffff;
                }
                .results-list {
                    margin: 20px 0;
                }
                .results-list a {
                    display: block;
                    margin-bottom: 10px;
                    padding: 15px 20px;
                    background-color: #ffffff;
                    color: #000000;
                    text-decoration: none;
                    border: 1px solid #000000;
                    border-radius: 3px;
                    transition: all 0.2s;
                }
                .results-list a:hover {
                    background-color: #000000;
                    color: #ffffff;
                }
                .results-list .icon {
                    display: inline-block;
                    width: 30px;
                    margin-right: 15px;
                    font-size: 1.2em;
                    text-align: center;
                }
                .results-nav {
                    margin: 20px 0;
                    display: flex;
                    flex-wrap: wrap;
                    gap: 10px;
                }
                .results-nav-button {
                    display: inline-block;
                    padding: 10px 15px;
                    background-color: #ffffff;
                    color: #000000;
                    text-decoration: none;
                    border: 1px solid #000000;
                    border-radius: 3px;
                    transition: all 0.2s;
                    flex: 1 1 auto;
                    min-width: 50px;
                    text-align: center;
                }
                .results-nav-button:hover {
                    background-color: #000000;
                    color: #ffffff;
                }
                .results-nav-button.current {
                    background-color: #000000;
                    color: #ffffff;
                    font-weight: 600;
                    cursor: default;
                }
                .results-nav-button .icon {
                    display: inline-block;
                    margin-right: 8px;
                    font-size: 1.1em;
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
                """
            }
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
