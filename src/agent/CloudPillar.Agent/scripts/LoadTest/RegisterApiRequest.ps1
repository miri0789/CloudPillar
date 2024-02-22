Invoke-Register -instanceCount 2 -devicePrefix "loadingTest" -secretKey "1"

function Invoke-Register {
    param (
        [int]$instanceCount, 
        [string]$devicePrefix,
        [string]$secretKey
    )

    $baseUrl = 'http://localhost:5192/RegisterByCertificate/Register' 
 
    $headers = @{
        "accept" = "*/*"
        "Content-Type" = "application/json"
    }       

    for ($i = 0; $i -lt $instanceCount; $i++) {   
        $apiUrl = "{0}?deviceId={1}&secretKey={2}" -f $baseUrl, "${devicePrefix}-$i", $secretKey

        Write-Host $apiUrl
            try {
            $response = Invoke-RestMethod -Method Post -Uri $apiUrl -Headers $headers
            Write-Host "Response:", $response

        } catch {
            $errorMessage = $_.Exception.Message
            $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            Write-Host "[$timestamp] Error: $errorMessage. Request URL: $apiUrl"
        }
    }
}