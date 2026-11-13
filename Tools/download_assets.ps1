param([string]$OutDir = "Assets/Models/PolyPizza")
$ErrorActionPreference = "Continue"
New-Item -ItemType Directory -Force $OutDir | Out-Null
$terms = [ordered]@{
  "BotolPlastik"  = "plastic bottle"; "GelasPlastik" = "plastic cup"; "KantongPlastik" = "plastic bag"
  "KulitPisang"   = "banana peel";    "Apel" = "apple core";          "Daun" = "leaf"
  "Kertas"        = "crumpled paper"; "Kardus" = "cardboard box";     "Koran" = "newspaper"
  "Kaleng"        = "soda can";       "KalengSarden" = "tin can";     "TutupBotol" = "bottle cap"
  "TongSampah"    = "recycle bin";    "Pohon" = "tree";               "Semak" = "bush"
  "Bangku"        = "park bench"
}
$credits = @()
foreach ($k in $terms.Keys) {
  $q = [uri]::EscapeDataString($terms[$k])
  try {
    $s = (Invoke-WebRequest -UseBasicParsing "https://poly.pizza/search/$q").Content
    $m = [regex]::Matches($s, 'href="/m/([^"]+)"[^>]*?>.*?alt="([^"]*)"') | Select -First 1
    if (!$m) { Write-Host "$k : tidak ditemukan"; continue }
    $id = $m.Groups[1].Value; $title = $m.Groups[2].Value
    $p = (Invoke-WebRequest -UseBasicParsing "https://poly.pizza/m/$id").Content
    $glb = ([regex]::Match($p, 'https://static\.poly\.pizza/[0-9a-f\-]+\.glb')).Value
    $lic = ([regex]::Match($p, '"Licence":"([^"]+)"')).Groups[1].Value
    $author = ([regex]::Match($p, '"Creator":\{[^}]*?"Username":"([^"]+)"')).Groups[1].Value
    Invoke-WebRequest -UseBasicParsing $glb -OutFile "$OutDir/$k.glb"
    Write-Host "$k <- '$title' ($lic) OK"
    $credits += "$k.glb : '$title' by $author - $lic - https://poly.pizza/m/$id"
  } catch { Write-Host "$k : gagal - $_" }
}
$credits | Set-Content "$OutDir/CREDITS.txt"
