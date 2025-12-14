# Publish the application
dotnet publish CatCam.Web/CatCam.Web.csproj \
    --configuration Release \
    --runtime linux-arm64 \
    --self-contained true \
    -p:PublishSingleFile=false
scp -r ./CatCam.Web/bin/Release/net10.0/linux-arm64/publish/* pi@192.168.178.57:/home/pi/catcam
