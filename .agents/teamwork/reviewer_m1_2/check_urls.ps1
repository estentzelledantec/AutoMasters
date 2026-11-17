$filePath = "c:\Users\stent\source\repos\AutoMasters\cars_database.json"
$cars = Get-Content -Raw -Encoding UTF8 $filePath | ConvertFrom-Json

Write-Host "Checking $($cars.Count) image URLs for accessibility..."

Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)")

$allOk = $true

foreach ($car in $cars) {
    $url = $car.imageUrl
    try {
        # Send HEAD or GET request
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Get, $url)
        # Only fetch headers if possible, or complete request
        $response = $client.SendAsync($request, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        
        $status = [int]$response.StatusCode
        $contentType = $response.Content.Headers.ContentType.MediaType
        
        if ($response.IsSuccessStatusCode) {
            Write-Host "OK ($status, $contentType): ID $($car.id) - $($car.brand) $($car.model)"
        } else {
            Write-Warning "FAIL ($status): ID $($car.id) - $($car.brand) $($car.model) - URL: $url"
            $allOk = $false
        }
    } catch {
        Write-Warning "ERROR: ID $($car.id) - $($car.brand) $($car.model) - Exception: $_"
        $allOk = $false
    }
}

if ($allOk) {
    Write-Host "`nAll $($cars.Count) image URLs are live and publicly accessible!"
} else {
    Write-Host "`nSome URLs failed!"
}
