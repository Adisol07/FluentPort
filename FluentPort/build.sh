#!/bin/bash

# MacOS ARM64
echo Building FluentPort Desktop app for osx-arm64..
dotnet restore
dotnet build -c Release -r osx-arm64
dotnet publish -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
mkdir -p bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/MacOS
cp bin/Release/net8.0/osx-arm64/publish/FluentPort bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/MacOS/FluentPort
cp bin/Release/net8.0/osx-arm64/publish/FluentPort.pdb bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/MacOS/FluentPort.pdb
cp bin/Release/net8.0/osx-arm64/publish/FluentPort.SDK.pdb bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/MacOS/FluentPort.SDK.pdb
cp bin/Release/net8.0/osx-arm64/publish/libAvaloniaNative.dylib bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/MacOS/libAvaloniaNative.dylib
cp bin/Release/net8.0/osx-arm64/publish/libHarfBuzzSharp.dylib bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/MacOS/libHarfBuzzSharp.dylib
cp bin/Release/net8.0/osx-arm64/publish/libSkiaSharp.dylib bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/MacOS/libSkiaSharp.dylib
cat Info.plist >bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/Info.plist
mkdir -p bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/Resources
cp ../GlobalAssets/fluentport-logo_1024x1024.icns bin/Release/net8.0/osx-arm64/publish/FluentPort.app/Contents/Resources/AppIcon.icns
sudo touch bin/Release/net8.0/osx-arm64/publish/FluentPort.app
killall Finder
echo osx-arm64 build done

# Windows x86
echo Building FluentPort Desktop app for win-x64
dotnet restore
dotnet build -c Release -r win-x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
echo win-x64 build done

# Windows ARM64
echo Building FluentPort Desktop app for win-arm64
dotnet restore
dotnet build -c Release -r win-arm64
dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
echo win-arm64 build done

# Move to publish
mkdir publish
mkdir publish/osx-arm64
mkdir publish/win-x64
mkdir publish/win-arm64
cp -r bin/Release/net8.0/osx-arm64/publish/FluentPort.app publish/osx-arm64/FluentPort.app
cp bin/Release/net8.0/win-x64/publish/FluentPort.exe publish/win-x64/FluentPort.exe
cp bin/Release/net8.0/win-arm64/publish/FluentPort.exe publish/win-arm64/FluentPort.exe
