@echo off
set VERSION=13
set OUTPUT_DIR=c:\code\packages

dotnet pack ./src/Qwack.Options --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Dates --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR%  /p:DebugType=Embedded
dotnet pack ./src/Qwack.Core --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Math --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Math.Interpolation --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Options --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Paths --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Providers --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Transport --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Futures --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Models --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Utils --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Random --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Storage --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded
dotnet pack ./src/Qwack.Serialization --configuration Release /p:Version=%VERSION% --output %OUTPUT_DIR% /p:DebugType=Embedded

