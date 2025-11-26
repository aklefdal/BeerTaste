module BeerTaste.Web.Templates.Similar

open Oxpecker.ViewEngine
open BeerTaste.Common.Results
open BeerTaste.Web.Templates.Layout
open BeerTaste.Web.Templates.Navigation
open BeerTaste.Web.Localization

let view (beerTasteGuid: string) (language: Language) (results: TasterPairResult list) =
    let t = getTranslations language

    // Extract unique taster names from results
    let tasterNames =
        results
        |> List.collect (fun r -> [ r.Name1; r.Name2 ])
        |> List.distinct
        |> List.sort

    layout t.MostSimilarTasters beerTasteGuid language [
        h1 () { raw t.MostSimilarTasters }

        renderNavigation beerTasteGuid t ResultPage.Similar

        // Dropdown for highlighting taster
        div (class' = "highlight-controls") {
            label (for' = "taster-highlight") { raw $"{t.Highlight}: " }

            select (id = "taster-highlight") {
                option (value = "") { raw t.None }

                for taster in tasterNames do
                    option (value = taster) { raw taster }
            }
        }

        table (id = "similar-tasters-table") {
            thead () {
                tr () {
                    th () { raw t.Rank }
                    th () { raw t.Taster1 }
                    th () { raw t.Taster2 }
                    th (class' = "value") { raw t.Correlation }
                }
            }

            tbody () {
                for i, result in results |> List.indexed do
                    tr () {
                        td () { raw (string (i + 1)) }
                        td (class' = "taster1") { raw result.Name1 }
                        td (class' = "taster2") { raw result.Name2 }
                        td (class' = "value") { raw $"%.2f{result.Value}" }
                    }
            }
        }

        // JavaScript for highlighting
        script () {
            raw
                """
                document.getElementById('taster-highlight').addEventListener('change', function() {
                    const selectedTaster = this.value;
                    const rows = document.querySelectorAll('#similar-tasters-table tbody tr');
                    
                    rows.forEach(function(row) {
                        const taster1Cell = row.querySelector('.taster1');
                        const taster2Cell = row.querySelector('.taster2');
                        const taster1 = taster1Cell ? taster1Cell.textContent : '';
                        const taster2 = taster2Cell ? taster2Cell.textContent : '';
                        
                        if (selectedTaster && (taster1 === selectedTaster || taster2 === selectedTaster)) {
                            row.classList.add('highlighted');
                        } else {
                            row.classList.remove('highlighted');
                        }
                    });
                });
                """
        }

        // CSS for highlighting
        style () {
            raw
                """
                .highlight-controls {
                    margin: 20px 0;
                    padding: 10px;
                    background-color: #f5f5f5;
                    border: 1px solid #ddd;
                    border-radius: 3px;
                }
                .highlight-controls label {
                    font-weight: 600;
                    margin-right: 10px;
                }
                .highlight-controls select {
                    padding: 5px 10px;
                    border: 1px solid #000;
                    border-radius: 3px;
                    background-color: white;
                    font-size: 14px;
                }
                tr.highlighted {
                    background-color: #ffff99 !important;
                }
                tr.highlighted:hover {
                    background-color: #ffff66 !important;
                }
                """
        }
    ]
