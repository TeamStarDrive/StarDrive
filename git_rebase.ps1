Set-Location -Path "c:\Users\Alexandre\Documents\GitHub\StarDrive"
Write-Host "=== Staging changes ===" -ForegroundColor Cyan
git add .
git status

Write-Host "`n=== Committing changes ===" -ForegroundColor Cyan
git commit -m "Add Portuguese language support and race translations"

Write-Host "`n=== Fetching remote ===" -ForegroundColor Cyan
git fetch origin

Write-Host "`n=== Rebasing on origin/main ===" -ForegroundColor Cyan
git rebase origin/main

Write-Host "`n=== Rebase completed ===" -ForegroundColor Green
git log --oneline -5
