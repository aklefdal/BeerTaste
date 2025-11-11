# Beer Tasting Results Slide Navigator
# Use arrow keys to navigate: ← → or type slide number (1-6)

$slides = @(
    "01-best-beers.md",
    "02-controversial-beers.md", 
    "03-deviant-tasters.md",
    "04-similar-tasters.md",
    "05-strong-beers.md",
    "06-cheap-alcohol.md"
)

$currentSlide = 0
$slidesPath = ".\slides\"

function Show-Slide {
    param($index)
    
    Clear-Host
    $filePath = Join-Path $slidesPath $slides[$index]
    
    if (Test-Path $filePath) {
        Get-Content $filePath | Write-Host
        Write-Host ""
        Write-Host "Navigation: ← → (arrows) | 1-6 (slide number) | q (quit)" -ForegroundColor Yellow
        Write-Host "Current slide: $($index + 1) of $($slides.Count)" -ForegroundColor Green
    } else {
        Write-Host "Slide file not found: $filePath" -ForegroundColor Red
    }
}

function Start-Presentation {
    Write-Host "Starting Beer Tasting Results Presentation..." -ForegroundColor Cyan
    Write-Host "Make sure you're in the beertaste directory!" -ForegroundColor Yellow
    Start-Sleep 2
    
    while ($true) {
        Show-Slide $currentSlide
        
        $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        
        switch ($key.VirtualKeyCode) {
            37 { # Left arrow
                if ($currentSlide -gt 0) { $currentSlide-- }
            }
            39 { # Right arrow  
                if ($currentSlide -lt $slides.Count - 1) { $currentSlide++ }
            }
            49 { $currentSlide = 0 } # 1
            50 { $currentSlide = 1 } # 2
            51 { $currentSlide = 2 } # 3
            52 { $currentSlide = 3 } # 4
            53 { $currentSlide = 4 } # 5
            54 { $currentSlide = 5 } # 6
            81 { # Q
                Clear-Host
                Write-Host "Thanks for viewing the beer tasting results!" -ForegroundColor Green
                return
            }
            27 { # ESC
                Clear-Host
                Write-Host "Thanks for viewing the beer tasting results!" -ForegroundColor Green
                return
            }
        }
    }
}

# Start the presentation
Start-Presentation