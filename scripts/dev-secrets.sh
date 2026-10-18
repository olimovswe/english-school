#!/usr/bin/env bash
# Lokal muhit uchun: .env dagi qiymatlardan Host.Api va Host.Worker user-secrets'ini toʻldiradi.
# Parollar repoga tushmaydi. Qayta ishga tushirish xavfsiz.
set -euo pipefail
cd "$(dirname "$0")/.."

if [[ ! -f .env ]]; then
  echo ".env topilmadi. Avval: cp .env.example .env" >&2
  exit 1
fi

set -a
# shellcheck source=/dev/null
source .env
set +a

postgres="Host=localhost;Port=${POSTGRES_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

for project in src/Host.Api src/Host.Worker; do
  dotnet user-secrets set "ConnectionStrings:Postgres" "$postgres" --project "$project" > /dev/null
done

# JWT imzo kaliti faqat API'ga kerak. Mavjud kalit saqlanadi: aks holda chiqarilgan tokenlar yaroqsiz boʻlardi.
if ! dotnet user-secrets list --project src/Host.Api | grep -q '^Identity:Jwt:SigningKey = '; then
  dotnet user-secrets set "Identity:Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/Host.Api > /dev/null
fi

echo "user-secrets yangilandi: Host.Api, Host.Worker"
