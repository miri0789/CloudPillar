
$instanceCount = 2
$devicePrefix = "loadingTest"
$secretKey = "1"

$destinationPath = "./loadTest/1GBTest2.tmp"
$source = "10MB.tmp"

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$changeSpecId= "ChangeSpecId_$timestamp"


Invoke-Register -instanceCount $instanceCount -devicePrefix $devicePrefix -secretKey $secretKey

#wait until all devices are provisioning with x509 and run the next script with params above:

Invoke-AddChangeSpec -source $source -destinationPath $destinationPath -instanceCount $instanceCount -devicePrefix $devicePrefix -secretKey $secretKey -changeSpecId $changeSpecId

Invoke-SendFileDownload -instanceCount $instanceCount -devicePrefix $devicePrefix -secretKey $secretKey -fileName $source  -actionIndex 0 -changeSpecId $changeSpecId


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

function Invoke-AddChangeSpec {
    param (
        [string]$source,
        [string]$destinationPath,
        [int]$instanceCount, 
        [string]$devicePrefix,
        [string]$secretKey,
        [string]$changeSpecId
    )

    #$sign = "NIo9gB/9TLrCsaSKNyrkAAkY+8MrowxiB0Mk+2SNRQh3xSd/ZIHkYEWYW13gluoANrAZfhDHm46uo/aUJd42C47XifpCzZZrKOaQVZjcrL4QIwergb2NLc6Fi5hmZ5IT"
    $baseUrl = 'http://localhost:5192/ChangeSpec/AssignChangeSpec'
 
    $headers = @{
        "accept" = "*/*"
        "Content-Type" = "application/json"
    }
   
    $body = @{
        id = $changeSpecId
        patch = @{
            transitPackage = @(
                @{
                    source = "$source"
                    destinationPath = $destinationPath
                    action = "SingularDownload"
                    description = "overload test"
                    #sign = $sign
                }
            )
        }
    } | ConvertTo-Json -Depth 3

    $devices=''

    for ($i = 0; $i -lt $instanceCount; $i++) {  
   
        if ($i -ne $instanceCount - 1) {
                $devices +="${devicePrefix}-$i,"
        }
        else{
            $devices+="${devicePrefix}-$i"
        }
    }

    $apiUrl = $baseUrl+"?devices="+$devices+"&changeSpecKey=changeSpec"
    
    Write-Host $apiUrl

    try {
        $response = Invoke-RestMethod -Method Post -Uri $apiUrl -Headers $headers -Body $body
        Write-Host "Response:", $response

    } catch {
        $errorMessage = $_.Exception.Message
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Write-Host "[$timestamp] Error: $errorMessage. Request URL: $apiUrl"
    }
}

function Invoke-SendFileDownload{
        param (
            [int]$instanceCount = 4, 
            [string]$devicePrefix = "nm",
            [string]$secretKey = "1",
            [string]$fileName ,
            [int]$chunkSize = 65536,
            [string]$completedRanges = "",
            [int]$startPosition = 0 ,
            [int]$endPosition,
            [int]$actionIndex = 0 ,
            [string]$changeSpecId 
    
        )

        $baseUrl = 'http://localhost:5192/LoadTesting/SendFileDownloadAsync' 
 
        $headers = @{
            "accept" = "*/*"
            "Content-Type" = "application/json"
        }       

        $body = @{
            MessageType = 0
            FileName = $fileName
            ChunkSize = $chunkSize
            CompletedRanges = $completedRanges
            StartPosition = $startPosition
            EndPosition = if ($endPosition -eq 0) { $null } else { $endPosition }
            ActionIndex = $actionIndex
            ChangeSpecId = $changeSpecId
   
        } | ConvertTo-Json -Depth 3
         Write-Host $body

        for ($i = 0; $i -lt $instanceCount; $i++) {   
           $apiUrl = "{0}?deviceId={1}" -f $baseUrl, "${devicePrefix}-$i"

            Write-Host $apiUrl

             try {
                $response = Invoke-RestMethod -Method Post -Uri $apiUrl -Headers $headers -Body $body
                Write-Host "Response:", $response

            } catch {
                $errorMessage = $_.Exception.Message
                $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
                Write-Host "[$timestamp] Error: $errorMessage. Request URL: $apiUrl"

            }
        }
    }
