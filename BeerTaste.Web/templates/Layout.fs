module BeerTaste.Web.Templates.Layout

open Oxpecker.ViewEngine

let layout (pageTitle: string) (content: HtmlElement list) =
    html() {
        head() {
            meta(charset="utf-8")
            meta(name="viewport", content="width=device-width, initial-scale=1")
            title() { raw pageTitle }
            style() {
                raw """
                body {
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    max-width: 1200px;
                    margin: 0 auto;
                    padding: 20px;
                    background-color: #f5f5f5;
                }
                h1 {
                    color: #333;
                    border-bottom: 3px solid #f90;
                    padding-bottom: 10px;
                }
                h2 {
                    color: #555;
                    margin-top: 30px;
                }
                table {
                    width: 100%;
                    border-collapse: collapse;
                    background-color: white;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    margin-top: 20px;
                }
                th {
                    background-color: #f90;
                    color: white;
                    padding: 12px;
                    text-align: left;
                    font-weight: 600;
                }
                td {
                    padding: 10px 12px;
                    border-bottom: 1px solid #eee;
                }
                tr:hover {
                    background-color: #fafafa;
                }
                tr:nth-child(even) {
                    background-color: #f9f9f9;
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
                    background-color: #fff;
                    color: #333;
                    text-decoration: none;
                    border-radius: 5px;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    transition: all 0.3s;
                }
                .nav a:hover {
                    background-color: #f90;
                    color: white;
                    transform: translateY(-2px);
                    box-shadow: 0 4px 8px rgba(0,0,0,0.2);
                }
                """
            }
        }
        body() {
            for element in content do
                element
        }
    }
