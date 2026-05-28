# Script para testar Portuguese localization
$gameDir = "c:\Users\Alexandre\Documents\GitHub\StarDrive\game"
$exePath = Join-Path $gameDir "StarDrive.exe"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "StarDrive - Teste Portugues (BR)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if (-not (Test-Path $exePath)) {
    Write-Host "ERRO: Executavel nao encontrado!" -ForegroundColor Red
    Write-Host "Procurando em: $exePath"
    exit 1
}

Write-Host "`nExecutavel encontrado: $exePath" -ForegroundColor Green
Write-Host "`nInstruções de teste:" -ForegroundColor Yellow
Write-Host "  1. Ao iniciar o jogo, vá para OPTIONS/SETTINGS" -ForegroundColor White
Write-Host "  2. Procure pela opção LANGUAGE e selecione 'Português (BR)'" -ForegroundColor White
Write-Host "  3. Confirme as mudanças" -ForegroundColor White
Write-Host "  4. Verifique se a interface muda para português" -ForegroundColor White
Write-Host "  5. Comece um novo jogo e valide os nomes das raças em português" -ForegroundColor White

Write-Host "`nIniciando jogo..." -ForegroundColor Cyan
Write-Host ""

# Iniciar o jogo
& $exePath
