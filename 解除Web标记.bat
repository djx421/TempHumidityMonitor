@echo off
chcp 65001 >nul
title 解除文件Web标记
echo ============================================
echo  解除所有文件的 Web 标记 (Zone.Identifier)
echo ============================================
echo.
echo 正在处理项目文件...
echo.

set "PROJECT_DIR=%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$total = 0; $unblocked = 0; " ^
  "Get-ChildItem -LiteralPath '%PROJECT_DIR%' -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object { " ^
  "  $total++; " ^
  "  try { " ^
  "    $stream = $_.FullName + ':Zone.Identifier'; " ^
  "    if (Test-Path -LiteralPath $stream) { " ^
  "      Remove-Item -LiteralPath $stream -Force -ErrorAction SilentlyContinue; " ^
  "      $unblocked++; " ^
  "      Write-Host ('  已解除: ' + $_.Name) -ForegroundColor Yellow; " ^
  "    } " ^
  "  } catch {} " ^
  "}; " ^
  "Write-Host ''; " ^
  "Write-Host ('完成！共扫描 ' + $total + ' 个文件，解除了 ' + $unblocked + ' 个文件的 Web 标记') -ForegroundColor Green; " ^
  "if ($unblocked -eq 0) { Write-Host '没有发现带有 Web 标记的文件' -ForegroundColor Cyan }"

echo.
echo 现在可以重新打开 Visual Studio 编译项目了。
echo.
pause
