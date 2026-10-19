# syntax=docker/dockerfile:1
# Bitta fayldan ikki image: API va Worker. Ikkalasi bir xil yechimdan, bir xil build bosqichida yigʻiladi.
#   docker build --target api    -t english-school-api .
#   docker build --target worker -t english-school-worker .
# Worker image'i bir martalik migratsiyani ham bajaradi: docker run english-school-worker migrate

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Restore qatlami faqat loyiha fayllari yoki paket versiyalari oʻzgarganda qayta quriladi.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/BuildingBlocks.Abstractions/BuildingBlocks.Abstractions.csproj src/BuildingBlocks.Abstractions/
COPY src/BuildingBlocks/BuildingBlocks.csproj src/BuildingBlocks/
COPY src/Host.Api/Host.Api.csproj src/Host.Api/
COPY src/Host.Worker/Host.Worker.csproj src/Host.Worker/
COPY src/Modules/AiGateway.Contracts/Modules.AiGateway.Contracts.csproj src/Modules/AiGateway.Contracts/
COPY src/Modules/AiGateway/Modules.AiGateway.csproj src/Modules/AiGateway/
COPY src/Modules/Analytics.Contracts/Modules.Analytics.Contracts.csproj src/Modules/Analytics.Contracts/
COPY src/Modules/Analytics/Modules.Analytics.csproj src/Modules/Analytics/
COPY src/Modules/Curriculum.Contracts/Modules.Curriculum.Contracts.csproj src/Modules/Curriculum.Contracts/
COPY src/Modules/Curriculum/Modules.Curriculum.csproj src/Modules/Curriculum/
COPY src/Modules/Discipline.Contracts/Modules.Discipline.Contracts.csproj src/Modules/Discipline.Contracts/
COPY src/Modules/Discipline/Modules.Discipline.csproj src/Modules/Discipline/
COPY src/Modules/Identity.Contracts/Modules.Identity.Contracts.csproj src/Modules/Identity.Contracts/
COPY src/Modules/Identity/Modules.Identity.csproj src/Modules/Identity/
COPY src/Modules/Practice.Contracts/Modules.Practice.Contracts.csproj src/Modules/Practice.Contracts/
COPY src/Modules/Practice/Modules.Practice.csproj src/Modules/Practice/
COPY src/Modules/Retention.Contracts/Modules.Retention.Contracts.csproj src/Modules/Retention.Contracts/
COPY src/Modules/Retention/Modules.Retention.csproj src/Modules/Retention/
COPY src/Modules/Speech.Contracts/Modules.Speech.Contracts.csproj src/Modules/Speech.Contracts/
COPY src/Modules/Speech/Modules.Speech.csproj src/Modules/Speech/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/Host.Api/Host.Api.csproj \
    && dotnet restore src/Host.Worker/Host.Worker.csproj

# .editorconfig analizator qoidalarini beradi (masalan, EF migratsiyalari generatsiya qilingan kod): usiz
# TreatWarningsAsErrors bilan build yiqiladi. Restore'ga taʼsir qilmaydi, shuning uchun undan keyin.
COPY .editorconfig ./
COPY src/ src/
# -m:1: kichik build serverlarida xotira yetishi uchun (yechim parallel yigʻilganda 2 GB'dan oshishi mumkin).
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Host.Api/Host.Api.csproj -c Release -o /app/api --no-restore -m:1 -p:UseAppHost=false \
    && dotnet publish src/Host.Worker/Host.Worker.csproj -c Release -o /app/worker --no-restore -m:1 -p:UseAppHost=false

# Ishga tushirish asosi: tzdata (vaqt mintaqalari tekshiriladi) va ICU bor; jarayon root emas (APP_UID, 1654).
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
ENV DOTNET_NOLOGO=1 \
    Speech__StoragePath=/data/audio
# Audio ombori volume sifatida ulanadi; papka egasi ilova foydalanuvchisi, aks holda yangi volume'ga yozib boʻlmaydi.
RUN mkdir -p /data/audio && chown "$APP_UID" /data/audio
WORKDIR /app
USER $APP_UID

FROM runtime AS api
COPY --from=build /app/api ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "EnglishSchool.Host.Api.dll"]

FROM runtime AS worker
COPY --from=build /app/worker ./
ENTRYPOINT ["dotnet", "EnglishSchool.Host.Worker.dll"]
