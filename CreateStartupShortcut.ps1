$WshShell = New-Object -ComObject WScript.Shell
$ShortcutPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\GoogleLensCapture.lnk"
$TargetPath = "c:\YOON\CSrepos\GoogleLensScreenCapture\bin\Release\net10.0-windows\win-x64\publish\GoogleLensCapture.exe"
$WorkingDirectory = "c:\YOON\CSrepos\GoogleLensScreenCapture\bin\Release\net10.0-windows\win-x64\publish"

if (Test-Path $TargetPath) {
    if (Test-Path $ShortcutPath) { Remove-Item $ShortcutPath }
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $TargetPath
    $Shortcut.WorkingDirectory = $WorkingDirectory
    $Shortcut.Save()
    Write-Host "시작 프로그램 폴더에 바로가기($ShortcutPath)가 업데이트되었습니다."
} else {
    Write-Error "실행 파일($TargetPath)을 찾을 수 없습니다."
}
