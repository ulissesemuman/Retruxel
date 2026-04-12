# Script para deletar releases duplicadas do GitHub
# Uso: .\delete-releases.ps1

$repo = "ulissesemuman/Retruxel"
$tag = "v0.7.0-alpha"

Write-Host "Buscando releases para tag $tag..." -ForegroundColor Cyan

# Listar todas as releases
$releases = gh api repos/$repo/releases --jq ".[] | select(.tag_name == `"$tag`") | {id: .id, name: .name, draft: .draft}"

if ($releases) {
    $releaseList = $releases | ConvertFrom-Json
    
    if ($releaseList -is [Array]) {
        Write-Host "Encontradas $($releaseList.Count) releases:" -ForegroundColor Yellow
        foreach ($release in $releaseList) {
            $status = if ($release.draft) { "DRAFT" } else { "PUBLISHED" }
            Write-Host "  - ID: $($release.id) | Name: $($release.name) | Status: $status"
        }
    } else {
        Write-Host "Encontrada 1 release:" -ForegroundColor Yellow
        $status = if ($releaseList.draft) { "DRAFT" } else { "PUBLISHED" }
        Write-Host "  - ID: $($releaseList.id) | Name: $($releaseList.name) | Status: $status"
        $releaseList = @($releaseList)
    }
    
    Write-Host "`nDeletando releases..." -ForegroundColor Red
    foreach ($release in $releaseList) {
        Write-Host "Deletando release ID $($release.id)..." -ForegroundColor Yellow
        gh api -X DELETE repos/$repo/releases/$($release.id)
        Write-Host "  ✓ Deletada" -ForegroundColor Green
    }
    
    Write-Host "`n✓ Todas as releases da tag $tag foram deletadas!" -ForegroundColor Green
} else {
    Write-Host "Nenhuma release encontrada para a tag $tag" -ForegroundColor Yellow
}
