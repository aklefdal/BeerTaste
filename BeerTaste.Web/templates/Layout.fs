module BeerTaste.Web.Templates.Layout

open Oxpecker.ViewEngine

let layout (pageTitle: string) (content: HtmlElement list) =
    html () {
        head () {
            meta (charset = "utf-8")
            meta (name = "viewport", content = "width=device-width, initial-scale=1")
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
                """
            }
        }

        body () {
            for element in content do
                element
        }
    }
