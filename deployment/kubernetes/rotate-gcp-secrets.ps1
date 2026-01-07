param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectId
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function New-RandomBase64([int]$bytes) {
    $buffer = New-Object byte[] $bytes
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($buffer)
    }
    finally {
        $rng.Dispose()
    }
    [Convert]::ToBase64String($buffer)
}

function New-RandomPassword([int]$length = 32) {
    $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*_-+'
    $buffer = New-Object byte[] $length
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($buffer)
    }
    finally {
        $rng.Dispose()
    }

    $sb = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $length; $i++) {
        $sb.Append($chars[$buffer[$i] % $chars.Length]) | Out-Null
    }
    $sb.ToString()
}

function Ensure-SecretExists([string]$name) {
    gcloud secrets describe $name --project $ProjectId 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        gcloud secrets create $name --replication-policy='automatic' --project $ProjectId | Out-Null
    }
}

function Add-SecretVersion([string]$name, [string]$value) {
    Ensure-SecretExists $name

    $tmp = New-TemporaryFile
    try {
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($tmp.FullName, $value, $utf8NoBom)
        gcloud secrets versions add $name --data-file=$($tmp.FullName) --project $ProjectId | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "gcloud failed to add a version for secret '$name' (exit code $LASTEXITCODE)."
        }
    }
    finally {
        Remove-Item -Force $tmp.FullName
    }
}

$mysqlRootPassword = New-RandomPassword 32
$jwtSecret = New-RandomBase64 64
$passwordPepper = New-RandomBase64 32
$rabbitmqPassword = New-RandomPassword 32

$mongoConn = "mongodb://root:$mysqlRootPassword@mongodb:27017/?authSource=admin"

$authConn = "Server=auth-db;Port=3306;Database=GymHive;User=root;Password=$mysqlRootPassword;SslMode=None;AllowPublicKeyRetrieval=True;"
$gymConn = "Server=gym-db;Port=3306;Database=GymHiveGyms;User=root;Password=$mysqlRootPassword;SslMode=None;AllowPublicKeyRetrieval=True;"
$notifConn = "Server=notifications-db;Port=3306;Database=GymHiveNotifications;User=root;Password=$mysqlRootPassword;SslMode=None;AllowPublicKeyRetrieval=True;"
$workoutConn = "Server=workout-db;Port=3306;Database=GymHiveWorkoutLogs;User=root;Password=$mysqlRootPassword;SslMode=None;AllowPublicKeyRetrieval=True;"

$secrets = @(
    @{ Name = 'mysql-root-password'; Value = $mysqlRootPassword },
    @{ Name = 'jwt-secret'; Value = $jwtSecret },
    @{ Name = 'password-pepper'; Value = $passwordPepper },
    @{ Name = 'rabbitmq-password'; Value = $rabbitmqPassword },
    @{ Name = 'mongodb-connection-string'; Value = $mongoConn },
    @{ Name = 'auth-db-connection-string'; Value = $authConn },
    @{ Name = 'gym-db-connection-string'; Value = $gymConn },
    @{ Name = 'notifications-db-connection-string'; Value = $notifConn },
    @{ Name = 'workout-db-connection-string'; Value = $workoutConn }
)

foreach ($s in $secrets) {
    Add-SecretVersion $s.Name $s.Value
    Write-Host "Updated secret version: $($s.Name)"
}

Write-Host 'All secrets updated (values not displayed).'
