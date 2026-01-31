@echo off
echo ================================
echo  Gotify Client - Build Script
echo ================================
echo.

echo Nettoyage des anciens builds...
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul

echo.
echo Compilation en mode Release...
dotnet build -c Release

if %errorlevel% equ 0 (
    echo.
    echo ================================
    echo Build reussi !
    echo ================================
    echo.
    echo L'executable se trouve dans:
    echo bin\Release\net6.0-windows\GotifyClient.exe
    echo.
    
    choice /C YN /M "Voulez-vous publier une version autonome (sans .NET requis)"
    if %errorlevel% equ 1 (
        echo.
        echo Publication d'une version autonome...
        dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
        
        if %errorlevel% equ 0 (
            echo.
            echo ================================
            echo Publication reussie !
            echo ================================
            echo.
            echo Version autonome dans:
            echo bin\Release\net6.0-windows\win-x64\publish\GotifyClient.exe
        )
    )
) else (
    echo.
    echo ================================
    echo Erreur de compilation !
    echo ================================
    echo.
    echo Verifiez que .NET 6.0 SDK est installe.
)

echo.
pause
