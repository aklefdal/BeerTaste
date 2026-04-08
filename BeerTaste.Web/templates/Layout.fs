module BeerTaste.Web.Templates.Layout

open Oxpecker.ViewEngine
open BeerTaste.Web.Localization

// Generate the Firebase SDK scripts and configuration
let firebaseScripts (firebaseConfig: FirebaseConfig option) (t: Translations) =
    match firebaseConfig with
    | Some config when isFirebaseConfigured firebaseConfig ->
        Fragment() {
            // Firebase SDK scripts
            script (src = "https://www.gstatic.com/firebasejs/10.7.1/firebase-app-compat.js") { () }
            script (src = "https://www.gstatic.com/firebasejs/10.7.1/firebase-auth-compat.js") { () }

            // Firebase initialization and auth handling
            script () {
                raw
                    $"""
                    const firebaseConfig = {{
                        apiKey: "{config.ApiKey}",
                        authDomain: "{config.AuthDomain}",
                        projectId: "{config.ProjectId}"
                    }};
                    firebase.initializeApp(firebaseConfig);

                    firebase.auth().onAuthStateChanged(function(user) {{
                        const loginWidget = document.getElementById('login-widget');
                        if (user) {{
                            loginWidget.innerHTML = '<span class="user-name">' + (user.displayName || user.email) + '</span> <a href="#" id="logout-link">{t.Logout}</a>';
                            document.getElementById('logout-link').addEventListener('click', function(e) {{
                                e.preventDefault();
                                firebase.auth().signOut();
                            }});
                        }} else {{
                            loginWidget.innerHTML = '<a href="#" id="login-link">{t.Login}</a>';
                            document.getElementById('login-link').addEventListener('click', function(e) {{
                                e.preventDefault();
                                const provider = new firebase.auth.GoogleAuthProvider();
                                firebase.auth().signInWithPopup(provider);
                            }});
                        }}
                    }});
                    """
            }
        }
    | _ -> Fragment() { () }

// Login widget placeholder (filled by JavaScript when Firebase is configured)
let loginWidget (firebaseConfig: FirebaseConfig option) (t: Translations) : HtmlElement =
    if isFirebaseConfigured firebaseConfig then
        span (id = "login-widget", class' = "login-widget") { raw t.Login }
    else
        span (id = "login-widget", style = "display: none;") { () }

let topNavigation
    (beerTasteGuid: string)
    (t: Translations)
    (currentLanguage: Language)
    (firebaseConfig: FirebaseConfig option)
    =
    div (class' = "nav") {
        a (class' = "nav-button", href = $"/{beerTasteGuid}") {
            span (class' = "icon") { raw "🏠" }
            raw t.Home
        }

        a (class' = "nav-button", href = $"/{beerTasteGuid}/beers") { raw t.Beers }
        a (class' = "nav-button", href = $"/{beerTasteGuid}/tasters") { raw t.Tasters }
        a (class' = "nav-button", href = $"/{beerTasteGuid}/scores") { raw t.Scores }
        a (class' = "nav-button", href = $"/{beerTasteGuid}/results") { raw t.Results }

        div (style = "float: right; display: flex; align-items: center; gap: 15px;") {
            loginWidget firebaseConfig t

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

let layout
    (pageTitle: string)
    (beerTasteGuid: string)
    (language: Language)
    (firebaseConfig: FirebaseConfig option)
    (content: HtmlElement list)
    =
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
            topNavigation beerTasteGuid t language firebaseConfig

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

            // Firebase scripts (if configured)
            firebaseScripts firebaseConfig t
        }
    }
