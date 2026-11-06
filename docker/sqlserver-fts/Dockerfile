# SQL Server 2022 sa komponentom Full-Text Search.
# Zvanični image mcr.microsoft.com/mssql/server NEMA Full-Text Search, pa ga doinstaliramo
# iz Microsoft-ovog apt repozitorijuma (paket mssql-server-fts).
FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl gnupg ca-certificates \
    && curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /etc/apt/trusted.gpg.d/microsoft.gpg \
    && curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list -o /etc/apt/sources.list.d/mssql-server-2022.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends mssql-server-fts \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

USER mssql
