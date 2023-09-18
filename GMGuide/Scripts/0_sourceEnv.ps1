param(
    [string]$EnvName = "dev"
)

$files = @("../../Envs/all.env", "../../Envs/$EnvName.env")
foreach ($file in $files) {
    Get-Content $file | ForEach-Object {
        if ($_ -match '^(.+?)=(.+)$') {
            $name,$value = $Matches[1..2]
            Set-Item -Path "Env:$name" -Value $value
        }
    }
}

Write-Host "`nThe Environemnt Variables are set for $EnvName `n"


