$ErrorActionPreference = "Stop"

$filePath = "c:\Users\stent\source\repos\AutoMasters\cars_database.json"
Write-Host "Verifying: $filePath"

if (-not (Test-Path $filePath)) {
    Write-Error "File not found: $filePath"
    exit 1
}

$rawJson = Get-Content -Raw -Encoding UTF8 $filePath
$cars = $rawJson | ConvertFrom-Json

Write-Host "Total vehicles count: $($cars.Count)"

$expectedFields = @(
    "id",
    "brand",
    "model",
    "variant",
    "year",
    "powerHp",
    "rarity",
    "copiesInCirculation",
    "officialPrice",
    "imageUrl",
    "description",
    "topSpeedKmH",
    "zeroToHundred"
)

$rarityTiers = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$validRarities = @("COMMUNE", "PEU COMMUNE", "RARE", "ÉPIQUE", "LÉGENDAIRE", "MYTHIQUE")

$missingFieldCount = 0
$invalidRarityCount = 0
$invalidUrlCount = 0
$invalidNumericCount = 0

$index = 0
foreach ($car in $cars) {
    $index++
    $carProps = $car.PSObject.Properties.Name
    
    # Check 13 fields
    foreach ($field in $expectedFields) {
        if (-not ($carProps -contains $field)) {
            Write-Warning "Car index $index (ID: $($car.id)) missing field: $field"
            $missingFieldCount++
        } else {
            $val = $car.$field
            if ($null -eq $val -or ($val -is [string] -and [string]::IsNullOrWhiteSpace($val))) {
                Write-Warning "Car index $index (ID: $($car.id)) has null/empty field: $field"
                $missingFieldCount++
            }
        }
    }
    
    # Check rarity
    if ($car.rarity) {
        [void]$rarityTiers.Add($car.rarity)
        if ($validRarities -notcontains $car.rarity -and $validRarities -notcontains ($car.rarity.Replace("É", "E").Replace("È", "E"))) {
            Write-Warning "Car index $index (ID: $($car.id)) has invalid rarity: $($car.rarity)"
            $invalidRarityCount++
        }
    }

    # Check numeric bounds
    if ($car.officialPrice -le 0) {
        Write-Warning "Car index $index (ID: $($car.id)) non-positive officialPrice: $($car.officialPrice)"
        $invalidNumericCount++
    }
    if ($car.powerHp -le 0) {
        Write-Warning "Car index $index (ID: $($car.id)) non-positive powerHp: $($car.powerHp)"
        $invalidNumericCount++
    }
    if ($car.copiesInCirculation -le 0) {
        Write-Warning "Car index $index (ID: $($car.id)) non-positive copiesInCirculation: $($car.copiesInCirculation)"
        $invalidNumericCount++
    }
    if ($car.year -lt 1900 -or $car.year -gt 2030) {
        Write-Warning "Car index $index (ID: $($car.id)) unexpected year: $($car.year)"
        $invalidNumericCount++
    }
    if ($car.topSpeedKmH -le 0) {
        Write-Warning "Car index $index (ID: $($car.id)) non-positive topSpeedKmH: $($car.topSpeedKmH)"
        $invalidNumericCount++
    }
    if ($car.zeroToHundred -le 0) {
        Write-Warning "Car index $index (ID: $($car.id)) non-positive zeroToHundred: $($car.zeroToHundred)"
        $invalidNumericCount++
    }

    # Check URL structure
    $uriResult = $null
    $isUri = [System.Uri]::TryCreate($car.imageUrl, [System.UriKind]::Absolute, [ref]$uriResult)
    if (-not $isUri -or ($uriResult.Scheme -ne "http" -and $uriResult.Scheme -ne "https")) {
        Write-Warning "Car index $index (ID: $($car.id)) invalid URL format: $($car.imageUrl)"
        $invalidUrlCount++
    }
}

Write-Host "`n--- Summary of Verification ---"
Write-Host "Total Cars: $($cars.Count)"
Write-Host "Missing/Empty Fields: $missingFieldCount"
Write-Host "Invalid Rarities: $invalidRarityCount"
Write-Host "Invalid Numeric Values: $invalidNumericCount"
Write-Host "Invalid URLs format: $invalidUrlCount"
Write-Host "Rarity Tiers Represented: $($rarityTiers -join ', ')"
Write-Host "Rarity Count: $($rarityTiers.Count)"
