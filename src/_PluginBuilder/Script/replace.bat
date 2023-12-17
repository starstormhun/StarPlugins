@echo off
set pattern=%~2
set pattern=%pattern:$=\$%
powershell -Command "(gc %~1) -replace '%pattern%', '%~3' | Out-File -encoding ASCII %~1"