# Параметри конфігурації
param(
    [string]$GitRepoUrl = "",
    [string]$RepoFolder = "C:\Deploy\MyApp",
    [string]$SiteName = "MyDotNetApp",
    [int]$Port = 80,
    [string]$Slic3rVersion = "1.3.0"
)

# Функція логування
function Write-Log {
    param([string]$Message, [string]$Type = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Type] $Message"
    Write-Host $logMessage
    Add-Content -Path "C:\Deploy\deployment.log" -Value $logMessage
}

# Функція перевірки успішності команди
function Test-CommandSuccess {
    param([string]$ErrorMessage)
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) {
        Write-Log $ErrorMessage "ERROR"
        throw $ErrorMessage
    }
}

try {
    Write-Log "=== Початок розгортання системи ==="
    
    # Створення робочої директорії
    New-Item -Path "C:\Deploy" -ItemType Directory -Force | Out-Null
    Write-Log "Створено робочу директорію C:\Deploy"

    # ==================== 1. Встановлення Git ====================
    Write-Log "Встановлення Git..."
    $gitInstaller = "C:\Deploy\Git-Installer.exe"
    
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Invoke-WebRequest -Uri "https://github.com/git-for-windows/git/releases/download/v2.43.0.windows.1/Git-2.43.0-64-bit.exe" `
            -OutFile $gitInstaller -UseBasicParsing
        
        Start-Process -FilePath $gitInstaller -ArgumentList "/VERYSILENT", "/NORESTART" -Wait
        
        # Оновлення PATH
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        
        Write-Log "Git встановлено успішно"
    } else {
        Write-Log "Git вже встановлено"
    }

    # ==================== 2. Клонування репозиторію ====================
    Write-Log "Клонування репозиторію з $GitRepoUrl..."
    
    if (Test-Path $RepoFolder) {
        Write-Log "Видалення старої версії репозиторію..."
        Remove-Item -Path $RepoFolder -Recurse -Force
    }
    
    git clone $GitRepoUrl $RepoFolder
    Test-CommandSuccess "Помилка при клонуванні репозиторію"
    Write-Log "Репозиторій успішно клоновано"

    # ==================== 3. Встановлення Slic3r ====================
    Write-Log "Встановлення Slic3r..."
    $slic3rFolder = "C:\Program Files\Slic3r"
    $slic3rZip = "C:\Deploy\Slic3r.zip"
    
    if (-not (Test-Path $slic3rFolder)) {
        New-Item -Path $slic3rFolder -ItemType Directory -Force | Out-Null
        
        # Завантаження Slic3r (portable версія)
        Invoke-WebRequest -Uri "https://dl.slic3r.org/windows/Slic3r-$Slic3rVersion-win64-portable.zip" `
            -OutFile $slic3rZip -UseBasicParsing
        
        Expand-Archive -Path $slic3rZip -DestinationPath $slic3rFolder -Force
        
        # Додавання до PATH
        $machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        if ($machinePath -notlike "*$slic3rFolder*") {
            [Environment]::SetEnvironmentVariable("Path", "$machinePath;$slic3rFolder", "Machine")
        }
        
        Write-Log "Slic3r встановлено в $slic3rFolder"
    } else {
        Write-Log "Slic3r вже встановлено"
    }

    # ==================== 4. Встановлення .NET SDK 10 ====================
    Write-Log "Встановлення .NET SDK 10..."
    $dotnetInstaller = "C:\Deploy\dotnet-sdk-10-installer.exe"
    
    if (-not (dotnet --list-sdks | Select-String "10.")) {
        Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/latest/dotnet-sdk-win-x64.exe" `
            -OutFile $dotnetInstaller -UseBasicParsing
        
        Start-Process -FilePath $dotnetInstaller -ArgumentList "/quiet", "/norestart" -Wait
        Test-CommandSuccess "Помилка при встановленні .NET SDK"
        
        Write-Log ".NET SDK 10 встановлено успішно"
    } else {
        Write-Log ".NET SDK 10 вже встановлено"
    }

    # ==================== 5. Встановлення ASP.NET Core Hosting Bundle ====================
    Write-Log "Встановлення ASP.NET Core Hosting Bundle..."
    $hostingBundleInstaller = "C:\Deploy\hosting-bundle-installer.exe"
    
    Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/latest/dotnet-hosting-win.exe" `
        -OutFile $hostingBundleInstaller -UseBasicParsing
    
    Start-Process -FilePath $hostingBundleInstaller -ArgumentList "/quiet", "/norestart", "OPT_NO_SHAREPOINT=1", "OPT_NO_ANCM=0" -Wait
    Test-CommandSuccess "Помилка при встановленні Hosting Bundle"
    Write-Log "ASP.NET Core Hosting Bundle встановлено"

    # ==================== 6. Встановлення та конфігурація IIS ====================
    Write-Log "Встановлення IIS..."
    
    # Встановлення IIS з необхідними компонентами
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationDevelopment -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45 -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-HealthAndDiagnostics -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-Security -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-Performance -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerManagementTools -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-DirectoryBrowsing -All -NoRestart
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45 -All -NoRestart
    
    Write-Log "IIS встановлено успішно"
    
    # Імпорт модуля IIS
    Import-Module WebAdministration
    
    # Запуск служби IIS
    Start-Service W3SVC
    Set-Service W3SVC -StartupType Automatic
    Write-Log "Служба IIS запущена"

    # ==================== 7. Білд застосунку ====================
    Write-Log "Білд застосунку..."
    
    Set-Location $RepoFolder
    
    # Відновлення пакетів
    dotnet restore
    Test-CommandSuccess "Помилка при відновленні NuGet пакетів"
    
    # Білд у Release режимі
    $publishPath = "$RepoFolder\publish"
    dotnet publish -c Release -o $publishPath
    Test-CommandSuccess "Помилка при білді застосунку"
    
    Write-Log "Застосунок успішно збілдено в $publishPath"

    # ==================== 8. Створення сайту в IIS ====================
    Write-Log "Створення сайту в IIS..."
    
    # Видалення існуючого сайту, якщо є
    if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
        Remove-Website -Name $SiteName
        Write-Log "Видалено старий сайт $SiteName"
    }
    
    # Видалення Application Pool, якщо є
    if (Get-WebAppPoolState -Name $SiteName -ErrorAction SilentlyContinue) {
        Remove-WebAppPool -Name $SiteName
        Write-Log "Видалено старий Application Pool"
    }
    
    # Створення нового Application Pool
    New-WebAppPool -Name $SiteName
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "startMode" -Value "AlwaysRunning"
    Write-Log "Створено Application Pool: $SiteName"
    
    # Створення сайту
    New-Website -Name $SiteName `
        -Port $Port `
        -PhysicalPath $publishPath `
        -ApplicationPool $SiteName `
        -Force
    
    Write-Log "Створено сайт: $SiteName на порту $Port"

    # ==================== 9. Налаштування файрволу ====================
    Write-Log "Налаштування правил файрволу..."
    
    $firewallRule = Get-NetFirewallRule -DisplayName "IIS-$SiteName" -ErrorAction SilentlyContinue
    if (-not $firewallRule) {
        New-NetFirewallRule -DisplayName "IIS-$SiteName" `
            -Direction Inbound `
            -Protocol TCP `
            -LocalPort $Port `
            -Action Allow
        Write-Log "Додано правило файрволу для порту $Port"
    }

    # ==================== 10. Перевірка статусу ====================
    Write-Log "Перевірка статусу сайту..."
    
    Start-Sleep -Seconds 5
    $website = Get-Website -Name $SiteName
    
    if ($website.State -eq "Started") {
        Write-Log "✓ Сайт успішно запущено!" "SUCCESS"
        Write-Log "✓ URL: http://localhost:$Port" "SUCCESS"
        
        # Отримання публічної IP адреси VM
        try {
            $publicIP = (Invoke-WebRequest -Uri "http://ifconfig.me/ip" -UseBasicParsing).Content.Trim()
            Write-Log "✓ Публічний URL: http://${publicIP}:$Port" "SUCCESS"
        } catch {
            Write-Log "Не вдалося отримати публічну IP адресу" "WARNING"
        }
    } else {
        Write-Log "Сайт не запущено. Статус: $($website.State)" "ERROR"
    }

    Write-Log "=== Розгортання завершено успішно ==="
    
    # Виведення підсумкової інформації
    Write-Host "`n================================" -ForegroundColor Green
    Write-Host "  РОЗГОРТАННЯ ЗАВЕРШЕНО" -ForegroundColor Green
    Write-Host "================================" -ForegroundColor Green
    Write-Host "Сайт: $SiteName" -ForegroundColor Cyan
    Write-Host "Порт: $Port" -ForegroundColor Cyan
    Write-Host "Шлях: $publishPath" -ForegroundColor Cyan
    Write-Host "Логи: C:\Deploy\deployment.log" -ForegroundColor Cyan
    Write-Host "================================`n" -ForegroundColor Green

} catch {
    Write-Log "КРИТИЧНА ПОМИЛКА: $($_.Exception.Message)" "ERROR"
    Write-Log $_.ScriptStackTrace "ERROR"
    exit 1
}

# Опціонально: перезапуск VM для застосування всіх змін
# Restart-Computer -Force