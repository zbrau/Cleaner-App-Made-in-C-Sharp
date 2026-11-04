param(
    [string]$Thumbprint = "BC9A6982D023B2971796F297E49A5B825D6A56F4",
    [string]$BinDir = "f:\PROYECTOS\C+\AppCleanerPro\bin\Debug\net10.0-windows"
)

$cert = Get-ChildItem "Cert:\CurrentUser\My\$Thumbprint" -ErrorAction SilentlyContinue
if (-not $cert) {
    Write-Warning "Certificado no encontrado en el almacén personal."
    exit 0
}

$files = @(
    (Join-Path $BinDir "AppCleanerPro.exe"),
    (Join-Path $BinDir "AppCleanerPro.dll")
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $sig = Set-AuthenticodeSignature -FilePath $file -Certificate $cert -TimestampServer "http://timestamp.digicert.com" -HashAlgorithm SHA256 -ErrorAction SilentlyContinue
        if ($sig.Status -eq "Valid") {
            Write-Host "       Firmado OK: $(Split-Path $file -Leaf)"
        } else {
            Write-Host "       Aviso al firmar $(Split-Path $file -Leaf): $($sig.StatusMessage)"
        }
    }
}
