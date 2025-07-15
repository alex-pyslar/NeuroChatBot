# Этап сборки: используем .NET SDK для компиляции приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем файлы решения и проекта для восстановления зависимостей
COPY *.sln .
COPY src/*/*.csproj ./src/
RUN dotnet restore

# Копируем весь исходный код, включая .env, и собираем приложение
COPY . .
WORKDIR /app/src
RUN dotnet publish -c Release -o /out --no-restore

# Этап выполнения: используем легкий образ ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Копируем скомпилированные файлы и .env из этапа сборки
COPY --from=build /out .
COPY --from=build /app/.env .

# Указываем порт для веб-приложения (если применимо)
EXPOSE 80

# Задаем переменные окружения из .env файла
# Примечание: в .NET Core/5+ рекомендуется использовать System.Environment или IConfiguration для чтения .env
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Запускаем приложение
ENTRYPOINT ["dotnet", "NeuroChatBot.dll"]