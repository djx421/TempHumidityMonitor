@echo off
chcp 65001 >nul
title 温湿度监控 - 启动中...

set "PROJECT_DIR=%~dp0"

:: 静默清除 Web 标记
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Get-ChildItem -LiteralPath '%PROJECT_DIR%' -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object { " ^
  "  try { " ^
  "    $stream = $_.FullName + ':Zone.Identifier'; " ^
  "    if (Test-Path -LiteralPath $stream) { Remove-Item -LiteralPath $stream -Force -ErrorAction SilentlyContinue } " ^
  "  } catch {} " ^
  "}" >nul 2>&1

:: 查找并打开 .sln 文件
for %%f in ("%PROJECT_DIR%*.sln") do (
    start "" "%%f"
    exit
)

echo 未找到 .sln 文件，请确认项目路径正确。
pause
